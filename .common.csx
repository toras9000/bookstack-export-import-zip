#r "nuget: BookStackApiClient, 25.7.0-lib.1"
#nullable enable
using BookStackApiClient;

record ExportMetadata(string service_url, string instance_id, BookStackVersion version, DateTime export_at);
