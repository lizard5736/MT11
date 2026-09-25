using System.IO;
using System.Reflection;
using System.Text;
using Grip.Interop;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace Grip.Services;

/// <summary>
/// Talks to the PawnIO kernel driver: load one of its signed modules, then call a named
/// function inside it. Protocol (IOCTL codes, buffer layout) matches LibreHardwareMonitor's
/// own PawnIo.cs exactly — read directly from their source before writing this, not
/// reconstructed from a description, since a wrong buffer layout talking to a kernel driver
/// is exactly the kind of mistake this project avoids raw PDH arrays over.
///
/// PawnIO itself is a separate install (winget id namazso.PawnIO) — Grip only detects it and
/// talks to it if present; see THIRD_PARTY_NOTICES.md for the embedded module's origin.
/// </summary>
internal sealed class PawnIoDevice : IDisposable
{
    private const int FunctionNameLength = 32;
    private const uint DeviceType = 41394u << 16;
    private const uint IoctlLoadBinary = DeviceType | (0x821u << 2);
    private const uint IoctlExecuteFunction = DeviceType | (0x841u << 2);

    private readonly SafeFileHandle _handle;

    private PawnIoDevice(SafeFileHandle handle) => _handle = handle;

    /// <summary>Same registry key LibreHardwareMonitor checks: PawnIO's own installer writes
    /// its uninstall entry here, so a version being present means the driver is installed
    /// (not necessarily that it's currently loaded — CreateFile is the real test for that).</summary>
    public static bool IsInstalled
    {
        get
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO");
                return key?.GetValue("DisplayVersion") is string;
            }
            catch (System.Security.SecurityException)
            {
                return false;
            }
        }
    }

    /// <summary>Opens the driver and loads a module from an embedded resource. Null if PawnIO
    /// isn't installed, the module fails to load, or anything else goes wrong — every failure
    /// here means "no reading", never a crash.</summary>
    public static PawnIoDevice? LoadModuleFromResource(Assembly assembly, string resourceName)
    {
        try
        {
            var handle = NativeMethods.CreateFile(@"\\?\GLOBALROOT\Device\PawnIO",
                NativeMethods.GENERIC_READ | NativeMethods.GENERIC_WRITE,
                NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE,
                IntPtr.Zero, NativeMethods.OPEN_EXISTING, NativeMethods.FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);
            if (handle.IsInvalid) return null;

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                handle.Dispose();
                return null;
            }
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            byte[] module = memory.ToArray();

            bool loaded = NativeMethods.DeviceIoControl(handle, IoctlLoadBinary, module, (uint)module.Length,
                null, 0, out _, IntPtr.Zero);
            if (!loaded)
            {
                handle.Dispose();
                return null;
            }

            return new PawnIoDevice(handle);
        }
        catch (Exception ex)
        {
            Log.Error("PawnIO module load failed", ex);
            return null;
        }
    }

    /// <summary>Calls one named function the loaded module exposes. Every input/output slot is
    /// a 64-bit integer, matching the module's own DEFINE_IOCTL_SIZED contract. Null on any
    /// failure — a module rejecting a call (e.g. an MSR outside its allow-list) is expected
    /// behavior, not an error to throw over.</summary>
    public long[]? Execute(string name, long[] input, int outputCount)
    {
        try
        {
            byte[] inBuffer = new byte[FunctionNameLength + input.Length * sizeof(long)];
            Encoding.ASCII.GetBytes(name, 0, Math.Min(name.Length, FunctionNameLength - 1), inBuffer, 0);
            Buffer.BlockCopy(input, 0, inBuffer, FunctionNameLength, input.Length * sizeof(long));

            byte[] outBuffer = new byte[outputCount * sizeof(long)];
            if (!NativeMethods.DeviceIoControl(_handle, IoctlExecuteFunction, inBuffer, (uint)inBuffer.Length,
                    outBuffer, (uint)outBuffer.Length, out uint returned, IntPtr.Zero))
                return null;

            var result = new long[returned / sizeof(long)];
            Buffer.BlockCopy(outBuffer, 0, result, 0, (int)returned);
            return result;
        }
        catch (Exception ex)
        {
            Log.Error($"PawnIO execute '{name}' failed", ex);
            return null;
        }
    }

    public void Dispose() => _handle.Dispose();
}
