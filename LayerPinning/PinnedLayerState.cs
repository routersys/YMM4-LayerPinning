using System.Runtime.CompilerServices;
using YukkuriMovieMaker.Project;

namespace LayerPinning
{
    internal sealed class PinnedLayerState
    {
        public const int PinnableLayer = 0;
        public const int NoLayer = -1;

        private static readonly ConditionalWeakTable<Timeline, PinnedLayerState> states = new();

        private static int pinnedStateCount;

        private int pinnedLayer = NoLayer;

        public static bool IsAnyPinned => Volatile.Read(ref pinnedStateCount) > 0;

        public int PinnedLayer => pinnedLayer;

        public bool IsPinned => pinnedLayer != NoLayer;

        public event EventHandler? Changed;

        public static PinnedLayerState Of(Timeline timeline)
            => states.GetValue(timeline, _ => new PinnedLayerState());

        public void Toggle()
        {
            var wasPinned = IsPinned;
            pinnedLayer = wasPinned ? NoLayer : PinnableLayer;
            Interlocked.Add(ref pinnedStateCount, wasPinned ? -1 : 1);
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
