namespace Grip.Services;

/// <summary>
/// CPU temperature for AMD Ryzen/EPYC (Zen through Zen 5, family 0x17-0x1A) via PawnIO's
/// AMDFamily17 module, reading the SMN "Tctl" thermal-control sensor the same way
/// LibreHardwareMonitor's Amd17Cpu class does — same source register, same -49°C offset
/// detection, verified against their code before writing this rather than reconstructed
/// from memory of how AMD's sensor works.
///
/// Tctl, not Tdie: on models where the two differ, Tctl reads a few degrees hotter by
/// design (it's AMD's fan-curve target, not the literal die temperature) — the simpler,
/// more commonly shown of the two, and the one this can get right without a per-model
/// Tdie-offset table there is no way to verify against real AMD hardware.
/// </summary>
internal sealed class AmdCpuTemperatureReader
{
    private const uint ThmTconCurTmp = 0x00059800;
    private const uint TempRangeSelMask = 0x80000;
    private const uint TempTjSelMask = 0x30000;

    private readonly PawnIoDevice? _device;

    public bool IsAvailable => _device != null;

    public AmdCpuTemperatureReader()
    {
        if (!PawnIoDevice.IsInstalled) return;
        _device = PawnIoDevice.LoadModuleFromResource(typeof(AmdCpuTemperatureReader).Assembly, "Grip.Resources.PawnIO.AMDFamily17.bin");
    }

    /// <summary>Celsius, or null if PawnIO isn't installed, this isn't a supported AMD CPU
    /// (the module itself checks vendor and family and refuses to load otherwise), or the
    /// read failed.</summary>
    public double? ReadTemperature()
    {
        if (_device == null) return null;
        var result = _device.Execute("ioctl_read_smn", new long[] { ThmTconCurTmp }, 1);
        if (result is not { Length: 1 }) return null;

        uint raw = (uint)result[0];
        // Newer Zen parts can signal the 49°C adjustment through TJ_SEL[17:16] as well as
        // RANGE_SEL[19] — checking both is the general rule, not one specific to a model list.
        bool hasOffset = (raw & TempRangeSelMask) != 0 || (raw & TempTjSelMask) == TempTjSelMask;
        double celsius = (raw >> 21) * 125 * 0.001;
        if (hasOffset) celsius -= 49.0;
        return celsius;
    }
}
