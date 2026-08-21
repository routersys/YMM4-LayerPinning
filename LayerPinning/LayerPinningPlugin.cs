using System.Reflection;
using YukkuriMovieMaker.Plugin;

namespace LayerPinning
{
    [PluginDetails(AuthorName = "routersys")]
    public sealed class LayerPinningPlugin : IPlugin
    {
        public LayerPinningPlugin()
        {
            LayerPinningPipeline.Initialize();
        }

        public string Name => Texts.LayerPinningPluginName;

        public PluginDetailsAttribute? Details => GetType().GetCustomAttribute<PluginDetailsAttribute>();
    }
}
