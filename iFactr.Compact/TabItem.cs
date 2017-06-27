using System;
using System.ComponentModel;
using System.Windows.Forms;
using iFactr.Compact.Annotations;
using iFactr.Core;
using iFactr.UI;

namespace iFactr.Compact
{
    class TabItem : MenuItem, ITabItem, INotifyPropertyChanged
    {
        internal int Index { get; set; }

        public TabItem()
        {
            Click += TabItem_Click;
        }

        void TabItem_Click(object sender, EventArgs e)
        {
            iApp.CurrentNavContext.ActivePane = Pane.Tabs;
            var selected = Selected;
            if (selected == null)
            {
                var stack = (HistoryStack)PaneManager.Instance.FromNavContext(Pane.Master, Index);
                if (Index == PaneManager.Instance.CurrentTab || stack.CurrentView == null)
                {
                    PaneManager.Instance.CurrentTab = Index;
                    CompactFactory.Navigate(NavigationLink, View);
                }
                else
                {
                    iApp.CurrentNavContext.ActivePane = Pane.Master;
                    PaneManager.Instance.CurrentTab = Index;
                    ((RootForm)CompactFactory.Instance.RootForm).Attach(stack.CurrentView, stack);
                }
            }
            else
            {
                selected(this, EventArgs.Empty);
            }
        }

        public IView View { get; set; }

        public IPairable Pair
        {
            get { return _pair; }
            set
            {
                if (_pair != null || value == null) return;
                _pair = value;
                _pair.Pair = this;
                OnPropertyChanged("Pair");
            }
        }
        private IPairable _pair;

        public bool Equals(ITabItem other)
        {
            var item = other as UI.TabItem;
            return item != null ? item.Equals(this) : ReferenceEquals(this, other);
        }

        public string BadgeValue
        {
            get { return string.Empty; }
            set { }
        }

        public string ImagePath
        {
            get { return string.Empty; }
            set { }
        }

        public Link NavigationLink
        {
            get { return _navigationLink; }
            set
            {
                if (value == _navigationLink) return;
                _navigationLink = value;
                OnPropertyChanged("NavigationLink");
            }
        }
        private Link _navigationLink;

        public string Title
        {
            get { return Text; }
            set
            {
                if (value == Text) return;
                Text = value;
                OnPropertyChanged("Title");
                OnPropertyChanged("Text");
            }
        }

        public Color TitleColor
        {
            get { return new Color(); }
            set { }
        }

        public Font TitleFont
        {
            get { return Font.PreferredTabFont; }
            set { }
        }

        public event EventHandler Selected;
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}