using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using iFactr.Core.Forms;
using iFactr.UI;
using iFactr.UI.Controls;
using Microsoft.WindowsCE.Forms;
using HorizontalAlignment = iFactr.UI.HorizontalAlignment;
using Point = System.Drawing.Point;

namespace iFactr.Compact
{
    public class SelectList : ComboBox, ISelectList, INotifyPropertyChanged
    {
        public SelectList()
        {
            ColumnIndex = -1;
            RowIndex = -1;
            ColumnSpan = 1;
            RowSpan = 1;
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            var p = Parent as GridCell;
            if (p != null) p.Highlight();
        }

        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            base.OnSelectedIndexChanged(e);

            var grid = Parent as GridControl;
            if (grid != null)
            {
                var item = SelectedItem as SelectListFieldItem;
                if (item == null)
                {
                    grid.SetSubmission(SubmitKey, StringValue);
                }
                else
                {
                    grid.SetSubmission(SubmitKey + ".Key", item.Key);
                    grid.SetSubmission(SubmitKey, item.Value);
                }
            }

            var handler = SelectionChanged;
            if (handler != null)
            {
                handler(Pair ?? this, new ValueChangedEventArgs<object>(_oldItem, SelectedItem));
                _oldItem = SelectedItem;
            }
            OnPropertyChanged("SelectedIndex");
            OnPropertyChanged("StringValue");
            OnPropertyChanged("SelectedItem");
        }

        public new IEnumerable Items
        {
            get { return base.Items; }
            set
            {
                if (base.Items.Cast<object>().Equivalent(value.Cast<object>(), true))
                {
                    return;
                }

                base.Items.Clear();
                foreach (var item in value)
                {
                    base.Items.Add(item);
                }
                SelectedIndex = Math.Max(SelectedIndex, 0);
                OnPropertyChanged("Items");
            }
        }
        public event ValueChangedEventHandler<object> SelectionChanged;
        private object _oldItem;

        public new int SelectedIndex
        {
            get { return base.SelectedIndex; }
            set
            {
                if (value == SelectedIndex) return;
                if (value == -1)
                    value = 0;
                _oldItem = SelectedItem;
                base.SelectedIndex = value;
            }
        }

        public new object SelectedItem
        {
            get { return SelectedIndex > -1 && SelectedIndex < Items.Count() ? Items.ElementAt(SelectedIndex) : null; }
            set
            {
                var index = Items.Count() > 0 ? Items.IndexOf(value) : SelectedIndex;
                if (index == SelectedIndex) return;
                if (index == -1) index = 0;
                _oldItem = SelectedItem;
                SelectedIndex = index;
            }
        }

        public void ShowList()
        {
            var msg = Message.Create(Handle, ComboBoxShowDropDown, (IntPtr)1, IntPtr.Zero);
            MessageWindow.SendMessage(ref msg);
        }

        public void HideList()
        {
            var msg = Message.Create(Handle, ComboBoxShowDropDown, (IntPtr)0, IntPtr.Zero);
            MessageWindow.SendMessage(ref msg);
        }

        private const int ComboBoxShowDropDown = 0x14F;

        public Visibility Visibility
        {
            get { return _visibility; }
            set
            {
                if (_visibility == value) return;
                _visibility = value;
                Visible = value == Visibility.Visible;
                OnPropertyChanged("Visibility");
            }
        }
        private Visibility _visibility;

        #region ISelectList Members

        public Color BackgroundColor
        {
            get { return _backgroundColor; }
            set
            {
                if (_backgroundColor == value) return;
                _backgroundColor = value;
                if (!_backgroundColor.IsDefaultColor)
                {
                    BackColor = _backgroundColor.ToColor();
                }
                OnPropertyChanged("BackgroundColor");
            }
        }
        private Color _backgroundColor;

        public new Font Font
        {
            get { return _font; }
            set
            {
                if (_font == value) return;
                _font = value;
                base.Font = _font.ToFont();
                OnPropertyChanged("Font");
            }
        }
        private Font _font;

        public Color ForegroundColor
        {
            get { return _foregroundColor; }
            set
            {
                if (_foregroundColor == value) return;
                _foregroundColor = value;
                if (!_foregroundColor.IsDefaultColor)
                {
                    ForeColor = _foregroundColor.ToColor();
                }
                OnPropertyChanged("ForegroundColor");
            }
        }

        private Color _foregroundColor;

        #endregion

        #region IControl Members

        public bool IsEnabled
        {
            get { return Enabled; }
            set
            {
                if (Enabled == value) return;
                Enabled = value;
                OnPropertyChanged("IsEnabled");
            }
        }

        public string StringValue
        {
            get { return Text; }
        }

        public string SubmitKey
        {
            get;
            set;
        }

        public new event ValidationEventHandler Validating;

        public void NullifyEvents()
        {
            Validating = null;
            SelectionChanged = null;
        }

        public bool Validate(out string[] errors)
        {
            var handler = Validating;
            if (handler != null)
            {
                var args = new ValidationEventArgs(SubmitKey, Text, StringValue);
                handler(Pair ?? this, args);

                if (args.Errors.Count > 0)
                {
                    errors = new string[args.Errors.Count];
                    args.Errors.CopyTo(errors, 0);
                    return false;
                }
            }

            errors = null;
            return true;
        }

        #endregion

        #region IElement Members

        public int ColumnIndex
        {
            get;
            set;
        }

        public int ColumnSpan
        {
            get;
            set;
        }

        public HorizontalAlignment HorizontalAlignment
        {
            get;
            set;
        }

        public string ID
        {
            get;
            set;
        }

        public Thickness Margin
        {
            get;
            set;
        }

        public new object Parent
        {
            get { return base.Parent; ; }
        }

        public int RowIndex
        {
            get;
            set;
        }

        public int RowSpan
        {
            get;
            set;
        }

        public VerticalAlignment VerticalAlignment
        {
            get;
            set;
        }

        public Size Measure(Size constraints)
        {
            return new Size(Math.Min(Width, constraints.Width), Math.Min(Height, constraints.Height));
        }

        public void SetLocation(UI.Point location, Size size)
        {
            Width = (int)size.Width;
            Height = (int)size.Height;
            Location = new Point((int)location.X, (int)location.Y);
        }

        #endregion

        #region IPairable Members

        public IPairable Pair
        {
            get;
            set;
        }

        public MetadataCollection Metadata
        {
            get { return _metadata ?? (_metadata = new MetadataCollection()); }
        }
        private MetadataCollection _metadata;

        #endregion

        #region IEquatable<IElement> Members

        public bool Equals(IElement other)
        {
            var control = other as Element;
            return control == null ? ReferenceEquals(this, other) : control.Equals(this);
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            var prop = PropertyChanged;
            if (prop != null) prop(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
