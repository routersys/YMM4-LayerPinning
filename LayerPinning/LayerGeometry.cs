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

        public static bool IsWithinSingleLayer(IFastCanvasItemsControlViewModel item, int layer)
            => IsWithinSingleLayer(item.Top, item.Height, LayerHeight, layer);

        public static bool IsWithinSingleLayer(double top, double height, int layerHeight, int layer)
        {
            if (layerHeight <= 0 || height > layerHeight + BandInset)
                return false;
            return (int)Math.Floor(top / layerHeight) == layer;
        }

        public static Rect Band(Rect viewport, int layer, int layerHeight)
            => new(viewport.X, layer * (double)layerHeight, viewport.Width, layerHeight);

        public static Rect FilterViewport(Rect viewport, int layer, int layerHeight)
            => new(viewport.X, layer * (double)layerHeight + BandInset, viewport.Width, Math.Max(0.0, layerHeight - BandInset * 2.0));

        public static double OffsetY(Rect viewport, int layer, int layerHeight)
            => viewport.Y - layer * (double)layerHeight;

        public static double? MapToPinnedLayer(double y, double viewportY, int layer, int layerHeight)
        {
            if (layerHeight <= 0 || y < viewportY || viewportY + layerHeight <= y)
                return null;
            return layer * (double)layerHeight + y - viewportY;
        }
    }
}
