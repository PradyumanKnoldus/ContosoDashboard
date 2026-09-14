# Data Model: Document Upload and Management

## Document

Represents a stored work document and its active metadata.

| Field | Type/constraint | Notes |
|---|---|---|
| DocumentId | `int`, identity key | Required by existing key convention |
| Title | required string, max 255 | User-provided display title |
| Description | nullable string, max 2000 | Optional searchable text |
| Category | required string, max 100 | One of the six specified text values, not an enum |
| Tags | nullable string or normalized tag representation | Custom searchable tags; implementation may normalize into a child table if query needs require it |
| FileName | required string, max 255 | Original display name only; never used as storage path |
| FilePath | required string, max 1024 | Portable relative GUID-based path |
| FileType | required string, max 255 | MIME type |
| FileSize | required `long` | Bytes, maximum 25 MB |
| UploadedByUserId | required `int` FK | Owner/uploader |
| ProjectId | nullable `int` FK | Optional project association |
| TaskId | nullable `int` FK | Optional task association; must belong to same project when both exist |
| UploadedDate | required UTC datetime | Creation timestamp |
| UpdatedDate | required UTC datetime | Metadata/file replacement timestamp |
| DeletedDate | nullable UTC datetime | Soft lifecycle marker for audit-safe retention; deleted content is inaccessible |

Indexes: `UploadedByUserId, UploadedDate`; `ProjectId, UploadedDate`; `Category`; `FileType`; `DeletedDate`; searchable fields needed by the chosen query strategy. Search queries MUST include access predicates before user search terms.

## DocumentShare

Represents explicit access granted to an authenticated user or existing team.

| Field | Type/constraint | Notes |
|---|---|---|
| DocumentShareId | `int`, identity key | Primary key |
| DocumentId | required `int` FK | Shared document |
| RecipientUserId | nullable `int` FK | Specific authenticated recipient |
| RecipientTeamKey | nullable string | Existing team identity; exact representation follows current team model |
| GrantedByUserId | required `int` FK | Must be owner, authorized project manager, or administrator |
| GrantedDate | required UTC datetime | Share time |
| RevokedDate | nullable UTC datetime | Allows access revocation without deleting audit history |

Constraint: exactly one recipient form is populated. Unique active share per document/recipient. Public, anonymous, and external recipients are not represented.

## DocumentActivity

Append-only audit record retained for seven years after document deletion.

| Field | Type/constraint | Notes |
|---|---|---|
| DocumentActivityId | `int`, identity key | Primary key |
| DocumentId | required `int` or retained reference | Reference may remain after soft deletion |
| ActorUserId | required `int` FK | User who performed the action |
| Action | required text | Upload, Download, Preview, Edit, Replace, Share, Delete, or related action |
| OccurredDate | required UTC datetime | Event time |
| RetainUntil | required UTC datetime | Seven years after the relevant deletion/retention start |
| Details | nullable bounded text | Non-sensitive context; never store document content |

Indexes: `DocumentId, OccurredDate`; `ActorUserId, OccurredDate`; `Action, OccurredDate`; `RetainUntil`.

## Relationships

- User 1-to-many Document as uploader.
- Project 1-to-many Document, optional on Document.
- TaskItem 1-to-many Document, optional on Document.
- Document 1-to-many DocumentShare.
- Document 1-to-many DocumentActivity.
- User 1-to-many DocumentShare as grantor and optional recipient.
- Document task association MUST reference the same project as the task when a project is present.

## State and Failure Transitions

1. **Pending**: upload request validated and scanning/storage work in progress; not visible to users.
2. **Active**: content saved, metadata committed, and upload audit event written.
3. **Replacing**: new content is validated/scanned and stored at a new unique path; old content remains available until metadata commit succeeds.
4. **Deleted**: active content is removed or inaccessible, `DeletedDate` is set, and delete audit event remains queryable.
5. **Failed**: no active document is exposed; cleanup is attempted and failure is logged for diagnosis.

The pending/replacing states may be workflow-local rather than persisted if the service can guarantee the same externally observable behavior.

## Validation Rules

- File size is greater than zero and no more than 25 MB.
- Extension and detected MIME type are both in the supported whitelist.
- Malware scan must pass before storage is made available.
- Title and category are required; category must match the predefined text list.
- All associations and recipients are authorized for the requesting user.
- File paths are generated from user/project scope plus a GUID and normalized extension; input filenames are display-only.
- Deleted documents are excluded from normal list/search/download/preview results.
