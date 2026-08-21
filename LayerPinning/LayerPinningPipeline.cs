using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using HarmonyLib;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Views;

namespace LayerPinning
{
    internal static class LayerPinningPipeline
    {
        private const string HarmonyId = "LayerPinning";

        private static int initialized;

        public static bool IsActive { get; private set; }

        public static void Initialize()
        {
            if (Interlocked.Exchange(ref initialized, 1) != 0)
                return;
            try
            {
                IsActive = Apply();
            }
            catch
            {
                IsActive = false;
            }
        }

        private static bool Apply()
        {
            var isInViewPort = NonPublicMembers.IsInViewPortMethod;
            if (isInViewPort is null || !NonPublicMembers.IsAvailable)
                return false;
            var harmony = new Harmony(HarmonyId);
            harmony.Patch(isInViewPort, postfix: new HarmonyMethod(typeof(LayerPinningPipeline), nameof(HidePinnedLayer)));
            EventManager.RegisterClassHandler(typeof(TimelineView), FrameworkElement.LoadedEvent, new RoutedEventHandler(OnTimelineViewLoaded));
            EventManager.RegisterClassHandler(typeof(ContextMenu), ContextMenu.OpenedEvent, new RoutedEventHandler(LayerPinMenu.OnContextMenuOpened), true);
            AttachToLoadedViews();
            return true;
        }

        private static void HidePinnedLayer(FastCanvasItemsControl __instance, IFastCanvasItemsControlViewModel item, ref bool __result)
        {
            if (__result && PinnedCanvasRegistry.ShouldHide(__instance, item))
                __result = false;
        }

        private static void OnTimelineViewLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is TimelineView view)
                PinnedLayerHost.Attach(view);
        }

        private static void AttachToLoadedViews()
        {
            var application = Application.Current;
            application?.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () =>
            {
                foreach (Window window in application.Windows)
                    AttachDescendants(window);
            });
        }

        private static void AttachDescendants(DependencyObject root)
        {
            var count = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is TimelineView view)
                {
                    PinnedLayerHost.Attach(view);
                    continue;
                }
                AttachDescendants(child);
            }
        }
    }
}
