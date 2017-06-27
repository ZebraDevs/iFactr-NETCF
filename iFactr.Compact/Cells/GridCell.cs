using System;
using System.Drawing;
using System.Linq;
using iFactr.Core;
using iFactr.UI;
using iFactr.UI.Controls;
using iFactr.UI.Instructions;
using MonoCross.Navigation;
using Color = iFactr.UI.Color;
using Point = System.Drawing.Point;

namespace iFactr.Compact
{
    public class GridCell : GridControl, IGridCell, ILayoutInstruction
    {
        public GridCell()
        {
            Height = (int)_maxHeight;
        }

        public override double MinWidth
        {
            get { return Parent.Width; }
            set { }
        }

        public override double MaxWidth
        {
            get { return Parent.Width; }
            set { }
        }

        public override double MaxHeight
        {
            get { return _maxHeight; }
            set { _maxHeight = value; }
        }
        private double _maxHeight = Cell.StandardCellHeight * CompactFactory.Instance.DpiScale;

        #region IGridCell Members

        public Link AccessoryLink
        {
            get { return _accessoryLink; }
            set
            {
                _accessoryLink = value;
                if (value == null)
                {
                    if (_accessory == null) return;
                    RemoveChild(_accessory);
                }
                else
                {

                    if (_accessory != null)
                    {
                        if (_accessory.Parent == null)
                        {
                            AddChild(_accessory);
                        }
                        return;
                    }
                    _accessory = new Accessory();
                    _accessory.Clicked += Accessory_Click;
                    AddChild(_accessory);
                }
            }
        }
        private Link _accessoryLink;
        private Accessory _accessory;

        public Link NavigationLink
        {
            get { return _navigationLink; }
            set
            {
                _navigationLink = value;
                OnPropertyChanged("NavigationLink");
            }
        }
        private Link _navigationLink;

        public Color SelectionColor
        {
            get { return _selectionColor.ToColor(); }
            set
            {
                _selectionColor = value.IsDefaultColor ? SystemColors.Highlight : value.ToColor();
            }
        }
        private System.Drawing.Color _selectionColor;

        public SelectionStyle SelectionStyle
        {
            get;
            set;
        }

        public event EventHandler Selected;

        void Accessory_Click(object sender, EventArgs e)
        {
            if (!this.RaiseEvent("AccessorySelected", EventArgs.Empty))
            {
                iApp.Navigate(AccessoryLink, Parent.Parent as IMXView);
            }
        }
        public event EventHandler AccessorySelected;

        public void NullifyEvents()
        {
            Selected = null;
            AccessorySelected = null;
        }

        public void Select()
        {
            Highlight();

            var selected = Selected;
            if (selected != null)
            {
                selected(Pair ?? this, EventArgs.Empty);
            }
            else
            {
                CompactFactory.Navigate(NavigationLink, Parent.Parent as IMXView);
            }
        }

        public void Highlight()
        {
            if (Parent != null && Parent.Parent is SmoothListbox)
            {
                ((SmoothListbox)Parent.Parent).SelectedItem = this;
            }

            if (SelectionStyle < SelectionStyle.HighlightOnly)
            {
                if (this.GetChild<UI.Controls.TextBox>() != null ||
                   this.GetChild<UI.Controls.TextArea>() != null ||
                   this.GetChild<UI.Controls.PasswordBox>() != null)
                {
                    var selected = Selected;
                    if (selected != null)
                    {
                        selected(Pair ?? this, EventArgs.Empty);
                    }
                }

                return;
            }

            var code = SelectionColor.A == 0 ? null : SelectionColor.HexCode;
            foreach (var highlighted in Children.OfType<IPairable>()
                .Where(c => c is IHighlight || c.Pair is IHighlight)
                .Select(c => c is IHighlight ? (IHighlight)c : (IHighlight)c.Pair))
            {
                highlighted.Highlight = true;
            }
            BackgroundHexCode = code;
            Redraw();
        }

        public void Deselect()
        {
            var code = _backgroundColor.A == 0 ? null : _backgroundColor.HexCode;
            foreach (var highlighted in Children.OfType<IPairable>()
                .Where(c => c is IHighlight || c.Pair is IHighlight)
                .Select(c => c is IHighlight ? (IHighlight)c : (IHighlight)c.Pair))
            {
                highlighted.Highlight = false;
            }
            BackgroundHexCode = code;
            Redraw();
        }

        #endregion

        #region ICell Members

        public Color BackgroundColor
        {
            get { return _backgroundColor; }
            set
            {
                if (_backgroundColor == value) return;
                _backgroundColor = value;
                BackgroundHexCode = value.HexCode;
                OnPropertyChanged("BackgroundColor");
            }
        }
        private Color _backgroundColor;

        #endregion

        #region IPairable Members

        public IPairable Pair
        {
            get { return _pair; }
            set
            {
                if (_pair == null && value != null)
                {
                    _pair = value;
                    _pair.Pair = this;
                }
            }
        }
        private IPairable _pair;

        #endregion

        #region IEquatable<ICell> Members

        public bool Equals(ICell other)
        {
            var cell = other as Cell;
            if (cell != null)
            {
                return cell.Equals(this);
            }

            return base.Equals(other);
        }

        #endregion

        public void Layout()
        {
            var pair = Pair as ILayoutInstruction;
            if (pair != null)
            {
                pair.Layout();
            }

            if (_accessory == null) return;
            Columns.Add(new Column(1, LayoutUnitType.Auto));
            _accessory.ColumnIndex = Columns.Count - 1;
            _accessory.ColumnSpan = 1;
            _accessory.RowIndex = 0;
            _accessory.RowSpan = Rows.Count;
        }

        public void SelectControlAt(Point controlLocation)
        {
            if (Children.Select(c => c as System.Windows.Forms.Control ?? c.Pair as System.Windows.Forms.Control)
                .Where(control => control != null && control.Top <= controlLocation.Y && control.Bottom >= controlLocation.Y &&
                                  control.Left <= controlLocation.X && control.Right >= controlLocation.X)
                .Any(control => ((IPairable)control).RaiseEvent("Clicked", EventArgs.Empty)))
            {
                return;
            }
            if (Children.Select(c => c as IPaintable ?? c.Pair as IPaintable)
                .Where(control => control != null && control.Location.Y <= controlLocation.Y && control.Location.Y + control.Size.Height >= controlLocation.Y &&
                                  control.Location.X <= controlLocation.X && control.Location.X + control.Size.Width >= controlLocation.X)
                .Any(control => ((IPairable)control).RaiseEvent("Clicked", EventArgs.Empty)))
            {
                return;
            }
            Select();
        }
    }
}