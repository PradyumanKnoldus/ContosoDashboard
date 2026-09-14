# File Storage and Scan Contracts

## IFileStorageService

```text
UploadAsync(Stream content, string relativePath, string contentType, CancellationToken) -> StorageResult
DeleteAsync(string relativePath, CancellationToken) -> bool
OpenReadAsync(string relativePath, CancellationToken) -> Stream?
ExistsAsync(string relativePath, CancellationToken) -> bool
```

The local implementation resolves only normalized relative paths beneath a configured non-`wwwroot` root. It rejects traversal, absolute paths, and invalid generated names. The service never accepts a user filename as the stored path.

## IFileScanService

```text
ScanAsync(Stream content, string fileName, string contentType, CancellationToken) -> FileScanResult
```

The deterministic training implementation recognizes safe and malware-positive test fixtures and returns a non-available result for scan failure. Business logic must not expose content unless the scan passes. Production can replace this service with a real scanner without changing `DocumentService` behavior.

## Upload sequencing

1. Validate request and authorization.
2. Scan content from a rewindable stream.
3. Generate a GUID-based relative path.
4. Save content through `IFileStorageService`.
5. Persist metadata and audit event in EF Core.
6. On persistence failure, delete the stored path or record cleanup failure.

Replacement uses a new path and retains the previous active path until metadata persistence succeeds.
