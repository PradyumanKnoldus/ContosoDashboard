# Tasks: Document Upload and Management

**Input**: Design documents from `specs/001-document-upload-management/`
**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/](contracts/)

**Organization**: Tasks are grouped by user story so each independently testable increment can be implemented and validated.

**Routing note**: The repository setup script currently resolves the Git `main` branch to `specs/main` and reports that `plan.md` is missing. This task list uses the explicit feature locator in `.specify/feature.json`: `specs/001-document-upload-management`.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the testable project surface without changing production behavior.

- [X] T001 Create the `ContosoDashboard.Tests` test project targeting `net10.0` and reference `ContosoDashboard/ContosoDashboard.csproj` in `ContosoDashboard.Tests/ContosoDashboard.Tests.csproj`
- [X] T002 [P] Add the test project to a repository solution or document its standalone `dotnet test` invocation in `ContosoDashboard.Tests/ContosoDashboard.Tests.csproj`
- [X] T003 [P] Create shared test fixtures for seeded users, projects, project memberships, and isolated EF Core data in `ContosoDashboard.Tests/Fixtures/TestDataFactory.cs`
- [X] T004 [P] Create fake storage and scanner implementations used by tests in `ContosoDashboard.Tests/Fakes/FakeFileStorageService.cs` and `ContosoDashboard.Tests/Fakes/FakeFileScanService.cs`

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Build the shared domain, persistence, storage, scanning, authorization, and endpoint foundations required by every story.

**Checkpoint**: No user story implementation should begin until these tasks are complete and the application builds.

- [X] T005 [P] Create the `Document` entity with integer `DocumentId`, required `Title` max 255, nullable `Description` max 2000, required text `Category` max 100, nullable `Tags`, display-only `FileName` max 255, relative `FilePath` max 1024, `FileType` max 255, `long FileSize`, uploader/project/task foreign keys, UTC timestamps, and nullable `DeletedDate` in `ContosoDashboard/Models/Document.cs`
- [X] T006 [P] Create the `DocumentShare` entity with integer key, document/grantor/recipient references, exactly one authenticated user or existing team recipient, grant/revoke timestamps, and active-recipient uniqueness fields in `ContosoDashboard/Models/DocumentShare.cs`
- [X] T007 [P] Create the append-only `DocumentActivity` entity with document, actor, action text, occurrence time, `RetainUntil`, and bounded non-sensitive details in `ContosoDashboard/Models/DocumentActivity.cs`
- [X] T008 Add `DbSet` declarations, foreign-key relationships, delete behavior, uniqueness constraints, and indexes for documents, shares, and activities in `ContosoDashboard/Data/ApplicationDbContext.cs`
- [X] T009 [P] Define the `IFileStorageService` contract and local implementation with a configured root outside `wwwroot`, normalized relative paths, traversal/absolute-path rejection, GUID-based paths, async upload/delete/open/exists operations, and no use of client filenames as stored paths in `ContosoDashboard/Services/FileStorageService.cs`
- [X] T010 [P] Define `IFileScanService` and implement deterministic offline safe/malware fixture results, scan-failure rejection, and cancellation behavior in `ContosoDashboard/Services/FileScanService.cs`
- [X] T011 [P] Add document request/result, filter/sort, share, audit-report, and authorized-content DTOs with the six exact category values and supported file whitelist in `ContosoDashboard/Services/DocumentContracts.cs`
- [X] T012 Implement shared document authorization predicates for owner, team lead, project manager, administrator, project membership, task access, active user/team shares, deleted-state exclusion, and no public/external access in `ContosoDashboard/Services/DocumentAuthorization.cs`
- [X] T013 Register storage, scanning, and document service dependencies with configured local upload root in `ContosoDashboard/Program.cs`
- [X] T014 Create the initial `IDocumentService`/`DocumentService` structure and shared validation helpers for non-empty files, 25 MB maximum, extension/MIME whitelist, required title/category, authorized associations, and safe error results in `ContosoDashboard/Services/DocumentService.cs`
- [X] T015 Map authenticated `/documents/{documentId}/content` and `/documents/{documentId}/preview` endpoints that delegate to `OpenContentAsync`, use safe access-denied responses, and never expose physical paths in `ContosoDashboard/Program.cs`
- [X] T016 [P] Add foundational persistence and authorization tests for integer keys, text categories, relationship constraints, deleted-document exclusion, and role/share predicates in `ContosoDashboard.Tests/Foundational/DocumentFoundationTests.cs`
- [X] T017 [P] Add storage and scanner contract tests for path traversal rejection, GUID path generation, root isolation, safe fixtures, malware fixtures, and scan failure in `ContosoDashboard.Tests/Foundational/StorageAndScanTests.cs`

