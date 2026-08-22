using System.Runtime.CompilerServices;
using YukkuriMovieMaker.Project;

namespace LayerPinning
{
    internal sealed class PinnedLayerState
    {
        public const int TopLayer = 0;

        private static readonly ConditionalWeakTable<Timeline, PinnedLayerState> states = new();

        private static int pinnedStateCount;

        private int pinnedCount;

        public static bool IsAnyPinned => Volatile.Read(ref pinnedStateCount) > 0;

        public int PinnedCount => pinnedCount;

        public bool IsPinned => pinnedCount > TopLayer;

        public event EventHandler? Changed;

        public static PinnedLayerState Of(Timeline timeline)
            => states.GetValue(timeline, _ => new PinnedLayerState());

        public bool IsPinnedLayer(int layer)
            => TopLayer <= layer && layer < pinnedCount;

        public bool CanToggle(int layer)
            => TopLayer <= layer && layer <= pinnedCount;

        public void Toggle(int layer)
        {
            if (!CanToggle(layer))
                return;
            SetPinnedCount(IsPinnedLayer(layer) ? layer : layer + 1);
        }

        private void SetPinnedCount(int count)
        {
            if (pinnedCount == count)
                return;
            var wasPinned = IsPinned;
            pinnedCount = count;
            if (wasPinned != IsPinned)
                Interlocked.Add(ref pinnedStateCount, IsPinned ? 1 : -1);
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
