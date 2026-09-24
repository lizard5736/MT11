using System.Windows.Threading;
using Grip.Core.Energy;
using Grip.Interop;
using static Grip.Interop.NativeMethods;

namespace Grip.Services;

/// <summary>
/// Keeps the PC awake with SetThreadExecutionState (the documented way; no
/// fake key presses). Optionally keeps the display on and overrides the lid
/// action for the session, restoring the user's power plan afterwards.
/// </summary>
public sealed class KeepAwakeService : IDisposable
{
    // Power setting GUIDs: SUB_BUTTONS / LIDACTION.
    private static readonly Guid SubButtons = new("4f971e89-eebd-4455-a8de-9e59040e7347");
    private static readonly Guid LidAction = new("5ca83367-6e45-459f-a27b-476b1d01c936");

    private readonly SettingsService _settings;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private (uint Ac, uint Dc)? _savedLid;

    public KeepAwakeSession? Session { get; private set; }

    /// <summary>The preset (in minutes) that started the session, for highlighting its chip.</summary>
    public int? ActivePreset { get; private set; }

    public bool IsActive => Session != null;

    public event EventHandler? Changed;

    /// <summary>Raised every second while a timed session runs.</summary>
    public event EventHandler? Tick;

    public KeepAwakeService(SettingsService settings)
    {
        _settings = settings;
        _timer.Tick += (_, _) =>
        {
            if (Session == null) { _timer.Stop(); return; }
            if (Session.IsExpired(DateTimeOffset.Now))
            {
                Stop();
                App.Services.Hud.Show(Localize("keepAwake.hud.off"), "WeatherMoon", UI.Common.HudTone.Neutral);
                return;
            }
            Tick?.Invoke(this, EventArgs.Empty);
        };
        _settings.Changed += (_, _) =>
        {
            if (IsActive) ApplyExecutionState();
        };
    }

    public static bool HasLid
    {
        get
        {
            try
            {
                return GetPwrCapabilities(out var caps) && caps.LidPresent;
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
            {
                return false;
            }
        }
    }

    public void Start(int minutes)
    {
        Session = KeepAwakeSession.ForMinutes(minutes, DateTimeOffset.Now);
        ActivePreset = minutes;
        Activate();
    }

    public void StartUntil(TimeOnly time)
    {
        Session = KeepAwakeSession.UntilTime(time, DateTimeOffset.Now);
        ActivePreset = null;
        Activate();
    }

    public void Toggle()
    {
        if (IsActive) Stop();
        else Start(_settings.Current.KeepAwake.DefaultMinutes);
    }

    public void Stop()
    {
        if (Session == null) return;
        Session = null;
        ActivePreset = null;
        _timer.Stop();
        SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
        RestoreLid();
        Log.Info("Keep awake off");
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void Activate()
    {
        ApplyExecutionState();
        _timer.Start();
        Log.Info(Session!.IsIndefinite ? "Keep awake on, no limit" : $"Keep awake on until {Session.Until:t}");
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyExecutionState()
    {
        var flags = EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_SYSTEM_REQUIRED;
        if (_settings.Current.KeepAwake.KeepDisplayOn) flags |= EXECUTION_STATE.ES_DISPLAY_REQUIRED;
        SetThreadExecutionState(flags);
        if (_settings.Current.KeepAwake.LidClosed && HasLid) OverrideLid();
        else RestoreLid();
    }

    /// <summary>Sets "when I close the lid: do nothing" for the active plan, remembering the old values.</summary>
    public bool OverrideLid()
    {
        if (_savedLid != null) return true;
        try
        {
            if (PowerGetActiveScheme(IntPtr.Zero, out var schemePtr) != 0) return false;
            var scheme = System.Runtime.InteropServices.Marshal.PtrToStructure<Guid>(schemePtr);
            LocalFree(schemePtr);
            var sub = SubButtons;
            var setting = LidAction;
            if (PowerReadACValueIndex(IntPtr.Zero, ref scheme, ref sub, ref setting, out uint ac) != 0) return false;
            if (PowerReadDCValueIndex(IntPtr.Zero, ref scheme, ref sub, ref setting, out uint dc) != 0) return false;
            uint r1 = PowerWriteACValueIndex(IntPtr.Zero, ref scheme, ref sub, ref setting, 0);
            uint r2 = PowerWriteDCValueIndex(IntPtr.Zero, ref scheme, ref sub, ref setting, 0);
            if (r1 != 0 || r2 != 0)
            {
                Log.Warn($"Lid override refused: {r1}/{r2}");
                LidOverrideFailed = true;
                return false;
            }
            PowerSetActiveScheme(IntPtr.Zero, ref scheme);
            _savedLid = (ac, dc);
            // If Grip is killed mid-session, the next launch puts the lid action back.
            TryWriteRecovery(ac, dc);
            LidOverrideFailed = false;
            return true;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            return false;
        }
    }

    /// <summary>True when Windows refused to change the lid action (usually needs admin rights).</summary>
    public bool LidOverrideFailed { get; private set; }

    private void RestoreLid()
    {
        if (_savedLid is not { } saved) return;
        try
        {
            if (PowerGetActiveScheme(IntPtr.Zero, out var schemePtr) != 0) return;
            var scheme = System.Runtime.InteropServices.Marshal.PtrToStructure<Guid>(schemePtr);
            LocalFree(schemePtr);
            var sub = SubButtons;
            var setting = LidAction;
            PowerWriteACValueIndex(IntPtr.Zero, ref scheme, ref sub, ref setting, saved.Ac);
            PowerWriteDCValueIndex(IntPtr.Zero, ref scheme, ref sub, ref setting, saved.Dc);
            PowerSetActiveScheme(IntPtr.Zero, ref scheme);
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException) { }
        finally
        {
            _savedLid = null;
            TryDeleteRecovery();
        }
    }

    private static string RecoveryFile => System.IO.Path.Combine(AppPaths.LocalDir, "lid-restore.txt");

    private static void TryWriteRecovery(uint ac, uint dc)
    {
        try { System.IO.File.WriteAllText(RecoveryFile, $"{ac} {dc}"); }
        catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException) { }
    }

    private static void TryDeleteRecovery()
    {
        try { System.IO.File.Delete(RecoveryFile); }
        catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException) { }
    }

    /// <summary>Restores a lid action left overridden by a previous run that didn't exit cleanly.</summary>
    public void RecoverAfterCrash()
    {
        try
        {
            if (!System.IO.File.Exists(RecoveryFile)) return;
            var parts = System.IO.File.ReadAllText(RecoveryFile).Split(' ');
            if (parts.Length == 2 && uint.TryParse(parts[0], out var ac) && uint.TryParse(parts[1], out var dc))
            {
                _savedLid = (ac, dc);
                RestoreLid();
                Log.Info("Restored the lid action after an unclean exit");
            }
            else TryDeleteRecovery();
        }
        catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException) { }
    }

    private static string Localize(string key) => Core.Localization.Localizer.Instance[key];

    public void Dispose()
    {
        Stop();
        _timer.Stop();
    }
}
