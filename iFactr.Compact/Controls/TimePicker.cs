using System;
using System.Windows.Forms;
using iFactr.UI;
using iFactr.UI.Controls;

namespace iFactr.Compact
{
    internal class TimePicker : PickerBase, ITimePicker
    {
        public TimePicker()
        {
            Format = DateTimePickerFormat.Time;
            ShowUpDown = true;
        }

        public override void NullifyEvents()
        {
            base.NullifyEvents();
            TimeChanged = null;
        }

        public DateTime? Time { get; set; }
        public string TimeFormat { get; set; }
        public event ValueChangedEventHandler<DateTime?> TimeChanged;
    }
}