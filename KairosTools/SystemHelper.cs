using System;
using System.Diagnostics;

namespace KairosTools
{
    public static class SystemHelper
    {
        // ============ SysMain ============

        public static bool IsSysMainEnabled()
        {
            string startType = RunPowerShell(
                "(Get-Service -Name 'SysMain').StartType.ToString()");
            string status = RunPowerShell(
                "(Get-Service -Name 'SysMain').Status.ToString()");

            startType = startType?.Trim();
            status = status?.Trim();

            return status == "Running" && startType != "Disabled";
        }

        public static void EnableSysMain()
        {
            RunPowerShell(
                "Set-Service -Name 'SysMain' -StartupType Automatic; " +
                "Start-Service -Name 'SysMain'");
        }

        public static void DisableSysMain()
        {
            RunPowerShell(
                "Stop-Service -Name 'SysMain' -Force; " +
                "Set-Service -Name 'SysMain' -StartupType Disabled");
        }

        // ============ 内存压缩 ============

        public static bool IsMemoryCompressionEnabled()
        {
            string result = RunPowerShell(
                "(Get-MMAgent).MemoryCompression.ToString()");
            return result?.Trim().Equals("True", StringComparison.OrdinalIgnoreCase) == true;
        }

        public static void EnableMemoryCompression()
        {
            RunPowerShell("Enable-MMAgent -MemoryCompression");
        }

        public static void DisableMemoryCompression()
        {
            RunPowerShell("Disable-MMAgent -MemoryCompression");
        }

        // ============ HAGS（硬件加速GPU调度） ============

        /// <summary>HwSchMode=2 表示启用，=1 表示禁用</summary>
        public static bool IsHagsEnabled()
        {
            string result = RunPowerShell(
                "(Get-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers' " +
                "-Name 'HwSchMode' -ErrorAction SilentlyContinue).HwSchMode");
            return result?.Trim() == "2";
        }

        public static void EnableHags()
        {
            RunPowerShell(
                "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers' " +
                "-Name 'HwSchMode' -Value 2 -Type DWord");
        }

        public static void DisableHags()
        {
            RunPowerShell(
                "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers' " +
                "-Name 'HwSchMode' -Value 1 -Type DWord");
        }

        // ============ 通用：调 powershell.exe 执行 ============

        private static string RunPowerShell(string command)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -NonInteractive -Command \"" +
                                command.Replace("\"", "\\\"") + "\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var proc = Process.Start(psi))
                {
                    string output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit(10000);

                    if (proc.ExitCode != 0) return null;
                    return output.Trim();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("PowerShell 执行失败: " + ex.Message);
                return null;
            }
        }
    }
}