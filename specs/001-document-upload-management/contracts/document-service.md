# Document Service Contract

Internal application contract between Blazor pages/endpoints and the document business layer.

## Operations

```text
ListAccessibleAsync(requestingUserId, filters, sort, page) -> DocumentListResult
GetByIdAsync(documentId, requestingUserId) -> DocumentDetails? 
UploadAsync(requestingUserId, UploadRequest, Stream) -> DocumentResult
UpdateMetadataAsync(documentId, requestingUserId, MetadataUpdate) -> DocumentResult?
ReplaceFileAsync(documentId, requestingUserId, FileReplacement, Stream) -> DocumentResult?
DeleteAsync(documentId, requestingUserId) -> OperationResult
ShareAsync(documentId, requestingUserId, ShareRequest) -> ShareResult
GetSharedWithMeAsync(requestingUserId) -> DocumentListResult
GetProjectDocumentsAsync(projectId, requestingUserId) -> DocumentListResult
GetTaskDocumentsAsync(taskId, requestingUserId) -> DocumentListResult
GetRecentAsync(requestingUserId, count = 5) -> IReadOnlyList<DocumentSummary>
GetAuditReportAsync(requestingUserId, ReportFilter) -> AuditReport
OpenContentAsync(documentId, requestingUserId, preview) -> AuthorizedContent?
```

## Rules

- Every operation receives the authenticated user identity; callers must not pass an authorization decision as input.
- List/search/content operations scope data to owner, project/task access, active explicit shares, team shares, or administrator privileges before applying user filters.
- Upload and replacement validate size, extension, detected MIME type, title/category, association, scan result, and generated storage path.
- Owner, authorized project manager, and administrator may manage according to the clarified role rules; administrators have full management and audit access.
- Sharing accepts only authenticated existing users/teams, can grant access beyond project membership, and never creates public/external access.
- Mutating operations create audit events and return safe validation/authorization errors without exposing storage paths or scanner internals.
