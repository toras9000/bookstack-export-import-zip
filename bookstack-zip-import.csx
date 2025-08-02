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
        ServiceUrl = new Uri("http://localhost:8841/"),

        // API token of the user performing the export
        ApiToken = "444455556666777788889999aaaabbbb",

        // API secret of the user performing the export
        ApiSecret = "ccccddddeeeeffff0000111122223333",
    },

};

return await Paved.ProceedAsync(async () =>
{
    // Prepare console
    using var outenc = ConsoleWig.OutputEncodingPeriod(Encoding.UTF8);
    using var signal = new SignalCancellationPeriod();

    // Title display
    WriteLine($"Importing data into BookStack");
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

    // Have the user enter the location of the data to be captured.
    WriteLine("Specify the directory for the imported data.");
    Write(">");
    var importInput = ReadLine()?.Unquote().CancelIfWhite();
    var importDataDir = CurrentDir.RelativeDirectory(importInput);

    // Reads information from imported data
    if (!importDataDir.Exists) throw new PavedMessageException("ImportImport data directory does not exist.", PavedMessageKind.Warning);
    var exportMeta = await importDataDir.RelativeFile("export-meta.json").ReadJsonAsync<ExportMetadata>(signal.Token) ?? throw new PavedMessageException("Information is not readable when exporting.");
    if (version < exportMeta.version) throw new PavedMessageException("Importing into versions older than the original is not supported.", PavedMessageKind.Warning);

    // Create context instance
    var context = new ImportContext(helper, http, importDataDir, signal.Token);

    // Enumeration options for single-level searches
    var oneLvEnum = new EnumerationOptions();
    oneLvEnum.RecurseSubdirectories = false;
    oneLvEnum.MatchType = MatchType.Simple;
    oneLvEnum.MatchCasing = MatchCasing.PlatformDefault;
    oneLvEnum.ReturnSpecialDirectories = false;
    oneLvEnum.IgnoreInaccessible = true;

    // Enumerate the directory of book information to be imported
    foreach (var bookZipFile in context.ImportDir.EnumerateFiles("*B.*", oneLvEnum))
    {
        // Indicate the status.
        WriteLine($"Importing: {Chalk.Green[bookZipFile.Name]} ...");

        // Import zip
        var imports = await context.Helper.Try((c, t) => c.CreateImportsAsync(bookZipFile.FullName, cancelToken: t));

        // Run import
        await context.Helper.Try((c, t) => c.RunImportsAsync(imports.id, default, t));
    }

    WriteLine($"Completed");

});

record ImportContext(BookStackClientHelper Helper, HttpClient Http, DirectoryInfo ImportDir, CancellationToken CancelToken);
