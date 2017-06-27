using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Size = iFactr.UI.Size;

namespace iFactr.Compact
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public static class CoreDll
    {
        public static Size MeasureString(string text, Font font, Size constraints, bool multiline, bool textboxControl)
        {
            if (String.IsNullOrEmpty(text))
                return new Size();
            Graphics gr = CompactFactory.Instance.RootForm.CreateGraphics();
            var bounds = new Rect
            {
                Left = 0,
                Top = 0,
                Bottom = (int)constraints.Height,
                Right = (int)constraints.Width,
            };
            var hFont = font.ToHfont();
            var hdc = gr.GetHdc();
            var originalObject = SelectObject(hdc, hFont);
            int flags = DT_CALCRECT;
            if (multiline) flags |= DT_WORDBREAK;
            if (textboxControl) flags |= DT_EDITCONTROL;
            DrawText(hdc, text, text.Length, ref bounds, flags);
            SelectObject(hdc, originalObject);
            gr.ReleaseHdc(hdc);
            return new Size(bounds.Right - bounds.Left, bounds.Bottom - bounds.Top + (textboxControl ? 6 : 0));
        }

        [DllImport("coredll.dll")]
        public static extern int SystemParametersInfo(int uiAction, int uiParam, IntPtr pvParam, int fWinIni);

        [DllImport("coredll.dll", SetLastError = true)]
        public static extern int AddFontResource(string lpName);

        [DllImport("coredll.dll", SetLastError = true)]
        public static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("coredll.dll")]
        public static extern int GetAsyncKeyState(Keys vkey);

        [DllImport("coredll", SetLastError = true)]
        public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

        [DllImport("coredll.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteObject(IntPtr hObject);

        [DllImport("coredll.dll", SetLastError = true)]
        internal static extern int SetTextColor(IntPtr hdc, int crColor);

        [DllImport("coredll.dll", SetLastError = true)]
        public static extern int DrawText(IntPtr hdc, string lpStr, int nCount, ref Rect lpRect, int wFormat);

        [DllImport("coredll.dll", SetLastError = true)]
        public static extern uint SetBkColor(IntPtr hdc, int crColor);

        [DllImport("coredll.dll", SetLastError = true)]
        public static extern uint SetBkMode(IntPtr hdc, int mode);

        [DllImport("CoreDll.dll", SetLastError = true)]
        public static extern bool GetMouseMovePoints(Point[] pptBuf, uint nBufPoints, ref uint pnPointsRetrieved);

        public static Point MousePosition
        {
            get
            {
                Point[] p = new Point[1];
                uint ret = 0;
                if (GetMouseMovePoints(p, 1, ref ret))
                {
                    return new Point(p[0].X / 4, (p[0].Y / 4) - SystemInformation.MenuHeight);
                }
                return new Point(Control.MousePosition.X, Control.MousePosition.Y - SystemInformation.MenuHeight);
            }
        }

        public const int DT_WORDBREAK = 0x00000010;
        public const int DT_CALCRECT = 0x00000400;
        public const int DT_NOPREFIX = 0x00000800;
        public const int DT_EDITCONTROL = 0x00002000;
        public const int DT_END_ELLIPSIS = 0x00008000;

        private const int FILE_DEVICE_HAL = 0x00000101;
        private const int FILE_ANY_ACCESS = 0x0;
        private const int METHOD_BUFFERED = 0x0;

        internal const int IOCTL_HAL_GET_DEVICEID = ((FILE_DEVICE_HAL) << 16) | ((FILE_ANY_ACCESS) << 14) | ((21) << 2) | (METHOD_BUFFERED);
        internal const int ERROR_NOT_SUPPORTED = 0x32;
        internal const int ERROR_INSUFFICIENT_BUFFER = 0x7A;

        public const int SPI_SETFONTSMOOTHING = 0x0000004B;    // use uiParam to set

        [DllImport("coredll.dll")]
        internal static extern bool KernelIoControl(Int32 IoControlCode, IntPtr InputBuffer, Int32 InputBufferSize, byte[] OutputBuffer, Int32 OutputBufferSize, ref Int32 BytesReturned);
    }
}