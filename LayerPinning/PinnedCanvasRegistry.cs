using System.Runtime.CompilerServices;
using YukkuriMovieMaker.Controls;

namespace LayerPinning
{
    internal static class PinnedCanvasRegistry
    {
        private static readonly ConditionalWeakTable<FastCanvasItemsControl, PinnedLayerState> sources = new();

        public static void Register(FastCanvasItemsControl source, PinnedLayerState state)
        {
            if (!sources.TryGetValue(source, out _))
                sources.Add(source, state);
        }

        public static void Unregister(FastCanvasItemsControl source)
            => sources.Remove(source);

        public static bool ShouldHide(FastCanvasItemsControl source, IFastCanvasItemsControlViewModel item)
        {
            if (!PinnedLayerState.IsAnyPinned)
                return false;
            if (!sources.TryGetValue(source, out var state) || !state.IsPinned)
                return false;
            return LayerGeometry.IsWithinSingleLayer(item, state.PinnedLayer);
        }
    }
}
