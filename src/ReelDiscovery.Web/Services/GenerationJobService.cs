using System.Collections.Concurrent;
using System.IO.Compression;
using ReelDiscovery.Models;
using ReelDiscovery.Services;

namespace ReelDiscovery.Web.Services;

public enum JobStatus
{
    Running,
    Completed,
    Failed,
    Cancelled
}

public class GenerationJob
{
    public Guid Id { get; } = Guid.NewGuid();
    public string ShortId => Id.ToString("N")[..8];
    public JobStatus Status { get; internal set; } = JobStatus.Running;
    public GenerationProgress Progress { get; internal set; } = new();
    public GenerationResult? Result { get; internal set; }
    public string? Error { get; internal set; }
    public string OutputFolder { get; internal set; } = string.Empty;
    public string DatasetYaml { get; internal set; } = string.Empty;
    public string Topic { get; internal set; } = string.Empty;
    public DateTime StartedUtc { get; } = DateTime.UtcNow;
    internal CancellationTokenSource Cts { get; } = new();

    /// <summary>Raised whenever progress or status changes. Subscribers must marshal to their own context.</summary>
    public event Action? Updated;
    internal void RaiseUpdated() => Updated?.Invoke();
}

/// <summary>
/// Singleton registry that runs dataset generation as background jobs so they
/// survive page navigation and can be observed live from the UI.
/// </summary>
public class GenerationJobService
{
    private readonly ConcurrentDictionary<Guid, GenerationJob> _jobs = new();
    private readonly DatasetYamlService _yamlService;
    private readonly string _dataRoot;

    public GenerationJobService(IWebHostEnvironment env, DatasetYamlService yamlService)
    {
        _yamlService = yamlService;
        _dataRoot = Environment.GetEnvironmentVariable("REELDISCOVERY_DATA")
            ?? Path.Combine(env.ContentRootPath, "data");
    }

    public string DataRoot => _dataRoot;

    public GenerationJob? Get(Guid id) => _jobs.TryGetValue(id, out var job) ? job : null;

    public IReadOnlyList<GenerationJob> All =>
        _jobs.Values.OrderByDescending(j => j.StartedUtc).ToList();

    public GenerationJob Start(WizardState state)
    {
        var job = new GenerationJob
        {
            Topic = state.Topic
        };
        job.OutputFolder = Path.Combine(_dataRoot, "output", job.ShortId);
        state.Config.OutputFolder = job.OutputFolder;
        job.DatasetYaml = _yamlService.Serialize(state);
        _jobs[job.Id] = job;

        _ = Task.Run(async () =>
        {
            try
            {
                Directory.CreateDirectory(job.OutputFolder);

                // Save the dataset definition alongside the output so every
                // corpus is reproducible from its own dataset.yaml.
                await File.WriteAllTextAsync(
                    Path.Combine(job.OutputFolder, "dataset.yaml"), job.DatasetYaml);

                var generator = new EmailGenerator(
                    state.CreateTextProvider(),
                    state.CreateImageProvider(),
                    state.CreateSpeechProvider());
                var progress = new Progress<GenerationProgress>(p =>
                {
                    // The generator mutates a shared instance; snapshot it.
                    job.Progress = new GenerationProgress
                    {
                        TotalEmails = p.TotalEmails,
                        CompletedEmails = p.CompletedEmails,
                        TotalAttachments = p.TotalAttachments,
                        CompletedAttachments = p.CompletedAttachments,
                        TotalImages = p.TotalImages,
                        CompletedImages = p.CompletedImages,
                        CurrentOperation = p.CurrentOperation,
                        CurrentStoryline = p.CurrentStoryline
                    };
                    job.RaiseUpdated();
                });

                var result = await generator.GenerateEmailsAsync(state, progress, job.Cts.Token);
                job.Result = result;
                state.Result = result;
                job.Status = result.WasCancelled ? JobStatus.Cancelled : JobStatus.Completed;
            }
            catch (OperationCanceledException)
            {
                job.Status = JobStatus.Cancelled;
            }
            catch (Exception ex)
            {
                job.Status = JobStatus.Failed;
                job.Error = ex.Message;
            }
            finally
            {
                job.RaiseUpdated();
            }
        });

        return job;
    }

    public void Cancel(Guid id) => Get(id)?.Cts.Cancel();

    /// <summary>Zips a finished job's output folder and returns the zip path.</summary>
    public string CreateZip(GenerationJob job)
    {
        var zipDir = Path.Combine(_dataRoot, "zips");
        Directory.CreateDirectory(zipDir);
        var zipPath = Path.Combine(zipDir, $"reeldiscovery_{job.ShortId}.zip");

        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }
        ZipFile.CreateFromDirectory(job.OutputFolder, zipPath, CompressionLevel.Fastest, false);
        return zipPath;
    }
}
