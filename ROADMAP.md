# ReelDiscovery Roadmap

The goal: the most realistic, most pathological e-discovery test corpora you can generate on demand. Real collections are messy in ways synthetic data usually isn't, with nested containers, legacy encodings, mixed languages, and broken files. That mess is exactly what processing engines need to be tested against, and it is what this roadmap builds toward.

## Platform (in progress)

| Phase | Scope | Status |
|-------|-------|--------|
| A | Extract cross-platform `ReelDiscovery.Core` from the WinForms shell | ✅ Done |
| B | Blazor Server web UI with background jobs, live progress, zip download | ✅ Done |
| C | `dataset.yaml` definitions: form/YAML editing, export/import, saved with every corpus | ✅ Done (v1) |
| D | Docker image + compose, CI on Linux and Windows | ✅ Done |
| E | Model-agnostic providers: OpenAI, Anthropic Claude, xAI, Gemini, Groq/Llama, Ollama, custom OpenAI-compatible endpoints | ✅ Done |
| F | Headless CLI (`reeldiscovery generate -f dataset.yaml`) for CI pipelines | Planned |
| G | Test project: round-trip generated output through MimeKit/OpenXML validators | Planned |

## Content phases

### Phase 1: Nested containers (highest e-discovery value)
Recursive extraction is what processing engines are graded on; almost nothing generates pathological container corpora on demand.

- ZIP attachments containing multiple child files
- Nested ZIPs (zip-in-zip) to a configurable depth
- Emails as attachments: `message/rfc822` parts (.eml inside .eml), and .eml inside a zip inside an email
- Edge cases: empty zips, zero-byte entries, password-protected zips (password in a manifest), colliding entry names, non-ASCII entry names

Prerequisite: refactor `Attachment` from the hardcoded enum to a pluggable `IAttachmentGenerator` registry with `Children` support.

### Phase 2: Encodings and transfer encodings
Everything is currently UTF-8 + Base64. Real mailboxes aren't.

- Vary `Content-Transfer-Encoding`: quoted-printable, 7bit, 8bit
- Legacy body charsets: windows-1252, ISO-8859-1/6/8, Shift_JIS, ISO-2022-JP, EUC-KR, GB18030, Big5, KOI8-R (via `System.Text.Encoding.CodePages`)
- RFC 2047 encoded-word subjects and display names; RFC 2231 attachment filenames
- "Messy mailbox" mode: mislabeled charsets, missing charset parameters, malformed boundaries

### Phase 3: Multilingual content (CJK, Hebrew, Arabic, Devanagari)
- Per-storyline / per-character language selection; mixed-language threads; emoji subjects
- Office layer fixes: `EastAsia` and `ComplexScript` run fonts, `w:lang`, `BiDi` paragraph properties for RTL scripts
- Non-ASCII filenames (CJK, RTL), long-path and max-component cases
- ICS line folding at 75 octets per RFC 5545 (required before multi-byte descriptions)

### Phase 4: Modern and expanded file types
- **HEIC**: transcode DALL-E PNG via libheif, with realistic iPhone naming (`IMG_0123.HEIC`)
- **EXIF-bearing JPEGs**: camera model, GPS consistent with the storyline, capture time consistent with the email date
- **PDF** (most common real-world attachment; QuestPDF or docx conversion)
- Cheap wins: .txt, .csv, .rtf, .html, .vcf, .md, .json, .xml
- **MSG** output via MsgKit; **mbox** export (MimeKit native)
- Hidden-content documents: tracked changes, comments, custom metadata, speaker notes, hidden sheets/columns, formulas
- Deliberately broken files: truncated docx, extension/content mismatches, zero-byte attachments, duplicate Message-IDs

### Phase 5: Dataset realism and tooling
- Near-duplicates (minor edits) and exact duplicates across custodians for dedup testing
- Dataset manifest (JSON/CSV): every file with hash, language, charset, container path, intended edge case, enabling automated test assertions
- Load-file output (Concordance DAT / Opticon)
- Multi-custodian folder structures and PST-adjacent exports

## Licensing

The upstream LICENSE (GPL-3.0) and README (MIT) contradict each other; clarification is pending in [ghanderson77-ops/ReelDiscovery#3](https://github.com/ghanderson77-ops/ReelDiscovery/issues/3). Nothing in this roadmap depends on the outcome; GPL-3.0 permits all of the above, including hosting the web UI as a service.
