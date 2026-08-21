using System.Windows;

namespace LayerPinning.Tests;

public class LayerGeometryTests
{
    [Theory]
    [InlineData(0.0, 40.0, true)]
    [InlineData(4.0, 36.0, true)]
    [InlineData(40.0, 40.0, false)]
    [InlineData(44.0, 36.0, false)]
    [InlineData(0.0, 80.0, false)]
    public void IsWithinSingleLayerMatchesOnlyTheGivenLayer(double top, double height, bool expected)
    {
        Assert.Equal(expected, LayerGeometry.IsWithinSingleLayer(top, height, 40, 0));
    }

    [Fact]
    public void IsWithinSingleLayerRejectsInvalidLayerHeight()
    {
        Assert.False(LayerGeometry.IsWithinSingleLayer(0.0, 0.0, 0, 0));
    }

    [Fact]
    public void FilterViewportExcludesTheNeighbouringLayers()
    {
        var filter = LayerGeometry.FilterViewport(new Rect(0.0, 500.0, 800.0, 300.0), 0, 40);
        Assert.Equal(LayerGeometry.BandInset, filter.Top);
        Assert.Equal(40.0 - LayerGeometry.BandInset, filter.Bottom);
        Assert.True(filter.Bottom < 40.0);
    }

    [Fact]
    public void BandAndOffsetPlaceThePinnedLayerAtTheTopOfTheViewport()
    {
        var viewport = new Rect(0.0, 500.0, 800.0, 300.0);
        var band = LayerGeometry.Band(viewport, 0, 40);
        var offset = LayerGeometry.OffsetY(viewport, 0, 40);
        Assert.Equal(0.0, band.Top);
        Assert.Equal(40.0, band.Height);
        Assert.Equal(viewport.Y, band.Top + offset);
    }

    [Fact]
    public void MapToPinnedLayerRewritesOnlyThePinnedBand()
    {
        Assert.Equal(0.0, LayerGeometry.MapToPinnedLayer(500.0, 500.0, 0, 40));
        Assert.Equal(39.0, LayerGeometry.MapToPinnedLayer(539.0, 500.0, 0, 40));
        Assert.Null(LayerGeometry.MapToPinnedLayer(540.0, 500.0, 0, 40));
        Assert.Null(LayerGeometry.MapToPinnedLayer(499.0, 500.0, 0, 40));
    }

    [Fact]
    public void MapToPinnedLayerRejectsInvalidLayerHeight()
    {
        Assert.Null(LayerGeometry.MapToPinnedLayer(500.0, 500.0, 0, 0));
    }
}
