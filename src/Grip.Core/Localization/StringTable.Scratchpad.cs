namespace Grip.Core.Localization;

public static partial class StringTable
{
    private static void AddScratchpad(Action<string, string, string> A)
    {
        A("scratchpad.title", "Блокнот", "Scratchpad");
        A("scratchpad.untitled", "Без названия", "Untitled");
        A("scratchpad.new", "Новая заметка", "New note");
        A("scratchpad.preview", "Предпросмотр Markdown", "Markdown preview");
        A("scratchpad.autosave", "Сохраняется автоматически", "Saves automatically");
        A("scratchpad.delete.confirm", "Удалить заметку «{0}»?", "Delete note “{0}”?");
        A("scratchpad.export", "Сохранить как файл", "Save as file");
        A("scratchpad.exported", "Заметка сохранена", "Note saved");
        A("scratchpad.exportFailed", "Не получилось сохранить файл: {0}", "Couldn't save the file: {0}");
        A("scratchpad.fileFilter", "Markdown (*.md)|*.md|Текст (*.txt)|*.txt|Все файлы (*.*)|*.*", "Markdown (*.md)|*.md|Text (*.txt)|*.txt|All files (*.*)|*.*");
    }
}
