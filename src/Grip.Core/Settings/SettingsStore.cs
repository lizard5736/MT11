using System.Text.Json;
using System.Text.Json.Serialization;
using Grip.Core.Actions;
using Grip.Core.Features;
using Grip.Core.Radial;

namespace Grip.Core.Settings;

/// <summary>
/// Loads and saves <see cref="AppSettings"/>. Writes are atomic (temp file +
/// replace), and a file that fails to parse is set aside instead of being
/// silently overwritten, so a bad edit never costs the user everything.
/// </summary>
public sealed class SettingsStore
{
    public static readonly JsonSerializerOptions JsonOptions = CreateOptions();

    private readonly string _path;
    private readonly object _gate = new();

    public SettingsStore(string path) => _path = path;

    public string FilePath => _path;

    public static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = null,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    /// <summary>Reads settings, falling back to defaults. Never throws for a missing or broken file.</summary>
    public AppSettings Load(out string? problem)
    {
        problem = null;
        lock (_gate)
        {
            if (!File.Exists(_path))
                return Normalize(new AppSettings());

            try
            {
                var json = File.ReadAllText(_path);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
                return Normalize(settings);
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or NotSupportedException)
            {
                problem = ex.Message;
                TrySetAside();
                return Normalize(new AppSettings());
            }
        }
    }

    public void Save(AppSettings settings)
    {
        lock (_gate)
        {
            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            WriteAtomically(_path, json);
        }
    }

    public static string Serialize(AppSettings settings) => JsonSerializer.Serialize(settings, JsonOptions);

    /// <summary>Parses an exported file. Throws <see cref="JsonException"/> with a readable message when it is not ours.</summary>
    public static AppSettings Deserialize(string json)
    {
        var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions)
                       ?? throw new JsonException("Empty settings file.");
        return Normalize(settings);
    }

    public static void WriteAtomically(string path, string contents)
    {
        var temp = path + ".tmp";
        File.WriteAllText(temp, contents);
        if (File.Exists(path))
            File.Replace(temp, path, destinationBackupFileName: null, ignoreMetadataErrors: true);
        else
            File.Move(temp, path);
    }

    private void TrySetAside()
    {
        try
        {
            var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);
            File.Move(_path, Path.ChangeExtension(_path, $".broken-{stamp}.json"), overwrite: true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    /// <summary>Fills gaps left by older files and clamps values a hand edit could break.</summary>
    public static AppSettings Normalize(AppSettings s)
    {
        s.General ??= new GeneralSettings();
        s.Panel ??= new PanelSettings();
        s.Features ??= new Dictionary<string, bool>();
        s.Hotkeys ??= new Dictionary<string, string>();
        s.Clipboard ??= new ClipboardSettings();
        s.UrlCleaner ??= new UrlCleanerSettings();
        s.KeepAwake ??= new KeepAwakeSettings();
        s.QuickToggles ??= new QuickTogglesSettings();
        s.CommandBar ??= new CommandBarSettings();
        s.RadialMenu ??= new RadialMenuSettings();
        s.Shelf ??= new ShelfSettings();

        s.Panel.SectionOrder ??= new List<string>();
        s.Panel.HiddenSections ??= new List<string>();
        s.Panel.CollapsedSections ??= new List<string>();

        foreach (var action in ActionCatalog.All.Where(a => a.CanHaveHotkey))
            if (!s.Hotkeys.ContainsKey(action.Id))
                s.Hotkeys[action.Id] = action.DefaultHotkey;

        foreach (var feature in FeatureCatalog.All)
            if (!s.Features.ContainsKey(feature.Id) && feature.IsReady)
                s.Features[feature.Id] = FeatureCatalog.DefaultInstalled(feature.Id);

        var c = s.Clipboard;
        c.MaxItems = Math.Clamp(c.MaxItems, 10, 5000);
        c.RetentionDays = Math.Clamp(c.RetentionDays, 0, 3650);
        c.AutoClearSeconds = Math.Clamp(c.AutoClearSeconds, 5, 24 * 3600);
        c.IgnoredApps ??= new List<string>();

        s.UrlCleaner.ExtraParameters ??= new List<string>();
        s.UrlCleaner.SkipDomains ??= new List<string>();

        s.KeepAwake.Presets ??= new List<int>();
        if (s.KeepAwake.Presets.Count == 0) s.KeepAwake.Presets.AddRange(new[] { 0, 15, 30, 60, 120, 240 });
        s.KeepAwake.Presets = s.KeepAwake.Presets.Where(m => m is >= 0 and <= 7 * 24 * 60).Distinct().Take(8).ToList();
        s.KeepAwake.DefaultMinutes = Math.Clamp(s.KeepAwake.DefaultMinutes, 0, 7 * 24 * 60);

        s.QuickToggles.Visible ??= new List<string>();
        s.CommandBar.Scripts ??= new List<ScriptEntry>();

        var r = s.RadialMenu;
        r.Profiles ??= new List<RadialProfile>();
        if (r.Profiles.Count == 0) r.Profiles.Add(RadialProfile.CreateDefault());
        foreach (var profile in r.Profiles)
        {
            profile.Items ??= new List<RadialItem>();
            if (profile.Items.Count > RadialProfile.MaxItems)
                profile.Items = profile.Items.Take(RadialProfile.MaxItems).ToList();
        }
        if (r.Profiles.All(p => p.Id != r.ActiveProfileId)) r.ActiveProfileId = r.Profiles[0].Id;

        s.SchemaVersion = AppSettings.CurrentSchemaVersion;
        return s;
    }
}
