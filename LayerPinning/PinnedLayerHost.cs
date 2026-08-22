using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Plugin;
using YukkuriMovieMaker.Settings;
using YukkuriMovieMaker.ViewModels;
using YukkuriMovieMaker.Views;

namespace LayerPinning
{
    internal sealed class PinnedLayerHost
    {
        private const string LayerLabelsPath = "LayerLabels";
        private const string LayerLinesPath = "LayerLines";
        private const string VerticalLinesPath = "VerticalLines.Lines";

        private static readonly ConditionalWeakTable<TimelineView, PinnedLayerHost> hosts = new();
        private static readonly string[] mirroredPaths = ["Spaces", "LayerLines", "VerticalLines.Lines", "ItemBackgrounds", "Items"];

        private readonly TimelineView view;
        private readonly List<PinnedGroup> groups = [];
        private readonly List<FastCanvasItemsControl> sources = [];
        private readonly HashSet<FastCanvasItemsControl> mirrors = [];
        private FrameworkElement? cursorSource;
        private TimelineViewModel? viewModel;
        private PinnedLayerState? state;
        private bool isBuilt;
        private bool isBound;

        private PinnedLayerHost(TimelineView view)
        {
            this.view = view;
            view.Unloaded += OnUnloaded;
            view.DataContextChanged += OnDataContextChanged;
        }

        public static void Attach(TimelineView view)
        {
            if (!hosts.TryGetValue(view, out var host))
            {
                host = new PinnedLayerHost(view);
                hosts.Add(view, host);
            }
            host.Build();
            host.Bind();
        }

        private void Build()
        {
            if (isBuilt)
                return;
            var canvases = new List<FastCanvasItemsControl>();
            Collect(view, canvases);
            var main = new List<FastCanvasItemsControl>();
            FastCanvasItemsControl? label = null;
            foreach (var canvas in canvases)
            {
                var path = PathOf(canvas);
                if (path == LayerLabelsPath)
                    label = canvas;
                else if (path is not null && Array.IndexOf(mirroredPaths, path) >= 0)
                    main.Add(canvas);
            }
            if (main.Count == 0 || label is null)
                return;
            var mainScrollViewer = FindAncestor<ScrollViewer>(main[0]);
            var labelScrollViewer = FindAncestor<ScrollViewer>(label);
            if (mainScrollViewer?.Content is not FrameworkElement mainContent || labelScrollViewer is null)
                return;
            var mainAdornerLayer = AdornerLayer.GetAdornerLayer(mainContent);
            var labelAdornerLayer = AdornerLayer.GetAdornerLayer(label);
            if (mainAdornerLayer is null || labelAdornerLayer is null)
                return;
            AddGroup(mainScrollViewer, mainAdornerLayer, mainContent, main, withChrome: true);
            AddGroup(labelScrollViewer, labelAdornerLayer, label, [label], withChrome: false);
            cursorSource = mainContent;
            isBuilt = true;
        }

        private void AddGroup(ScrollViewer scrollViewer, AdornerLayer adornerLayer, FrameworkElement adorned, IReadOnlyList<FastCanvasItemsControl> canvases, bool withChrome)
        {
            var group = new PinnedGroup(scrollViewer, adornerLayer, new PinnedLayerAdorner(adorned));
            foreach (var canvas in canvases)
            {
                if (withChrome && PathOf(canvas) == LayerLinesPath)
                    AddOverlay(group, BandOverlay.EndOfTimeline());
                var mirror = CreateMirror(canvas);
                mirrors.Add(mirror);
                sources.Add(canvas);
                group.Mirrors.Add(new PinnedMirror(mirror, PathOf(canvas) == VerticalLinesPath));
                group.Adorner.Children.Add(mirror);
            }
            if (withChrome)
                AddOverlay(group, BandOverlay.CurrentPosition());
            scrollViewer.ScrollChanged += OnScrollChanged;
            scrollViewer.SizeChanged += OnSizeChanged;
            groups.Add(group);
        }

        private static void AddOverlay(PinnedGroup group, BandOverlay overlay)
        {
            group.Overlays.Add(overlay);
            group.Adorner.Children.Add(overlay.Host);
        }

