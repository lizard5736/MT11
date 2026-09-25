namespace Grip.Core.Localization;

/// <summary>
/// Every UI string, Russian and English side by side. Plural entries hold
/// "|"-separated forms (three for Russian, two for English).
/// </summary>
public static partial class StringTable
{
    public static IReadOnlyDictionary<string, (string Ru, string En)> All { get; } = Build();

    private static Dictionary<string, (string Ru, string En)> Build()
    {
        var d = new Dictionary<string, (string Ru, string En)>(StringComparer.Ordinal);
        void A(string key, string ru, string en) => d.Add(key, (ru, en));

        AddCommon(A);
        AddPanel(A);
        AddFeatures(A);
        AddActions(A);
        AddSettings(A);
        AddTools(A);
        AddMonitor(A);
        AddScratchpad(A);
        return d;
    }

    private static void AddCommon(Action<string, string, string> A)
    {
        A("app.name", "Grip", "Grip");
        A("app.tagline", "Мультитул для Windows 11", "A multitool for Windows 11");

        A("common.on", "Вкл.", "On");
        A("common.off", "Выкл.", "Off");
        A("common.install", "Установить", "Install");
        A("common.uninstall", "Удалить", "Uninstall");
        A("common.apply", "Применить", "Apply");
        A("common.cancel", "Отмена", "Cancel");
        A("common.ok", "OK", "OK");
        A("common.save", "Сохранить", "Save");
        A("common.close", "Закрыть", "Close");
        A("common.open", "Открыть", "Open");
        A("common.delete", "Удалить", "Delete");
        A("common.clear", "Очистить", "Clear");
        A("common.reset", "Сбросить", "Reset");
        A("common.add", "Добавить", "Add");
        A("common.remove", "Убрать", "Remove");
        A("common.edit", "Изменить", "Edit");
        A("common.copy", "Копировать", "Copy");
        A("common.copied", "Скопировано", "Copied");
        A("common.paste", "Вставить", "Paste");
        A("common.search", "Поиск", "Search");
        A("common.settings", "Настройки", "Settings");
        A("common.quit", "Выход", "Quit");
        A("common.back", "Назад", "Back");
        A("common.done", "Готово", "Done");
        A("common.yes", "Да", "Yes");
        A("common.no", "Нет", "No");
        A("common.soon", "Скоро", "Soon");
        A("common.beta", "Бета", "Beta");
        A("common.browse", "Обзор…", "Browse…");
        A("common.moveUp", "Выше", "Move up");
        A("common.moveDown", "Ниже", "Move down");
        A("common.show", "Показывать", "Show");
        A("common.none", "Нет", "None");
        A("common.default", "По умолчанию", "Default");
        A("common.openFolder", "Открыть папку", "Open folder");
        A("common.confirm", "Подтверждение", "Confirm");
        A("common.name", "Название", "Name");

        A("time.d", "д", "d");
        A("time.h", "ч", "h");
        A("time.min", "мин", "min");
        A("time.s", "с", "s");
        A("time.justNow", "только что", "just now");
        A("time.minutesAgo", "{0} мин назад", "{0} min ago");
        A("time.hoursAgo", "{0} ч назад", "{0} h ago");
        A("time.yesterday", "вчера", "yesterday");
        A("time.until", "до {0}", "until {0}");
        A("time.minutes", "минута|минуты|минут", "minute|minutes");
        A("time.hours", "час|часа|часов", "hour|hours");
        A("time.days", "день|дня|дней", "day|days");
        A("time.seconds", "секунда|секунды|секунд", "second|seconds");

        A("hud.copied", "Скопировано", "Copied");
        A("error.title", "Что-то пошло не так", "Something went wrong");
        A("error.desc", "Grip поймал ошибку и продолжает работать. Подробности сохранены в журнале.",
            "Grip caught an error and keeps running. The details are in the log.");
        A("error.copy", "Скопировать подробности", "Copy details");
        A("error.hotkeys", "Не удалось назначить сочетания: {0}. Их заняла другая программа — поменяйте их в настройках.",
            "Couldn't register {0}: another app took them. Change them in Settings.");
    }

