using System.Collections.Generic;
using System.ComponentModel;
using iFactr.Compact.Annotations;
using iFactr.UI;

namespace iFactr.Compact
{
    class ActionMenu : IMenu, INotifyPropertyChanged
    {
        private readonly List<IMenuButton> _buttons = new List<IMenuButton>();

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

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

        public bool Equals(IMenu other)
        {
            var menu = other as Menu;
            return menu != null ? menu.Equals(this) : ReferenceEquals(this, other);
        }

        public Color BackgroundColor
        {
            get { return new Color(); }
            set { }
        }

        public Color ForegroundColor
        {
            get { return new Color(); }
            set { }
        }

        public string ImagePath
        {
            get { return null; }
            set { }
        }

        public Color SelectionColor
        {
            get { return new Color(); }
            set {  }
        }

        public string Title
        {
            get { return null; }
            set { }
        }

        public int ButtonCount
        {
            get { return _buttons.Count; }
        }

        public void Add(IMenuButton menuButton)
        {
            var button = CompactFactory.GetNativeObject<MenuButton>(menuButton, "menuButton");
            _buttons.Add(button);
        }

        public IMenuButton GetButton(int index)
        {
            var item = _buttons[index];
            return (item.Pair as IMenuButton) ?? item;
        }
    }
}