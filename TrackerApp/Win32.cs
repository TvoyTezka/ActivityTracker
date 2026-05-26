using System.Runtime.InteropServices;
using System.Text;

namespace TrackerApp;

public class Win32
{
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowTextLengthW(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowTextW(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [DllImport("kernel32.dll")]
    public static extern uint GetTickCount();

    [DllImport("kernel32.dll")]
    public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

    [DllImport("kernel32.dll")]
    public static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    public static extern bool QueryFullProcessImageNameW(
        IntPtr hProcess, uint dwFlags, StringBuilder lpExeName, ref uint lpdwSize);

    [StructLayout(LayoutKind.Sequential)]
    public struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    public const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

    public static (string process, string title) GetForegroundInfo()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
            return ("<unknown>", "");

        GetWindowThreadProcessId(hwnd, out uint pid);

        var len = GetWindowTextLengthW(hwnd) + 1;
        var titleBuf = new StringBuilder(len);
        GetWindowTextW(hwnd, titleBuf, len);
        var title = titleBuf.ToString();

        var hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        var processName = "<unknown>";
        if (hProcess != IntPtr.Zero)
        {
            var buf = new StringBuilder(260);
            uint size = 260;
            if (QueryFullProcessImageNameW(hProcess, 0, buf, ref size))
                processName = Path.GetFileName(buf.ToString());
            CloseHandle(hProcess);
        }

        return (processName, title);
    }

    public static double GetIdleSeconds()
    {
        var info = new LASTINPUTINFO();
        info.cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>();
        GetLastInputInfo(ref info);
        return (GetTickCount() - info.dwTime) / 1000.0;
    }
}
