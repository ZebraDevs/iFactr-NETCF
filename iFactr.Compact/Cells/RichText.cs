using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using iFactr.Core.Controls;
using iFactr.Core.Layers;
using MonoCross;
using MonoCross.Utilities;
using iFactr.UI;
using System.ComponentModel;

namespace iFactr.Compact
{
    public class RichText : WebBrowser, IRichContentCell, INotifyPropertyChanged
    {
        private static int _richCellCount;
        private readonly string id;

        public RichText()
        {
            id = string.Format("richcell{0}.html", _richCellCount++);
        }

        protected override void OnNavigating(WebBrowserNavigatingEventArgs e)
        {
            if (e.Url.OriginalString.Contains("#height="))
            {
                Height = (int)((int.Parse(e.Url.OriginalString.Substring(e.Url.OriginalString.LastIndexOf('=') + 1)) + 8) * CompactFactory.Instance.DpiScale);
                if (Parent != null) ((SmoothListbox)View).LayoutItems(this);
                e.Cancel = true;
            }
            else base.OnNavigating(e);
        }

        #region IRichContentCell Members

        public Color ForegroundColor
        {
            get;
            set;
        }

        public void Load()
        {
            var sb = new StringBuilder();
            string path = Device.ApplicationPath.AppendPath(id);
            const string script = "onload='window.location.href=\"#height=\"+document.getElementById(\"page\").offsetHeight'";
            string text;
            if (Items.Any())
            {
                for (int index = 0; index < Items.Count; index++)
                {
                    var item = Items[index];
                    string frag;
                    var icon = item as Icon;
                    if (icon != null)
                    {
                        if (icon.Location.StartsWith("data"))
                        {
                            var i = new BitmapImage(icon.Location);
                            string ext = i.Format == ImageFileFormat.PNG ? "png" : "jpg";
                            var location = string.Format("{0}.{1}.{2}", id, index, ext);
                            i.Save(Device.ApplicationPath.AppendPath(location), i.Format);
                            icon.Location = location;
                        }
                        frag = item.GetHtml();
                        frag = frag.Insert(frag.Length - 4, script);
                    }
                    else frag = item.GetHtml();
                    sb.Append(frag);
                }
                text = sb.ToString();
            }
            else
            {
                text = _text;
            }

            string fore = ";color:#" + (ForegroundColor.IsDefaultColor ? string.Empty : ForegroundColor.HexCode.Substring(3));
            string back = ";background-color:#" + (BackgroundColor.IsDefaultColor ? string.Empty : BackgroundColor.HexCode.Substring(3));
            string html = string.Format("<html><body style='overflow-y:hidden{0}{1}'{4}><div id=\"page\"><font face='{3}'>{2}</div></font></body></html>", fore, back, text, UI.Font.PreferredLabelFont.Name, script);
            Device.File.Save(path, html);

            Navigate(new Uri("file://" + path.Replace('\\', '/')));
            var prop = PropertyChanged;
            if (prop != null) prop(this, new PropertyChangedEventArgs("DocumentText"));
        }

        #endregion

        #region ICell Members

        public Color BackgroundColor
        {
            get;
            set;
        }

        public double MaxHeight
        {
            get;
            set;
        }

        public double MinHeight
        {
            get;
            set;
        }

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

        public MetadataCollection Metadata
        {
            get { return _metadata ?? (_metadata = new MetadataCollection()); }
        }
        private MetadataCollection _metadata;

        #endregion

        #region IEquatable<ICell> Members

        public bool Equals(ICell other)
        {
            var control = other as Cell;
            if (control != null)
            {
                return control.Equals(this);
            }

            return base.Equals(other);
        }
        #endregion

        #region IHtmlText Members

        public List<PanelItem> Items
        {
            get;
            set;
        }

        string IHtmlText.Text
        {
            get
            {
                return _text;
            }
            set
            {
                _text = value;
                var prop = PropertyChanged;
                if (prop != null)
                {
                    prop(this, new PropertyChangedEventArgs("Text"));
                }
            }
        }
        private string _text;

        public IListView View { get; set; }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}