    private static void AddPanel(Action<string, string, string> A)
    {
        A("tray.tooltip", "Grip", "Grip");
        A("tray.tooltip.awake", "Grip · не спит", "Grip · awake");
        A("tray.tooltip.readout", "{0} · CPU {1}%", "{0} · CPU {1}%");
        A("tray.menu.panel", "Открыть панель", "Open panel");
        A("tray.menu.settings", "Настройки…", "Settings…");
        A("tray.menu.quit", "Выйти из Grip", "Quit Grip");

        A("panel.settings", "Настройки", "Settings");
        A("panel.layout.tabs", "Вкладки", "Tabs");
        A("panel.layout.list", "Список", "List");
        A("panel.quit", "Выход", "Quit");
        A("panel.status.ready", "Всё спокойно", "All quiet");
        A("panel.status.awake", "Не спит", "Awake");
        A("panel.status.awakeUntil", "Не спит · до {0}", "Awake · until {0}");
        A("panel.empty", "Все разделы скрыты. Включите нужные в настройках.", "Every section is hidden. Turn some back on in Settings.");
        A("panel.section.keepAwake", "Не спать", "Keep awake");
        A("panel.section.clipboard", "Буфер обмена", "Clipboard");
        A("panel.section.utilities", "Утилиты", "Utilities");
        A("panel.section.controls", "Управление", "Controls");
        A("panel.section.toggles", "Переключатели", "Quick toggles");
        A("panel.section.monitor", "Мониторинг", "Monitoring");

        A("keepAwake.title", "Не давать уснуть", "Keep awake");
        A("keepAwake.off", "Компьютер засыпает как обычно", "Your PC sleeps as usual");
        A("keepAwake.indefinite", "Активно, пока не выключите", "On until you turn it off");
        A("keepAwake.remaining", "Ещё {0} · до {1}", "{0} left · until {1}");
        A("keepAwake.preset.infinite", "Без срока", "No limit");
        A("keepAwake.preset.until", "До…", "Until…");
        A("keepAwake.display", "Не гасить экран", "Keep the display on");
        A("keepAwake.display.desc", "Экран не погаснет, пока режим активен", "The screen stays on while this is active");
        A("keepAwake.lid", "Работать с закрытой крышкой", "Keep working with the lid closed");
        A("keepAwake.lid.desc", "Закрытая крышка не усыпит ноутбук, пока режим активен. Следите за вентиляцией.",
            "Closing the lid won't put the laptop to sleep while this is active. Mind the airflow.");
        A("keepAwake.until.title", "Не спать до", "Stay awake until");
        A("keepAwake.until.tomorrow", "завтра", "tomorrow");
        A("keepAwake.hud.on", "Не спит: {0}", "Awake: {0}");
        A("keepAwake.hud.onIndefinite", "Не спит, пока не выключите", "Awake until you turn it off");
        A("keepAwake.hud.off", "Компьютер снова может спать", "Sleep is back on");

        A("utilities.commandBar.desc", "Приложения, окна, расчёты и эмодзи в одном поле", "Apps, windows, math and emoji in one field");
        A("utilities.clipboard.desc", "Всё скопированное: поиск, закрепы, картинки и файлы", "Everything you copied: search, pins, images and files");
        A("utilities.radial.desc", "Колесо любимых действий вокруг курсора", "A wheel of favorite actions around the pointer");
        A("utilities.shelf.desc", "Временное место для файлов, текста и ссылок", "A parking spot for files, text and links");
        A("utilities.cleanUrl.desc", "Убрать трекеры из ссылки в буфере", "Strip trackers from the link on the clipboard");
        A("utilities.pastePlain.desc", "Вставить чистый текст без оформления", "Paste bare text without its styling");

        A("controls.group.clipboard", "Буфер и ссылки", "Clipboard and links");
        A("controls.group.tools", "Инструменты", "Tools");
        A("controls.clipboardHistory.desc", "Запоминать всё, что вы копируете", "Remember everything you copy");
        A("controls.autoClear.title", "Автоочистка буфера", "Auto-clear clipboard");
        A("controls.autoClear.desc", "Очищать буфер через {0}", "Clear the clipboard after {0}");
        A("controls.autoClean.title", "Автоочистка ссылок", "Auto-clean links");
        A("controls.autoClean.desc", "Убирать трекеры при копировании", "Strip trackers as you copy");
        A("controls.shake.title", "Полка по встряхиванию", "Shake for the shelf");
        A("controls.shake.desc", "Поводите файлом влево-вправо при перетаскивании", "Wiggle a file left and right while dragging");
        A("controls.radialMouse.title", "Радиальное меню на мыши", "Radial menu on the mouse");
        A("controls.radialMouse.desc", "Открывать удержанием кнопки: {0}", "Open by holding: {0}");
        A("controls.count", "{0}/{1}", "{0}/{1}");
    }