        private void Bind()
        {
            if (!isBuilt)
                return;
            var next = view.DataContext as TimelineViewModel;
            if (isBound && ReferenceEquals(next, viewModel))
            {
                Update();
                return;
            }
            Unbind();
            if (next is null)
                return;
            var timeline = NonPublicMembers.TimelineOf(next);
            if (timeline is null)
                return;
            viewModel = next;
            state = PinnedLayerState.Of(timeline);
            state.Changed += OnStateChanged;
            foreach (var source in sources)
                PinnedCanvasRegistry.Register(source, state);
            foreach (var group in groups)
            {
                foreach (var mirror in group.Mirrors)
                    mirror.Control.DataContext = viewModel;
                foreach (var overlay in group.Overlays)
                    overlay.Host.DataContext = viewModel;
            }
            if (cursorSource is not null)
            {
                cursorSource.PreviewMouseMove += OnCursorMove;
                cursorSource.PreviewMouseDown += OnCursorButtonDown;
            }
            SettingsBase<YMMSettings>.Default.PropertyChanged += OnSettingsChanged;
            isBound = true;
            Refresh();
        }

        private void Unbind()
        {
            if (!isBound)
                return;
            isBound = false;
            SettingsBase<YMMSettings>.Default.PropertyChanged -= OnSettingsChanged;
            if (cursorSource is not null)
            {
                cursorSource.PreviewMouseMove -= OnCursorMove;
                cursorSource.PreviewMouseDown -= OnCursorButtonDown;
            }
            if (state is not null)
                state.Changed -= OnStateChanged;
            foreach (var source in sources)
                PinnedCanvasRegistry.Unregister(source);
            foreach (var group in groups)
            {
                group.Detach();
                foreach (var mirror in group.Mirrors)
                    mirror.Control.DataContext = null;
                foreach (var overlay in group.Overlays)
                    overlay.Host.DataContext = null;
            }
            state = null;
            viewModel = null;
        }

        private void Refresh()
        {
            Update();
            foreach (var source in sources)
                NonPublicMembers.Refresh(source);
        }

        private void Update()
        {
            var pinnedCount = state?.PinnedCount ?? 0;
            var layerHeight = LayerGeometry.LayerHeight;
            foreach (var group in groups)
            {
                if (pinnedCount <= 0 || layerHeight <= 0)
                {
                    group.Detach();
                    continue;
                }
                var viewport = ViewportOf(group.ScrollViewer);
                var offsetY = LayerGeometry.OffsetY(viewport);
                group.Attach();
                group.Adorner.SetBand(LayerGeometry.Band(viewport, pinnedCount, layerHeight), offsetY);
                var filter = LayerGeometry.FilterViewport(viewport, pinnedCount, layerHeight);
                foreach (var mirror in group.Mirrors)
                    mirror.SetBand(viewport, filter, offsetY);
                foreach (var overlay in group.Overlays)
                    overlay.SetBand(pinnedCount, layerHeight);
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) => Unbind();

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e) => Bind();

        private void OnScrollChanged(object sender, ScrollChangedEventArgs e) => Update();

        private void OnSizeChanged(object sender, SizeChangedEventArgs e) => Update();

        private void OnStateChanged(object? sender, EventArgs e) => Refresh();

