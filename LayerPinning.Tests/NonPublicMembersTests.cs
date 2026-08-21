using System.Reflection;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.ViewModels;

namespace LayerPinning.Tests;

public class NonPublicMembersTests
{
    [Fact]
    public void AllInternalMembersAreResolved()
    {
        Assert.True(NonPublicMembers.IsAvailable);
    }

    [Fact]
    public void IsInViewPortKeepsTheSignatureThePatchRequires()
    {
        var method = NonPublicMembers.IsInViewPortMethod;
        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method!.ReturnType);
        var parameters = method.GetParameters();
        Assert.Single(parameters);
        Assert.Equal("item", parameters[0].Name);
        Assert.Equal(typeof(IFastCanvasItemsControlViewModel), parameters[0].ParameterType);
    }

    [Fact]
    public void UpdateAllKeepsTheSignatureTheRefreshRequires()
    {
        var method = typeof(FastCanvasItemsControl).GetMethod("UpdateAll", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        Assert.Empty(method!.GetParameters());
    }

    [Fact]
    public void TimelineViewModelKeepsTheTimelineField()
    {
        var field = typeof(TimelineViewModel).GetField("timeline", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        Assert.Equal(typeof(Timeline), field!.FieldType);
    }

    [Theory]
    [InlineData("Items")]
    [InlineData("Spaces")]
    [InlineData("LayerLines")]
    [InlineData("ItemBackgrounds")]
    [InlineData("LayerLabels")]
    [InlineData("VerticalLines.Lines")]
    [InlineData("CurrentPositionX")]
    [InlineData("CanvasEndLineX")]
    [InlineData("CanvasWidth")]
    [InlineData("TimelineCursorPosition")]
    [InlineData("TimelineCursorPositionWhenRightClick")]
    public void TimelineViewModelKeepsTheBoundMemberPath(string path)
    {
        var type = typeof(TimelineViewModel);
        foreach (var segment in path.Split('.'))
        {
            var property = type.GetProperty(segment, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(property);
            type = property!.PropertyType;
        }
    }

    [Fact]
    public void LayerLabelViewModelKeepsTheLayerIdentifier()
    {
        var property = typeof(TimelineLayerLabelItemViewModel).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(property);
        Assert.Equal(typeof(int), property!.PropertyType);
    }
}
