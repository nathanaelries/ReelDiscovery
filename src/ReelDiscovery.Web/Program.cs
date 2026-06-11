using ReelDiscovery.Services;
using ReelDiscovery.Web.Components;
using ReelDiscovery.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<DatasetYamlService>();
builder.Services.AddSingleton<GenerationJobService>();
builder.Services.AddScoped<WizardSession>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

// Download a finished job's output as a zip.
app.MapGet("/download/{id:guid}", (Guid id, GenerationJobService jobs) =>
{
    var job = jobs.Get(id);
    if (job is null || job.Status == JobStatus.Running || !Directory.Exists(job.OutputFolder))
    {
        return Results.NotFound();
    }

    var zipPath = jobs.CreateZip(job);
    return Results.File(zipPath, "application/zip", Path.GetFileName(zipPath));
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
