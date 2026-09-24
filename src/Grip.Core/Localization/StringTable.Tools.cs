namespace Grip.Core.Localization;

public static partial class StringTable
{
    private static void AddTools(Action<string, string, string> A)
    {
        // Clipboard history
        A("clipboard.title", "История буфера", "Clipboard history");
        A("clipboard.search", "Поиск в истории", "Search history");
        A("clipboard.filter.all", "Все", "All");
        A("clipboard.filter.text", "Текст", "Text");
        A("clipboard.filter.links", "Ссылки", "Links");
        A("clipboard.filter.images", "Картинки", "Images");
        A("clipboard.filter.files", "Файлы", "Files");
        A("clipboard.filter.pinned", "Закреплённые", "Pinned");
        A("clipboard.empty", "История пуста. Скопируйте что-нибудь — оно появится здесь.", "Nothing here yet. Copy something and it shows up here.");
        A("clipboard.noResults", "Ничего не найдено", "No matches");
        A("clipboard.disabled", "История буфера выключена", "Clipboard history is off");
        A("clipboard.enable", "Включить", "Turn on");
        A("clipboard.openHistory", "Вся история", "Full history");
        A("clipboard.pin", "Закрепить", "Pin");
        A("clipboard.unpin", "Открепить", "Unpin");
        A("clipboard.delete", "Удалить", "Delete");
        A("clipboard.paste", "Вставить", "Paste");
        A("clipboard.pastePlain", "Вставить без форматирования", "Paste as plain text");
        A("clipboard.copy", "Копировать", "Copy");
        A("clipboard.fileCount", "файл|файла|файлов", "file|files");
        A("clipboard.image", "Картинка {0} × {1}", "Image {0} × {1}");
        A("clipboard.items", "запись|записи|записей", "item|items");
        A("clipboard.chars", "символ|символа|символов", "character|characters");
        A("clipboard.fromApp", "из {0}", "from {0}");
        A("clipboard.hint", "↑↓ выбор   Enter вставить   Shift+Enter без оформления   Ctrl+P закрепить   Del удалить",
            "↑↓ select   Enter paste   Shift+Enter plain   Ctrl+P pin   Del delete");
        A("clipboard.pasteFailed", "Не получилось вставить: окно запущено от имени администратора. Запись уже в буфере.",
            "Couldn't paste: that window runs as administrator. The item is on the clipboard.");
        A("clipboard.cleared", "Буфер очищен", "Clipboard cleared");
        A("clipboard.historyCleared", "История очищена", "History cleared");
        A("clipboard.plainPasted", "Вставлено без форматирования", "Pasted as plain text");
        A("clipboard.noText", "В буфере нет текста", "No text on the clipboard");
        A("clipboard.missingFiles", "Некоторых файлов уже нет на месте", "Some files are no longer there");

        // Links
        A("url.cleaned", "Ссылка очищена: −{0}", "Link cleaned: −{0}");
        A("url.unwrapped", "Редирект раскрыт, ссылка очищена", "Redirect unwrapped, link cleaned");
        A("url.nothing", "В ссылке нет трекеров", "No trackers in that link");
        A("url.noLink", "В буфере нет ссылки", "No link on the clipboard");

        // Quick toggles
        A("toggle.darkMode", "Тёмная тема", "Dark mode");
        A("toggle.desktopIcons", "Значки рабочего стола", "Desktop icons");
        A("toggle.hiddenFiles", "Скрытые файлы", "Hidden files");
        A("toggle.fileExtensions", "Расширения файлов", "File extensions");
        A("toggle.lock", "Заблокировать", "Lock");
        A("toggle.displayOff", "Погасить экран", "Screen off");
        A("toggle.emptyBin", "Очистить корзину", "Empty bin");
        A("toggle.eject", "Извлечь диски", "Eject drives");
        A("toggle.sleep", "Сон", "Sleep");
        A("toggle.emptyBin.confirm", "Очистить корзину? Файлы удалятся навсегда.", "Empty the Recycle Bin? The files are gone for good.");
        A("toggle.emptyBin.done", "Корзина очищена", "Recycle Bin emptied");
        A("toggle.emptyBin.alreadyEmpty", "Корзина уже пуста", "The Recycle Bin is already empty");
        A("toggle.eject.none", "Нет съёмных дисков", "No removable drives");
        A("toggle.eject.done", "Извлечено: {0}", "Ejected: {0}");
        A("toggle.eject.failed", "Не удалось извлечь {0}: диск занят", "Couldn't eject {0}: it's in use");
        A("toggle.state.on", "Вкл.", "On");
        A("toggle.state.off", "Выкл.", "Off");
        A("toggle.darkMode.hud.on", "Тёмная тема", "Dark mode");
        A("toggle.darkMode.hud.off", "Светлая тема", "Light mode");
        A("toggle.desktopIcons.hud.on", "Значки на рабочем столе видны", "Desktop icons shown");
        A("toggle.desktopIcons.hud.off", "Значки на рабочем столе скрыты", "Desktop icons hidden");
        A("toggle.hiddenFiles.hud.on", "Скрытые файлы видны", "Hidden files shown");
        A("toggle.hiddenFiles.hud.off", "Скрытые файлы спрятаны", "Hidden files hidden");
        A("toggle.fileExtensions.hud.on", "Расширения файлов видны", "File extensions shown");
        A("toggle.fileExtensions.hud.off", "Расширения файлов скрыты", "File extensions hidden");

        // Command Bar
        A("commandBar.placeholder", "Приложения, окна, расчёты, эмодзи…", "Apps, windows, math, emoji…");
        A("commandBar.section.best", "Лучшее совпадение", "Top hit");
        A("commandBar.section.apps", "Приложения", "Apps");
        A("commandBar.section.windows", "Окна", "Windows");
        A("commandBar.section.actions", "Действия", "Actions");
        A("commandBar.section.settings", "Параметры Windows", "Windows settings");
        A("commandBar.section.clipboard", "Буфер обмена", "Clipboard");
        A("commandBar.section.calc", "Калькулятор", "Calculator");
        A("commandBar.section.units", "Конвертер", "Converter");
        A("commandBar.section.emoji", "Эмодзи", "Emoji");
        A("commandBar.section.web", "Интернет", "Web");
        A("commandBar.section.scripts", "Скрипты", "Scripts");
        A("commandBar.webSearch", "Искать «{0}»", "Search for “{0}”");
        A("commandBar.webSearch.in", "в {0}", "on {0}");
        A("commandBar.copyResult", "Enter — скопировать", "Enter to copy");
        A("commandBar.open", "Открыть", "Open");
        A("commandBar.switchTo", "Перейти", "Switch to");
        A("commandBar.run", "Запустить", "Run");
        A("commandBar.hint", "↑↓ выбор   Enter открыть   Esc закрыть", "↑↓ select   Enter open   Esc close");
        A("commandBar.layoutFixed", "Показаны результаты для «{0}»", "Showing results for “{0}”");
        A("commandBar.emoji.copied", "Эмодзи скопирован", "Emoji copied");
        A("commandBar.runFailed", "Не удалось запустить: {0}", "Couldn't start: {0}");
        A("commandBar.kind.app", "Приложение", "App");
        A("commandBar.kind.window", "Окно", "Window");
        A("commandBar.kind.setting", "Параметры", "Settings");
        A("commandBar.kind.action", "Действие", "Action");
        A("commandBar.kind.script", "Скрипт", "Script");

        // Radial menu
        A("radial.profile.main", "Основной", "Main");
        A("radial.item.snip", "Скриншот", "Screenshot");
        A("radial.item.commandBar", "Команды", "Commands");
        A("radial.item.clipboard", "Буфер", "Clipboard");
        A("radial.item.keepAwake", "Не спать", "Awake");
        A("radial.item.lock", "Блокировка", "Lock");
        A("radial.item.explorer", "Проводник", "File Explorer");
        A("radial.item.media", "Музыка", "Media");
        A("radial.item.taskManager", "Диспетчер", "Tasks");
        A("radial.back", "Назад", "Back");
        A("radial.failed", "Не удалось выполнить: {0}", "Couldn't run: {0}");

        // Shelf
        A("shelf.title", "Полка", "Shelf");
        A("shelf.drop", "Перетащите сюда файлы, текст или ссылки", "Drop files, text or links here");
        A("shelf.dragHint", "Тяните предметы наружу, чтобы использовать", "Drag items out to use them");
        A("shelf.clear", "Очистить полку", "Clear the shelf");
        A("shelf.copyPaths", "Копировать пути", "Copy paths");
        A("shelf.showInFolder", "Показать в папке", "Show in folder");
        A("shelf.items", "предмет|предмета|предметов", "item|items");
        A("shelf.pin", "Не прятать", "Keep open");
        A("shelf.text", "Текст", "Text");
        A("shelf.link", "Ссылка", "Link");
    }
}