## Phase 3: User Story 1 - Upload and Organize a Document (Priority: P1) 🎯 MVP

**Goal**: An authenticated employee can upload supported work files with required metadata, receive progress/results, and securely associate documents with permitted projects.

**Independent Test**: Upload a supported safe fixture as an employee, verify metadata and secure storage, then verify clear rejection and no active document for oversized, unsupported, empty, unsafe, or malware-positive inputs.

### Tests for User Story 1

- [X] T018 [P] [US1] Add upload contract tests for required title/category, six category values, optional description/tags/project/task, 25 MB limit, supported MIME types, and generated metadata in `ContosoDashboard.Tests/US1/DocumentUploadValidationTests.cs`
- [X] T019 [P] [US1] Add upload authorization and compensation tests for project membership, unauthorized associations, scan rejection, storage failure, metadata persistence failure cleanup, and audit creation in `ContosoDashboard.Tests/US1/DocumentUploadServiceTests.cs`

### Implementation for User Story 1

- [X] T020 [US1] Implement `UploadAsync` in `ContosoDashboard/Services/DocumentService.cs` using validate -> authorize -> scan -> generate unique path -> save file -> persist metadata -> audit/notification sequencing and cleanup on persistence failure
- [X] T021 [US1] Add upload progress state, multi-file selection, required title/category fields, optional metadata, supported-file validation, clear success/error messages, and safe retry behavior in `ContosoDashboard/Pages/Documents.razor`
- [X] T022 [US1] Add accessible upload modal styles and stable progress/error presentation in `ContosoDashboard/wwwroot/css/site.css`
- [X] T023 [US1] Register upload success/failure and project-document notification creation through `INotificationService` in `ContosoDashboard/Services/DocumentService.cs`
- [X] T024 [US1] Seed or fixture representative safe and malware-positive training files without storing real user content in `ContosoDashboard/Data/ApplicationDbContext.cs` and `ContosoDashboard.Tests/Fixtures/TestFiles/`

**Checkpoint**: A user can independently upload a safe supported document, see progress/result feedback, and verify invalid content never becomes available.

## Phase 4: User Story 2 - Find, Preview, and Use Accessible Documents (Priority: P1)

**Goal**: Authorized users can list, filter, sort, search, preview, and download only documents they can access.

**Independent Test**: Seed accessible, shared, project, and inaccessible documents; verify list/search/filter/sort results and successful PDF/image preview/download while unauthorized identifiers reveal no content or metadata.

### Tests for User Story 2

- [ ] T025 [P] [US2] Add list/filter/sort/search tests covering title, description, tags, uploader, project, category, date range, file size, 500-document performance target, and access-first query scoping in `ContosoDashboard.Tests/US2/DocumentDiscoveryTests.cs`
- [ ] T026 [P] [US2] Add authorized endpoint tests for PDF/image preview, download disposition, MIME type, access-safe denial, deleted documents, missing content, and download/preview audit activity in `ContosoDashboard.Tests/US2/DocumentAccessEndpointTests.cs`

### Implementation for User Story 2

