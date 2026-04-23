using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace VideoManager
{
    public class MasonryPanel : Panel
    {
        public static readonly DependencyProperty ColumnWidthProperty = DependencyProperty.Register(
            "ColumnWidth", typeof(double), typeof(MasonryPanel),
            new FrameworkPropertyMetadata(200.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public double ColumnWidth
        {
            get => (double)GetValue(ColumnWidthProperty);
            set => SetValue(ColumnWidthProperty, value);
        }

        public static readonly DependencyProperty ColumnSpanProperty = DependencyProperty.RegisterAttached(
            "ColumnSpan", typeof(int), typeof(MasonryPanel),
            new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        public static int GetColumnSpan(UIElement element) => (int)element.GetValue(ColumnSpanProperty);
        public static void SetColumnSpan(UIElement element, int value) => element.SetValue(ColumnSpanProperty, value);

        protected override Size MeasureOverride(Size availableSize)
        {
            if (InternalChildren.Count == 0) return new Size(0, 0);

            double colWidth = ColumnWidth;
            int numColumns = Math.Max(1, (int)(availableSize.Width / colWidth));
            if (double.IsInfinity(availableSize.Width)) numColumns = 1;

            double[] colHeights = new double[numColumns];

            foreach (UIElement child in InternalChildren)
            {
                int span = Math.Max(1, Math.Min(GetColumnSpan(child), numColumns));
                double childWidth = span * colWidth;
                child.Measure(new Size(childWidth, double.PositiveInfinity));

                int minCol = 0;
                double minMaxHeight = double.MaxValue;

                for (int i = 0; i <= numColumns - span; i++)
                {
                    double currentMax = 0;
                    for (int s = 0; s < span; s++)
                    {
                        if (colHeights[i + s] > currentMax) currentMax = colHeights[i + s];
                    }
                    if (currentMax < minMaxHeight)
                    {
                        minMaxHeight = currentMax;
                        minCol = i;
                    }
                }

                for (int s = 0; s < span; s++)
                {
                    colHeights[minCol + s] = minMaxHeight + child.DesiredSize.Height;
                }
            }

            return new Size(double.IsInfinity(availableSize.Width) ? colWidth : availableSize.Width, colHeights.Max());
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (InternalChildren.Count == 0) return finalSize;

            double colWidth = ColumnWidth;
            int numColumns = Math.Max(1, (int)(finalSize.Width / colWidth));
            double[] colHeights = new double[numColumns];
            double actualColWidth = finalSize.Width / numColumns;

            foreach (UIElement child in InternalChildren)
            {
                int span = Math.Max(1, Math.Min(GetColumnSpan(child), numColumns));
                
                int minCol = 0;
                double minMaxHeight = double.MaxValue;

                for (int i = 0; i <= numColumns - span; i++)
                {
                    double currentMax = 0;
                    for (int s = 0; s < span; s++)
                    {
                        if (colHeights[i + s] > currentMax) currentMax = colHeights[i + s];
                    }
                    if (currentMax < minMaxHeight)
                    {
                        minMaxHeight = currentMax;
                        minCol = i;
                    }
                }
                
                double x = minCol * actualColWidth;
                double y = minMaxHeight;
                
                child.Arrange(new Rect(x, y, span * actualColWidth, child.DesiredSize.Height));
                
                for (int s = 0; s < span; s++)
                {
                    colHeights[minCol + s] = minMaxHeight + child.DesiredSize.Height;
                }
            }

            return finalSize;
        }
    }
}
