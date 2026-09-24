using System.Windows;
using System.Windows.Threading;
using Grip.Services;
using Grip.UI;
using Grip.UI.Common;

namespace Grip;

public partial class App : Application
{
    private static AppServices? _services;
    private SingleInstance? _instance;

    public static AppServices Services => _services ?? throw new InvalidOperationException("Grip is not started yet.");

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Developer preview: render every screen to PNG files and exit (see tools/preview).
        int previewIndex = Array.IndexOf(e.Args, "--render-previews");
        if (previewIndex >= 0)
        {
            var dir = previewIndex + 1 < e.Args.Length ? e.Args[previewIndex + 1] : "previews";
            AppPaths.UseRoot(System.IO.Path.Combine(dir, "_data"));
            AppPaths.EnsureCreated();
            _services = new AppServices(preview: true);
            PreviewRenderer.RenderAll(dir);
            Shutdown();
            return;
        }

        _instance = new SingleInstance();
        if (!_instance.IsFirst && e.Args.Contains("--elevated-restart"))
            _instance.WaitForOwnership(TimeSpan.FromSeconds(10));
        if (!_instance.IsFirst)
        {
            _instance.SignalFirstInstance();
            Shutdown();
            return;
        }

        AppPaths.EnsureCreated();
        Log.Init();
        Log.Info($"Grip {typeof(App).Assembly.GetName().Version} starting");
        DispatcherUnhandledException += OnUnhandled;
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            Log.Error("Unhandled exception", args.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Log.Error("Unobserved task exception", args.Exception);
            args.SetObserved();
        };

        _services = new AppServices();
        _services.Start();
        _instance.ListenForActivation(() => Dispatcher.BeginInvoke(() => Services.Panel.Show()));

        bool autostart = e.Args.Contains("--autostart");
        if (!Services.Settings.Current.General.OnboardingCompleted)
            new OnboardingWindow().Show();
        else if (!autostart)
            Services.Panel.Show();
    }

    private void OnUnhandled(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error("UI exception", e.Exception);
        e.Handled = true;
        ErrorWindow.ShowOnce(e.Exception);
    }

    public static void Quit()
    {
        Current.Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            _services?.Dispose();
        }
        catch (Exception ex)
        {
            Log.Error("Shutdown failed", ex);
        }
        _instance?.Dispose();
        Log.Info("Grip stopped");
        base.OnExit(e);
    }
}
