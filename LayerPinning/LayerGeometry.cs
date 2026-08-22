using System.Windows;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Plugin;
using YukkuriMovieMaker.Settings;

namespace LayerPinning
{
    internal static class LayerGeometry
    {
        public const double BandInset = 0.5;

        public static int LayerHeight => SettingsBase<YMMSettings>.Default.LayerHeight;

        public static bool IsWithinPinnedLayers(IFastCanvasItemsControlViewModel item, int pinnedCount)
            => IsWithinPinnedLayers(item.Top, item.Height, LayerHeight, pinnedCount);

        public static bool IsWithinPinnedLayers(double top, double height, int layerHeight, int pinnedCount)
        {
            if (layerHeight <= 0 || pinnedCount <= 0 || height > layerHeight + BandInset)
                return false;
            var layer = (int)Math.Floor(top / layerHeight);
            return PinnedLayerState.TopLayer <= layer && layer < pinnedCount;
        }

        public static double BandHeight(int pinnedCount, int layerHeight)
            => Math.Max(0.0, pinnedCount * (double)layerHeight);

        public static Rect Band(Rect viewport, int pinnedCount, int layerHeight)
            => new(viewport.X, 0.0, viewport.Width, BandHeight(pinnedCount, layerHeight));

        public static Rect FilterViewport(Rect viewport, int pinnedCount, int layerHeight)
            => new(viewport.X, BandInset, viewport.Width, Math.Max(0.0, BandHeight(pinnedCount, layerHeight) - BandInset * 2.0));

        public static double OffsetY(Rect viewport)
            => viewport.Y;

        public static double? MapToPinnedLayers(double y, double viewportY, int pinnedCount, int layerHeight)
        {
            if (layerHeight <= 0 || pinnedCount <= 0)
                return null;
            if (y < viewportY || viewportY + BandHeight(pinnedCount, layerHeight) <= y)
                return null;
            return y - viewportY;
        }
    }
}
