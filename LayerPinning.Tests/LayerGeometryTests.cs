using System.Windows;

namespace LayerPinning.Tests;

public class LayerGeometryTests
{
    [Theory]
    [InlineData(0.0, 40.0, 1, true)]
    [InlineData(4.0, 36.0, 1, true)]
    [InlineData(40.0, 40.0, 1, false)]
    [InlineData(40.0, 40.0, 2, true)]
    [InlineData(80.0, 40.0, 2, false)]
    [InlineData(0.0, 80.0, 2, false)]
    [InlineData(0.0, 40.0, 0, false)]
    public void IsWithinPinnedLayersMatchesOnlyTheContiguousBlock(double top, double height, int pinnedCount, bool expected)
    {
        Assert.Equal(expected, LayerGeometry.IsWithinPinnedLayers(top, height, 40, pinnedCount));
    }

    [Fact]
    public void IsWithinPinnedLayersRejectsInvalidLayerHeight()
    {
        Assert.False(LayerGeometry.IsWithinPinnedLayers(0.0, 0.0, 0, 1));
    }

    [Theory]
    [InlineData(0, 0.0)]
    [InlineData(1, 40.0)]
    [InlineData(3, 120.0)]
    public void BandHeightGrowsWithThePinnedCount(int pinnedCount, double expected)
    {
        Assert.Equal(expected, LayerGeometry.BandHeight(pinnedCount, 40));
    }

    [Fact]
    public void FilterViewportExcludesTheLayerBelowTheBlock()
    {
        var filter = LayerGeometry.FilterViewport(new Rect(0.0, 500.0, 800.0, 300.0), 2, 40);
        Assert.Equal(LayerGeometry.BandInset, filter.Top);
        Assert.Equal(80.0 - LayerGeometry.BandInset, filter.Bottom);
        Assert.True(filter.Bottom < 80.0);
    }

    [Fact]
    public void BandAndOffsetPlaceTheBlockAtTheTopOfTheViewport()
    {
        var viewport = new Rect(0.0, 500.0, 800.0, 300.0);
        var band = LayerGeometry.Band(viewport, 2, 40);
        var offset = LayerGeometry.OffsetY(viewport);
        Assert.Equal(0.0, band.Top);
        Assert.Equal(80.0, band.Height);
        Assert.Equal(viewport.Y, band.Top + offset);
    }

    [Fact]
    public void MapToPinnedLayersRewritesOnlyTheBand()
    {
        Assert.Equal(0.0, LayerGeometry.MapToPinnedLayers(500.0, 500.0, 2, 40));
        Assert.Equal(79.0, LayerGeometry.MapToPinnedLayers(579.0, 500.0, 2, 40));
        Assert.Null(LayerGeometry.MapToPinnedLayers(580.0, 500.0, 2, 40));
        Assert.Null(LayerGeometry.MapToPinnedLayers(499.0, 500.0, 2, 40));
    }

    [Fact]
    public void MapToPinnedLayersRejectsUnpinnedState()
    {
        Assert.Null(LayerGeometry.MapToPinnedLayers(500.0, 500.0, 0, 40));
        Assert.Null(LayerGeometry.MapToPinnedLayers(500.0, 500.0, 1, 0));
    }
}
