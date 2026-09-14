# Research: Document Upload and Management

## Decision 1: Keep file content behind an application storage abstraction

**Decision**: Implement `IFileStorageService` with a local filesystem implementation. Store only portable relative paths in the database and keep the root directory outside `wwwroot`.

**Rationale**: This satisfies offline training, prevents direct static-file exposure, and preserves the specified Azure Blob migration boundary. GUID-based stored names prevent path traversal and collisions.

**Alternatives considered**: Writing uploads directly to `wwwroot` was rejected because it bypasses authorization. Direct `System.IO` calls in Blazor pages were rejected because they couple presentation to infrastructure and make migration/testing harder.

## Decision 2: Use a mapped authorized endpoint for file retrieval

**Decision**: Map download and preview handlers in `Program.cs` or a focused endpoint module. The handler delegates authorization and stream lookup to `IDocumentService` and returns a file result only after access succeeds.

**Rationale**: The current application maps Blazor fallback routes and static files but has no upload endpoint. An application endpoint can enforce ownership, project membership, explicit share grants, administrator privileges, and audit logging before returning bytes.

**Alternatives considered**: Static-file middleware was rejected because it cannot apply per-document authorization. A public blob URL was rejected because external/public sharing is out of scope.

## Decision 3: Represent malware scanning as a deterministic local boundary

**Decision**: Add `IFileScanService`. The training implementation returns deterministic results for safe content and configured malware fixtures; tests cover both outcomes. Production can replace it with a real scanner without changing document business behavior.

**Rationale**: The feature must remain runnable offline while never making unscanned content available. A named boundary makes that rule explicit and testable.

**Alternatives considered**: Extension-only validation was rejected because it cannot satisfy the malware requirement. Rejecting every upload was rejected because it makes the training feature unusable.

## Decision 4: Use service-level authorization over query and mutation paths

**Decision**: `DocumentService` receives the requesting user ID and role context, scopes every list/search query to accessible documents, and checks the same policy before mutations or file retrieval.

**Rationale**: Existing `ProjectService` and `TaskService` already use service-level IDOR protections. Centralizing document policy avoids relying on UI visibility and prevents identifier-based bypasses.

**Alternatives considered**: UI-only hiding was rejected because it does not protect direct requests. A separate authorization server was rejected as incompatible with the local training scope.

## Decision 5: Compensate storage/database failures explicitly

**Decision**: Upload order is validate -> authorize -> scan -> generate unique relative path -> save file -> save metadata -> write audit event/notifications. If metadata persistence fails after file save, delete the file or record a cleanup failure. File replacement writes the new content first and removes the old content only after metadata succeeds.

**Rationale**: This follows the stakeholder-specified order and prevents broken records, duplicate paths, and inconsistent replacements.

**Alternatives considered**: Persisting metadata before saving bytes was rejected because it creates unusable records when storage fails. In-place replacement was rejected because a failed write could destroy the last good version.

## Decision 6: Retain audit records for seven years

**Decision**: Keep `DocumentActivity` rows after document content and active metadata are deleted; store action time and a retention/expiry value or apply a documented seven-year retention policy.

**Rationale**: The clarified requirement needs post-deletion audit reporting without keeping deleted content available to users.

**Alternatives considered**: Cascading audit deletion was rejected because it defeats administrator reporting. Indefinite retention was rejected because the feature specifies a bounded seven-year period.

## Decision 7: Add focused automated coverage without changing the application architecture

**Decision**: Add a test project for service authorization, input validation, transaction compensation, scanner/storage behavior, and endpoint access. Use isolated test data and fake storage/scanning implementations; use the existing application build and quickstart for end-to-end verification.

**Rationale**: The repository currently has no test project, but the constitution requires verifiable changes and the feature has high authorization risk.

**Alternatives considered**: Manual testing alone was rejected because it cannot reliably prove IDOR resistance. A full browser automation suite was deferred because the initial feature can be validated with service/endpoint tests plus the focused quickstart.
