using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace KairosTools
{
    public static class PowerPlanHelper
    {
        public class PlanInfo
        {
            public string Guid { get; set; }
            public string Name { get; set; }
            public bool IsActive { get; set; }
        }

        public static List<PlanInfo> GetPowerPlans()
        {
            var list = new List<PlanInfo>();
            string output = RunPowerCfg("/list");
            foreach (Match m in Regex.Matches(output,
                @"电源方案 GUID:\s*([0-9a-fA-F\-]+)\s*\((.+?)\)(\s*\*)?", RegexOptions.Multiline))
            {
                list.Add(new PlanInfo
                {
                    Guid = m.Groups[1].Value,
                    Name = m.Groups[2].Value.Trim(),
                    IsActive = m.Groups[3].Success
                });
            }
            return list;
        }

        public static void ActivatePlan(string guid) => RunPowerCfg($"/setactive {guid}");

        public static bool DeletePlan(string guid)
        {
            var active = GetActivePlanGuid();
            if (string.Equals(active, guid, StringComparison.OrdinalIgnoreCase))
            {
                string balancedGuid = FindBalancedGuid();
                if (balancedGuid != null) RunPowerCfg($"/setactive {balancedGuid}");
                else return false;
            }
            string err = RunPowerCfg($"/delete {guid}", true);
            return string.IsNullOrWhiteSpace(err);
        }

        /// <summary>用注册表检测休眠是否启用（最可靠）</summary>
        public static bool IsHibernateEnabled()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Control\Power"))
                {
                    if (key == null) return false;
                    var val = key.GetValue("HibernateEnabled");
                    if (val is int i) return i == 1;
                }
            }
            catch { }
            return false;
        }

        public static void DisableHibernate() => RunPowerCfg("/h off");
        public static void EnableHibernate() => RunPowerCfg("/h on");

        public static string ImportPlan(string powFilePath)
        {
            if (!File.Exists(powFilePath)) return null;
            string output = RunPowerCfg($"/import \"{powFilePath}\"");
            var m = Regex.Match(output, @"GUID:\s*([0-9a-fA-F\-]+)");
            return m.Success ? m.Groups[1].Value : null;
        }

        private static string GetActivePlanGuid()
        {
            foreach (var p in GetPowerPlans()) if (p.IsActive) return p.Guid;
            return null;
        }

        private static string FindBalancedGuid()
        {
            foreach (var p in GetPowerPlans()) if (p.Name.Contains("平衡")) return p.Guid;
            return null;
        }

        private static string RunPowerCfg(string arguments, bool captureError = false)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powercfg",
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using (var proc = Process.Start(psi))
                {
                    string stdout = proc.StandardOutput.ReadToEnd();
                    string stderr = proc.StandardError.ReadToEnd();
                    proc.WaitForExit(10000);
                    if (captureError) return stderr ?? "";
                    return string.IsNullOrWhiteSpace(stdout) ? (stderr ?? "") : stdout;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("powercfg 执行失败: " + ex.Message);
                return "";
            }
        }
    }
}