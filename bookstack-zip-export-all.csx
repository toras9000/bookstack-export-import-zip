#r "nuget: BookStackApiClient, 25.7.0-lib.1"
#r "nuget: Kokuban, 0.2.0"
#r "nuget: Lestaly.General, 0.102.0"
#load ".common.csx"
#nullable enable
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using BookStackApiClient;
using BookStackApiClient.Utility;
using Kokuban;
using Lestaly;

var settings = new
{
    BookStack = new
    {
        // Target BookStack service address
        ServiceUrl = new Uri("http://localhost:8831/"),

        // API token of the user performing the export
        ApiToken = "00001111222233334444555566667777",

        // API secret of the user performing the export
        ApiSecret = "88889999aaaabbbbccccddddeeeeffff",
    },

    Local = new
    {
        // Destination directory for export data.
        ExportDir = ThisSource.RelativeDirectory("exports"),
    },
};

return await Paved.ProceedAsync(async () =>
{
    // Prepare console
    using var outenc = ConsoleWig.OutputEncodingPeriod(Encoding.UTF8);
    using var signal = new SignalCancellationPeriod();

    // Title display
    WriteLine($"Exporting data from BookStack");
    WriteLine($"  Service Address: {settings.BookStack.ServiceUrl}");
    WriteLine();

    // Create client and helper
    var apiUri = new Uri(settings.BookStack.ServiceUrl, "/api/");
    using var http = new HttpClient();
    using var client = new BookStackClient(apiUri, settings.BookStack.ApiToken, settings.BookStack.ApiSecret);
    using var helper = new BookStackClientHelper(client, signal.Token);
    helper.LimitHandler += (args) =>
    {
        WriteLine(Chalk.Yellow[$"Caught in API call rate limitation. Rate limit: {args.Exception.RequestsPerMin} [per minute], {args.Exception.RetryAfter} seconds to lift the limit."]);
        WriteLine(Chalk.Yellow[$"It will automatically retry after a period of time has elapsed."]);
        WriteLine(Chalk.Yellow[$"[Waiting...]"]);
        return ValueTask.CompletedTask;
    };

    // Detect BookStack version
    var system = await helper.Try((c, t) => c.SystemAsync(t));
    var version = BookStackVersion.Parse(system.version);
    if (version < BookStackVersion.Parse("25.07")) throw new PavedMessageException("Unsupported BookStack version.", PavedMessageKind.Warning);

    // Determine output directory
    var exportTime = DateTime.Now;
    var exportDir = settings.Local.ExportDir.RelativeDirectory($"{exportTime:yyyy.MM.dd-HH.mm.ss}").WithCreate();
    WriteLine($"Export to {exportDir.FullName}");
    WriteLine();

    // Options for saving JSON
    var jsonOptions = new JsonSerializerOptions();
    jsonOptions.WriteIndented = true;

    // Create context instance
    var context = new ExportContext(helper, http, jsonOptions, exportDir, signal.Token);

    // Output export information
    var exportMeta = new ExportMetadata(settings.BookStack.ServiceUrl.AbsoluteUri, system.instance_id, version, exportTime);
    await exportDir.RelativeFile("export-meta.json").WriteJsonAsync(exportMeta, jsonOptions);

    // Retrieve information for each book
    await foreach (var book in context.Helper.EnumerateAllBooksAsync())
    {
        // Indicate the status.
        WriteLine($"Exporting book: {Chalk.Green[book.name]} ...");

        // Export zip
        var bookZipFile = exportDir.RelativeFile($"{book.id:D4}B.{book.name.ToFileName()}.zip");
        await context.Helper.Try((c, t) => c.ExportBookZipAsync(book.id, t)).AsTask().WriteToFileAsync(bookZipFile, context.CancelToken);
    }

    WriteLine($"Completed");

});

record ExportContext(BookStackClientHelper Helper, HttpClient Http, JsonSerializerOptions JsonOptions, DirectoryInfo ExportDir, CancellationToken CancelToken);

ShelfMetadata createMetadata(ReadShelfResult shelf, ContentPermissionsItem permissions)
    => new(
        shelf.id, shelf.name, shelf.slug, shelf.description_html,
        shelf.books.Select(b => b.id).ToArray(),
        shelf.created_at, shelf.updated_at,
        shelf.created_by, shelf.updated_by, shelf.owned_by,
        shelf.tags, shelf.cover, permissions
    );