    private static void AddFeatures(Action<string, string, string> A)
    {
        A("group.ClipboardFiles", "Буфер и файлы", "Clipboard and files");
        A("group.Tools", "Инструменты", "Tools");
        A("group.EnergyDisplay", "Энергия и экраны", "Energy and displays");
        A("group.Sound", "Звук", "Sound");
        A("group.Monitor", "Мониторинг", "Monitoring");
        A("group.Windows", "Окна", "Windows");
        A("group.Input", "Клавиатура и мышь", "Keyboard and mouse");
        A("group.Capture", "Захват и медиа", "Capture and media");
        A("group.Apps", "Приложения", "Apps");

        A("energy.Idle", "В покое ничего не делает", "Nothing at rest");
        A("energy.Events", "Реагирует на события системы", "Reacts to system events");
        A("energy.Keyboard", "Слушает клавиатуру", "Listens to the keyboard");
        A("energy.Mouse", "Слушает мышь", "Listens to the mouse");
        A("energy.Inputs", "Слушает клавиатуру и мышь", "Listens to keyboard and mouse");
        A("energy.Periodic", "Опрашивает по таймеру", "Samples on a timer");

        A("preset.essentials.title", "Основное", "Essentials");
        A("preset.essentials.desc", "История буфера, чистые ссылки, Command Bar, переключатели и «Не спать».",
            "Clipboard history, clean links, Command Bar, quick toggles and keep awake.");
        A("preset.clipboard.title", "Буфер и файлы", "Clipboard and files");
        A("preset.clipboard.desc", "Всё для копирования и перетаскивания: история, ссылки и полка.",
            "Everything for copying and dragging: history, links and the shelf.");
        A("preset.quiet.title", "Тихий режим", "Quiet mode");
        A("preset.quiet.desc", "Только инструменты по запросу. Ничто не слушает клавиатуру, мышь и буфер.",
            "On-demand tools only. Nothing listens to the keyboard, mouse or clipboard.");

        void F(string id, string ru, string en, string ruDesc, string enDesc)
        {
            A($"feature.{id}.title", ru, en);
            A($"feature.{id}.desc", ruDesc, enDesc);
        }

        F("clipboardHistory", "История буфера", "Clipboard history",
            "Поиск по скопированному тексту, картинкам и файлам, закрепы и вставка по горячей клавише.",
            "Search copied text, images and files, pin favorites and paste with a shortcut.");
        F("pastePlain", "Вставка без форматирования", "Paste as plain text",
            "Вставляет чистый текст и возвращает исходное содержимое буфера на место.",
            "Pastes bare text and puts the original clipboard back afterwards.");
        F("urlCleaner", "Чистые ссылки", "Clean links",
            "Убирает из ссылок метки слежки — вручную или сразу при копировании.",
            "Strips tracking tags from links, on demand or as you copy.");
        F("shelf", "Полка", "Shelf",
            "Место у курсора, куда можно на время положить файлы, текст и ссылки при перетаскивании.",
            "A spot near the pointer to park files, text and links while you drag.");
        F("imageToFile", "Картинка → файл", "Image → file",
            "Вставляет скопированную картинку в папку Проводника как PNG-файл.",
            "Pastes a copied image into an Explorer folder as a PNG file.");
        F("commandBar", "Command Bar", "Command Bar",
            "Одно поле для приложений, окон, параметров Windows, расчётов, единиц и эмодзи.",
            "One field for apps, windows, Windows settings, math, units and emoji.");
        F("radialMenu", "Радиальное меню", "Radial menu",
            "Колесо приложений, файлов и действий вокруг курсора, с профилями и подменю.",
            "A wheel of apps, files and actions around the pointer, with profiles and submenus.");
        F("quickToggles", "Быстрые переключатели", "Quick toggles",
            "Тёмная тема, значки рабочего стола, скрытые файлы, корзина, блокировка и другое в один клик.",
            "Dark mode, desktop icons, hidden files, Recycle Bin, lock and more in one click.");
        F("quickPanel", "Быстрая панель", "Quick panel",
            "Плавающая палитра любимых инструментов по горячей клавише.",
            "A floating palette of favorite tools on a shortcut.");
        F("scratchpad", "Блокнот", "Scratchpad",
            "Заметки во вкладках с автосохранением и предпросмотром Markdown, прямо из трея.",
            "Autosaved notes in tabs with a Markdown preview, right from the tray.");
        F("cleaningMode", "Режим уборки", "Cleaning mode",
            "Блокирует клавиатуру, пока вы её протираете.",
            "Locks the keyboard while you wipe it.");
        F("keepAwake", "Не спать", "Keep awake",
            "Не даёт компьютеру уснуть: по таймеру, до нужного времени или пока не выключите.",
            "Keeps your PC awake on a timer, until a set time or until you turn it off.");
        F("brightness", "Яркость мониторов", "Display brightness",
            "Яркость каждого экрана: встроенного — штатно, внешних — по DDC/CI.",
            "Brightness per screen: built-in natively, external over DDC/CI.");
        F("bluetoothSleep", "Bluetooth во сне", "Bluetooth on sleep",
            "Отключает Bluetooth на время сна и включает обратно после пробуждения.",
            "Turns Bluetooth off during sleep and back on after wake.");
        F("mixer", "Микшер громкости", "Volume mixer",
            "Громкость каждого приложения отдельно, прямо в панели.",
            "Per-app volume right in the panel.");
        F("appOutput", "Выход по приложениям", "Per-app output",
            "Своё устройство вывода звука для каждого приложения.",
            "Pick an audio output for each app.");
        F("outputSwitcher", "Переключатель выходов", "Output switcher",
            "Смена аудиовыхода горячей клавишей и тише при отключении наушников.",
            "Switch outputs with a shortcut and lower the volume when headphones unplug.");
        F("micTools", "Микрофон", "Microphone",
            "Выбор микрофона, уровень и выключение всех микрофонов одной клавишей.",
            "Choose the mic, set its level and mute every mic with one key.");
        F("monitorCpu", "Процессор", "Processor",
            "Загрузка процессора с историей.", "Processor load with history.");
        F("monitorGpu", "Видеокарта", "Graphics card",
            "Загрузка, топ-3 процесса, температура и память. Температура и память — пока только для NVIDIA (через NVML); для AMD и Intel ещё не сделаны.",
            "Load, top 3 processes, temperature and memory. Temperature and memory are NVIDIA-only for now (via NVML); AMD and Intel aren't done yet.");
        F("monitorMemory", "Память", "Memory",
            "Занятая память и приложения, которые её едят.", "Memory in use and the apps eating it.");
        F("monitorDisk", "Диски", "Disks",
            "Свободное место на дисках. Скорость чтения и записи — в планах.", "Free space on your drives. Read/write speed is still planned.");
        F("monitorNetwork", "Сеть", "Network",
            "Скорость, трафик за сессию и локальный IP. Тест скорости — в планах.", "Speed, session traffic and local IP. A speed test is still planned.");
        F("monitorBattery", "Батарея", "Battery",
            "Заряд и состояние питания. Здоровье и циклы — в планах.", "Charge and power state. Health and cycle count are still planned.");
        F("cpuTemperature", "Температура CPU", "CPU temperature",
            "Датчики процессора через драйвер PawnIO.", "Processor sensors through the PawnIO driver.");
        F("trayReadouts", "Показатели в трее", "Tray readouts",
            "Загрузка процессора во всплывающей подсказке над значком — наведи курсор, без клика.",
            "CPU load in the tray icon's hover tooltip — no click needed.");
        F("monitorAlerts", "Предупреждения", "Alerts",
            "Всплывающее уведомление при высокой нагрузке на процессор, видеокарту или память, разряженной батарее и нехватке места на диске.",
            "A pop-up notice for high CPU, GPU or memory load, low battery and low disk space.");
        F("windowSwitcher", "Переключатель окон", "Window switcher",
            "Окна с живыми превью, поиском и фильтром по мониторам.", "Windows with live previews, search and a per-monitor filter.");
        F("windowLayout", "Раскладка окон", "Window layout",
            "Половины, трети, углы и во весь экран — горячими клавишами. Перенос между мониторами уже есть в Windows (Win+Shift+стрелки).",
            "Halves, thirds, corners and full screen via shortcuts. Moving between monitors is already built into Windows (Win+Shift+arrows).");
        F("closeProtection", "Защита от закрытия", "Close protection",
            "Защищает от случайного Alt+F4, Ctrl+Q и Ctrl+W: удержание или двойное нажатие.",
            "Guards against a stray Alt+F4, Ctrl+Q or Ctrl+W with a hold or double press.");
        F("quitOnClose", "Выход при закрытии", "Quit on close",
            "Полностью закрывает выбранные программы, которые прячутся в трей.", "Fully quits chosen apps that hide in the tray.");
        F("textSnippets", "Текстовые сниппеты", "Text snippets",
            "Сокращения разворачиваются в текст с датой, временем и буфером.", "Short triggers expand into text with date, time and clipboard.");
        F("smoothScroll", "Плавная прокрутка", "Smooth scrolling",
            "Мягкая прокрутка колесом мыши с настройкой скорости.", "A fluid mouse wheel with adjustable speed.");
        F("pointerAcceleration", "Ускорение указателя", "Pointer acceleration",
            "Отключение «повышенной точности» одним переключателем.", "Turn off “enhance pointer precision” with one switch.");
        F("focusFollowsMouse", "Фокус за мышью", "Focus follows mouse",
            "Окно под курсором становится активным после паузы.", "The window under the pointer comes forward after a pause.");
        F("scrollDirection", "Направление прокрутки", "Scroll direction",
            "Инверсия колеса мыши отдельно от тачпада.", "Invert the mouse wheel independently of the touchpad.");
        F("horizontalScroll", "Горизонтальная прокрутка", "Sideways scrolling",
            "Колесо прокручивает вбок, пока зажата клавиша.", "The wheel scrolls sideways while you hold a key.");
        F("mouseButtons", "Кнопки мыши", "Mouse buttons",
            "Клавиши и жесты на дополнительные кнопки мыши.", "Keys and gestures on extra mouse buttons.");
        F("clickFilter", "Фильтр двойных кликов", "Double-click filter",
            "Отсекает ложные двойные клики изношенной мыши.", "Drops phantom double clicks from a worn mouse.");
        F("keyDebounce", "Антидребезг клавиатуры", "Key debounce",
            "Убирает повторы букв на изношенной клавиатуре.", "Filters repeated letters from a worn keyboard.");
        F("superKey", "Super key", "Super key",
            "Caps Lock превращается в модификатор для своих сочетаний.", "Caps Lock becomes a modifier for your own shortcuts.");
        F("screenshot", "Скриншоты", "Screenshots",
            "Область, окно или экран; аннотации, обрезка, замазывание и закрепление поверх окон.",
            "Area, window or screen; annotate, crop, redact and pin above other windows.");
        F("scrollingCapture", "Длинный скриншот", "Scrolling capture",
            "Снимок страницы целиком, с прокруткой.", "Capture a whole page by scrolling it.");
        F("screenRecorder", "Запись экрана", "Screen recording",
            "Раздельные дорожки системы и микрофона, обрезка, зум и экспорт в видео или GIF.",
            "Separate system and mic tracks, trimming, zooms and export to video or GIF.");
        F("screenOcr", "Текст с экрана", "Text from screen",
            "Распознавание текста и QR-кодов без интернета.", "Offline text and QR code recognition.");
        F("colorPicker", "Пипетка", "Color picker",
            "Цвет с экрана в HEX, RGB, HSL и CSS.", "A screen color as HEX, RGB, HSL or CSS.");
        F("cameraPreview", "Зеркало камеры", "Camera mirror",
            "Проверка камеры в плавающем окне перед звонком.", "Check your camera in a floating window before a call.");
        F("mediaTools", "Медиаинструменты", "Media tools",
            "Сжатие видео, конвертация картинок, водяные знаки и GIF — всё локально.",
            "Compress video, convert images, add watermarks and make GIFs, all locally.");
        F("winget", "Менеджер winget", "winget manager",
            "Поиск, установка и удаление программ без терминала.", "Search, install and remove apps without a terminal.");
        F("appUpdates", "Обновления программ", "App updates",
            "Обновления winget и Microsoft Store одним списком.", "winget and Microsoft Store updates in one list.");
        F("cleaner", "Очистка", "Cleaner",
            "Кэш, журналы и временные файлы — вручную или по расписанию.", "Caches, logs and temp files, manually or on a schedule.");
        F("messengerDownloads", "Загрузки мессенджеров", "Messenger downloads",
            "Правила хранения для файлов из Telegram и WhatsApp.", "Retention rules for Telegram and WhatsApp files.");
        F("uninstaller", "Деинсталлятор", "Uninstaller",
            "Удаление программ вместе с остатками в AppData и реестре.", "Remove apps along with their leftovers in AppData and the registry.");
        F("portManager", "Порты", "Ports",
            "Кто занимает сетевой порт, и завершение процесса.", "See who holds a network port and stop it.");
    }

