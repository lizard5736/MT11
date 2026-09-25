using System.IO;
using System.Text.Json;
using System.Windows.Threading;
using Grip.Core.Notes;

namespace Grip.Services;

/// <summary>
/// Owns the scratchpad's notes and saves them shortly after each edit — the
/// same debounced-timer pattern ClipboardService uses for history, just
/// without the image files. Loaded once at startup so the window can show
/// existing notes the instant it opens instead of a blank flash while a file
/// read happens.
/// </summary>
public sealed class NotepadService : IDisposable
{
    private readonly DispatcherTimer _saveTimer = new() { Interval = TimeSpan.FromMilliseconds(800) };

    public NoteStore Notes { get; } = new();

    public NotepadService()
    {
        Load();
        if (Notes.Notes.Count == 0) Notes.Add(); // never show the window with zero tabs to type into
        _saveTimer.Tick += (_, _) =>
        {
            _saveTimer.Stop();
            Save();
        };
        Notes.Changed += (_, _) =>
        {
            _saveTimer.Stop();
            _saveTimer.Start();
        };
    }

    private void Load()
    {
        try
        {
            if (File.Exists(AppPaths.NotesFile))
                Notes.Load(File.ReadAllText(AppPaths.NotesFile));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            Log.Warn("Notes could not be read: " + ex.Message);
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(AppPaths.LocalDir);
            Core.Settings.SettingsStore.WriteAtomically(AppPaths.NotesFile, Notes.Serialize());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warn("Notes could not be saved: " + ex.Message);
        }
    }

    public void Dispose()
    {
        _saveTimer.Stop();
        Save();
    }
}
