using System.Reflection;
using HarmonyLib;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.ViewModels;

namespace LayerPinning
{
    internal static class NonPublicMembers
    {
        private static readonly FieldInfo? timelineField = AccessTools.Field(typeof(TimelineViewModel), "timeline");
        private static readonly MethodInfo? updateAllMethod = AccessTools.Method(typeof(FastCanvasItemsControl), "UpdateAll");

        public static MethodInfo? IsInViewPortMethod => AccessTools.Method(typeof(FastCanvasItemsControl), "IsInViewPort");

        public static bool IsAvailable => timelineField is not null && updateAllMethod is not null && IsInViewPortMethod is not null;

        public static Timeline? TimelineOf(TimelineViewModel viewModel)
            => timelineField?.GetValue(viewModel) as Timeline;

        public static void Refresh(FastCanvasItemsControl control)
            => updateAllMethod?.Invoke(control, null);
    }
}
