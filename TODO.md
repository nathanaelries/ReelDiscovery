# TODO

Near-term, actionable items. See [ROADMAP.md](ROADMAP.md) for the bigger picture.

## Web UI polish
- [ ] Wire `ModelConfigurationDialog` equivalent: custom model ids and pricing in the web UI (currently fixed to the default model list)
- [ ] Token usage / estimated cost display during generation (Core's `TokenUsageTracker` already collects this)
- [ ] Per-job log panel surfacing storyline-level errors as they happen, not only at the end
- [ ] Replace the YAML `<textarea>` with Monaco (schema-validated YAML editing)
- [ ] Persist jobs to disk so the Jobs page survives a server restart (output folders already do)
- [ ] Authentication option for shared deployments (currently single-tenant, deploy behind a reverse proxy)

## Platform
- [ ] Headless CLI: `reeldiscovery generate -f dataset.yaml -o ./out` (Core is ready for this)
- [ ] Test project (xUnit): YAML round-trip, EML parse-back via MimeKit, OpenXML validator on generated docs
- [ ] Publish Docker image to ghcr.io from CI on tagged releases
- [ ] WinForms app: optionally load/save dataset.yaml for parity with the web UI

## Core engine
- [ ] Refactor `Attachment`/`AttachmentType` enum to a pluggable `IAttachmentGenerator` registry with container (`Children`) support (prerequisite for ZIP/nesting work, see Roadmap Phase 1)
- [ ] ICS line folding at 75 octets (RFC 5545); add `TZID` handling instead of UTC-only timestamps
- [ ] Make image (`dall-e-3`) and TTS (`tts-1`) model ids configurable instead of hardcoded in `OpenAIService`
- [ ] Telemetry: make the webhook endpoint configurable; document it for self-hosted deployments

## Providers (model-agnostic follow-ups)
- [ ] Editable pricing for non-OpenAI/Anthropic models (xAI/Gemini/Groq presets currently track cost as $0)
- [ ] Media providers beyond OpenAI: gpt-image-1, Gemini Imagen, ElevenLabs TTS behind `IImageGenerationProvider`/`ISpeechGenerationProvider`
- [ ] WinForms desktop app: surface the provider dropdown (Core supports it; the desktop UI is still OpenAI-only)
- [ ] Anthropic: consider adaptive thinking (`thinking: {type: adaptive}`) for storyline quality on Opus models
- [ ] Graceful fallback when an OpenAI-compatible endpoint rejects `response_format: json_object` (retry without it)

## Housekeeping
- [ ] Resolve license ambiguity once upstream responds to [issue #3](https://github.com/ghanderson77-ops/ReelDiscovery/issues/3); align LICENSE and README
- [ ] Add CONTRIBUTING.md once the license question is settled
- [ ] Releases: attach both the Windows exe and a docker image reference
