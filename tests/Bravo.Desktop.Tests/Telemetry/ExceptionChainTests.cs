using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Bravo.Desktop.Diagnostics;
using Bravo.Desktop.Telemetry;
using Xunit;

namespace Bravo.Desktop.Tests.Telemetry;

public sealed class ExceptionChainTests
{
    private readonly PersonalNames _names = new(@"C:\Users\jdoe", "jdoe", "LAPTOP-42");

    [Fact]
    public void Create_InnerExceptions_ComeAfterTheOneThatHoldsThem()
    {
        var exception = new InvalidOperationException("outer", new IOException("middle", new FormatException("inner")));

        var chain = ExceptionChain.Create(exception, _names);

        Assert.Equal(
            [(1, 0, "System.InvalidOperationException"), (2, 1, "System.IO.IOException"), (3, 2, "System.FormatException")],
            chain.Select(link => (link.Id, link.OuterId, link.TypeName)));
    }

    [Fact]
    public void Create_Aggregate_HoldsEachOfItsExceptionsWithTheirInnerOnes()
    {
        var exception = new AggregateException(
            new InvalidOperationException("first"),
            new IOException("second", new FormatException("inner of the second")));

        var chain = ExceptionChain.Create(exception, _names);

        Assert.Equal(
            [(1, 0, "AggregateException"), (2, 1, "InvalidOperationException"), (3, 1, "IOException"), (4, 3, "FormatException")],
            chain.Select(link => (link.Id, link.OuterId, link.TypeName.Split('.')[^1])));
    }

    [Fact]
    public void Create_AggregateOfOneException_StartsWithThatException()
    {
        var exception = new AggregateException(new IOException("the only one", new FormatException("inner")));

        var chain = ExceptionChain.Create(exception, _names);

        Assert.Equal(
            [(1, 0, "IOException"), (2, 1, "FormatException")],
            chain.Select(link => (link.Id, link.OuterId, link.TypeName.Split('.')[^1])));
    }

    [Fact]
    public void Create_LongerChain_KeepsTheFirstTen()
    {
        Exception exception = new FormatException("innermost");
        for (var depth = 0; depth < 15; depth++)
        {
            exception = new InvalidOperationException($"level {depth}", exception);
        }

        var chain = ExceptionChain.Create(exception, _names);

        Assert.Equal(ExceptionChain.MaxExceptions, chain.Count);
        Assert.Equal("level 14", chain[0].Message);
    }

    [Fact]
    public void Create_Message_LosesTheSensitiveTextAndThePersonalNames()
    {
        var exception = new InvalidOperationException(
            @"Login failed for <pii>CONTOSO\jdoe</pii> on LAPTOP-42 reading C:\Users\jdoe\model.pbix.");

        var link = Assert.Single(ExceptionChain.Create(exception, _names));

        Assert.Equal(@"Login failed for <pii>[REDACTED]</pii> on %COMPUTERNAME% reading %USERPROFILE%\model.pbix.", link.Message);
    }

    [Fact]
    public void Create_ExceptionNeverThrown_HasNoFrames()
    {
        var link = Assert.Single(ExceptionChain.Create(new InvalidOperationException("never thrown"), _names));

        Assert.Empty(link.Frames);
        Assert.True(link.HasFullStack);
    }

    [Fact]
    public void Create_ThrownException_HasItsFramesFromWhereItWasThrown()
    {
        var exception = Catch(() => Throw());

        var link = Assert.Single(ExceptionChain.Create(exception, _names));

        var first = link.Frames[0];
        Assert.Equal((0, $"{typeof(ExceptionChainTests).FullName}.{nameof(Throw)}"), (first.Level, first.Method));
        Assert.Equal(typeof(ExceptionChainTests).Assembly.FullName, first.Assembly);
        Assert.True(first.Line > 0);
        Assert.Equal(Enumerable.Range(0, link.Frames.Count), link.Frames.Select(frame => frame.Level));
    }

    // The folder of the sources, which the symbols name, stands here for the profile of the user.
    [Fact]
    public void Create_SourceFileInAPersonalFolder_LosesTheFolder()
    {
        var exception = Catch(() => Throw());
        var path = new StackTrace(exception, fNeedFileInfo: true).GetFrame(0)!.GetFileName()!;
        var folder = path[..path.LastIndexOfAny(['\\', '/'])];
        var names = new PersonalNames(folder, "jdoe", "LAPTOP-42");

        var link = Assert.Single(ExceptionChain.Create(exception, names));

        Assert.Equal("%USERPROFILE%" + path[folder.Length..], link.Frames[0].FileName);
    }

    // As the SDK does: the frames of both ends, where the exception was thrown and where the work began.
    [Fact]
    public void Create_StackLongerThanTheLimit_KeepsTheFramesOfBothEnds()
    {
        var exception = Catch(() => Recurse(400));
        var all = new StackTrace(exception, fNeedFileInfo: true).FrameCount;

        var link = Assert.Single(ExceptionChain.Create(exception, _names));

        Assert.False(link.HasFullStack);
        Assert.InRange(link.Frames.Sum(frame => frame.Length), 1, ExceptionChain.MaxStackLength);
        Assert.Equal((0, all - 1), (link.Frames[0].Level, link.Frames[^1].Level));
        Assert.True(link.Frames.Zip(link.Frames.Skip(1)).All(pair => pair.First.Level < pair.Second.Level));
    }

    private static Exception Catch(Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            return exception;
        }

        throw new InvalidOperationException("Nothing was thrown.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Throw()
    {
        throw new InvalidOperationException("thrown");
    }

    // Not a tail call, which the compiler of the runtime could turn into a jump: every call is a frame.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Recurse(int depth)
    {
        if (depth == 0)
        {
            throw new InvalidOperationException("deep");
        }

        return Recurse(depth - 1) + 1;
    }
}
