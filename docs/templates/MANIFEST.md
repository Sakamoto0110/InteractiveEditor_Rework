# Module Manifest Template

**Document ID:** TEMPLATE-MANIFEST

**Schema version:** 1

**Kind:** guide

**Lifecycle:** current

**Subject:** template for a module documentation manifest

**Surface:** template

**Boundary:** global

**Authority role:** non-normative

**Mutation:** authored

**Indexing:** exclude

Copy this file to `docs/modules/<boundary>/MANIFEST.md`, replace every template
value, and keep routing facts aligned with the project files.

Required manifest metadata:

```text
Projects: repository-relative project file paths, comma-separated
Language: csharp
Artifact: managed-assembly, executable, or none
Platforms: supported platforms, comma-separated
Target frameworks: target framework monikers, comma-separated
Module dependencies: boundary keys this module builds against, comma-separated, or none
Third-party dependencies: package names, comma-separated, or none
Distribution: package, sample, or unshipped
Stability: stable, preview, experimental, or unshipped
Public API: repository-relative project path whose compiled assembly is the API, or none
Profiles: validation profile IDs, comma-separated, or none
Technical reference: repository-relative REFERENCE.md path
Related boundaries: boundary keys, comma-separated, or none
Source: repository-relative source paths, comma-separated
Tests: repository-relative test paths, comma-separated, or none
Runtime scenarios: repository-relative scenario paths, comma-separated, or none
```

The body routes the introduction, technical reference, the module's archive
records, every on-demand surface that exists, relevant proposals, source, tests, scenarios, and related
boundaries. Do not reproduce the public symbol inventory.
