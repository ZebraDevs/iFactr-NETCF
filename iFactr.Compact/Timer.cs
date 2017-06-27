using System;
using iFactr.UI;

namespace iFactr.Compact
{
    internal class Timer : System.Windows.Forms.Timer, ITimer
    {
        public Timer()
        {
            base.Enabled = false;
            Tick += Timer_Tick;
        }

        void Timer_Tick(object sender, EventArgs e)
        {
            IsEnabled = false;

            var elapse = Elapsed;
            if (elapse != null) { elapse(this, EventArgs.Empty); }
        }

        #region ITimer Members

        public bool IsEnabled
        {
            get { return base.Enabled; }
            set { base.Enabled = value; }
        }

        public new double Interval
        {
            get { return base.Interval; }
            set { base.Interval = (int)value; }
        }

        public event EventHandler Elapsed;

        public void Start()
        {
            IsEnabled = true;
        }

        public void Stop()
        {
            IsEnabled = false;
        }

        #endregion
    }
}