using System;
using System.Collections.Generic;
using iFactr.Core;
using iFactr.Core.Layers;
using iFactr.UI;
using iFactr.UI.Controls;

namespace iFactr.Compact
{
    public class GridView : GridControl, IGridView
    {
        public override double MinWidth
        {
            get { return Parent.Width; }
            set { }
        }

        public override double MinHeight
        {
            get { return (Parent.Height - CompactFactory.TopPadding) / CompactFactory.Instance.DpiScale; }
            set { }
        }

        public override double MaxWidth
        {
            get { return HorizontalScrollingEnabled ? base.MaxWidth : Parent.Width; }
            set { base.MaxWidth = value; }
        }

        public override double MaxHeight
        {
            get { return VerticalScrollingEnabled ? base.MaxHeight : (Parent.Height - CompactFactory.TopPadding) / CompactFactory.Instance.DpiScale; }
            set { base.MaxHeight = value; }
        }

        public bool HorizontalScrollingEnabled { get; set; }

        public bool VerticalScrollingEnabled { get; set; }

        public IMenu Menu { get; set; }

        #region Submission

        public void Submit(string url)
        {
            Submit(new Link(url));
        }

        public void Submit(Link link)
        {
            var submitValues = (_model is iLayer) ? ((iLayer)_model).GetFieldValues() : GetSubmissionValues();

            var args = new SubmissionEventArgs(link, ValidationErrors);
            Submitting.Raise(this, args);

            if (args.Cancel)
                return;

            link.Parameters.AddRange(submitValues);
            CompactFactory.Navigate(link, this);
        }
        public event SubmissionEventHandler Submitting;

        public IDictionary<string, string> GetSubmissionValues()
        {
            return _submitValues;
        }

        private readonly Dictionary<string, string> _submitValues = new Dictionary<string, string>();

        public ValidationErrorCollection ValidationErrors { get; private set; }

        #endregion

        #region IHistoryEntry members

        public Link BackLink
        {
            get { return _backLink; }
            set
            {
                if (_backLink == value) return;
                _backLink = value;
                OnPropertyChanged("BackLink");
            }
        }
        private Link _backLink;

        public string StackID
        {
            get { return _stackID; }
            set
            {
                if (_stackID == value) return;
                _stackID = value;
                OnPropertyChanged("StackID");
            }
        }
        private string _stackID;

        public Pane OutputPane
        {
            get { return _outputPane; }
            set
            {
                if (_outputPane == value) return;
                _outputPane = value;
                OnPropertyChanged("OutputPane");
            }
        }
        private Pane _outputPane;

        public PopoverPresentationStyle PopoverPresentationStyle
        {
            get { return _popoverPresentationStyle; }
            set
            {
                if (_popoverPresentationStyle == value) return;
                _popoverPresentationStyle = value;
                OnPropertyChanged("PopoverPresentationStyle");
            }
        }
        private PopoverPresentationStyle _popoverPresentationStyle;

        public ShouldNavigateDelegate ShouldNavigate
        {
            get { return _shouldNavigate; }
            set
            {
                if (_shouldNavigate == value) return;
                _shouldNavigate = value;
                OnPropertyChanged("ShouldNavigate");
            }
        }
        private ShouldNavigateDelegate _shouldNavigate;

        public IHistoryStack Stack
        {
            get { return PaneManager.Instance.FromNavContext(OutputPane, PaneManager.Instance.CurrentTab); }
        }

        public event EventHandler Activated;
        public event EventHandler Deactivated;

        #endregion

        #region IMXView members

        private object _model;

        public virtual Type ModelType { get { return _model == null ? typeof(object) : _model.GetType(); } }

        public object GetModel()
        {
            return _model;
        }

        public virtual void SetModel(object model)
        {
            _model = model;
        }

        public virtual void Render()
        {
            Rendering.Raise(this, EventArgs.Empty);
            if (!VerticalScrollingEnabled || this.GetChild<ILabel>("Spacer") != null) return;

            AddChild(new Label
            {
                ID = "Spacer",
                Text = " ",
                ColumnIndex = 0,
                ColumnSpan = 2,
                RowIndex = Rows.Count,
                RowSpan = 1,
            });
            Rows.Add(Row.AutoSized);
        }
        public event EventHandler Rendering;

        #endregion

        #region IView members

        public Color HeaderColor
        {
            get { return _headerColor; }
            set
            {
                if (_headerColor == value) return;
                _headerColor = value;
                OnPropertyChanged("HeaderColor");
            }
        }
        private Color _headerColor;

        public PreferredOrientation PreferredOrientations
        {
            get { return _preferredOrientations; }
            set
            {
                if (_preferredOrientations == value) return;
                _preferredOrientations = value;
                OnPropertyChanged("PreferredOrientations");
            }
        }
        private PreferredOrientation _preferredOrientations;

        public string Title
        {
            get { return _title; }
            set
            {
                if (_title == value) return;
                _title = value;
                OnPropertyChanged("Title");
            }
        }
        private string _title;

        public Color TitleColor
        {
            get { return _titleColor; }
            set
            {
                if (_titleColor == value) return;
                _titleColor = value;
                OnPropertyChanged("TitleColor");
            }
        }
        private Color _titleColor;

        public new double Width { get { return base.Width; } }

        public new double Height { get { return base.Height; } }

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
            return _pair == null ? other == null : Pair.Equals(other.Pair);
        }

        #endregion
    }
}