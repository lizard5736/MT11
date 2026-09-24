namespace Grip.Core.Search;

public sealed record WindowsSettingEntry(string Target, string NameRu, string NameEn, string[] Keywords, string Icon)
{
    public string Name(Localization.UiLanguage language) => language == Localization.UiLanguage.Ru ? NameRu : NameEn;

    /// <summary>ms-settings: pages open through the shell; the rest are programs or control panels.</summary>
    public bool IsSettingsUri => Target.StartsWith("ms-settings:", StringComparison.OrdinalIgnoreCase);
}

/// <summary>Windows Settings pages and classic system tools, searchable in both languages.</summary>
public static class WindowsSettingsCatalog
{
    public static IReadOnlyList<WindowsSettingEntry> All { get; } = new List<WindowsSettingEntry>
    {
        new("ms-settings:display", "Дисплей", "Display", new[] { "экран", "разрешение", "масштаб", "screen", "resolution", "scale", "hdr" }, "Desktop"),
        new("ms-settings:nightlight", "Ночной свет", "Night light", new[] { "ночной", "night", "blue light", "синий свет" }, "WeatherMoon"),
        new("ms-settings:sound", "Звук", "Sound", new[] { "громкость", "динамики", "микрофон", "audio", "volume", "speakers", "microphone" }, "Speaker"),
        new("ms-settings:apps-volume", "Микшер громкости", "Volume mixer", new[] { "микшер", "mixer", "app volume" }, "Options"),
        new("ms-settings:notifications", "Уведомления", "Notifications", new[] { "уведомления", "не беспокоить", "do not disturb", "focus" }, "Alert"),
        new("ms-settings:powersleep", "Питание и сон", "Power & sleep", new[] { "сон", "питание", "батарея", "power", "sleep", "battery", "энергия" }, "Flash"),
        new("ms-settings:batterysaver", "Батарея", "Battery", new[] { "батарея", "экономия", "battery saver" }, "Battery"),
        new("ms-settings:storagesense", "Память устройства", "Storage", new[] { "диск", "место", "хранилище", "storage", "disk", "space", "контроль памяти" }, "HardDrive"),
        new("ms-settings:multitasking", "Многозадачность", "Multitasking", new[] { "snap", "окна", "привязка", "windows", "alt tab" }, "WindowMultiple"),
        new("ms-settings:clipboard", "Буфер обмена Windows", "Windows clipboard", new[] { "буфер", "clipboard", "win v" }, "Clipboard"),
        new("ms-settings:about", "О системе", "About this PC", new[] { "система", "имя компьютера", "характеристики", "system", "specs", "pc name" }, "Info"),
        new("ms-settings:bluetooth", "Bluetooth и устройства", "Bluetooth & devices", new[] { "блютуз", "устройства", "наушники", "devices", "headphones" }, "Bluetooth"),
        new("ms-settings:printers", "Принтеры и сканеры", "Printers & scanners", new[] { "принтер", "сканер", "printer", "scanner" }, "Document"),
        new("ms-settings:mousetouchpad", "Мышь", "Mouse", new[] { "мышь", "курсор", "прокрутка", "mouse", "cursor", "scroll" }, "Cursor"),
        new("ms-settings:devices-touchpad", "Сенсорная панель", "Touchpad", new[] { "тачпад", "touchpad", "жесты", "gestures" }, "Cursor"),
        new("ms-settings:typing", "Ввод текста", "Typing", new[] { "клавиатура", "автоисправление", "keyboard", "autocorrect" }, "Keyboard"),
        new("ms-settings:autoplay", "Автозапуск носителей", "AutoPlay", new[] { "автозапуск", "флешка", "autoplay", "usb" }, "Play"),
        new("ms-settings:network-status", "Сеть и интернет", "Network & internet", new[] { "интернет", "сеть", "network", "internet", "ethernet" }, "Globe"),
        new("ms-settings:network-wifi", "Wi-Fi", "Wi-Fi", new[] { "вайфай", "беспроводная", "wifi", "wireless" }, "Globe"),
        new("ms-settings:network-vpn", "VPN", "VPN", new[] { "впн", "vpn" }, "Shield"),
        new("ms-settings:network-proxy", "Прокси", "Proxy", new[] { "прокси", "proxy" }, "Globe"),
        new("ms-settings:network-airplanemode", "Режим «в самолёте»", "Airplane mode", new[] { "самолёт", "airplane", "flight" }, "Globe"),
        new("ms-settings:personalization", "Персонализация", "Personalization", new[] { "оформление", "обои", "personalize" }, "DarkTheme"),
        new("ms-settings:personalization-background", "Фон рабочего стола", "Background", new[] { "обои", "фон", "wallpaper", "background" }, "Image"),
        new("ms-settings:colors", "Цвета", "Colors", new[] { "тёмная тема", "светлая", "акцент", "dark mode", "theme", "accent" }, "DarkTheme"),
        new("ms-settings:themes", "Темы", "Themes", new[] { "тема", "theme" }, "DarkTheme"),
        new("ms-settings:lockscreen", "Экран блокировки", "Lock screen", new[] { "блокировка", "lock screen" }, "LockClosed"),
        new("ms-settings:taskbar", "Панель задач", "Taskbar", new[] { "панель задач", "трей", "значки", "taskbar", "tray", "icons" }, "Board"),
        new("ms-settings:fonts", "Шрифты", "Fonts", new[] { "шрифт", "font" }, "TextT"),
        new("ms-settings:appsfeatures", "Установленные приложения", "Installed apps", new[] { "программы", "удалить", "приложения", "apps", "uninstall", "programs" }, "Apps"),
        new("ms-settings:defaultapps", "Приложения по умолчанию", "Default apps", new[] { "по умолчанию", "браузер", "default", "browser" }, "Apps"),
        new("ms-settings:startupapps", "Автозагрузка", "Startup apps", new[] { "автозагрузка", "автозапуск", "startup" }, "Flash"),
        new("ms-settings:yourinfo", "Учётная запись", "Your account", new[] { "аккаунт", "профиль", "account", "profile" }, "Person"),
        new("ms-settings:signinoptions", "Варианты входа", "Sign-in options", new[] { "пароль", "пин", "вход", "password", "pin", "hello" }, "LockClosed"),
        new("ms-settings:dateandtime", "Дата и время", "Date & time", new[] { "время", "дата", "часовой пояс", "time", "date", "timezone" }, "Timer"),
        new("ms-settings:regionlanguage", "Язык и регион", "Language & region", new[] { "язык", "регион", "раскладка", "language", "region", "keyboard layout" }, "Globe"),
        new("ms-settings:speech", "Речь", "Speech", new[] { "голос", "распознавание", "speech", "voice" }, "Mic"),
        new("ms-settings:gaming-gamebar", "Game Bar", "Game Bar", new[] { "игры", "запись", "gaming", "xbox" }, "Record"),
        new("ms-settings:easeofaccess", "Специальные возможности", "Accessibility", new[] { "доступность", "accessibility", "экранная лупа", "magnifier" }, "Eye"),
        new("ms-settings:privacy", "Конфиденциальность", "Privacy & security", new[] { "приватность", "безопасность", "privacy", "security" }, "Shield"),
        new("ms-settings:privacy-microphone", "Доступ к микрофону", "Microphone access", new[] { "микрофон", "microphone" }, "Mic"),
        new("ms-settings:privacy-webcam", "Доступ к камере", "Camera access", new[] { "камера", "вебкамера", "camera", "webcam" }, "Camera"),
        new("ms-settings:windowsupdate", "Центр обновления Windows", "Windows Update", new[] { "обновление", "апдейт", "update", "upgrade" }, "ArrowSync"),
        new("ms-settings:windowsdefender", "Безопасность Windows", "Windows Security", new[] { "антивирус", "защитник", "defender", "antivirus", "virus" }, "Shield"),
        new("ms-settings:recovery", "Восстановление", "Recovery", new[] { "сброс", "восстановление", "reset", "recovery" }, "ArrowReset"),
        new("ms-settings:backup", "Резервное копирование", "Backup", new[] { "бэкап", "копия", "backup" }, "Archive"),
        new("ms-settings:developers", "Для разработчиков", "For developers", new[] { "разработчик", "developer", "dev mode" }, "WindowConsole"),

        new("taskmgr.exe", "Диспетчер задач", "Task Manager", new[] { "процессы", "задачи", "tasks", "processes", "task manager" }, "DataPie"),
        new("devmgmt.msc", "Диспетчер устройств", "Device Manager", new[] { "драйверы", "устройства", "drivers", "devices" }, "DeveloperBoard"),
        new("diskmgmt.msc", "Управление дисками", "Disk Management", new[] { "разделы", "диски", "partitions", "disks" }, "HardDrive"),
        new("control.exe", "Панель управления", "Control Panel", new[] { "панель", "control" }, "Settings"),
        new("regedit.exe", "Редактор реестра", "Registry Editor", new[] { "реестр", "registry", "regedit" }, "WindowConsole"),
        new("services.msc", "Службы", "Services", new[] { "сервисы", "службы", "services" }, "Settings"),
        new("eventvwr.msc", "Просмотр событий", "Event Viewer", new[] { "журнал", "события", "events", "logs" }, "Document"),
        new("resmon.exe", "Монитор ресурсов", "Resource Monitor", new[] { "ресурсы", "resources", "monitor" }, "DataPie"),
        new("mmsys.cpl", "Звук (классический)", "Sound (classic)", new[] { "звук", "устройства воспроизведения", "playback devices", "recording" }, "Speaker"),
        new("ncpa.cpl", "Сетевые подключения", "Network connections", new[] { "адаптер", "сетевые", "adapter", "connections" }, "Globe"),
        new("sysdm.cpl", "Свойства системы", "System properties", new[] { "переменные среды", "environment variables", "system properties" }, "Info"),
        new("powercfg.cpl", "Электропитание", "Power options", new[] { "схема питания", "power plan" }, "Flash"),
        new("cmd.exe", "Командная строка", "Command Prompt", new[] { "cmd", "консоль", "console" }, "WindowConsole"),
        new("wt.exe", "Терминал", "Terminal", new[] { "терминал", "powershell", "terminal" }, "WindowConsole"),
        new("snippingtool.exe", "Ножницы", "Snipping Tool", new[] { "скриншот", "снимок", "screenshot", "snip" }, "Screenshot"),
        new("calc.exe", "Калькулятор Windows", "Windows Calculator", new[] { "калькулятор", "calculator" }, "Calculator"),
        new("notepad.exe", "Блокнот", "Notepad", new[] { "блокнот", "текст", "notepad", "text" }, "Edit"),
        new("mspaint.exe", "Paint", "Paint", new[] { "рисовать", "paint", "draw" }, "Color"),
        new("explorer.exe", "Проводник", "File Explorer", new[] { "файлы", "папки", "files", "folders", "explorer" }, "Folder"),
    };
}
