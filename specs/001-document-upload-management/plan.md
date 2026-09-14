# Implementation Plan: Document Upload and Management

**Branch**: `001-document-upload-management` | **Date**: 2026-09-14 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/001-document-upload-management/spec.md`

## Summary

Add offline document management to the existing Blazor Server dashboard. The implementation will introduce integer-keyed document, sharing, and audit entities; a service layer that centralizes validation and role-based authorization; local filesystem storage outside `wwwroot`; deterministic offline malware scanning behind `IFileScanService`; and an `IFileStorageService` abstraction that can later be replaced by Azure Blob Storage. Blazor pages will provide upload, browsing, search, preview, download, metadata management, sharing, project/task attachment, dashboard, notification, and administrator reporting workflows. Owners, project managers, and administrators will receive the management permissions defined by the spec, with administrators having full document management and audit access. All file retrieval will pass through an authorized endpoint rather than static-file middleware.

## Technical Context

**Language/Version**: C# on .NET 10 (`ContosoDashboard.csproj`), with existing ASP.NET Core/Blazor Server code using EF Core 8 packages  
**Primary Dependencies**: ASP.NET Core Blazor Server, Entity Framework Core SQL Server, cookie-based mock authentication, Bootstrap 5.3 and Bootstrap Icons  
**Storage**: SQL Server LocalDB for metadata; local filesystem under an application data directory outside `wwwroot` for content; future storage providers implement `IFileStorageService`  
**Testing**: Add focused service and authorization tests using an isolated EF Core test database plus repeatable `dotnet build` and quickstart validation; security-sensitive paths require unauthorized-access coverage  
**Target Platform**: Local/offline Windows development and training deployment running the existing ASP.NET Core web application  
**Project Type**: Single server-rendered web application with Blazor Server pages and mapped HTTP endpoints  
**Performance Goals**: Uploads up to 25 MB complete within 30 seconds under typical conditions; document lists and searches return within 2 seconds for up to 500 accessible documents; PDF/image previews load within 3 seconds  
**Constraints**: No required cloud services; supported file whitelist only; files outside `wwwroot`; generated GUID-based relative paths; integer document IDs; text categories; seven-year audit retention; no public or external sharing; mock authentication remains training-only  
**Scale/Scope**: Existing training application and its seeded users/projects; initial list/search target is up to 500 accessible documents per user and the feature supports the five prioritized user journeys in the specification

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Training-First Scope**: PASS. The design is local/offline and explicitly keeps malware scanning, authentication, and storage implementations suitable for training rather than production claims.
- **Layered Architecture and Abstractions**: PASS. Models, EF configuration, storage/scanning interfaces, business services, authorized endpoints, and Blazor pages remain separate.
- **Security by Default**: PASS. Every document operation receives the requesting identity, applies ownership/role/project/share checks in the service layer, and serves bytes only through an authorized endpoint. Generated paths never use the client filename.
- **Verifiable Changes**: PASS. The plan includes service authorization tests, validation/error-path tests, endpoint access tests, and the runnable quickstart scenarios.
- **Simplicity and Observability**: PASS. The design reuses existing services and notification patterns, adds only required abstractions, logs auditable actions, and keeps storage-provider replacement behind one interface.
- **Security and Training Constraints**: PASS. Documentation will identify the mock identity and deterministic scanner as training-only and record production replacement points.
- **Development Workflow and Quality Gates**: PASS. Implementation will be decomposed by data, services, endpoints/UI, integrations, and tests; each slice will be build-checked before the next.

## Project Structure

### Documentation (this feature)

```text
specs/001-document-upload-management/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── document-service.md
│   ├── file-storage.md
│   └── file-access-endpoint.md
└── tasks.md                 # Created by /speckit.tasks
```

### Source Code (repository root)

```text
ContosoDashboard/
├── Data/
│   └── ApplicationDbContext.cs
├── Models/
│   ├── Document.cs
│   ├── DocumentShare.cs
│   └── DocumentActivity.cs
├── Services/
│   ├── DocumentService.cs
│   ├── FileStorageService.cs
│   └── FileScanService.cs
├── Pages/
│   ├── Documents.razor
│   ├── DocumentDetails.razor
│   ├── ProjectDetails.razor       # project document section
│   ├── Tasks.razor                # task attachment entry point
│   └── Index.razor                # recent documents/count
├── Services/
│   └── DashboardService.cs        # document summary contract extension
├── Program.cs                     # DI registrations and authorized file endpoints
└── wwwroot/css/site.css           # only feature-specific presentation styles
```

Test code should live in a new `ContosoDashboard.Tests/` project if no existing test project is available, with focused unit/integration coverage for `DocumentService`, storage/scanning workflows, and the authorized file endpoint.

**Structure Decision**: Extend the existing single ASP.NET Core/Blazor project and its Models/Data/Services/Pages layers. Add a separate test project only for verifiable feature behavior; do not introduce a new application, API, or cloud service.

## Phase 0: Research Decisions

Research findings are recorded in [research.md](research.md). Key decisions are:

1. Use a service-owned upload transaction: validate metadata and access, scan, generate relative path, write content, persist metadata, and compensate storage on persistence failure.
2. Use an authorized mapped endpoint for download/preview rather than `UseStaticFiles` for the upload directory.
3. Use deterministic local scanning fixtures behind `IFileScanService`; production scanning remains a replaceable boundary.
4. Use EF Core relationships and indexed query fields for access filtering and the 500-document list/search target.
5. Keep audit records after content deletion and enforce seven-year retention through an explicit retention field/policy.

## Phase 1: Design Outputs

- [data-model.md](data-model.md) defines fields, relationships, indexes, validation, lifecycle, and retention.
- [contracts/document-service.md](contracts/document-service.md) defines service operations and authorization outcomes.
- [contracts/file-storage.md](contracts/file-storage.md) defines local storage and scanner abstractions.
- [contracts/file-access-endpoint.md](contracts/file-access-endpoint.md) defines the authorized download/preview contract.
- [quickstart.md](quickstart.md) defines build, seed-data, upload, authorization, failure, integration, and audit validation scenarios.

## Post-Design Constitution Re-check

- **Training-First Scope**: PASS. No required external dependency is introduced.
- **Layered Architecture and Abstractions**: PASS. Storage and scanning remain replaceable, and UI does not access the filesystem directly.
- **Security by Default**: PASS. The service and endpoint both enforce access; share grants are explicit and bounded to authenticated users/teams.
- **Verifiable Changes**: PASS. The quickstart and proposed test project cover success, invalid input, authorization, compensation, notification, and audit behavior.
- **Simplicity and Observability**: PASS. Existing notification and authentication patterns are reused; audit events and failures are logged without exposing file contents.

## Complexity Tracking

No constitution violations or unjustified complexity are identified. The separate test project and three small interfaces are required to make authorization, offline scanning, and storage replacement independently verifiable.
