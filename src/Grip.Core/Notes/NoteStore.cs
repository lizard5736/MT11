using System.Text.Json;

namespace Grip.Core.Notes;

/// <summary>One note in the scratchpad. Its tab label is derived from <see cref="Content"/>
/// (see <see cref="NoteTitle"/>) rather than kept as a separate field — nothing to rename,
/// nothing to fall out of sync with what the note actually says.</summary>
public sealed class Note
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Content { get; set; } = "";
    public DateTimeOffset Modified { get; set; }
}

/// <summary>
/// The scratchpad's notes, in tab order. Not thread-safe: the app touches it
/// from the UI thread only, same as ClipboardHistory.
/// </summary>
public sealed class NoteStore
{
    private readonly List<Note> _notes = new();

    public IReadOnlyList<Note> Notes => _notes;

    public event EventHandler? Changed;

    public Note Add()
    {
        var note = new Note { Modified = DateTimeOffset.UtcNow };
        _notes.Add(note);
        Changed?.Invoke(this, EventArgs.Empty);
        return note;
    }

    public Note? Find(string id) => _notes.FirstOrDefault(n => n.Id == id);

    public bool Remove(string id)
    {
        var note = Find(id);
        if (note == null) return false;
        _notes.Remove(note);
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public void SetContent(string id, string content, DateTimeOffset now)
    {
        var note = Find(id);
        if (note == null || note.Content == content) return;
        note.Content = content;
        note.Modified = now;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    // ---------- persistence ----------

    private sealed class Snapshot
    {
        public int Version { get; set; } = 1;
        public List<Note> Notes { get; set; } = new();
    }

    public string Serialize() =>
        JsonSerializer.Serialize(new Snapshot { Notes = _notes.ToList() }, Settings.SettingsStore.JsonOptions);

    public void Load(string json)
    {
        var snapshot = JsonSerializer.Deserialize<Snapshot>(json, Settings.SettingsStore.JsonOptions);
        _notes.Clear();
        if (snapshot?.Notes != null) _notes.AddRange(snapshot.Notes.Where(n => !string.IsNullOrEmpty(n.Id)));
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
