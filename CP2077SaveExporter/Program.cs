using System.Text.Json;
using WolvenKit.Common.Services;
using WolvenKit.Core.Compression;
using WolvenKit.RED4.Save.IO;
using WolvenKit.RED4.TweakDB.Helper;

namespace CP2077SaveExporter;

internal static class Program
{
    private const int ExitOk = 0;
    private const int ExitUsage = 1;
    private const int ExitLoadError = 2;
    private const int ExitInitError = 3;

    private const string RawFileName = "save.raw.json";
    private const string EnrichedFileName = "save.enriched.json";

    private enum ExportOutputMode
    {
        Raw,
        Enriched,
        Both,
    }

    private static async Task<int> Main(string[] args)
    {
        Console.Error.WriteLine("CP2077 read-only save exporter (WolvenKit-based).");

        if (args.Length == 0)
        {
            PrintUsage();
            return ExitUsage;
        }

        if (args.Length == 1 && args[0] is "-h" or "--help")
        {
            PrintUsage();
            return ExitOk;
        }

        if (!TryParseArgs(args, out var savPath, out var mode, out var outputDirectory, out var parseError))
        {
            if (parseError != null)
            {
                Console.Error.WriteLine(parseError);
            }

            return ExitUsage;
        }

        // Match CP2077SaveEditor Form2: Oodle off for embedded kark decompression used by HashService / streams.
        CompressionSettings.Get().UseOodle = false;

        // Editor also calls ModManager.LoadTypes() (separate assembly) to register modded RED classes for modded saves.
        // That path is not available here without referencing the editor; modded ScriptableSystems may fail to parse.

        savPath = Path.GetFullPath(savPath);

        if (!File.Exists(savPath))
        {
            Console.Error.WriteLine($"File not found: {savPath}");
            return ExitLoadError;
        }

        HashService? hashService = null;
        TweakDBStringHelper? tweakDbStrings = null;

        try
        {
            // Older WolvenKit: HashService loads embedded pools synchronously in the constructor (no Task Loaded).
            Console.Error.WriteLine("Initializing hash / string resolution (WolvenKit.Common)...");
            hashService = new HashService();

            Console.Error.WriteLine("Loading TweakDB string sidecar (optional CRC-based map; parallel to HashService pools)...");
            using (var tweakStream = typeof(HashService).Assembly.GetManifestResourceStream("WolvenKit.Common.Resources.tweakdbstr.kark"))
            {
                if (tweakStream != null)
                {
                    tweakDbStrings = new TweakDBStringHelper();
                    tweakDbStrings.LoadFromStream(tweakStream);
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Initialization failed: {ex.Message}");
            return ExitInitError;
        }

        Console.Error.WriteLine($"Reading save: {savPath}");
        var (code, save) = SaveLoader.Load(savPath);
        if (code != EFileReadErrorCodes.NoError || save == null)
        {
            Console.Error.WriteLine($"ReadFile failed: {code}");
            return ExitLoadError;
        }

        IReadOnlyDictionary<uint, string>? knownFacts = null;
        var factsPath = Path.Combine(AppContext.BaseDirectory, "Facts.json");
        if (File.Exists(factsPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(factsPath).ConfigureAwait(false);
                knownFacts = TryParseFactsJsonObject(json);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Warning: could not parse Facts.json; quest fact names will be omitted. " + ex.Message);
            }
        }
        else
        {
            Console.Error.WriteLine("Warning: Facts.json not beside executable; quest fact names will be hash-only.");
        }

        _ = tweakDbStrings;

        var snapshot = ProgressExtractor.Build(save, hashService!, knownFacts);

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        var outDir = outputDirectory == null
            ? Directory.GetCurrentDirectory()
            : Path.GetFullPath(outputDirectory);
        var rawPath = Path.Combine(outDir, RawFileName);
        var enrichedPath = Path.Combine(outDir, EnrichedFileName);

        if (mode == ExportOutputMode.Raw || mode == ExportOutputMode.Both)
        {
            var rawView = new RawExportSnapshot
            {
                Header = snapshot.Header,
                Raw = snapshot.Raw,
            };
            await JsonExporter.WriteAsync(rawPath, rawView, jsonOptions).ConfigureAwait(false);
            Console.Error.WriteLine($"Wrote: {rawPath}");
        }

        if (mode == ExportOutputMode.Enriched || mode == ExportOutputMode.Both)
        {
            await JsonExporter.WriteAsync(enrichedPath, snapshot, jsonOptions).ConfigureAwait(false);
            Console.Error.WriteLine($"Wrote: {enrichedPath}");
        }

        return ExitOk;
    }

    private static void PrintUsage()
    {
        Console.Error.WriteLine("Usage: CP2077SaveExporter [--mode raw|enriched|both] <path-to-sav.dat> [output-directory]");
        Console.Error.WriteLine("Default mode: enriched");
        Console.Error.WriteLine("Output directory defaults to the current working directory when omitted.");
        Console.Error.WriteLine($"Files written: {RawFileName} and/or {EnrichedFileName}");
    }

    private static bool TryParseArgs(
        string[] args,
        out string savPath,
        out ExportOutputMode mode,
        out string? outputDirectory,
        out string? error)
    {
        savPath = "";
        mode = ExportOutputMode.Enriched;
        outputDirectory = null;
        error = null;

        if (args[0] == "--mode")
        {
            if (args.Length is < 3 or > 4)
            {
                error = args.Length switch
                {
                    1 => "--mode requires a mode value and a save path.",
                    2 => "Missing save path.",
                    _ => "Unexpected extra arguments.",
                };
                return false;
            }

            var m = args[1].ToLowerInvariant();
            switch (m)
            {
                case "raw":
                    mode = ExportOutputMode.Raw;
                    break;
                case "enriched":
                    mode = ExportOutputMode.Enriched;
                    break;
                case "both":
                    mode = ExportOutputMode.Both;
                    break;
                default:
                    error = $"Invalid --mode value: {args[1]} (expected raw, enriched, or both)";
                    return false;
            }

            savPath = args[2];
            if (string.IsNullOrWhiteSpace(savPath))
            {
                error = "Missing save path.";
                return false;
            }

            if (savPath.StartsWith('-'))
            {
                error = $"Unknown option: {savPath}";
                return false;
            }

            if (args.Length == 4)
            {
                var od = args[3];
                if (string.IsNullOrWhiteSpace(od))
                {
                    error = "Invalid output directory.";
                    return false;
                }

                if (od.StartsWith('-'))
                {
                    error = $"Unknown option: {od}";
                    return false;
                }

                outputDirectory = od;
            }

            return true;
        }

        if (args.Length is < 1 or > 2)
        {
            error = args.Length > 2 && args[1] == "--mode"
                ? "Invalid argument order: use CP2077SaveExporter [--mode raw|enriched|both] <path-to-sav.dat> [output-directory]"
                : "Unexpected extra arguments.";
            return false;
        }

        if (args.Length == 2 && args[1] == "--mode")
        {
            error = "Invalid argument order: use CP2077SaveExporter [--mode raw|enriched|both] <path-to-sav.dat> [output-directory]";
            return false;
        }

        if (args[0].StartsWith('-'))
        {
            error = $"Unknown option: {args[0]}";
            return false;
        }

        savPath = args[0];
        if (string.IsNullOrWhiteSpace(savPath))
        {
            error = "Missing save path.";
            return false;
        }

        if (args.Length == 2)
        {
            var od = args[1];
            if (string.IsNullOrWhiteSpace(od))
            {
                error = "Invalid output directory.";
                return false;
            }

            if (od.StartsWith('-'))
            {
                error = $"Unknown option: {od}";
                return false;
            }

            outputDirectory = od;
        }

        return true;
    }

    /// <summary>
    /// Facts.json is a single JSON object whose keys are decimal fact hashes (as JSON strings) and values are fact names (strings).
    /// Uses <see cref="JsonDocument"/> so deserialization does not depend on Dictionary key typing quirks.
    /// </summary>
    private static Dictionary<uint, string>? TryParseFactsJsonObject(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var dict = new Dictionary<uint, string>();
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (!uint.TryParse(prop.Name, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var key))
            {
                continue;
            }

            if (prop.Value.ValueKind == JsonValueKind.String)
            {
                dict[key] = prop.Value.GetString() ?? "";
            }
        }

        return dict;
    }
}
