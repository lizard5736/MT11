using Grip.Core.Actions;
using Grip.Core.Input;

namespace Grip.UI.Settings.Pages;

public static class HotkeysPage
{
    public static void Build(PageBuilder b)
    {
        var settings = App.Services.Settings.Current;
        foreach (var category in Enum.GetValues<ActionCategory>())
        {
            var actions = ActionCatalog.All
                .Where(a => a.Category == category && a.CanHaveHotkey && (a.FeatureId == null || settings.IsInstalled(a.FeatureId)))
                .ToList();
            if (actions.Count == 0) continue;
            b.Card("action.category." + category);
            foreach (var action in actions) b.Hotkey(action.Id);
        }

        b.Card();
        b.Button("hotkeys.reset", null, "hotkeys.reset", () =>
        {
            App.Services.Settings.Update(s =>
            {
                foreach (var action in ActionCatalog.All.Where(a => a.CanHaveHotkey))
                    s.Hotkeys[action.Id] = action.DefaultHotkey;
            });
            SettingsWindowRebuild();
        }, icon: "ArrowReset", rowIcon: "ArrowReset");
    }

    private static void SettingsWindowRebuild()
    {
        foreach (var window in System.Windows.Application.Current.Windows.OfType<SettingsWindow>()) window.RequestRebuild();
    }
}
