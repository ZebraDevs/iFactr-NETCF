using iFactr.UI;
using iFactr.UI.Controls;

namespace iFactr.Compact
{
    public class PasswordBox : TextBase, IPasswordBox
    {
        public PasswordBox()
        {
            IsPassword = true;
            TextChanged += PasswordBoxChanged;
        }

        private void PasswordBoxChanged(object sender, ValueChangedEventArgs<string> args)
        {
            var passCh = PasswordChanged;
            if (passCh != null) passCh(this, args);
            OnPropertyChanged("Password");
        }

        #region IPasswordBox Members

        public string Password
        {
            get { return Text; }
            set { Text = value; }
        }

        public override void NullifyEvents()
        {
            base.NullifyEvents();
            TextChanged += PasswordBoxChanged;
        }

        public event ValueChangedEventHandler<string> PasswordChanged;

        #endregion
    }
}