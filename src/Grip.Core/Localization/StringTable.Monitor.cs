namespace Grip.Core.Localization;

public static partial class StringTable
{
    private static void AddMonitor(Action<string, string, string> A)
    {
        A("monitor.disk.free", "{0} свободно", "{0} free");
        A("monitor.network.session", "За сессию: ↓{0} ↑{1}", "This session: ↓{0} ↑{1}");
        A("monitor.battery.charging", "Заряжается", "Charging");
        A("monitor.battery.onBattery", "От батареи", "On battery");
        A("monitor.battery.none", "Аккумулятор не найден", "No battery found");
        A("monitor.gpu.unavailable", "Нет данных на этой машине", "No data on this machine");
        A("monitor.process.kill", "Завершить процесс", "End process");
        A("monitor.process.kill.confirm", "Завершить процесс «{0}»?", "End process “{0}”?");
        A("monitor.process.kill.failed", "Не удалось завершить «{0}»", "Couldn't end “{0}”");
        A("monitor.alert.cpu", "Процессор загружен на {0}%", "CPU load at {0}%");
        A("monitor.alert.memory", "Память заполнена на {0}%", "Memory at {0}%");
        A("monitor.alert.gpu", "Видеокарта загружена на {0}%", "GPU load at {0}%");
        A("monitor.alert.battery", "Батарея разряжена: {0}%", "Battery low: {0}%");
        A("monitor.alert.disk", "Диск «{0}» почти заполнен", "Disk “{0}” is almost full");
    }
}
