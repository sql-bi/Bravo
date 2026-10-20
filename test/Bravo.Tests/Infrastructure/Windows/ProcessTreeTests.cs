using System;
using Sqlbi.Bravo.Infrastructure.Windows;
using Xunit;

namespace Bravo.Tests.Infrastructure.Windows;

public class ProcessTreeTests
{
    private static readonly ProcessTree s_tree = new(
    [
        new ProcessEntry(ProcessId: 0, ParentProcessId: 0, "[System Process]"),
        new ProcessEntry(ProcessId: 100, ParentProcessId: 10, "PBIDesktop.exe"),
        new ProcessEntry(ProcessId: 200, ParentProcessId: 100, "msmdsrv.exe"),
        new ProcessEntry(ProcessId: 300, ParentProcessId: 100, "Bravo.exe"),
        new ProcessEntry(ProcessId: 400, ParentProcessId: 300, "msedgewebview2.exe"),
    ]);

    [Fact]
    public void GetParentId_ProcessInTheSnapshot_ReturnsItsParent()
    {
        Assert.Equal(100, s_tree.GetParentId(300));
    }

    [Fact]
    public void GetParentId_ParentNotInTheSnapshot_ReturnsTheRecordedId()
    {
        // The parent exited: its identifier is still recorded on the child.
        Assert.Equal(10, s_tree.GetParentId(100));
    }

    [Fact]
    public void GetParentId_ProcessNotInTheSnapshot_ReturnsNull()
    {
        Assert.Null(s_tree.GetParentId(999));
    }

    [Fact]
    public void GetChildIds_WithoutImageName_ReturnsEveryChild()
    {
        Assert.Equal([200, 300], s_tree.GetChildIds(100));
    }

    [Theory]
    [InlineData("msmdsrv.exe")]
    [InlineData("MSMDSRV.EXE")]
    public void GetChildIds_WithImageName_ReturnsTheMatchingChildren(string imageName)
    {
        Assert.Equal([200], s_tree.GetChildIds(100, imageName));
    }

    [Fact]
    public void GetChildIds_ProcessThatIsItsOwnParent_IsNotItsOwnChild()
    {
        Assert.Empty(s_tree.GetChildIds(0));
    }

    [Fact]
    public void Constructor_DuplicateProcessId_KeepsTheFirstEntry()
    {
        var tree = new ProcessTree(
        [
            new ProcessEntry(ProcessId: 1, ParentProcessId: 2, "a.exe"),
            new ProcessEntry(ProcessId: 1, ParentProcessId: 3, "b.exe"),
        ]);

        Assert.Equal(2, tree.GetParentId(1));
    }

    [Fact]
    public void Capture_ContainsTheCurrentProcess()
    {
        var tree = ProcessTree.Capture();

        Assert.NotNull(tree.GetParentId(Environment.ProcessId));
    }
}
