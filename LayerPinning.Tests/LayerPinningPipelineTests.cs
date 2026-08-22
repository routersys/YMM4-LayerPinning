using System.ComponentModel;
using System.Runtime.ExceptionServices;
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
