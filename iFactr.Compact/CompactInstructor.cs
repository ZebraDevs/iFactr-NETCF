using System.Linq;
using iFactr.UI;
using iFactr.UI.Controls;
using iFactr.UI.Instructions;

namespace iFactr.Compact
{
    class CompactInstructor : UniversalInstructor
    {
        protected override void OnLayout(ILayoutInstruction element)
        {
            base.OnLayout(element);
            var pairable = element as IPairable;
            var contentCell = pairable == null ? element as ContentCell : pairable.Pair as ContentCell ?? element as ContentCell;
            var headerCell = pairable == null ? element as HeaderedControlCell : pairable.Pair as HeaderedControlCell ?? element as HeaderedControlCell;
            if (contentCell != null)
            {
                if (!string.IsNullOrEmpty(contentCell.SubtextLabel.Text) && contentCell.MinHeight == Cell.StandardCellHeight)
                {
                    contentCell.MinHeight = 64;
                }

                if (contentCell.Image.Dimensions != new Size(1, 1) && contentCell.Image.Dimensions != Size.Empty)
                {
                    var margin = contentCell.Image.Margin;
                    if (margin.Right < Thickness.LeftMargin)
                    {
                        margin.Right = Thickness.LeftMargin;
                        contentCell.Image.Margin = margin;
                    }
                }
            }
            if (headerCell != null)
            {
                OnLayoutHeaderedCell(headerCell);
            }
        }

        // if controls.count < 3 && all are image or switch then set beside
        private void OnLayoutHeaderedCell(HeaderedControlCell cell)
        {
            var grid = ((IPairable)cell).Pair as IGridBase;
            if (grid == null)
                return;
            var controls = grid.Children.Where(c => c != cell.Header).ToList();

            var first = controls.FirstOrDefault();
            if (controls.Count == 0 || controls.Count > 2 || !(first is ISwitch) && !(first is IImage)) return;

            grid.Columns.Clear();
            grid.Rows.Clear();
            grid.Columns.Add(Column.OneStar);
            grid.Columns.Add(Column.AutoSized);

            grid.Rows.Add(Row.AutoSized);
            grid.Rows.Add(Row.AutoSized);

            if (!string.IsNullOrEmpty(cell.Header.Text))
            {
                cell.Header.Font = Font.PreferredHeaderFont.Size > 0 ? Font.PreferredHeaderFont : Font.PreferredLabelFont;
                cell.Header.Lines = 1;
                cell.Header.VerticalAlignment = VerticalAlignment.Center;
                cell.Header.HorizontalAlignment = HorizontalAlignment.Left;
                cell.Header.RowIndex = 0;
                cell.Header.ColumnIndex = 0;
                first.Margin = new Thickness(Thickness.LargeHorizontalSpacing, 0, 0, 0);
            }
            else first.Margin = new Thickness();

            first.VerticalAlignment = VerticalAlignment.Center;
            first.RowIndex = 0;
            first.ColumnIndex = 1;

            if (string.IsNullOrEmpty(cell.Header.Text))
            {
                grid.Columns[2] = new Column(0, LayoutUnitType.Absolute);
            }
        }
    }
}