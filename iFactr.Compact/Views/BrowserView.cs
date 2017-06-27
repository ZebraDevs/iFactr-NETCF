using System;
using System.Diagnostics;
using System.Windows.Forms;
using iFactr.Core;
using iFactr.Core.Layers;
using MonoCross.Utilities;
using iFactr.UI;
using Link = iFactr.UI.Link;

namespace iFactr.Compact
{
    class BrowserView : WebBrowser, IBrowserView
    {
        private object _model;

        public Type ModelType
        {
            get { return _model == null ? typeof(Browser) : _model.GetType(); }
        }

        public object GetModel()
        {
            return _model;
        }


        public void SetModel(object model)
        {
            _model = model;
        }

        public void Render()
        {
            var render = Rendering;
            if (render != null)
            {
                render(this, EventArgs.Empty);
            }
        }

        public IPairable Pair
        {
            get { return _pair; }
            set
            {
                if (_pair != null) return;
                _pair = value;
                _pair.Pair = this;
            }
        }
        private IPairable _pair;

        public bool Equals(IView other)
        {
            return Pair == null ? other == null : Pair.Equals(other.Pair);
        }

        public Color HeaderColor
        {
            get;
            set;
        }

        public new double Height
        {
            get { return base.Height; }
        }

        public PreferredOrientation PreferredOrientations { get; set; }

        public string Title { get; set; }

        public Color TitleColor { get; set; }

        public new double Width
        {
            get { return base.Width; }
        }

        public event EventHandler<LoadFinishedEventArgs> LoadFinished;
        public event EventHandler Rendering;

        public void SetBackground(Color color)
        {
        }

        public void SetBackground(string imagePath, ContentStretch stretch)
        {
        }

        public Link BackLink
        {
            get;
            set;
        }

        public string StackID
        {
            get;
            set;
        }

        public Pane OutputPane
        {
            get;
            set;
        }

        public PopoverPresentationStyle PopoverPresentationStyle { get; set; }

        public ShouldNavigateDelegate ShouldNavigate { get; set; }

        public IHistoryStack Stack
        {
            get { return PaneManager.Instance.FromNavContext(PaneManager.Instance.TopmostPane); }
        }

        public MetadataCollection Metadata
        {
            get { return _metadata ?? (_metadata = new MetadataCollection()); }
        }
        private MetadataCollection _metadata;

        public event EventHandler Activated;
        public event EventHandler Deactivated;


        public bool EnableDefaultControls
        {
            get;
            set;
        }

        public IMenu Menu
        {
            get;
            set;
        }

        public new void GoBack()
        {
            base.GoBack();
        }

        public new void GoForward()
        {
            base.GoForward();
        }

        public void LaunchExternal(string url)
        {
            if (url.StartsWith("/"))
                url = "file://" + url;
            Process.Start(url, null);
        }

        public void Load(string url)
        {
            if (url.StartsWith("/"))
                url = "file://" + url;

            try
            {
                Navigate(new Uri(url));
            }
            catch (UriFormatException)
            {
                var block = new iBlock
                {
                    Items =
                    {
                        new iFactr.Core.Controls.Label { Text = Device.Resources.GetString("FailedNavigation"), },
                    }
                };
                LoadFromString(block.Text);
            }
        }

        public void LoadFromString(string html)
        {
            string path = Device.ApplicationPath.AppendPath("browser.html");
            Device.File.Save(path, html);
            Navigate(new Uri("file://" + path.Replace('\\', '/')));
        }

        protected override void OnNavigated(WebBrowserNavigatedEventArgs e)
        {
            var loadFinished = LoadFinished;
            if (loadFinished != null) loadFinished(Pair ?? this, new LoadFinishedEventArgs(e.Url.OriginalString));
            base.OnNavigated(e);
        }
    }
}