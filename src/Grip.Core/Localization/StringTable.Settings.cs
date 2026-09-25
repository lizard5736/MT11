namespace Grip.Core.Localization;

public static partial class StringTable
{
    private static void AddSettings(Action<string, string, string> A)
    {
        A("settings.title", "Настройки Grip", "Grip Settings");
        A("settings.search", "Поиск настроек", "Search settings");
        A("settings.noResults", "Ничего не найдено", "No matches");
        A("settings.results", "Результаты поиска", "Search results");
        A("settings.nav.essentials", "Основное", "Essentials");
        A("settings.nav.clipboard", "Буфер и файлы", "Clipboard and files");
        A("settings.nav.tools", "Инструменты", "Tools");
        A("settings.nav.energy", "Энергия", "Energy");

        A("settings.page.general", "Общие", "General");
        A("settings.page.general.desc", "Язык, тема, автозапуск и вид панели.", "Language, theme, startup and panel layout.");
        A("settings.page.features", "Функции", "Features");
        A("settings.page.features.desc", "Устанавливайте только то, чем пользуетесь. Удалённая функция исчезает из приложения и перестаёт загружаться, а её настройки сохраняются.",
            "Install only what you use. An uninstalled feature disappears from the app and stops loading, and its settings are kept.");
        A("settings.page.access", "Доступы", "Access");
        A("settings.page.access.desc", "Какие права нужны Grip и что сейчас работает в фоне.", "What Grip needs and what runs in the background right now.");
        A("settings.page.hotkeys", "Горячие клавиши", "Shortcuts");
        A("settings.page.hotkeys.desc", "Все сочетания Grip в одном месте. Нажмите на поле, затем нужное сочетание.",
            "Every Grip shortcut in one place. Click a field, then press the shortcut.");
        A("settings.page.clipboard", "Буфер обмена", "Clipboard");
        A("settings.page.clipboard.desc", "История скопированного, автоочистка и вставка без форматирования.",
            "Copy history, auto-clear and plain-text paste.");
        A("settings.page.links", "Ссылки", "Links");
        A("settings.page.links.desc", "Очистка ссылок от меток слежки и редиректов.", "Strip tracking tags and redirects from links.");
        A("settings.page.shelf", "Полка", "Shelf");
        A("settings.page.shelf.desc", "Временное место для перетаскиваемых файлов, текста и ссылок.", "A parking spot for files, text and links you drag.");
        A("settings.page.commandBar", "Command Bar", "Command Bar");
        A("settings.page.commandBar.desc", "Что искать и чем дополнить строку команд.", "What the command field searches and what it can run.");
        A("settings.page.radial", "Радиальное меню", "Radial menu");
        A("settings.page.radial.desc", "Колесо действий вокруг курсора: профили, пункты и способ вызова.", "The wheel around the pointer: profiles, items and how to open it.");
        A("settings.page.toggles", "Переключатели", "Quick toggles");
        A("settings.page.toggles.desc", "Какие переключатели показывать в панели и в каком порядке.", "Which toggles the panel shows and in what order.");
        A("settings.page.keepAwake", "Не спать", "Keep awake");
        A("settings.page.keepAwake.desc", "Сроки, экран и режим с закрытой крышкой.", "Durations, the display and lid-closed mode.");
        A("settings.page.about", "О программе", "About");
        A("settings.page.about.desc", "Версия, данные, экспорт и импорт настроек.", "Version, data, settings export and import.");

        // Features hub
        A("hub.count", "Установлено {0} из {1}", "{0} of {1} installed");
        A("hub.installAll", "Установить все", "Install all");
        A("hub.uninstallAll", "Удалить все", "Uninstall all");
        A("hub.uninstallAll.confirm", "Удалить все функции? Их настройки сохранятся, вернуть можно в один клик.",
            "Uninstall every feature? Their settings are kept and come back with one click.");
        A("hub.presets", "Начать с набора", "Start with a bundle");
        A("hub.presets.hint", "Один клик настраивает Grip под ваш стиль работы. Всё остальное тоже в один клик.",
            "One click sets Grip up for how you work. Everything else stays one click away.");
        A("hub.applied", "Набор «{0}» применён", "“{0}” applied");
        A("hub.energy.tip", "Что функция держит в фоне, пока включена. Удалённые функции не загружаются вообще.",
            "What the feature keeps running while it's on. Uninstalled features load nothing at all.");

        // General
        A("general.language", "Язык", "Language");
        A("general.language.system", "Как в Windows", "Same as Windows");
        A("general.theme", "Тема", "Theme");
        A("general.theme.dark", "Графит", "Graphite");
        A("general.theme.light", "Светлая", "Light");
        A("general.theme.system", "Как в Windows", "Same as Windows");
        A("general.startup", "Запускать вместе с Windows", "Start with Windows");
        A("general.startup.desc", "Grip тихо появится в трее после входа в систему.", "Grip quietly appears in the tray after you sign in.");
        A("general.hud", "Подсказки на экране", "On-screen hints");
        A("general.hud.desc", "Короткие сообщения вроде «Ссылка очищена» внизу экрана.", "Short notes such as “Link cleaned” at the bottom of the screen.");
        A("general.panel", "Панель", "Panel");
        A("general.panel.layout", "Вид панели", "Panel layout");
        A("general.panel.layout.desc", "Вкладки показывают один раздел, список — все сразу.", "Tabs show one section at a time, the list shows them all.");
        A("general.panel.compact", "Компактный режим", "Compact layout");
        A("general.panel.compact.desc", "Меньше отступов — больше помещается.", "Tighter spacing fits more in.");
        A("general.panel.sections", "Разделы панели", "Panel sections");
        A("general.panel.sections.desc", "Порядок и видимость разделов.", "Section order and visibility.");
        A("general.reset", "Сбросить все настройки", "Reset all settings");
        A("general.reset.desc", "История буфера останется на месте.", "Your clipboard history stays.");
        A("general.reset.confirm", "Вернуть все настройки к исходным?", "Reset every setting to its default?");
        A("general.trayHint.title", "Закрепите значок в трее", "Pin the tray icon");
        A("general.trayHint.desc", "Windows прячет новые значки в скрытую область. Перетащите значок Grip на панель задач или включите его в параметрах.",
            "Windows hides new icons in the overflow. Drag the Grip icon onto the taskbar or turn it on in Settings.");
        A("general.trayHint.open", "Параметры панели задач", "Taskbar settings");

        // Access
        A("access.admin.title", "Права администратора", "Administrator rights");
        A("access.admin.granted", "Grip запущен от имени администратора", "Grip runs as administrator");
        A("access.admin.normal", "Grip запущен с обычными правами", "Grip runs with standard rights");
        A("access.admin.desc", "Windows не даёт обычным программам нажимать клавиши и вставлять текст в окна, запущенные от имени администратора, и не даёт читать температуру процессора через PawnIO. Если нужно то или другое, перезапустите Grip с правами администратора.",
            "Windows doesn't let regular apps press keys or paste into windows running as administrator, or read CPU temperature through PawnIO. If you need either, restart Grip as administrator.");
        A("access.admin.restart", "Перезапустить от имени администратора", "Restart as administrator");
        A("access.background.title", "Что работает в фоне", "What runs in the background");
        A("access.background.desc", "Только установленные и включённые функции. Остальные ничего не загружают.", "Only installed, switched-on features. Everything else loads nothing.");
        A("access.background.keyboard", "Слушают клавиатуру", "Listening to the keyboard");
        A("access.background.mouse", "Слушают мышь", "Listening to the mouse");
        A("access.background.events", "Реагируют на события", "Reacting to events");
        A("access.background.hotkeys", "Горячие клавиши", "Shortcuts");
        A("access.background.none", "ничего", "nothing");
        A("access.data.title", "Где хранятся данные", "Where your data lives");
        A("access.data.desc", "Настройки и история буфера хранятся только на этом компьютере. Grip не использует интернет, не собирает статистику и не требует аккаунта.",
            "Settings and clipboard history stay on this PC. Grip doesn't use the internet, collect statistics or need an account.");

        // Hotkeys
        A("hotkeys.recording", "Нажмите сочетание…", "Press a shortcut…");
        A("hotkeys.none", "Не задано", "Not set");
        A("hotkeys.conflict", "Занято другой программой", "Taken by another app");
        A("hotkeys.duplicate", "Уже используется: {0}", "Already used by {0}");
        A("hotkeys.invalid", "Добавьте Ctrl, Alt или Win", "Add Ctrl, Alt or Win");
        A("hotkeys.reset", "Вернуть стандартные", "Restore defaults");
        A("hotkeys.clear", "Убрать сочетание", "Remove shortcut");
        A("hotkeys.featureOff", "Функция не установлена", "Feature not installed");
        A("hotkeys.ok", "Работает", "Active");

        // Clipboard page
        A("settings.clipboard.history", "Сохранять историю буфера", "Keep clipboard history");
        A("settings.clipboard.history.desc", "Текст, ссылки, картинки и файлы, которые вы копируете.", "Text, links, images and files you copy.");
        A("settings.clipboard.maxItems", "Размер истории", "History size");
        A("settings.clipboard.maxItems.desc", "Закреплённые записи не считаются и не удаляются.", "Pinned items don't count and are never removed.");
        A("settings.clipboard.retention", "Хранить записи", "Keep items");
        A("settings.clipboard.retention.forever", "Пока есть место", "Until the limit");
        A("settings.clipboard.images", "Сохранять картинки", "Save images");
        A("settings.clipboard.files", "Сохранять скопированные файлы", "Save copied files");
        A("settings.clipboard.files.desc", "В истории сохраняются пути к файлам, а не сами файлы.", "History keeps file paths, not the files themselves.");
        A("settings.clipboard.keep", "Сохранять после перезапуска", "Keep after restart");
        A("settings.clipboard.keep.desc", "История лежит в папке Grip на этом компьютере.", "History is stored in the Grip folder on this PC.");
        A("settings.clipboard.pasteOnSelect", "Вставлять при выборе", "Paste when chosen");
        A("settings.clipboard.pasteOnSelect.desc", "Enter вставит запись в приложение, из которого вы открыли историю. Если выключено — только скопирует.",
            "Enter pastes into the app you opened history from. Off means it only copies.");
        A("settings.clipboard.position", "Где открывать историю", "Where history opens");
        A("settings.clipboard.position.cursor", "У курсора", "At the pointer");
        A("settings.clipboard.position.center", "По центру экрана", "Screen center");
        A("settings.clipboard.preview", "Панель предпросмотра", "Preview pane");
        A("settings.clipboard.ignored", "Не запоминать из приложений", "Ignore these apps");
        A("settings.clipboard.ignored.desc", "Всё, что скопировано в этих приложениях, в историю не попадёт. Метку «не сохранять в истории» от менеджеров паролей Grip соблюдает всегда.",
            "Whatever you copy in these apps stays out of history. Grip always honors the “don't keep in history” flag password managers set.");
        A("settings.clipboard.ignored.placeholder", "Например, Telegram.exe", "For example, Telegram.exe");
        A("settings.clipboard.autoClear", "Автоочистка буфера", "Auto-clear the clipboard");
        A("settings.clipboard.autoClear.desc", "Очищает системный буфер через заданное время. История при этом сохраняется.",
            "Clears the system clipboard after a delay. History is kept.");
        A("settings.clipboard.autoClear.after", "Очищать через", "Clear after");
        A("settings.clipboard.clearOnLock", "Очищать при блокировке", "Clear when the PC locks");
        A("settings.clipboard.clearOnSleep", "Очищать перед сном", "Clear before sleep");
        A("settings.clipboard.pastePlain.desc", "Вставляет только текст, а исходное содержимое буфера возвращает на место.",
            "Pastes only the text and puts the original clipboard back.");
        A("settings.clipboard.clearHistory", "Очистить историю", "Clear history");
        A("settings.clipboard.clearHistory.confirm", "Удалить всю историю, кроме закреплённого?", "Delete all history except pinned items?");
        A("settings.clipboard.stats", "В истории: {0}, закреплено: {1}", "In history: {0}, pinned: {1}");

        // Links page
        A("settings.links.auto", "Чистить ссылки автоматически", "Clean links automatically");
        A("settings.links.auto.desc", "Как только вы копируете ссылку, Grip убирает из неё метки слежки.", "The moment you copy a link, Grip strips its tracking tags.");
        A("settings.links.unwrap", "Раскрывать редиректы", "Unwrap redirects");
        A("settings.links.unwrap.desc", "Ссылки вида google.com/url?q=… и vk.com/away.php?to=… превращаются в настоящий адрес.",
            "Links like google.com/url?q=… and vk.com/away.php?to=… become the real address.");
        A("settings.links.search", "Упрощать ссылки на поиск Google", "Simplify Google search links");
        A("settings.links.search.desc", "От ссылки остаётся только сам запрос.", "Only the query itself remains.");
        A("settings.links.hud", "Показывать подсказку", "Show a hint");
        A("settings.links.hud.desc", "«Ссылка очищена» внизу экрана.", "“Link cleaned” at the bottom of the screen.");
        A("settings.links.extra", "Свои параметры", "Your own parameters");
        A("settings.links.extra.desc", "Имена параметров, которые тоже нужно удалять, через запятую.", "Parameter names to strip as well, comma separated.");
        A("settings.links.skip", "Не трогать домены", "Leave these domains alone");
        A("settings.links.skip.desc", "Через запятую, например: example.com", "Comma separated, for example: example.com");
        A("settings.links.try", "Проверить ссылку", "Try a link");
        A("settings.links.try.placeholder", "Вставьте ссылку", "Paste a link");
        A("settings.links.try.removed", "Удалено: {0}", "Removed: {0}");
        A("settings.links.try.unwrapped", "Раскрыт редирект", "Redirect unwrapped");
        A("settings.links.try.clean", "Ссылка уже чистая", "Already clean");
        A("settings.links.try.invalid", "Это не ссылка", "That's not a link");

        // Shelf page
        A("settings.shelf.shake", "Встряхнуть, чтобы открыть", "Shake to open");
        A("settings.shelf.shake.desc", "Начните перетаскивать файл и быстро поводите мышью влево-вправо — полка появится рядом.",
            "Start dragging a file and wiggle the mouse left and right; the shelf appears next to it.");
        A("settings.shelf.nearCursor", "Открывать у курсора", "Open at the pointer");
        A("settings.shelf.nearCursor.desc", "Иначе полка появится у правого края экрана.", "Otherwise it appears at the right edge of the screen.");
        A("settings.shelf.keep", "Оставлять предметы после перетаскивания", "Keep items after dragging them out");

        // Command Bar page
        A("settings.cb.sources", "Что искать", "What to search");
        A("settings.cb.apps", "Приложения", "Apps");
        A("settings.cb.windows", "Открытые окна", "Open windows");
        A("settings.cb.settings", "Параметры Windows", "Windows settings");
        A("settings.cb.clipboard", "История буфера", "Clipboard history");
        A("settings.cb.calc", "Калькулятор", "Calculator");
        A("settings.cb.units", "Конвертер единиц", "Unit converter");
        A("settings.cb.emoji", "Эмодзи", "Emoji");
        A("settings.cb.web", "Поиск в интернете", "Web search");
        A("settings.cb.engine", "Поисковик", "Search engine");
        A("settings.cb.layout", "Исправлять раскладку", "Fix the keyboard layout");
        A("settings.cb.layout.desc", "Запрос «ыуеештпы» найдёт «settings», а «ntktuhfv» — «телеграм».", "Typing in the wrong layout still finds what you meant.");
        A("settings.cb.scripts", "Скрипты", "Scripts");
        A("settings.cb.scripts.desc", "Команды, которые можно запустить из Command Bar по названию.", "Commands you can run from the Command Bar by name.");
        A("settings.cb.script.command", "Команда или путь", "Command or path");
        A("settings.cb.script.args", "Аргументы", "Arguments");
        A("settings.cb.script.add", "Добавить скрипт", "Add script");

        // Radial page
        A("settings.radial.trigger", "Кнопка мыши", "Mouse button");
        A("settings.radial.trigger.desc", "Удерживайте кнопку, наведите на пункт и отпустите.", "Hold the button, point at an item and let go.");
        A("settings.radial.trigger.None", "Нет", "None");
        A("settings.radial.trigger.Middle", "Средняя кнопка", "Middle button");
        A("settings.radial.trigger.XButton1", "Боковая «Назад»", "Side button (Back)");
        A("settings.radial.trigger.XButton2", "Боковая «Вперёд»", "Side button (Forward)");
        A("settings.radial.size", "Размер", "Size");
        A("settings.radial.size.Small", "Маленький", "Small");
        A("settings.radial.size.Medium", "Средний", "Medium");
        A("settings.radial.size.Large", "Большой", "Large");
        A("settings.radial.release", "Выбор отпусканием", "Release to select");
        A("settings.radial.release.desc", "Отпустите сочетание клавиш над пунктом — он сработает.", "Let go of the shortcut over an item to run it.");
        A("settings.radial.profile", "Профиль", "Profile");
        A("settings.radial.profile.add", "Новый профиль", "New profile");
        A("settings.radial.profile.delete", "Удалить профиль", "Delete profile");
        A("settings.radial.profile.newName", "Профиль {0}", "Profile {0}");
        A("settings.radial.items", "Пункты", "Items");
        A("settings.radial.items.desc", "От 2 до 12 пунктов по кругу. Первый — сверху, дальше по часовой стрелке.",
            "2 to 12 items around the ring. The first sits on top, the rest follow clockwise.");
        A("settings.radial.item.add", "Добавить пункт", "Add item");
        A("settings.radial.item.kind", "Тип", "Type");
        A("settings.radial.item.label", "Подпись", "Label");
        A("settings.radial.item.target", "Что запускать", "What it runs");
        A("settings.radial.item.children", "Пункты подменю", "Submenu items");
        A("settings.radial.preview", "Предпросмотр", "Preview");
        A("radial.kind.Action", "Действие", "Action");
        A("radial.kind.App", "Приложение", "App");
        A("radial.kind.File", "Файл", "File");
        A("radial.kind.Folder", "Папка", "Folder");
        A("radial.kind.Url", "Ссылка", "Link");
        A("radial.kind.Keys", "Сочетание клавиш", "Keys");
        A("radial.kind.Submenu", "Подменю", "Submenu");

        // Keep awake page
        A("settings.keepAwake.default", "Срок по умолчанию", "Default duration");
        A("settings.keepAwake.default.desc", "Сколько не спать, когда режим включают из трея или горячей клавишей.", "How long to stay awake when switched on from the tray or a shortcut.");
        A("settings.keepAwake.tray", "Показывать статус на значке", "Show status on the tray icon");
        A("settings.keepAwake.tray.desc", "Янтарная точка на значке, пока компьютер не спит.", "An amber dot on the icon while the PC stays awake.");
        A("settings.keepAwake.lid.unsupported", "У этого компьютера нет крышки.", "This PC has no lid.");
        A("settings.keepAwake.lid.failed", "Windows не дала изменить действие крышки. Попробуйте запустить Grip от имени администратора.",
            "Windows refused to change the lid action. Try running Grip as administrator.");

        // About
        A("about.version", "Версия {0}", "Version {0}");
        A("about.desc", "Мультитул для Windows 11: всё полезное — за одним значком в трее. Без аккаунтов, рекламы и слежки.",
            "A multitool for Windows 11: everything useful behind one tray icon. No accounts, ads or tracking.");
        A("about.repo", "Исходный код", "Source code");
        A("about.data", "Папка с данными", "Data folder");
        A("about.logs", "Журналы", "Logs");
        A("about.export", "Экспорт настроек…", "Export settings…");
        A("about.import", "Импорт настроек…", "Import settings…");
        A("about.transfer", "Перенос настроек", "Move settings");
        A("about.transfer.desc", "Сохраните настройки в файл и загрузите их на другом компьютере.", "Save your settings to a file and load them on another PC.");
        A("about.exported", "Настройки сохранены", "Settings saved");
        A("about.imported", "Настройки загружены", "Settings loaded");
        A("about.importFailed", "Не получилось прочитать файл: {0}", "Couldn't read the file: {0}");
        A("about.notices", "Сторонние компоненты", "Third-party notices");
        A("about.notices.text", "Иконки — Fluent UI System Icons © Microsoft, лицензия MIT. MVVM — CommunityToolkit.Mvvm © .NET Foundation, лицензия MIT.",
            "Icons: Fluent UI System Icons © Microsoft, MIT License. MVVM: CommunityToolkit.Mvvm © .NET Foundation, MIT License.");
        A("about.fileFilter", "Настройки Grip (*.json)|*.json", "Grip settings (*.json)|*.json");

        // Onboarding
        A("onboarding.title", "Grip на месте", "Grip is ready");
        A("onboarding.subtitle", "Значок Grip живёт в трее, в правом нижнем углу. Нажмите на него — откроется панель.",
            "The Grip icon lives in the tray at the bottom right. Click it to open the panel.");
        A("onboarding.presets", "С чего начнём?", "Where do we start?");
        A("onboarding.hotkeys", "Главные сочетания", "Key shortcuts");
        A("onboarding.pinHint", "Не видно значка? Он в скрытой области за стрелкой ^. Перетащите его на панель задач.",
            "Can't see the icon? It's in the overflow behind the ^ arrow. Drag it onto the taskbar.");
        A("onboarding.start", "Поехали", "Let's go");
    }
}
