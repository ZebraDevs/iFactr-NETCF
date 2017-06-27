using System;
using System.Collections.Generic;
using System.Linq;
using iFactr.UI;
using iFactr.UI.Controls;

namespace iFactr.Compact
{
    class Toolbar : Grid, IToolbar
    {
        private int _width;
        public Toolbar()
        {
            Rows.Add(Row.AutoSized);
        }

        public void Layout()
        {
            Columns.Clear();

            var childControls = Children.ToList();
            foreach (var childControl in childControls)
            {
                RemoveChild(childControl);
            }

            if (PrimaryItems != null)
            {
                foreach (var toolbarItem in PrimaryItems)
                {
                    IElement item = toolbarItem is IToolbarButton || toolbarItem.Pair is IToolbarButton
                        ? CompactFactory.GetNativeObject<ToolbarButton>(toolbarItem, "toolbarItem")
                        : null;
                    //toolbarItem is IToolbarSeparator || toolbarItem.Pair is IToolbarSeparator ? CompactFactory.GetNativeObject<ToolbarSeparator>(toolbarItem, "toolbarItem") : null;
                    if (item != null)
                    {
                        Columns.Add(Column.AutoSized);
                        AddChild(item);
                    }
                }
            }

            if (SecondaryItems != null)
            {
                foreach (var toolbarItem in SecondaryItems)
                {
                    IElement item = toolbarItem is IToolbarButton || toolbarItem.Pair is IToolbarButton
                        ? CompactFactory.GetNativeObject<ToolbarButton>(toolbarItem, "toolbarItem")
                        : null;
                    //toolbarItem is IToolbarSeparator || toolbarItem.Pair is IToolbarSeparator ? CompactFactory.GetNativeObject<ToolbarSeparator>(toolbarItem, "toolbarItem") : null;
                    if (item != null)
                    {
                        Columns.Add(Column.AutoSized);
                        AddChild(item);
                    }
                }
            }

            var minSize = new Size(MinWidth, MinHeight);
            var maxSize = new Size(MaxWidth, MaxHeight);
            var newSize = this.PerformLayout(minSize, maxSize);
            if ((int)newSize.Height == 0) { return; }
            Size = newSize.ToSize();
            _width = Width;
        }

        protected override void OnResize(EventArgs e)
        {
            if (Parent == null || _width == (int)((IView)Parent).Width) return;
            Layout();
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
        public bool Equals(IToolbar other)
        {
            var toolbar = other as UI.Toolbar;
            return toolbar == null ? ReferenceEquals(this, other) : toolbar.Equals(this);
        }

        public Color BackgroundColor { get; set; }
        public Color ForegroundColor { get; set; }

        public IEnumerable<IToolbarItem> PrimaryItems { get; set; }
        public IEnumerable<IToolbarItem> SecondaryItems { get; set; }
    }
}