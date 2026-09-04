using System.Windows.Forms;
using Sqlbi.Bravo.Infrastructure.Windows.Dialogs;
using Xunit;

namespace Bravo.Tests.Infrastructure.Windows;

public class TaskDialogBuilderTests
{
    [Fact]
    public void Build_CopiesEverySettingToThePage()
    {
        var first = new TaskDialogCommandLinkButton("first", "first description");
        var second = new TaskDialogButton("second");

        var page = TaskDialogBuilder.Create()
            .WithCaption("caption")
            .WithHeading("heading")
            .WithText("text")
            .WithIcon(TaskDialogIcon.Warning)
            .WithFootnote("footnote", TaskDialogIcon.Information)
            .WithExpander("details", expanded: true, TaskDialogExpanderPosition.AfterFootnote, expandedButtonText: "Hide", collapsedButtonText: "Show")
            .AddButtons(first, second)
            .WithDefaultButton(second)
            .WithSizeToContent()
            .Build();

        Assert.Equal("caption", page.Caption);
        Assert.Equal("heading", page.Heading);
        Assert.Equal("text", page.Text);
        Assert.Same(TaskDialogIcon.Warning, page.Icon);
        Assert.Equal("footnote", page.Footnote?.Text);
        Assert.Same(TaskDialogIcon.Information, page.Footnote?.Icon);
        Assert.Equal("details", page.Expander?.Text);
        Assert.True(page.Expander?.Expanded);
        Assert.Equal(TaskDialogExpanderPosition.AfterFootnote, page.Expander?.Position);
        Assert.Equal("Hide", page.Expander?.ExpandedButtonText);
        Assert.Equal("Show", page.Expander?.CollapsedButtonText);
        Assert.Equal(new TaskDialogButton[] { first, second }, page.Buttons);
        Assert.Same(second, page.DefaultButton);
        Assert.True(page.SizeToContent);
    }
}
