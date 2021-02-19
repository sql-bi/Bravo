using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Sqlbi.Bravo.UI.Controls
{
    // Based on code from https://blog.allinsight.net/wpf-how-to-extend-the-shape-class-to-draw-a-pie-chart-or-a-part-of-a-circle/
    public class SimplePie : Shape
    {
        public double Angle
        {
            get => (double)GetValue(AngleProperty);
            set => SetValue(AngleProperty, value);
        }

        public static readonly DependencyProperty AngleProperty =
            DependencyProperty.Register(nameof(Angle), typeof(double), typeof(SimplePie), new PropertyMetadata(0.0, OnSimplePiePropertyChanged));

        private static void OnSimplePiePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SimplePie element)
            {
                // Force redraw
                element.UpdateLayout();
                element.InvalidateArrange();
                element.InvalidateMeasure();
                element.InvalidateVisual();
            }
        }

        // These could be made dependency properties but not necessary for our purposes
        public double CentreX { get; set; }
        public double CentreY { get; set; }
        public double Radius { get; set; }
        public double Rotation { get; set; }

        public static Point ComputeCartesianCoordinate(double angle, double radius)
        {
            var angleRadians = Math.PI / 180.0 * (angle - 90);

            var x = radius * Math.Cos(angleRadians);
            var y = radius * Math.Sin(angleRadians);

            return new Point(x, y);
        }

        protected override Geometry DefiningGeometry
        {
            get
            {
                var geometry = new StreamGeometry { FillRule = FillRule.EvenOdd };

                using (var context = geometry.Open())
                {
                    DrawGeometry(context);
                }

                geometry.Freeze();

                return geometry;
            }
        }

        private void DrawGeometry(StreamGeometryContext context)
        {
            var startPoint = new Point(CentreX, CentreY);

            var outerArcStartPoint = ComputeCartesianCoordinate(Rotation, Radius);
            outerArcStartPoint.Offset(CentreX, CentreY);

            var outerArcEndPoint = ComputeCartesianCoordinate(Rotation + Angle, Radius);
            outerArcEndPoint.Offset(CentreX, CentreY);

            var isLargeArc = Angle > 180.0;
            var outerArcSize = new Size(Radius, Radius);

            context.BeginFigure(startPoint, true, true);
            context.LineTo(outerArcStartPoint, true, true);
            context.ArcTo(outerArcEndPoint, outerArcSize, 0, isLargeArc, SweepDirection.Clockwise, true, true);
        }
    }

}
