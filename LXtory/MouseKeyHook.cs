using System;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace LXtory
{
    internal class MouseKeyHook
    {
        // Imported stuff from system DLL
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x201;
        private const int WM_RBUTTONDOWN = 0x204;
        private static readonly LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        // Action to send the button presses to
        private static Action<bool> action;

        public MouseKeyHook()
        {
            _hookID = SetHook(_proc);
        }

        // Set this process to listen mousebutton presses
        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }
        
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        // Read what button was pressed
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                if (wParam == (IntPtr)WM_LBUTTONDOWN)
                {
                    Console.WriteLine("LEFT MOUSE DOWN!");
                    action(true);
                }
                if (wParam == (IntPtr)WM_RBUTTONDOWN)
                {
                    Console.WriteLine("RIGHT MOUSE DOWN!");
                    action(false);
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        // Set where to send the button presses
        public static void SetAction(Action<bool> d)
        {
            action = d;
        }

        // Unhook
        public static void Unhook()
        {
            UnhookWindowsHookEx(_hookID);
        }
    }
}
