using System.Windows;
using GitHubUpdateNotifier;
using YukkuriMovieMaker.Plugin.Update;
using Notifier = GitHubUpdateNotifier.GitHubUpdateNotifier;

namespace LayerPinning
{
    internal static class LayerPinningUpdateNotifier
    {
        private const string Owner = "routersys";
        private const string Repository = "YMM4-LayerPinning";

        private static int started;

        public static void EnsureCheckedOnce()
        {
            if (Interlocked.Exchange(ref started, 1) != 0)
                return;

            _ = RunAsync();
        }

        private static async Task RunAsync()
        {
            try
            {
                var options = new UpdateNotifierOptions
                {
                    CurrentVersion = PluginVersion.FromAssemblyInformationalVersion(typeof(LayerPinningUpdateNotifier)),
                    IncludePrerelease = false,
                    MessageFormatter = FormatMessage,
                    NotificationHandler = ShowNotificationAsync,
                };

                await new Notifier(Owner, Repository, options).NotifyAsync().ConfigureAwait(false);
            }
            catch
            {
            }
        }

        private static string FormatMessage(UpdateInfo update)
            => string.Format(Texts.UpdateAvailableMessage, update.TagName) + Environment.NewLine + update.ReleaseUrl;

        private static Task ShowNotificationAsync(UpdateNotification notification, CancellationToken cancellationToken)
        {
            var application = Application.Current;
            if (application is null)
                return Task.CompletedTask;

            return application.Dispatcher.InvokeAsync(() =>
                MessageBox.Show(notification.Message, Texts.LayerPinningPluginName, MessageBoxButton.OK, MessageBoxImage.Information)).Task;
        }
    }
}
