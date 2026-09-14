# Authorized File Access Endpoint Contract

## Routes

```text
GET /documents/{documentId}/content
GET /documents/{documentId}/preview
```

Both routes require the existing authenticated cookie. The endpoint delegates to `IDocumentService.OpenContentAsync` and never reads arbitrary paths supplied by the caller.

## Response behavior

- `200`: authorized content stream with persisted MIME type and safe download/inline disposition.
- `401`: unauthenticated request, handled by existing login configuration.
- `403` or access-safe `404`: authenticated user lacks access or the document is deleted; do not reveal whether an inaccessible identifier exists.
- `404`: authorized document has no available content.
- `422`: content cannot be safely served due to invalid stored metadata/path; log details without returning them.

Successful responses create Download or Preview activity records. Failed authorization and invalid-path attempts must not reveal the storage root, physical path, or scanner details.
