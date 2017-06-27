using System;
using System.Windows.Forms;
using iFactr.UI;
using iFactr.Core;

namespace iFactr.Compact
{
    class MenuButton : MenuItem, IMenuButton
    {
        public event EventHandler Clicked;

        public string Title
        {
            get { return Text; }
        }

        public string ImagePath
        {
            get { return null; }
            set { }
        }

        public Link NavigationLink { get; set; }

        public IPairable Pair
        {
            get { return _pair; }
            set
            {
                if (_pair != null || value == null) return;
                _pair = value;
                _pair.Pair = this;
            }
        }
        private IPairable _pair;

        public MenuButton(string title)
        {
            Text = title;
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            var click = Clicked;
            if (click != null)
            {
                click(this, e);
            }
            else
            {
                CompactFactory.Navigate(NavigationLink, PaneManager.Instance.FromNavContext(PaneManager.Instance.TopmostPane).CurrentView);
            }
        }

        public bool Equals(IMenuButton other)
        {
            var item = other as UI.MenuButton;
            return item != null ? item.Equals(this) : ReferenceEquals(this, other);
        }
    }
}