namespace Grip.Core.Localization;

public static partial class StringTable
{
    private static void AddMonitor(Action<string, string, string> A)
    {
        A("monitor.memory.top", "Больше всего: {0}", "Using the most: {0}");
        A("monitor.disk.free", "{0} свободно", "{0} free");
        A("monitor.network.session", "За сессию: ↓{0} ↑{1}", "This session: ↓{0} ↑{1}");
        A("monitor.battery.charging", "Заряжается", "Charging");
        A("monitor.battery.onBattery", "От батареи", "On battery");
        A("monitor.battery.none", "Аккумулятор не найден", "No battery found");
    }
}
