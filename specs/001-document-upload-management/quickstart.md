# Quickstart Validation: Document Upload and Management

## Prerequisites

- .NET 10 SDK available (the project targets `net10.0`).
- SQL Server LocalDB or the configured local SQL Server connection available.
- Windows PowerShell from the repository root.
- The application remains a training-only local application; no cloud account or external scanner is required.

## Build and start

```powershell
dotnet restore .\ContosoDashboard\ContosoDashboard.csproj
dotnet build .\ContosoDashboard\ContosoDashboard.csproj
cd .\ContosoDashboard
dotnet run
```

Open the printed HTTPS URL and sign in using the seeded mock users. Use the administrator and employee identities to validate role boundaries.

## Core validation scenarios

1. **Employee upload**: Sign in as the employee, upload a supported PDF or image with a title and category, and confirm progress, success, metadata, and a secure document entry.
2. **Invalid upload**: Try an unsupported extension, an empty file, and a file over 25 MB. Confirm a clear error and no active document record or accessible stored file.
3. **Malware fixture**: Submit the deterministic malware fixture and confirm `IFileScanService` rejects it before availability. Submit the safe fixture and confirm it can complete the normal flow.
4. **Project access**: Upload to a project the employee belongs to. Confirm project members can view/download it and a user without project access cannot discover it by list, search, or identifier.
5. **Search and filters**: Create documents with different categories, dates, projects, tags, and uploaders. Verify sort/filter/search results and the 2-second target for the supported dataset.
6. **Preview/download**: Preview a permitted PDF/image and download a permitted file. Attempt the same endpoint as an unauthorized user and confirm denial without metadata or content leakage.
7. **Metadata and replacement**: As owner, edit metadata and replace the file. Confirm search reflects the update and the old file path is no longer accessible. Force a storage or persistence failure in tests and confirm the last valid version remains usable.
8. **Sharing**: Share with an authenticated user outside the project and confirm the recipient sees “Shared with Me” and receives an in-app notification. Confirm public/external recipient attempts are rejected.
9. **Role management**: Confirm team leads manage team-member documents, project managers manage documents in their projects, administrators can edit/replace/share/delete any document, and unauthorized users cannot mutate documents.
10. **Task/dashboard integration**: Attach/upload from an authorized task, verify project association, check the dashboard’s document count and five most recent documents, and confirm project notifications.
11. **Audit**: Perform upload, download, preview, edit, replace, share, and delete actions. As administrator, verify actor/document/action/time and seven-year retention metadata; confirm non-administrators cannot access reports.

## Automated checks

Run the focused test project once it is added:

```powershell
dotnet test .\ContosoDashboard.Tests\ContosoDashboard.Tests.csproj
```

Required test groups are documented in the plan/contracts and should cover authorization predicates, validation and scanner outcomes, storage/database compensation, authorized endpoint responses, notification creation, and audit retention. Finish with:

```powershell
dotnet build .\ContosoDashboard\ContosoDashboard.csproj --no-restore
```

## Expected outcomes

- No upload or retrieval path requires cloud services.
- Files are not directly addressable through `wwwroot`.
- All document queries exclude deleted/inaccessible documents.
- All completed document actions create audit records.
- The application continues to identify mock authentication and deterministic scanning as training-only.