- [ ] T027 [US2] Implement accessible document list/search/filter/sort queries and pagination in `ContosoDashboard/Services/DocumentService.cs`, applying authorization predicates before user search terms and excluding deleted documents
- [ ] T028 [US2] Implement `OpenContentAsync` with authorized stream lookup and Download/Preview activity records in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T029 [US2] Build the documents browsing table with title/category/date/size/project, search, category/project/date filters, sort controls, empty/loading/error states, preview, and download actions in `ContosoDashboard/Pages/Documents.razor`
- [ ] T030 [US2] Complete the mapped content/preview endpoint response handling for inline PDFs/images, downloads, safe 401/403/404/422 behavior, and no physical path leakage in `ContosoDashboard/Program.cs`
- [ ] T031 [US2] Add responsive table, filter, preview, and status styles in `ContosoDashboard/wwwroot/css/site.css`

**Checkpoint**: Discovery and content access are independently usable and cannot bypass document authorization through search or direct identifiers.

## Phase 5: User Story 3 - Manage Owned and Project Documents (Priority: P2)

**Goal**: Owners, project managers, and administrators can manage documents within their clarified permissions; users can share with authenticated users/teams, including outside project membership.

**Independent Test**: Exercise edit, replacement, share, revoke, and delete as employee, team lead, project manager, administrator, and unauthorized user; verify audit records, notifications, old-file removal, and public/external sharing rejection.

### Tests for User Story 3

- [ ] T032 [P] [US3] Add metadata edit and replacement tests for owner/admin permissions, validation, new-path-first replacement, old-version preservation on failure, and old-path inaccessibility after success in `ContosoDashboard.Tests/US3/DocumentManagementTests.cs`
- [ ] T033 [P] [US3] Add sharing and deletion authorization tests for owner/project-manager/admin permissions, team/user recipients, outside-project grants, revoke behavior, confirmation, public/external rejection, seven-year audit retention, and unauthorized mutation denial in `ContosoDashboard.Tests/US3/DocumentSharingDeletionTests.cs`

### Implementation for User Story 3

- [ ] T034 [US3] Implement metadata update and file replacement operations with owner/project-manager/administrator authorization, validation, new unique path, persistence compensation, and audit events in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T035 [US3] Implement share, revoke, and Shared with Me operations for authenticated existing users/teams, including outside-project grants and no public/external recipients, in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T036 [US3] Implement confirmed delete with owner/project-manager/administrator authorization, content removal/inaccessibility, `DeletedDate`, audit retention through seven years, and safe failure handling in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T037 [US3] Add document details/manage UI for metadata edit, replacement, confirmation delete, share/revoke recipient selection, Shared with Me, and permission-aware actions in `ContosoDashboard/Pages/DocumentDetails.razor`
- [ ] T038 [US3] Add share and project-document notification creation with failure logging that does not roll back an authorized document action in `ContosoDashboard/Services/DocumentService.cs`

**Checkpoint**: Management actions work independently with role/share boundaries and leave auditable, consistent document state.

## Phase 6: User Story 4 - Connect Documents to Work Activity (Priority: P2)

**Goal**: Documents participate in project/task views, dashboard summaries, recent documents, and notifications.

**Independent Test**: Upload and attach documents from an authorized task/project, then verify project/task associations, dashboard count/recent-five behavior, and project notifications for eligible users.

### Tests for User Story 4

- [ ] T039 [P] [US4] Add project/task association tests enforcing task-project consistency and task/project authorization in `ContosoDashboard.Tests/US4/DocumentIntegrationTests.cs`
- [ ] T040 [P] [US4] Add dashboard and notification integration tests for document count, five most recent documents, project additions, sharing notifications, and notification failure behavior in `ContosoDashboard.Tests/US4/DashboardDocumentIntegrationTests.cs`

### Implementation for User Story 4

