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

    private static async Task<int> Main(string[] args)
    {
        Console.Error.WriteLine("CP2077 read-only save exporter (WolvenKit-based).");

        if (args.Length < 1 || args[0] is "-h" or "--help")
        {
            Console.Error.WriteLine("Usage: CP2077SaveExporter <path-to-sav.dat> [output.json]");
            return args.Length < 1 ? ExitUsage : ExitOk;
        }

        // Match CP2077SaveEditor Form2: Oodle off for embedded kark decompression used by HashService / streams.
        CompressionSettings.Get().UseOodle = false;

        // Editor also calls ModManager.LoadTypes() (separate assembly) to register modded RED classes for modded saves.
        // That path is not available here without referencing the editor; modded ScriptableSystems may fail to parse.

        var savPath = Path.GetFullPath(args[0]);
        var outPath = args.Length >= 2
            ? Path.GetFullPath(args[1])
            : Path.Combine(Path.GetDirectoryName(savPath) ?? ".", "export.json");

        if (!File.Exists(savPath))
        {
            Console.Error.WriteLine($"File not found: {savPath}");
            return ExitLoadError;
        }

        HashService? hashService = null;
        TweakDBStringHelper? tweakDbStrings = null;

        try
        {
            Console.Error.WriteLine("Initializing hash / string resolution (WolvenKit.Common)...");
            hashService = new HashService();
            await hashService.Loaded.ConfigureAwait(false);

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

        await JsonExporter.WriteAsync(outPath, snapshot, jsonOptions).ConfigureAwait(false);
        Console.Error.WriteLine($"Wrote: {outPath}");
        return ExitOk;
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
