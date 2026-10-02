using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace KairosTools
{
    public static class MouseHelper
    {
        private const string MouseRegPath = @"Control Panel\Mouse";

        private const uint SPI_SETMOUSE = 0x0004;
        private const uint SPIF_UPDATEINIFILE = 0x01;
        private const uint SPIF_SENDCHANGE = 0x02;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(
            uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessageTimeout(
            IntPtr hWnd, uint Msg, UIntPtr wParam, string lParam,
            uint fuFlags, uint uTimeout, out UIntPtr lpdwResult);

        private const uint WM_SETTINGCHANGE = 0x001A;
        private static readonly IntPtr HWND_BROADCAST = new IntPtr(0xffff);
        private const uint SMTO_ABORTIFHUNG = 0x0002;

        public static bool IsMouseAccelerationEnabled()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(MouseRegPath, false))
            {
                if (key == null) return true;

                string speed = key.GetValue("MouseSpeed") as string;
                string t1 = key.GetValue("MouseThreshold1") as string;
                string t2 = key.GetValue("MouseThreshold2") as string;

                bool disabled = speed == "0" && t1 == "0" && t2 == "0";
                return !disabled;
            }
        }

        public static void EnableMouseAcceleration()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(MouseRegPath, true))
            {
                if (key == null) return;

                key.SetValue("MouseSpeed", "1", RegistryValueKind.String);
                key.SetValue("MouseThreshold1", "6", RegistryValueKind.String);
                key.SetValue("MouseThreshold2", "10", RegistryValueKind.String);

                TryDeleteValue(key, "MouseAccel");
                TryDeleteValue(key, "MouseAccel_Max");
                TryDeleteValue(key, "MouseAccel_Scale");

                byte[] xCurve = HexToBytes(
                    "00,00,00,00,00,00,00,00,15,6e,00,00,00,00,00,00," +
                    "00,40,01,00,00,00,00,00,00,29,00,00,00,00,00,00," +
                    "00,00,00,00,00,00,00,00");
                byte[] yCurve = HexToBytes(
                    "00,00,00,00,00,00,00,00,fd,11,00,00,00,00,00,00," +
                    "00,24,00,00,00,00,00,00,00,38,00,00,00,00,00,00," +
                    "00,00,00,00,00,00,00,00");

                key.SetValue("SmoothMouseXCurve", xCurve, RegistryValueKind.Binary);
                key.SetValue("SmoothMouseYCurve", yCurve, RegistryValueKind.Binary);
            }

            ApplyReliably(6, 10, 1);
        }

        public static void DisableMouseAcceleration()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(MouseRegPath, true))
            {
                if (key == null) return;

                key.SetValue("MouseSpeed", "0", RegistryValueKind.String);
                key.SetValue("MouseThreshold1", "0", RegistryValueKind.String);
                key.SetValue("MouseThreshold2", "0", RegistryValueKind.String);

                key.SetValue("MouseAccel", "0", RegistryValueKind.String);
                key.SetValue("MouseAccel_Max", "0", RegistryValueKind.String);
                key.SetValue("MouseAccel_Scale", "0", RegistryValueKind.String);

                TryDeleteValue(key, "SmoothMouseXCurve");
                TryDeleteValue(key, "SmoothMouseYCurve");
            }

            ApplyReliably(0, 0, 0);
        }

        private static void ApplyReliably(int t1, int t2, int accel)
        {
            ApplyMouseParams(t1, t2, accel);
            BroadcastMouseChange();
            Thread.Sleep(60);

            ApplyMouseParams(t1, t2, accel);
            BroadcastMouseChange();
            Thread.Sleep(40);

            ApplyMouseParams(t1, t2, accel);
        }

        private static void ApplyMouseParams(int threshold1, int threshold2, int acceleration)
        {
            int[] mouseParams = { threshold1, threshold2, acceleration };
            IntPtr ptr = Marshal.AllocHGlobal(sizeof(int) * 3);
            try
            {
                Marshal.Copy(mouseParams, 0, ptr, 3);
                SystemParametersInfo(SPI_SETMOUSE, 0, ptr,
                    SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        private static void BroadcastMouseChange()
        {
            SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, UIntPtr.Zero,
                "Control Panel\\Mouse", SMTO_ABORTIFHUNG, 5000, out _);
        }

        private static void TryDeleteValue(RegistryKey key, string name)
        {
            try { key.DeleteValue(name, false); } catch { }
        }

        private static byte[] HexToBytes(string hex)
        {
            string[] parts = hex.Split(',');
            byte[] result = new byte[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                result[i] = Convert.ToByte(parts[i].Trim(), 16);
            return result;
        }
    }
}