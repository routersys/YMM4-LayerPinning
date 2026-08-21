using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace LayerPinning
{
    internal sealed class PinnedLayerAdorner : Adorner
    {
        private const double SeparatorThickness = 1.0;

        private static readonly DependencyProperty[] inheritedProperties =
        [
            TextElement.ForegroundProperty,
            TextElement.FontFamilyProperty,
            TextElement.FontSizeProperty,
            TextElement.FontStyleProperty,
            TextElement.FontWeightProperty,
            TextElement.FontStretchProperty,
            FrameworkElement.FlowDirectionProperty,
        ];

        private readonly Grid container = new();
        private readonly Rectangle backdrop = new() { IsHitTestVisible = false };
        private readonly Grid content = new();
        private readonly Rectangle separator = new()
        {
            IsHitTestVisible = false,
            Height = SeparatorThickness,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        private readonly TranslateTransform translate = new();
        private readonly RectangleGeometry clip = new();

        public PinnedLayerAdorner(UIElement adornedElement)
            : base(adornedElement)
        {
            container.RenderTransform = translate;
            container.Clip = clip;
            container.Children.Add(backdrop);
            container.Children.Add(content);
            container.Children.Add(separator);
            separator.SetResourceReference(Shape.FillProperty, SystemColors.ControlDarkDarkBrushKey);
            BindInheritedProperties(adornedElement);
            BindBackdrop(adornedElement);
            AddLogicalChild(container);
            AddVisualChild(container);
        }

        public UIElementCollection Children => content.Children;

        protected override int VisualChildrenCount => 1;

        public void SetBand(Rect band, double offsetY)
        {
            clip.Rect = new Rect(band.X, band.Y, band.Width, band.Height + SeparatorThickness);
            translate.Y = offsetY;
            separator.Margin = new Thickness(0.0, band.Bottom, 0.0, 0.0);
        }

        protected override Visual GetVisualChild(int index) => container;

        protected override Size MeasureOverride(Size constraint)
        {
            container.Measure(constraint);
            return base.MeasureOverride(constraint);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            container.Arrange(new Rect(finalSize));
            return finalSize;
        }

        private void BindInheritedProperties(UIElement adornedElement)
        {
            foreach (var property in inheritedProperties)
                BindingOperations.SetBinding(container, property, Track(adornedElement, property));
        }

        private void BindBackdrop(DependencyObject adornedElement)
        {
            var current = adornedElement;
            while (current is not null)
            {
                var property = BackgroundPropertyOf(current);
                if (property is not null && IsOpaque(current.GetValue(property) as Brush))
                {
                    BindingOperations.SetBinding(backdrop, Shape.FillProperty, Track(current, property));
                    return;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            backdrop.SetResourceReference(Shape.FillProperty, SystemColors.ControlBrushKey);
        }

        private static Binding Track(object source, DependencyProperty property)
            => new() { Source = source, Path = new PropertyPath(property), Mode = BindingMode.OneWay };

        private static DependencyProperty? BackgroundPropertyOf(DependencyObject element)
            => element switch
            {
                Panel => Panel.BackgroundProperty,
                Border => Border.BackgroundProperty,
                Control => Control.BackgroundProperty,
                _ => null,
            };

        private static bool IsOpaque(Brush? brush)
        {
            if (brush is null || brush.Opacity <= 0.0)
                return false;
            return brush is not SolidColorBrush solid || solid.Color.A > 0;
        }
    }
}
