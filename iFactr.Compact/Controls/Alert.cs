using System.Windows.Forms;
using iFactr.Core;
using iFactr.UI;

namespace iFactr.Compact
{
    class Alert : IAlert
    {
        public Alert(string message, string title, AlertButtons buttons)
        {
            Message = message;
            Title = title;
            Buttons = buttons;
        }

        public Link CancelLink { get; set; }
        public Link OKLink { get; set; }
        public string Message { get; private set; }
        public string Title { get; private set; }
        public AlertButtons Buttons { get; private set; }
        public event AlertResultEventHandler Dismissed;

        public void Show()
        {
            MessageBoxButtons buttons;

            switch (Buttons)
            {
                case AlertButtons.OKCancel:
                    buttons = MessageBoxButtons.OKCancel;
                    break;
                case AlertButtons.YesNo:
                    buttons = MessageBoxButtons.YesNo;
                    break;
                default:
                    buttons = MessageBoxButtons.OK;
                    break;
            }

            var result = MessageBox.Show(Message, Title, buttons,
                Buttons == AlertButtons.OK ? MessageBoxIcon.Exclamation : MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            AlertResult res;
            switch (result)
            {
                case DialogResult.Cancel:
                    res = AlertResult.Cancel;
                    break;
                case DialogResult.Yes:
                    res = AlertResult.Yes;
                    break;
                case DialogResult.No:
                    res = AlertResult.No;
                    break;
                default:
                    res = AlertResult.OK;
                    break;
            }

            var dismissed = Dismissed;
            if (dismissed != null)
            {
                dismissed(this, new AlertResultEventArgs(res));
            }
            else switch (res)
                {
                    case AlertResult.OK:
                    case AlertResult.Yes:
                        iApp.Navigate(OKLink);
                        break;
                    case AlertResult.Cancel:
                    case AlertResult.No:
                        iApp.Navigate(CancelLink);
                        break;
                }
        }
    }
}