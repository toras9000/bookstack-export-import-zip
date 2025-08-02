#r "nuget: BookStackApiClient, 25.7.0-lib.1"
#nullable enable
using BookStackApiClient;

record ExportMetadata(string service_url, string instance_id, BookStackVersion version, DateTime export_at);

record ShelfMetadata(
    long id, string name, string slug, string description, long[] books,
    DateTime created_at, DateTime updated_at,
    User created_by, User updated_by, User owned_by,
    ContentTag[]? tags, ShelfCover? cover, ContentPermissionsItem permissions
);
