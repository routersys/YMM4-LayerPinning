using System.ComponentModel;
using System.Runtime.ExceptionServices;
using System.Windows;
using HarmonyLib;
using YukkuriMovieMaker.Controls;

namespace LayerPinning.Tests;

public class LayerPinningPipelineTests
{
    [Fact]
    public void InitializeAppliesTheHarmonyPatch()
    {
        LayerPinningPipeline.Initialize();

        Assert.True(LayerPinningPipeline.IsActive, "Harmony によるパッチの適用に失敗しました。");
        Assert.Contains(
            Harmony.GetAllPatchedMethods(),
            static method => method.Name == "IsInViewPort" && method.DeclaringType == typeof(FastCanvasItemsControl));
    }

    [Fact]
    public void PatchedIsInViewPortHidesThePinnedLayer()
    {
        LayerPinningPipeline.Initialize();
        Assert.True(LayerPinningPipeline.IsActive, "Harmony によるパッチの適用に失敗しました。");

        var isInViewPort = NonPublicMembers.IsInViewPortMethod;
        Assert.NotNull(isInViewPort);

        RunOnStaThread(() =>
        {
            var control = new FastCanvasItemsControl();
            var item = new StubItem(0.0, 0.0, 80.0, LayerGeometry.LayerHeight);
            Assert.True((bool)isInViewPort!.Invoke(control, [item])!);

            var state = new PinnedLayerState();
            state.Toggle(PinnedLayerState.TopLayer);
            PinnedCanvasRegistry.Register(control, state);
            try
            {
                Assert.False((bool)isInViewPort.Invoke(control, [item])!, "パッチが適用されていれば固定レイヤーは表示範囲外として扱われます。");
            }
            finally
            {
                PinnedCanvasRegistry.Unregister(control);
                state.Toggle(PinnedLayerState.TopLayer);
            }
        });
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    public void FilterViewportSelectsExactlyThePinnedLayers(int pinnedCount)
    {
        const int layerHeight = 40;
        var isInViewPort = NonPublicMembers.IsInViewPortMethod;
        Assert.NotNull(isInViewPort);
        var filter = LayerGeometry.FilterViewport(new Rect(0.0, 500.0, 800.0, 300.0), pinnedCount, layerHeight);

        RunOnStaThread(() =>
        {
            var mirror = new FastCanvasItemsControl { ViewPort = filter };
            for (var layer = 0; layer < pinnedCount + 3; layer++)
            {
                var item = new StubItem(0.0, layer * (double)layerHeight, 80.0, layerHeight);
                var selected = (bool)isInViewPort!.Invoke(mirror, [item])!;
                Assert.Equal(layer < pinnedCount, selected);
            }
        });
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    public void ThePatchHidesExactlyThePinnedLayersFromTheOriginalCanvas(int pinnedCount)
    {
        LayerPinningPipeline.Initialize();
        Assert.True(LayerPinningPipeline.IsActive, "Harmony によるパッチの適用に失敗しました。");

        var isInViewPort = NonPublicMembers.IsInViewPortMethod;
        Assert.NotNull(isInViewPort);
        var layerHeight = LayerGeometry.LayerHeight;

        RunOnStaThread(() =>
        {
            var control = new FastCanvasItemsControl { ViewPort = new Rect(0.0, 0.0, 100000.0, 100000.0) };
            var state = new PinnedLayerState();
            for (var layer = 0; layer < pinnedCount; layer++)
                state.Toggle(layer);
            Assert.Equal(pinnedCount, state.PinnedCount);

            PinnedCanvasRegistry.Register(control, state);
            try
            {
                for (var layer = 0; layer < pinnedCount + 3; layer++)
                {
                    var item = new StubItem(0.0, layer * (double)layerHeight, 80.0, layerHeight);
                    var visible = (bool)isInViewPort!.Invoke(control, [item])!;
                    Assert.Equal(pinnedCount <= layer, visible);
                }
            }
            finally
            {
                PinnedCanvasRegistry.Unregister(control);
                state.Toggle(PinnedLayerState.TopLayer);
            }
        });
    }

    private static void RunOnStaThread(Action action)
    {
        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                failure = ExceptionDispatchInfo.Capture(exception);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        failure?.Throw();
    }

    private sealed class StubItem(double left, double top, double width, double height) : IFastCanvasItemsControlViewModel
    {
        public double Left { get; } = left;

        public double Top { get; } = top;

        public double Width { get; } = width;

        public double Height { get; } = height;

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add { }
            remove { }
        }
    }
}
