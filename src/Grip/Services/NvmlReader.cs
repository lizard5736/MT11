using System.IO;
using System.Runtime.InteropServices;
using Grip.Interop;

namespace Grip.Services;

/// <summary>
/// GPU temperature and VRAM usage for NVIDIA cards, via NVML — the official NVIDIA
/// Management Library that ships with every NVIDIA driver, no separate install needed.
/// Structs and function signatures verified against NVIDIA's own public nvml.h.
///
/// nvml.dll is loaded dynamically (LoadLibrary/GetProcAddress) rather than a static
/// DllImport: it isn't reliably on the default DLL search path (NVIDIA's installer puts
/// it under Program Files, not System32 or PATH). Every failure — no NVIDIA driver, DLL
/// missing, init failure, no device — degrades to IsAvailable=false once at construction;
/// nothing here retries a hopeless path on every sample.
/// </summary>
internal sealed class NvmlReader
{
    private delegate int NvmlInit();
    private delegate int NvmlDeviceGetHandleByIndex(uint index, out IntPtr device);
    private delegate int NvmlDeviceGetTemperature(IntPtr device, int sensorType, out uint temperatureCelsius);
    private delegate int NvmlDeviceGetMemoryInfo(IntPtr device, out NvmlMemory memory);

    // The plain (v1) layout: three flat ulongs, no version field. NVML also defines a
    // newer nvmlMemory_v2_t with a leading version field and extra reserved space — the
    // v1 struct here is simpler and sufficient for used/total, so there's no reason to
    // take on a version-field mismatch risk for data this call doesn't need.
    [StructLayout(LayoutKind.Sequential)]
    private struct NvmlMemory
    {
        public ulong Total;
        public ulong Free;
        public ulong Used;
    }

    private const int NvmlSuccess = 0;
    private const int NvmlTemperatureGpu = 0;

    private readonly IntPtr _device;
    private readonly NvmlDeviceGetTemperature? _getTemperature;
    private readonly NvmlDeviceGetMemoryInfo? _getMemoryInfo;

    public bool IsAvailable { get; }

    public NvmlReader()
    {
        try
        {
            var library = NativeMethods.LoadLibraryW("nvml.dll");
            if (library == IntPtr.Zero)
            {
                string programFiles = Environment.GetEnvironmentVariable("ProgramW6432") ?? @"C:\Program Files";
                library = NativeMethods.LoadLibraryW(Path.Combine(programFiles, "NVIDIA Corporation", "NVSMI", "nvml.dll"));
            }
            if (library == IntPtr.Zero) return;

            var init = GetDelegate<NvmlInit>(library, "nvmlInit_v2");
            var getHandle = GetDelegate<NvmlDeviceGetHandleByIndex>(library, "nvmlDeviceGetHandleByIndex_v2");
            _getTemperature = GetDelegate<NvmlDeviceGetTemperature>(library, "nvmlDeviceGetTemperature");
            _getMemoryInfo = GetDelegate<NvmlDeviceGetMemoryInfo>(library, "nvmlDeviceGetMemoryInfo");
            if (init == null || getHandle == null || _getTemperature == null || _getMemoryInfo == null) return;

            IsAvailable = init() == NvmlSuccess && getHandle(0, out _device) == NvmlSuccess;
        }
        catch (Exception ex)
        {
            Log.Error("NVML init failed", ex);
        }
    }

    /// <summary>Celsius, or null if unavailable or the read failed.</summary>
    public double? ReadTemperature()
    {
        if (!IsAvailable) return null;
        try
        {
            return _getTemperature!(_device, NvmlTemperatureGpu, out var value) == NvmlSuccess ? value : null;
        }
        catch (Exception ex)
        {
            Log.Error("NVML temperature read failed", ex);
            return null;
        }
    }

    /// <summary>(UsedBytes, TotalBytes), or null if unavailable or the read failed.</summary>
    public (long Used, long Total)? ReadMemory()
    {
        if (!IsAvailable) return null;
        try
        {
            return _getMemoryInfo!(_device, out var memory) == NvmlSuccess ? ((long)memory.Used, (long)memory.Total) : null;
        }
        catch (Exception ex)
        {
            Log.Error("NVML memory read failed", ex);
            return null;
        }
    }

    private static T? GetDelegate<T>(IntPtr library, string name) where T : Delegate
    {
        var address = NativeMethods.GetProcAddress(library, name);
        return address == IntPtr.Zero ? null : Marshal.GetDelegateForFunctionPointer<T>(address);
    }
}
