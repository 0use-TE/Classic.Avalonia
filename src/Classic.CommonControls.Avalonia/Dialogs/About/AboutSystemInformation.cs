using System.Runtime.InteropServices;

namespace Classic.CommonControls.Dialogs;

/// <summary>Optional application-provided system information; no platform APIs are called by the dialog.</summary>
public sealed record AboutSystemInformation
{
    public string OperatingSystem { get; init; } = RuntimeInformation.OSDescription;
    public string Username { get; init; } = "";
    public string MemoryDescription { get; init; } = "Memory budget available to runtime:";
    public long TotalMemoryBytes { get; init; }
    public long AvailableMemoryBytes { get; init; }

    public static AboutSystemInformation ForCurrentRuntime()
    {
        var total = Math.Max(0, GC.GetGCMemoryInfo().TotalAvailableMemoryBytes);
        return new AboutSystemInformation
        {
            TotalMemoryBytes = total,
            AvailableMemoryBytes = Math.Clamp(total - GC.GetTotalMemory(false), 0, total)
        };
    }
}