- [ ] T041 [US4] Add project and task document query/attachment operations with task-project consistency checks to `ContosoDashboard/Services/DocumentService.cs`
- [ ] T042 [US4] Add project documents section and authorized upload/attachment entry point to `ContosoDashboard/Pages/ProjectDetails.razor`
- [ ] T043 [US4] Add task document list and upload/attach entry point to the authorized task detail surface in `ContosoDashboard/Pages/Tasks.razor` or the task details page created for this feature
- [ ] T044 [US4] Extend dashboard summary data with document count and recent-five document retrieval in `ContosoDashboard/Services/DashboardService.cs`
- [ ] T045 [US4] Add Recent Documents widget, document count summary card, and navigation to the dashboard in `ContosoDashboard/Pages/Index.razor`
- [ ] T046 [US4] Add project-document and share notification display compatibility to `ContosoDashboard/Pages/Notifications.razor` and notification type definitions in `ContosoDashboard/Models/Notification.cs`

**Checkpoint**: Existing project, task, dashboard, and notification workflows expose documents without weakening their current authorization behavior.

## Phase 7: User Story 5 - Audit Document Activity (Priority: P3)

**Goal**: Administrators can inspect seven-year-retained document activity and reports; non-administrators cannot.

**Independent Test**: Perform document actions as multiple roles, then generate administrator reports for types, active uploaders, and access patterns and verify actor/document/action/time coverage and non-admin denial.

### Tests for User Story 5

- [ ] T047 [P] [US5] Add audit event completeness and seven-year retention tests for upload, download, preview, edit, replacement, share, revoke, delete, and administrator management actions in `ContosoDashboard.Tests/US5/DocumentAuditTests.cs`
- [ ] T048 [P] [US5] Add administrator report authorization and aggregation tests for document types, active uploaders, access patterns, date filters, and non-administrator denial in `ContosoDashboard.Tests/US5/DocumentReportingTests.cs`

### Implementation for User Story 5

- [ ] T049 [US5] Implement administrator-only audit query and report aggregation operations in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T050 [US5] Add administrator audit/report page with date/action filters, summary tables, loading/error states, and role authorization in `ContosoDashboard/Pages/DocumentAudit.razor`
- [ ] T051 [US5] Add navigation visibility and administrator policy enforcement for the audit page in `ContosoDashboard/Shared/NavMenu.razor` and `ContosoDashboard/Program.cs`

**Checkpoint**: Administrator audit and reporting are independently usable and non-administrators cannot access them.

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Harden the complete feature, validate performance/security, and update training documentation.

- [ ] T052 [P] Add structured document-operation logging that excludes file contents, physical paths, scanner internals, and sensitive metadata in `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Program.cs`
- [ ] T053 [P] Add cleanup/reconciliation diagnostics for orphaned local files and failed replacement/deletion operations in `ContosoDashboard/Services/FileStorageService.cs` and `ContosoDashboard/Services/DocumentService.cs`
- [ ] T054 [P] Update training-only storage, deterministic scanning, mock authentication, and future Azure replacement guidance in `README.md`
- [ ] T055 [P] Add accessibility labels, keyboard-accessible controls, safe filename/title rendering, and responsive document UI verification in `ContosoDashboard/Pages/Documents.razor`, `ContosoDashboard/Pages/DocumentDetails.razor`, and `ContosoDashboard/wwwroot/css/site.css`
- [ ] T056 Run the complete `dotnet test .\ContosoDashboard.Tests\ContosoDashboard.Tests.csproj` suite and fix feature regressions without weakening authorization in `ContosoDashboard.Tests/`
- [ ] T057 Run `dotnet build .\ContosoDashboard\ContosoDashboard.csproj --no-restore` and resolve feature compile/configuration errors in the affected project files
- [ ] T058 Execute every scenario in `specs/001-document-upload-management/quickstart.md` and record any deviations in `specs/001-document-upload-management/quickstart.md`

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No feature dependencies; creates the test project and shared fixtures.
- **Foundational (Phase 2)**: Depends on Setup; blocks all user stories because entities, storage/scanning contracts, authorization, service skeleton, and content endpoints are shared.
- **User Story 1 (Phase 3)**: Depends on Foundational; MVP upload increment.
- **User Story 2 (Phase 4)**: Depends on Foundational and the active document shape from US1; its discovery tests can be prepared in parallel, but the working list assumes US1's upload contract.
- **User Story 3 (Phase 5)**: Depends on Foundational and the active document/content operations from US1/US2; management can be implemented after the service foundation, with UI integration following those operations.
- **User Story 4 (Phase 6)**: Depends on US1 document creation and Foundational project/task/notification services; can proceed in parallel with US2/US3 after shared contracts stabilize.
- **User Story 5 (Phase 7)**: Depends on all action-producing stories so reports have complete event coverage.
- **Polish (Phase 8)**: Depends on all desired user stories.

