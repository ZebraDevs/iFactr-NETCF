using System;
using System.Runtime.InteropServices;

namespace iFactr.Compact
{
    public class HookKeys
    {
        #region Delegates

        public delegate int HookProc(int code, IntPtr wParam, IntPtr lParam);
        public delegate void HookEventHandler(HookEventArgs e, KeyBoardInfo keyBoardInfo);
        public HookEventHandler HookEvent;

        #endregion

        #region Fields

        private HookProc hookDeleg;
        private static int hHook = 0;

        #endregion

        public HookKeys()
        {
        }

        ~HookKeys()
        {
            if (hHook != 0)
                this.Stop();
        }

        #region Public methods

        ///
        /// Starts the hook
        ///
        public void Start()
        {
            if (hHook != 0)
            {
                //Unhook the previouse one
                this.Stop();
            }
            hookDeleg = new HookProc(HookProcedure);
            hHook = SetWindowsHookEx(WH_KEYBOARD_LL, hookDeleg, GetModuleHandle(null), 0);
            if (hHook == 0)
            {
                throw new SystemException("Failed acquiring of the hook.");
            }
            AllKeys(true);
        }

        ///
        /// Stops the hook
        ///
        public void Stop()
        {
            UnhookWindowsHookEx(hHook);
            AllKeys(false);
        }
        #endregion

        #region Protected and private methods

        protected virtual void OnHookEvent(HookEventArgs hookArgs, KeyBoardInfo keyBoardInfo)
        {
            if (HookEvent != null)
            {
                HookEvent(hookArgs, keyBoardInfo);
            }
        }

        private int HookProcedure(int code, IntPtr wParam, IntPtr lParam)
        {
            KBDLLHOOKSTRUCT hookStruct = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
            if (code < 0)
                return CallNextHookEx(hookDeleg, code, wParam, lParam);
            // Let clients determine what to do
            HookEventArgs e = new HookEventArgs();
            e.Code = code;
            e.wParam = wParam;
            e.lParam = lParam;
            KeyBoardInfo keyInfo = new KeyBoardInfo();
            keyInfo.vkCode = hookStruct.vkCode;
            keyInfo.scanCode = hookStruct.scanCode;
            OnHookEvent(e, keyInfo);
            // Yield to the next hook in the chain
            return CallNextHookEx(hookDeleg, code, wParam, lParam);
        }

        #endregion

        #region P/Invoke declarations

        [DllImport("coredll.dll")]
        private static extern int AllKeys(bool bEnable);

        [DllImport("coredll.dll")]
        private static extern int SetWindowsHookEx(int type, HookProc hookProc, IntPtr hInstance, int m);
        [DllImport("coredll.dll")]
        private static extern IntPtr GetModuleHandle(string mod);
        [DllImport("coredll.dll")]
        private static extern int CallNextHookEx(
                HookProc hhk,
                int nCode,
                IntPtr wParam,
                IntPtr lParam
                );
        [DllImport("coredll.dll", SetLastError = true)]
        private static extern int UnhookWindowsHookEx(int idHook);
        private struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }
        const int WH_KEYBOARD_LL = 20;

        #endregion
    }

    #region event arguments

    public class HookEventArgs : EventArgs
    {
        public int Code;    // Hook code
        public IntPtr wParam;   // WPARAM argument
        public IntPtr lParam;   // LPARAM argument
    }
    public class KeyBoardInfo
    {
        public int vkCode;
        public int scanCode;
        public int flags;
        public int time;
    }
    #endregion
}