    private static void AddActions(Action<string, string, string> A)
    {
        A("action.panel", "Открыть панель Grip", "Open the Grip panel");
        A("action.settings", "Открыть настройки", "Open settings");
        A("action.commandBar", "Command Bar", "Command Bar");
        A("action.clipboard", "История буфера", "Clipboard history");
        A("action.pastePlain", "Вставить без форматирования", "Paste as plain text");
        A("action.cleanUrl", "Очистить ссылку в буфере", "Clean the link on the clipboard");
        A("action.clearClipboard", "Очистить буфер обмена", "Clear the clipboard");
        A("action.radial", "Радиальное меню", "Radial menu");
        A("action.shelf", "Полка", "Shelf");
        A("action.scratchpad", "Блокнот", "Scratchpad");
        A("action.keepAwake", "Не спать: вкл./выкл.", "Keep awake on/off");
        A("action.sys.lock", "Заблокировать компьютер", "Lock the PC");
        A("action.sys.displayOff", "Погасить экран", "Turn the screen off");
        A("action.sys.sleep", "Перевести в сон", "Sleep");
        A("action.sys.emptyRecycleBin", "Очистить корзину", "Empty the Recycle Bin");
        A("action.sys.darkMode", "Тёмная тема: вкл./выкл.", "Toggle dark mode");
        A("action.sys.desktopIcons", "Значки рабочего стола: показать/скрыть", "Show or hide desktop icons");
        A("action.sys.hiddenFiles", "Скрытые файлы: показать/скрыть", "Show or hide hidden files");
        A("action.sys.fileExtensions", "Расширения файлов: показать/скрыть", "Show or hide file extensions");
        A("action.sys.ejectDrives", "Извлечь съёмные диски", "Eject removable drives");
        A("action.media.playPause", "Воспроизведение / пауза", "Play / pause");
        A("action.media.next", "Следующий трек", "Next track");
        A("action.media.previous", "Предыдущий трек", "Previous track");
        A("action.media.volumeUp", "Громче", "Volume up");
        A("action.media.volumeDown", "Тише", "Volume down");
        A("action.media.mute", "Без звука", "Mute");
        A("action.win.snapLeft", "Прижать к левому краю", "Snap left");
        A("action.win.snapRight", "Прижать к правому краю", "Snap right");
        A("action.win.snapMaximize", "Развернуть на весь экран", "Maximize");
        A("action.win.snapRestore", "Вернуть как было", "Restore previous size");
        A("action.win.snapTopLeft", "В левый верхний угол", "Snap to top-left");
        A("action.win.snapTopRight", "В правый верхний угол", "Snap to top-right");
        A("action.win.snapBottomLeft", "В левый нижний угол", "Snap to bottom-left");
        A("action.win.snapBottomRight", "В правый нижний угол", "Snap to bottom-right");
        A("action.win.snapLeftThird", "В левую треть", "Snap to left third");
        A("action.win.snapCenterThird", "В среднюю треть", "Snap to center third");
        A("action.win.snapRightThird", "В правую треть", "Snap to right third");
        A("action.win.snapLeftTwoThirds", "На левые две трети", "Snap to left two-thirds");
        A("action.win.snapRightTwoThirds", "На правые две трети", "Snap to right two-thirds");
        A("action.win.snapTopLeftSixth", "В верхнюю левую шестую", "Snap to top-left sixth");
        A("action.win.snapTopCenterSixth", "В верхнюю среднюю шестую", "Snap to top-center sixth");
        A("action.win.snapTopRightSixth", "В верхнюю правую шестую", "Snap to top-right sixth");
        A("action.win.snapBottomLeftSixth", "В нижнюю левую шестую", "Snap to bottom-left sixth");
        A("action.win.snapBottomCenterSixth", "В нижнюю среднюю шестую", "Snap to bottom-center sixth");
        A("action.win.snapBottomRightSixth", "В нижнюю правую шестую", "Snap to bottom-right sixth");

        A("action.category.Grip", "Grip", "Grip");
        A("action.category.System", "Система", "System");
        A("action.category.Media", "Медиа", "Media");
        A("action.category.Window", "Окна", "Windows");
    }
}
