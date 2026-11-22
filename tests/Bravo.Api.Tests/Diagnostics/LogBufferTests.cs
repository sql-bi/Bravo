using System;
using System.Linq;
using Bravo.Api.Diagnostics;
using Xunit;

namespace Bravo.Api.Tests.Diagnostics;

public sealed class LogBufferTests
{
    private readonly LogBuffer _buffer = new(capacity: 4);

    [Fact]
    public void Read_RecordsAppendedBefore_ReturnsThemNumberedFromOne()
    {
        Append(3);

        var batch = _buffer.Read(after: 0);

        Assert.Equal([1, 2, 3], batch.Records.Select(record => record.Number));
        Assert.Equal(3, batch.Next);
    }

    [Fact]
    public void Read_AfterANumber_ReturnsOnlyTheRecordsThatFollowIt()
    {
        Append(3);

        var batch = _buffer.Read(after: 2);

        Assert.Equal([3], batch.Records.Select(record => record.Number));
        Assert.Equal(3, batch.Next);
    }

    [Fact]
    public void Read_NoRecordYet_ReturnsNoneAndNumberZero()
    {
        var batch = _buffer.Read(after: 0);

        Assert.Empty(batch.Records);
        Assert.Equal(0, batch.Next);
    }

    // A page that was open before the host started again asks after a number that the new host has not reached.
    [Fact]
    public void Read_NumberBeyondTheLastRecord_ReturnsNoneAndTheNumberToGoBackTo()
    {
        Append(2);

        var batch = _buffer.Read(after: 50);

        Assert.Empty(batch.Records);
        Assert.Equal(2, batch.Next);
    }

    [Fact]
    public void Append_MoreRecordsThanTheBufferKeeps_DropsTheOldestAndKeepsTheNumbers()
    {
        Append(6);

        var batch = _buffer.Read(after: 0);

        Assert.Equal([3, 4, 5, 6], batch.Records.Select(record => record.Number));
        Assert.Equal(6, batch.Next);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_CapacityThatIsNotPositive_Throws(int capacity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LogBuffer(capacity));
    }

    private void Append(int count)
    {
        for (var i = 0; i < count; i++)
        {
            _buffer.Append(number => new LogRecord
            {
                Number = number,
                Time = DateTimeOffset.UnixEpoch,
                Level = LogRecordLevel.Information,
                Category = "Bravo.Tests",
                EventId = 1,
                Message = "Something happened.",
            });
        }
    }
}
