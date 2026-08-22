namespace LayerPinning.Tests;

public class PinnedLayerStateTests
{
    [Fact]
    public void NothingIsPinnedAtFirstAndOnlyTheTopLayerCanBePinned()
    {
        var state = new PinnedLayerState();

        Assert.False(state.IsPinned);
        Assert.Equal(0, state.PinnedCount);
        Assert.True(state.CanToggle(0));
        Assert.False(state.CanToggle(1));
    }

    [Fact]
    public void PinningExtendsTheBlockOneLayerAtATime()
    {
        var state = new PinnedLayerState();

        state.Toggle(0);
        Assert.Equal(1, state.PinnedCount);
        Assert.True(state.CanToggle(1));
        Assert.False(state.CanToggle(2));

        state.Toggle(1);
        Assert.Equal(2, state.PinnedCount);
        Assert.True(state.IsPinnedLayer(0));
        Assert.True(state.IsPinnedLayer(1));
        Assert.False(state.IsPinnedLayer(2));
        Assert.True(state.CanToggle(2));
        Assert.False(state.CanToggle(3));

        state.Toggle(0);
    }

    [Fact]
    public void PinningIsRejectedForLayersThatWouldBreakTheBlock()
    {
        var state = new PinnedLayerState();

        state.Toggle(2);
        Assert.Equal(0, state.PinnedCount);

        state.Toggle(0);
        state.Toggle(3);
        Assert.Equal(1, state.PinnedCount);

        state.Toggle(0);
    }

    [Fact]
    public void UnpinningRemovesTheLayerAndEveryLayerBelowItInTheBlock()
    {
        var state = new PinnedLayerState();

        state.Toggle(0);
        state.Toggle(1);
        state.Toggle(2);
        Assert.Equal(3, state.PinnedCount);

        state.Toggle(1);
        Assert.Equal(1, state.PinnedCount);
        Assert.True(state.IsPinnedLayer(0));
        Assert.False(state.IsPinnedLayer(1));

        state.Toggle(0);
        Assert.Equal(0, state.PinnedCount);
        Assert.False(state.IsPinned);
    }

    [Fact]
    public void UnpinningTheThirdOfFourLayersLeavesTheTwoAboveIt()
    {
        var state = new PinnedLayerState();

        state.Toggle(0);
        state.Toggle(1);
        state.Toggle(2);
        state.Toggle(3);
        Assert.Equal(4, state.PinnedCount);

        state.Toggle(2);

        Assert.Equal(2, state.PinnedCount);
        Assert.True(state.IsPinnedLayer(0));
        Assert.True(state.IsPinnedLayer(1));
        Assert.False(state.IsPinnedLayer(2));
        Assert.False(state.IsPinnedLayer(3));

        state.Toggle(0);
    }

    [Fact]
    public void ChangedIsRaisedOnlyWhenTheBlockChanges()
    {
        var state = new PinnedLayerState();
        var raised = 0;
        state.Changed += (_, _) => raised++;

        state.Toggle(0);
        state.Toggle(5);
        state.Toggle(1);
        state.Toggle(0);

        Assert.Equal(3, raised);
    }
}
