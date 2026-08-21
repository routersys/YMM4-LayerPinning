using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace LayerPinning.Tests;

public class HarmonyPackagingTests
{
    private const string HarmonyAssemblyFileName = "LayerPinning.0Harmony.dll";

    private static readonly string[] mergedAssemblyPrefixes = ["Mono.Cecil", "MonoMod"];

    [Fact]
    public void HarmonyAssemblyIsDeployedNextToThePlugin()
    {
        Assert.True(File.Exists(HarmonyAssemblyPath), $"{HarmonyAssemblyPath} が存在しません。");
    }

    [Fact]
    public void HarmonyAssemblyCarriesNoUnmergedDependency()
    {
        var unmerged = ReferencedAssemblyNames(HarmonyAssemblyPath)
            .Where(static name => mergedAssemblyPrefixes.Any(name.StartsWith))
            .ToArray();
        Assert.True(unmerged.Length == 0, $"ILRepack による統合が行われていません。未統合の参照: {string.Join(", ", unmerged)}");
    }

    private static string HarmonyAssemblyPath => Path.Combine(AppContext.BaseDirectory, HarmonyAssemblyFileName);

    private static IEnumerable<string> ReferencedAssemblyNames(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = new PEReader(stream);
        var metadata = reader.GetMetadataReader();
        foreach (var handle in metadata.AssemblyReferences)
            yield return metadata.GetString(metadata.GetAssemblyReference(handle).Name);
    }
}
