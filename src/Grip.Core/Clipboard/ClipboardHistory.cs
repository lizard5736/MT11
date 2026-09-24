using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Grip.Core.Search;

namespace Grip.Core.Clipboard;

public enum ClipKind { Text, Link, Image, Files }

public enum ClipFilter { All, Text, Links, Images, Files, Pinned }

/// <summary>One remembered clipboard item.</summary>
public sealed class ClipEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public ClipKind Kind { get; set; }
    public string? Text { get; set; }
    public List<string>? Files { get; set; }
    /// <summary>File name of the stored PNG inside the history image folder.</summary>
    public string? ImageFile { get; set; }
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }
    public string Hash { get; set; } = "";
    public DateTimeOffset Created { get; set; }
    public DateTimeOffset LastUsed { get; set; }
    public bool Pinned { get; set; }
    public string? SourceApp { get; set; }
    public int UseCount { get; set; }

    [JsonIgnore]
    public string? ColorHex => Kind == ClipKind.Text ? ClipText.TryParseColor(Text) : null;

    /// <summary>What search matches against: the text, the file names or the source app.</summary>
    [JsonIgnore]
    public string SearchText => Kind switch
    {
        ClipKind.Files => string.Join(" ", Files ?? new List<string>()),
        ClipKind.Image => SourceApp ?? "",
        _ => Text ?? "",
    };

    public static ClipEntry FromText(string text, string? sourceApp, DateTimeOffset now)
    {
        var kind = ClipText.IsSingleUrl(text) ? ClipKind.Link : ClipKind.Text;
        return new ClipEntry
        {
            Kind = kind,
            Text = text,
            Hash = ClipHash.OfText(text),
            Created = now,
            LastUsed = now,
            SourceApp = sourceApp,
        };
    }

    public static ClipEntry FromFiles(IReadOnlyList<string> files, string? sourceApp, DateTimeOffset now) => new()
    {
        Kind = ClipKind.Files,
        Files = files.ToList(),
        Hash = ClipHash.OfFiles(files),
        Created = now,
        LastUsed = now,
        SourceApp = sourceApp,
    };

    public static ClipEntry FromImage(string imageFile, int width, int height, string hash, string? sourceApp, DateTimeOffset now) => new()
    {
        Kind = ClipKind.Image,
        ImageFile = imageFile,
        ImageWidth = width,
        ImageHeight = height,
        Hash = hash,
        Created = now,
        LastUsed = now,
        SourceApp = sourceApp,
    };
}

public static class ClipHash
{
    public static string OfText(string text) => Hex(SHA256.HashData(Encoding.UTF8.GetBytes("t:" + text)));

    public static string OfFiles(IEnumerable<string> files) =>
        Hex(SHA256.HashData(Encoding.UTF8.GetBytes("f:" + string.Join("\n", files.Select(f => f.ToUpperInvariant())))));

    public static string OfBytes(ReadOnlySpan<byte> data) => Hex(SHA256.HashData(data));

    private static string Hex(byte[] bytes) => Convert.ToHexString(bytes).ToLowerInvariant();
}

public static partial class ClipText
{
    /// <summary>Longest text kept in history; bigger copies are skipped to protect memory.</summary>
    public const int MaxTextLength = 2_000_000;

