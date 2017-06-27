using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using iFactr.Compact.Annotations;
using iFactr.Core;
using iFactr.Core.Layers;
using iFactr.UI;

namespace iFactr.Compact
{
    class MenuTabView : MenuItem, ITabView, INotifyPropertyChanged
    {
        private static readonly object SyncRoot = new object();
        public static MenuTabView Instance { get { return _instance; } }
        private static volatile MenuTabView _instance;

        public Type ModelType { get { return _model == null ? typeof(NavigationTabs) : _model.GetType(); } }

        public object GetModel() { return _model; }

        public void SetModel(object model) { _model = model; }
        private object _model;

        public void Render()
        {
            lock (SyncRoot) if (_instance == null) _instance = this;

            Rendering.Raise(this, EventArgs.Empty);
            if (!TabItems.Any()) return;
            if (string.IsNullOrEmpty(Text)) Text = "Go to...";

            PaneManager.Instance.Clear();
            var tabs = TabItems.Select(item => CompactFactory.GetNativeObject<TabItem>(item, item.Title)).ToList();
            for (int i = 0; i < tabs.Count; i++)
            {
                var tab = tabs[i];
                if (tab == null) continue;
                tab.View = this;
                tab.Index = i;
                MenuItems.Add(tab);
                var context = new iApp.AppNavigationContext { ActivePane = Pane.Master, ActiveTab = i };
                PaneManager.Instance.AddStack(new HistoryStack { Context = context }, context);
            }

            var cxt = new iApp.AppNavigationContext { ActivePane = Pane.Popover, };
            PaneManager.Instance.AddStack(new HistoryStack { Context = cxt }, cxt);
            if (PaneManager.Instance.CurrentTab > TabItems.Count() || PaneManager.Instance.CurrentTab < 0)
                PaneManager.Instance.CurrentTab = 0;
            CompactFactory.Navigate(TabItems.ElementAt(PaneManager.Instance.CurrentTab).NavigationLink, this);
        }

        public IPairable Pair
        {
            get { return _pair; }
            set
            {
                if (_pair != null) return;
                _pair = value;
                _pair.Pair = this;
                OnPropertyChanged("Pair");
            }
        }
        private IPairable _pair;

        public MetadataCollection Metadata
        {
            get { return _metadata ?? (_metadata = new MetadataCollection()); }
        }
        private MetadataCollection _metadata;

        public bool Equals(IView other)
        {
            return _pair == null ? other == null : Pair.Equals(other.Pair);
        }

        public Color HeaderColor
        {
            get { return new Color(); }
            set { }
        }

        public double Height
        {
            get { throw new NotImplementedException(); }
        }

        public PreferredOrientation PreferredOrientations
        {
            get { throw new NotImplementedException(); }
            set { }
        }

        public string Title
        {
            get { return Text; }
            set
            {
                if (Text == value) return;
                Text = value;
                OnPropertyChanged("Title");
            }
        }

        public Color TitleColor
        {
            get { throw new NotImplementedException(); }
            set { }
        }

        public double Width
        {
            get { return 0; }
        }

        public event EventHandler Rendering;
        public void SetBackground(Color color)
        {
        }

        public void SetBackground(string imagePath, ContentStretch stretch)
        {
        }

        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                if (_selectedIndex == value) return;
                _selectedIndex = value;
                OnPropertyChanged("SelectedIndex");
            }
        }
        private int _selectedIndex;

        public Color SelectionColor
        {
            get { throw new NotImplementedException(); }
            set { }
        }

        public IEnumerable<ITabItem> TabItems
        {
            get { return _tabItems; }
            set
            {
                if (value.Equals(_tabItems)) return;
                _tabItems = value;
                PaneManager.Instance.FromNavContext(Pane.Popover, 0).PopToRoot();
                OnPropertyChanged("TabItems");
            }
        }
        private IEnumerable<ITabItem> _tabItems;

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}