using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using iFactr.Core;
using iFactr.Core.Layers;
using iFactr.UI;
using MonoCross.Navigation;

namespace iFactr.Compact
{
    public class HistoryStack : IHistoryStack
    {
        #region Portable IHistoryStack members

        public string ID
        {
            get { return Context.ToString(); }
        }

        /// <summary>
        /// Gets the history stack's context.
        /// </summary>
        public iApp.AppNavigationContext Context
        {
            get;
            set;
        }

        public IEnumerable<IMXView> Views
        {
            get { return _views; }
        }
        private readonly List<IMXView> _views = new List<IMXView>();

        /// <summary>
        /// The view currently onscreen.
        /// </summary>
        public IMXView CurrentView
        {
            get { return _views.LastOrDefault(); }
        }

        /// <summary>
        /// The layer currently onscreen.
        /// </summary>
        public iLayer CurrentLayer
        {
            get
            {
                var view = CurrentView;
                return view == null ? null : view.GetModel() as iLayer;
            }
        }

        public void InsertView(int index, IMXView view)
        {
            if (index > Views.Count() - 1)
            {
                PushView(view);
            }
            else
            {
                _views.Insert(index, view);
            }
        }

        public IMXView PopView()
        {
            if (_views.Count > 1)
                return PopToView(_views[_views.Count - 2]).FirstOrDefault();
            if (Context.ActivePane == Pane.Master)
                CompactFactory.Instance.RootForm.Close();
            else
                PopToRoot();
            return null;
        }

        #endregion

        #region Fragment IHistoryStack members

        public IMXView[] PopToRoot()
        {
            if (Context.ActivePane == Pane.Popover)
            {
                var views = Views.ToArray();
                foreach (var view in views)
                {
                    _views.Remove(view);
                    var d = view is IListView ? CompactFactory.GetNativeObject<SmoothListbox>(view, "view") : null;
                    if (d != null)
                    {
                        d.Parent = null;
                        d.animationTimer.Enabled = false;
                        d.Dispose();
                    }
                }
                if (views.Any())
                {
                    var master = (HistoryStack)PaneManager.Instance.FromNavContext(Pane.Master, PaneManager.Instance.CurrentTab);
                    master.Align(NavigationType.Back);
                }
                return views;
            }

            var root = Views.FirstOrDefault();
            return PopToView(root);
        }

        public IMXView[] PopToView(IMXView view)
        {
            if (view == null) return null;
            if (!Views.Contains(view))
            {
                throw new ArgumentException();
            }

            var removed = new List<IMXView>();
            var layer = CurrentLayer;

            // Perform pop on Views collection
            IMXView current;
            while (!Equals(current = _views.LastOrDefault(), view))
            {
                _views.Remove(current);
                var d = current is IListView ? CompactFactory.GetNativeObject<SmoothListbox>(current, "view") : null;
                if (d != null)
                {
                    d.Parent = null;
                    d.animationTimer.Enabled = false;
                    d.Dispose();
                }
                removed.Add(current);
            }

            if (removed.Count <= 0)
            {
                return removed.ToArray();
            }

            Align(NavigationType.Back);
            if (layer != null)
            {
                layer.Unload();
            }
            var iview = removed.First() as IView;
            iview.RaiseEvent("Deactivated", EventArgs.Empty);
            return removed.ToArray();
        }

        public void PushView(IMXView view)
        {
            _views.Add(view);
            Align(NavigationType.Forward);
        }

        public void ReplaceView(IMXView currentView, IMXView newView)
        {
            var i = _views.IndexOf(currentView);
            var oldView = _views[i];
            _views[i] = newView;
            if (i < _views.Count - 1)
                return;
            var layer = oldView.GetModel() as iLayer;
            var view = oldView as IView;
            if (layer != null)
                layer.Unload();
            if (view != null)
                view.RaiseEvent("Deactivated", EventArgs.Empty);
            Align(NavigationType.Forward);
        }

        /// <summary>
        /// Syncronize the rendered stack with the <see cref="Views"/> collection
        /// </summary>
        public void Align(NavigationType navType)
        {
            var view = _views.LastOrDefault();
            if (view == null) return;
            var monoView = view as IView;
            if (monoView != null && monoView.Pair is Control)
            {
                view = CompactFactory.GetNativeObject<Control>(monoView, "view") as IMXView;
            }

            if (!(view is Control))
            {
                _views.Remove(view);
                return;
            }

            ((RootForm)CompactFactory.Instance.RootForm).Attach(view, this);
        }

        #endregion

        #region Obsolete IHistoryStack members

        /// <summary>
        /// Clears the history stack through the given layer.
        /// </summary>
        [Obsolete("Use PopToView instead.")]
        public void PopToLayer(iLayer layer)
        {
            var view = _views.FirstOrDefault(v => v.GetModel().Equals(layer));
            if (view != null) PopToView(view);
        }

        /// <summary>
        /// Gets the last layer pushed onto the history stack.
        /// </summary>
        /// <returns>The <see cref="IMXView"/> on the top of the history stack.</returns>
        /// <remarks>This can be used to get information about the previous Layer.</remarks>
        [Obsolete("Use Views instead.")]
        public iLayer Peek()
        {
            return History.LastOrDefault();
        }

        /// <summary>
        /// Pushes the <see cref="IHistoryStack.CurrentView"/> onto the History to make way for another view.
        /// </summary>
        /// <remarks>If the CurrentDisplay is associated with a LoginLayer, it will not be pushed to the stack history.</remarks>
        [Obsolete]
        public void PushCurrent()
        {
        }

        /// <summary>
        /// Clears the history and current display.
        /// </summary>
        /// <remarks>If this is a popover stack, the popover is closed. If this is a detail stack, it will show the vanity panel.</remarks>
        [Obsolete("Use PopToRoot instead.")]
        public void Clear(iLayer layer)
        {
            if (layer == null)
            {
                PopToRoot();
            }
            else
            {
                PopToLayer(layer);
            }
        }

        /// <summary>
        /// A stack of layers that used to be in the pane.
        /// </summary>
        [Obsolete("Use Views instead.")]
        public IEnumerable<iLayer> History
        {
            get { return _views.Select(v => v.GetModel() as iLayer).Take(_views.Count - 1); }
        }

        #endregion
    }
}