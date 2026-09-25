namespace Grip.Core.Localization;

public static partial class StringTable
{
    private static void AddScratchpad(Action<string, string, string> A)
    {
        A("scratchpad.title", "Блокнот", "Scratchpad");
        A("scratchpad.untitled", "Без названия", "Untitled");
        A("scratchpad.new", "Новая заметка", "New note");
        A("scratchpad.autosave", "Сохраняется автоматически", "Saves automatically");
        A("scratchpad.delete.confirm", "Удалить заметку «{0}»?", "Delete note “{0}”?");
    }
}
