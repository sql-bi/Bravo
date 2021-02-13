using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Sqlbi.Bravo.UI.Controls
{
    // Based on code from https://pascallaurin42.blogspot.com/2013/12/implementing-treemap-in-c.html
    public class TreeMapGrid : Grid
    {
        const double MinSliceRatio = 0.35;

        public void DrawTree(IEnumerable<ITreeMapInfo> data)
            => DrawTree(data, Convert.ToInt32(Width), Convert.ToInt32(Height));

        public void DrawTree(IEnumerable<ITreeMapInfo> data, int width, int height)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (width < 1)
            {
                throw new ArgumentException("Width must be a positive integer.");
            }

            if (height < 1)
            {
                throw new ArgumentException("Height must be a positive integer.");
            }

            var wrappedData = data.Select(x => new Element<ITreeMapInfo> { Object = x, Value = x.Size })
                                  .OrderByDescending(x => x.Value)
                                  .ToList();

            var slice = GetSlice(wrappedData, 1, MinSliceRatio);

            var rectangles = GetRectangles(slice, width, height).ToList();

            Children.Clear();
            RowDefinitions.Clear();
            ColumnDefinitions.Clear();

            for (var i = 0; i < height; i++)
            {
                RowDefinitions.Add(new RowDefinition());
            }

            for (var i = 0; i < width; i++)
            {
                ColumnDefinitions.Add(new ColumnDefinition());
            }

            foreach (var r in rectangles)
            {
                if (r.Width > 1 && r.Height > 1)
                {
                    var border = new Border();

                    var origItem = (r.Slice.Elements.First() as Element<ITreeMapInfo>).Object;
                    var baseColor = origItem.RectangleColor;

                    if (r.Width > 10 && r.Height > 10)
                    {
                        border.BorderThickness = new Thickness(2);
                        border.BorderBrush = new SolidColorBrush(baseColor);
                    }
                    else if (r.Width > 5 && r.Height > 5)
                    {
                        border.BorderThickness = new Thickness(1);
                        border.BorderBrush = new SolidColorBrush(baseColor);
                    }
                    else
                    {
                        border.BorderThickness = new Thickness(0);
                    }

                    border.Background = new SolidColorBrush(baseColor);
                    border.ToolTip = origItem.ToolTipText;

                    var rect = new Rectangle
                    {
                        Fill = new SolidColorBrush(Color.FromArgb(128, 255, 255, 255)),
                        Stretch = Stretch.Fill
                    };

                    var selectedBinding = new Binding(nameof(ITreeMapInfo.OverlayVisibility))
                    {
                        Source = origItem
                    };

                    rect.SetBinding(Rectangle.VisibilityProperty, selectedBinding);

                    border.Child = rect;

                    Grid.SetColumn(border, Convert.ToInt32(r.X));
                    Grid.SetRow(border, Convert.ToInt32(r.Y));
                    Grid.SetRowSpan(border, Convert.ToInt32(r.Height - 1));
                    Grid.SetColumnSpan(border, Convert.ToInt32(r.Width - 1));

                    Children.Add(border);
                }
            }
        }

        private Slice<T> GetSlice<T>(IEnumerable<Element<T>> elements, double totalSize, double sliceWidth)
        {
            if (!elements.Any()) return null;

            if (elements.Count() == 1)
            {
                return new Slice<T>
                {
                    Elements = elements,
                    Size = totalSize
                };
            }

            var sliceResult = GetElementsForSlice(elements, sliceWidth);

            return new Slice<T>
            {
                Elements = elements,
                Size = totalSize,
                SubSlices = new[]
                {
                    GetSlice(sliceResult.Elements, sliceResult.ElementsSize, sliceWidth),
                    GetSlice(sliceResult.RemainingElements, 1 - sliceResult.ElementsSize, sliceWidth)
                }
            };
        }

        private SliceResult<T> GetElementsForSlice<T>(IEnumerable<Element<T>> elements, double sliceWidth)
        {
            var elementsInSlice = new List<Element<T>>();
            var remainingElements = new List<Element<T>>();
            double current = 0;
            double total = elements.Sum(x => x.Value);

            foreach (var element in elements)
            {
                if (current > sliceWidth)
                {
                    remainingElements.Add(element);
                }
                else
                {
                    elementsInSlice.Add(element);
                    current += element.Value / total;
                }
            }

            return new SliceResult<T>
            {
                Elements = elementsInSlice,
                ElementsSize = current,
                RemainingElements = remainingElements
            };
        }

        private IEnumerable<SliceRectangle<T>> GetRectangles<T>(Slice<T> slice, int width, int height)
        {
            var area = new SliceRectangle<T>
            { Slice = slice, Width = width, Height = height };

            foreach (var rect in GetRectangles(area))
            {
                // Make sure no rectangle go outside the original area
                if (rect.X + rect.Width > area.Width) rect.Width = area.Width - rect.X;
                if (rect.Y + rect.Height > area.Height) rect.Height = area.Height - rect.Y;

                yield return rect;
            }
        }

        private IEnumerable<SliceRectangle<T>> GetRectangles<T>(SliceRectangle<T> sliceRectangle)
        {
            var isHorizontalSplit = sliceRectangle.Width >= sliceRectangle.Height;
            var currentPos = 0;
            foreach (var subSlice in sliceRectangle.Slice.SubSlices)
            {
                var subRect = new SliceRectangle<T> { Slice = subSlice };
                int rectSize;

                if (isHorizontalSplit)
                {
                    rectSize = (int)Math.Round(sliceRectangle.Width * subSlice.Size);
                    subRect.X = sliceRectangle.X + currentPos;
                    subRect.Y = sliceRectangle.Y;
                    subRect.Width = rectSize;
                    subRect.Height = sliceRectangle.Height;
                }
                else
                {
                    rectSize = (int)Math.Round(sliceRectangle.Height * subSlice.Size);
                    subRect.X = sliceRectangle.X;
                    subRect.Y = sliceRectangle.Y + currentPos;
                    subRect.Width = sliceRectangle.Width;
                    subRect.Height = rectSize;
                }

                currentPos += rectSize;

                if (subSlice.Elements.Count() > 1)
                {
                    foreach (var sr in GetRectangles(subRect))
                        yield return sr;
                }
                else if (subSlice.Elements.Count() == 1)
                {
                    yield return subRect;
                }
            }
        }

        internal class Element<T>
        {
            public T Object { get; set; }
            public double Value { get; set; }
        }

        internal class Slice<T>
        {
            public double Size { get; set; }
            public IEnumerable<Element<T>> Elements { get; set; }
            public IEnumerable<Slice<T>> SubSlices { get; set; }
        }

        internal class SliceRectangle<T>
        {
            public Slice<T> Slice { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
        }

        internal class SliceResult<T>
        {
            public IEnumerable<Element<T>> Elements { get; set; }
            public double ElementsSize { get; set; }
            public IEnumerable<Element<T>> RemainingElements { get; set; }
        }
    }
}
