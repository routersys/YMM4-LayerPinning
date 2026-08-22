using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using YukkuriMovieMaker.ViewModels;
using YukkuriMovieMaker.Views;

namespace LayerPinning
{
    internal static class LayerPinMenu
    {
        private static readonly ConditionalWeakTable<ContextMenu, PinMenuEntry> entries = new();

        public static void OnContextMenuOpened(object sender, RoutedEventArgs e)
        {
            if (sender is not ContextMenu menu || menu.DataContext is not TimelineLayerLabelItemViewModel label)
                return;
            var entry = entries.GetValue(menu, Create);
            var state = StateOf(menu.PlacementTarget);
            var isAvailable = state is not null && state.CanToggle(label.Id);
            entry.SetAvailability(isAvailable);
            entry.Item.Tag = isAvailable ? new PinTarget(state!, label.Id) : null;
            entry.Item.IsChecked = isAvailable && state!.IsPinnedLayer(label.Id);
        }

        private static PinMenuEntry Create(ContextMenu menu)
        {
            var separator = new Separator();
            var item = new MenuItem
            {
                Header = Texts.PinLayerMenuHeader,
                IsCheckable = true,
            };
            item.Click += OnClick;
            menu.Items.Add(separator);
            menu.Items.Add(item);
            return new PinMenuEntry(separator, item);
        }

        private static void OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem { Tag: PinTarget target })
                return;
            target.State.Toggle(target.Layer);
            LayerPinningUpdateNotifier.EnsureCheckedOnce();
        }

        private static PinnedLayerState? StateOf(object? placementTarget)
        {
            if (placementTarget is not DependencyObject origin)
                return null;
            var view = FindAncestor<TimelineView>(origin);
            if (view?.DataContext is not TimelineViewModel viewModel)
                return null;
            var timeline = NonPublicMembers.TimelineOf(viewModel);
            return timeline is null ? null : PinnedLayerState.Of(timeline);
        }

        private static T? FindAncestor<T>(DependencyObject origin) where T : DependencyObject
        {
            var current = origin;
            while (current is not null)
            {
                if (current is T found)
                    return found;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private sealed record PinTarget(PinnedLayerState State, int Layer);

        private sealed class PinMenuEntry(Separator separator, MenuItem item)
        {
            public MenuItem Item { get; } = item;

            public void SetAvailability(bool isAvailable)
            {
                var visibility = isAvailable ? Visibility.Visible : Visibility.Collapsed;
                separator.Visibility = visibility;
                Item.Visibility = visibility;
            }
        }
    }
}
