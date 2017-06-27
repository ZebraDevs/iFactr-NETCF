using System;
using System.Windows.Forms;
using iFactr.UI;
using iFactr.UI.Controls;

namespace iFactr.Compact
{
    internal class DatePicker : PickerBase, IDatePicker
    {
        public DatePicker()
        {
            Format = DateTimePickerFormat.Short;
        }

        public override void NullifyEvents()
        {
            base.NullifyEvents();
            DateChanged = null;
        }

        public DateTime? Date { get; set; }
        public string DateFormat { get; set; }
        public event ValueChangedEventHandler<DateTime?> DateChanged;

        public override void ShowPicker()
        {
            base.ShowPicker();
            int x = Width - (int)(10 * CompactFactory.Instance.DpiScale);
            int y = Height / 2;
            int lParam = x + y * 0x00010000;
            CoreDll.SendMessage(Handle, 0x00000201, (IntPtr)1, (IntPtr)lParam);
        }
    }
}