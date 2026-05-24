using System.Diagnostics;

namespace Lab31_ProcessManager;

public class ProcessInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string MachineName { get; set; } = "";
    public long MemoryMb { get; set; }
    public int Threads { get; set; }
    public string StartTime { get; set; } = "Немає доступу";

    public static ProcessInfo FromProcess(Process process)
    {
        string startTime;
        try { startTime = process.StartTime.ToString("dd.MM.yyyy HH:mm:ss"); }
        catch { startTime = "Немає доступу"; }

        return new ProcessInfo
        {
            Id = process.Id,
            Name = process.ProcessName,
            MachineName = process.MachineName,
            MemoryMb = process.WorkingSet64 / 1024 / 1024,
            Threads = SafeThreadCount(process),
            StartTime = startTime
        };
    }

    private static int SafeThreadCount(Process process)
    {
        try { return process.Threads.Count; }
        catch { return 0; }
    }
}