        private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(YMMSettings.LayerHeight))
                Refresh();
        }

        private void OnCursorMove(object sender, MouseEventArgs e)
        {
            if (MapToPinnedLayer() is { } point)
                viewModel!.TimelineCursorPosition.Value = point;
        }

        private void OnCursorButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right)
                return;
            if (MapToPinnedLayer() is { } point)
                viewModel!.TimelineCursorPositionWhenRightClick.Value = point;
        }

        private Point? MapToPinnedLayer()
        {
            if (viewModel is null || cursorSource is null || state is null || !state.IsPinned || groups.Count == 0)
                return null;
            var viewport = ViewportOf(groups[0].ScrollViewer);
            var position = Mouse.GetPosition(cursorSource);
            var mapped = LayerGeometry.MapToPinnedLayers(position.Y, viewport.Y, state.PinnedCount, LayerGeometry.LayerHeight);
            return mapped is null ? null : new Point(position.X, mapped.Value);
        }

        private static Rect ViewportOf(ScrollViewer scrollViewer)
            => new(scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset, scrollViewer.ViewportWidth, scrollViewer.ViewportHeight);

        private static FastCanvasItemsControl CreateMirror(FastCanvasItemsControl source)
        {
            var mirror = new FastCanvasItemsControl
            {
                ItemTemplate = source.ItemTemplate,
                IsHitTestVisible = source.IsHitTestVisible,
                UseLayoutRounding = source.UseLayoutRounding,
                SnapsToDevicePixels = source.SnapsToDevicePixels,
                HorizontalAlignment = source.HorizontalAlignment,
                VerticalAlignment = source.VerticalAlignment,
                Effect = source.Effect?.CloneCurrentValue(),
                ViewPort = Rect.Empty,
            };
            CopyBinding(source, mirror, FastCanvasItemsControl.ItemsProperty);
            CopyBinding(source, mirror, FrameworkElement.WidthProperty);
            CopyBinding(source, mirror, FrameworkElement.HeightProperty);
            return mirror;
        }

        private static void CopyBinding(DependencyObject source, DependencyObject target, DependencyProperty property)
        {
            var binding = BindingOperations.GetBindingBase(source, property);
            if (binding is not null)
                BindingOperations.SetBinding(target, property, binding);
        }

        private static string? PathOf(FastCanvasItemsControl control)
            => (BindingOperations.GetBindingBase(control, FastCanvasItemsControl.ItemsProperty) as Binding)?.Path?.Path;

        private void Collect(DependencyObject root, List<FastCanvasItemsControl> found)
        {
            var count = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is FastCanvasItemsControl canvas)
                {
                    if (!mirrors.Contains(canvas))
                        found.Add(canvas);
                    continue;
                }
                Collect(child, found);
            }
        }

        private static T? FindAncestor<T>(DependencyObject origin) where T : DependencyObject
        {
            var current = VisualTreeHelper.GetParent(origin);
            while (current is not null)
            {
                if (current is T found)
                    return found;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private sealed class PinnedGroup(ScrollViewer scrollViewer, AdornerLayer adornerLayer, PinnedLayerAdorner adorner)
        {
            private bool isAttached;

            public ScrollViewer ScrollViewer { get; } = scrollViewer;

            public PinnedLayerAdorner Adorner { get; } = adorner;

            public List<PinnedMirror> Mirrors { get; } = [];

            public List<BandOverlay> Overlays { get; } = [];

            public void Attach()
            {
                if (isAttached)
                    return;
                adornerLayer.Add(Adorner);
                isAttached = true;
            }

            public void Detach()
            {
                if (isAttached)
                {
                    adornerLayer.Remove(Adorner);
                    isAttached = false;
                }
                foreach (var mirror in Mirrors)
                    mirror.Clear();
            }
        }

        private sealed class PinnedMirror
        {
            private readonly bool followsViewport;
            private readonly TranslateTransform compensation = new();

            public PinnedMirror(FastCanvasItemsControl control, bool followsViewport)
            {
                Control = control;
                this.followsViewport = followsViewport;
                if (followsViewport)
                    control.RenderTransform = compensation;
            }

            public FastCanvasItemsControl Control { get; }

            public void SetBand(Rect viewport, Rect filter, double offsetY)
            {
                if (!followsViewport)
                {
                    Control.ViewPort = filter;
                    return;
                }
                compensation.Y = -offsetY;
                Control.ViewPort = viewport;
            }

            public void Clear() => Control.ViewPort = Rect.Empty;
        }

        private sealed class BandOverlay
        {
            private readonly Rectangle bar = new();

            private BandOverlay(string leftPath)
            {
                bar.SetBinding(Canvas.LeftProperty, new Binding(leftPath));
                Host.Children.Add(bar);
            }

            public Canvas Host { get; } = new() { IsHitTestVisible = false };

            public static BandOverlay EndOfTimeline()
            {
                var overlay = new BandOverlay("CanvasEndLineX.Value");
                overlay.bar.SetBinding(FrameworkElement.WidthProperty, new Binding("CanvasWidth.Value"));
                overlay.bar.SetResourceReference(Shape.FillProperty, SystemColors.ControlBrushKey);
                return overlay;
            }

            public static BandOverlay CurrentPosition()
            {
                var overlay = new BandOverlay("CurrentPositionX.Value");
                overlay.bar.Width = 1.0;
                overlay.bar.Fill = new SolidColorBrush(Color.FromRgb(0xFF, 0x00, 0x00));
                return overlay;
            }

            public void SetBand(int pinnedCount, int layerHeight)
            {
                Canvas.SetTop(bar, 0.0);
                bar.Height = LayerGeometry.BandHeight(pinnedCount, layerHeight);
            }
        }
    }
}