### User Story Dependencies

- **US1 (P1)**: No story dependency after Foundational; MVP.
- **US2 (P1)**: Depends on the document entity and upload shape from Foundational/US1; independently testable with seeded records.
- **US3 (P2)**: Depends on document content access and service authorization from Foundational/US1/US2.
- **US4 (P2)**: Depends on document association operations and existing project/task/dashboard/notification contracts; can run alongside US2 and US3 after Foundational.
- **US5 (P3)**: Depends on action audit events from US1-US4.

### Parallel Opportunities

- T003, T004, T005, T006, T007, T009, T010, T011, T016, and T017 can proceed in parallel after project setup where files do not overlap.
- Within US1, T018 and T019 are parallel test tracks; T021 and T022 can proceed after the service contract is stable.
- Within US2, T025 and T026 are parallel test tracks; T029 and T031 can proceed in parallel after service DTOs are stable.
- Within US3, T032 and T033 are parallel test tracks; T037 can proceed after the service operation signatures are agreed.
- Within US4, T039 and T040 are parallel test tracks; T042, T043, and T045 can proceed in parallel after document integration methods are available.
- Within US5, T047 and T048 are parallel test tracks; T050 and T051 can proceed in parallel after the report contract is stable.
- Polish documentation, logging, cleanup diagnostics, and UI accessibility work can proceed in parallel before final test/quickstart validation.

## Parallel Example: User Story 1

```text
Task: "T018 [US1] Upload validation tests in ContosoDashboard.Tests/US1/DocumentUploadValidationTests.cs"
Task: "T019 [US1] Upload authorization and compensation tests in ContosoDashboard.Tests/US1/DocumentUploadServiceTests.cs"

After service contracts stabilize:
Task: "T021 [US1] Upload UI in ContosoDashboard/Pages/Documents.razor"
Task: "T022 [US1] Upload progress/error styles in ContosoDashboard/wwwroot/css/site.css"
```

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 setup and Phase 2 foundational tasks.
2. Complete Phase 3 upload and organization tasks.
3. Run the US1 test suite and the upload/invalid-input quickstart scenarios.
4. Stop for review/demo before adding discovery or management workflows.

### Incremental Delivery

1. Deliver US1 as the secure upload MVP.
2. Add US2 for accessible discovery and content use.
3. Add US3 for management and explicit sharing.
4. Add US4 for project/task/dashboard/notification integration.
5. Add US5 for administrator audit/reporting.
6. Finish cross-cutting hardening and full quickstart validation.

### Parallel Team Strategy

1. Complete Phases 1-2 together because they establish shared entities and contracts.
2. After the foundation, assign US1 to the upload/service owner, US2 to discovery/endpoint work, and US4 to integration work.
3. Start US3 after content/service signatures stabilize; start US5 after action audit coverage exists.
4. Keep tasks touching `ApplicationDbContext.cs`, `Program.cs`, and `DocumentService.cs` coordinated to avoid merge conflicts.

## Notes

- `[P]` tasks touch different files and have no dependency on incomplete work.
- `[US#]` labels map each task to the corresponding user story in `spec.md`.
- Every task includes an exact repository path and is intended to be independently executable.
- Test tasks are included because the plan and constitution require verifiable authorization, persistence, endpoint, and failure behavior.
- Do not expose local upload roots, physical paths, scanner details, or document contents in user-facing errors or logs.