    [GeneratedRegex(@"^(https?|ftp)://[^\s/$.?#][^\s]*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UrlRegex();

    [GeneratedRegex(@"^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$", RegexOptions.CultureInvariant)]
    private static partial Regex HexColorRegex();

    [GeneratedRegex(@"^rgba?\(\s*(\d{1,3})\s*,\s*(\d{1,3})\s*,\s*(\d{1,3})\s*(?:,\s*[\d.]+\s*)?\)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RgbColorRegex();

    public static bool IsSingleUrl(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var trimmed = text.Trim();
        return trimmed.Length <= 8192 && UrlRegex().IsMatch(trimmed);
    }

    /// <summary>Normalizes "#abc", "#aabbcc", "#aarrggbb" and "rgb(…)" to "#RRGGBB" (or "#AARRGGBB").</summary>
    public static string? TryParseColor(string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 40) return null;
        var t = text.Trim();
        if (HexColorRegex().IsMatch(t))
        {
            var hex = t[1..].ToUpperInvariant();
            if (hex.Length == 3) hex = string.Concat(hex.Select(c => new string(c, 2)));
            return "#" + hex;
        }
        var m = RgbColorRegex().Match(t);
        if (m.Success)
        {
            var parts = new int[3];
            for (int i = 0; i < 3; i++)
            {
                parts[i] = int.Parse(m.Groups[i + 1].Value, System.Globalization.CultureInfo.InvariantCulture);
                if (parts[i] > 255) return null;
            }
            return $"#{parts[0]:X2}{parts[1]:X2}{parts[2]:X2}";
        }
        return null;
    }

    /// <summary>A single-line preview: whitespace runs collapsed, trimmed to <paramref name="max"/> chars.</summary>
    public static string Preview(string? text, int max = 400)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        var sb = new StringBuilder(Math.Min(text.Length, max + 1));
        bool space = false;
        foreach (var ch in text)
        {
            if (char.IsWhiteSpace(ch))
            {
                space = sb.Length > 0;
                continue;
            }
            if (space) { sb.Append(' '); space = false; }
            sb.Append(ch);
            if (sb.Length >= max) { sb.Append('…'); break; }
        }
        return sb.ToString();
    }
}

public sealed class ClipboardHistoryOptions
{
    public int MaxItems { get; set; } = 200;
    public int RetentionDays { get; set; }
}

/// <summary>
/// The ordered history (most recently used first). Copying something already
/// present promotes it instead of duplicating it. Pinned entries never age out.
/// Not thread-safe: the app touches it from the UI thread only.
/// </summary>
public sealed class ClipboardHistory
{
    private readonly List<ClipEntry> _items = new();

    public IReadOnlyList<ClipEntry> Items => _items;

    public int Count => _items.Count;

    public int PinnedCount => _items.Count(e => e.Pinned);

    public event EventHandler? Changed;

    /// <summary>Entries dropped by the last trim; the app deletes their image files.</summary>
    public event EventHandler<IReadOnlyList<ClipEntry>>? Removed;

    public ClipboardHistoryOptions Options { get; } = new();

    public ClipEntry AddOrPromote(ClipEntry candidate, DateTimeOffset now)
    {
        var existing = _items.FirstOrDefault(e => e.Hash == candidate.Hash && e.Kind == candidate.Kind);
        if (existing != null)
        {
            _items.Remove(existing);
            existing.LastUsed = now;
            existing.UseCount++;
            _items.Insert(0, existing);
            // A duplicate image carries a redundant file; the caller cleans it.
            if (candidate.ImageFile != null && candidate.ImageFile != existing.ImageFile)
                Removed?.Invoke(this, new[] { candidate });
            Trim(now);
            Changed?.Invoke(this, EventArgs.Empty);
            return existing;
        }

        candidate.Created = candidate.Created == default ? now : candidate.Created;
        candidate.LastUsed = now;
        _items.Insert(0, candidate);
        Trim(now);
        Changed?.Invoke(this, EventArgs.Empty);
        return candidate;
    }

    public void Promote(string id, DateTimeOffset now)
    {
        var entry = Find(id);
        if (entry == null) return;
        _items.Remove(entry);
        entry.LastUsed = now;
        entry.UseCount++;
        _items.Insert(0, entry);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public ClipEntry? Find(string id) => _items.FirstOrDefault(e => e.Id == id);

    public bool Remove(string id)
    {
        var entry = Find(id);
        if (entry == null) return false;
        _items.Remove(entry);
        Removed?.Invoke(this, new[] { entry });
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public void SetPinned(string id, bool pinned)
    {
        var entry = Find(id);
        if (entry == null || entry.Pinned == pinned) return;
        entry.Pinned = pinned;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Clear(bool keepPinned = true)
    {
        var removed = _items.Where(e => !keepPinned || !e.Pinned).ToList();
        if (removed.Count == 0) return;
        _items.RemoveAll(e => !keepPinned || !e.Pinned);
        Removed?.Invoke(this, removed);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Applies the size and age limits. Pinned entries are exempt from both.</summary>
    public void Trim(DateTimeOffset now)
    {
        var removed = new List<ClipEntry>();
        if (Options.RetentionDays > 0)
        {
            var cutoff = now - TimeSpan.FromDays(Options.RetentionDays);
            foreach (var e in _items.Where(e => !e.Pinned && e.LastUsed < cutoff).ToList())
            {
                _items.Remove(e);
                removed.Add(e);
            }
        }

        int unpinned = _items.Count(e => !e.Pinned);
        for (int i = _items.Count - 1; i >= 0 && unpinned > Options.MaxItems; i--)
        {
            if (_items[i].Pinned) continue;
            removed.Add(_items[i]);
            _items.RemoveAt(i);
            unpinned--;
        }

        if (removed.Count > 0) Removed?.Invoke(this, removed);
    }

    public IEnumerable<ClipEntry> Query(string? query, ClipFilter filter)
    {
        IEnumerable<ClipEntry> source = filter switch
        {
            ClipFilter.Text => _items.Where(e => e.Kind == ClipKind.Text),
            ClipFilter.Links => _items.Where(e => e.Kind == ClipKind.Link),
            ClipFilter.Images => _items.Where(e => e.Kind == ClipKind.Image),
            ClipFilter.Files => _items.Where(e => e.Kind == ClipKind.Files),
            ClipFilter.Pinned => _items.Where(e => e.Pinned),
            _ => _items,
        };

        if (string.IsNullOrWhiteSpace(query)) return source;

        var terms = TextNormalizer.Normalize(query).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return source
            .Select((e, index) => (Entry: e, Index: index, Score: Score(e, terms)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Index)
            .Select(x => x.Entry);
    }

    private static int Score(ClipEntry entry, string[] terms)
    {
        var haystack = TextNormalizer.Normalize(entry.SearchText + " " + (entry.SourceApp ?? ""));
        int score = 0;
        foreach (var term in terms)
        {
            int index = haystack.IndexOf(term, StringComparison.Ordinal);
            if (index < 0) return 0;
            bool wordStart = index == 0 || !char.IsLetterOrDigit(haystack[index - 1]);
            score += wordStart ? 3 : 1;
        }
        return score;
    }

    // ---------- persistence ----------

    private sealed class Snapshot
    {
        public int Version { get; set; } = 1;
        public List<ClipEntry> Items { get; set; } = new();
    }

    public string Serialize() =>
        JsonSerializer.Serialize(new Snapshot { Items = _items.ToList() }, Settings.SettingsStore.JsonOptions);

    public void Load(string json)
    {
        var snapshot = JsonSerializer.Deserialize<Snapshot>(json, Settings.SettingsStore.JsonOptions);
        _items.Clear();
        if (snapshot?.Items != null)
            _items.AddRange(snapshot.Items.Where(e => !string.IsNullOrEmpty(e.Hash)));
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
