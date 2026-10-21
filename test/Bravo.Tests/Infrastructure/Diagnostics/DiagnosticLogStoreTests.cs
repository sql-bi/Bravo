using System.Linq;
using Sqlbi.Bravo.Infrastructure.Diagnostics;
using Xunit;

namespace Bravo.Tests.Infrastructure.Diagnostics;

public class DiagnosticLogStoreTests
{
    [Fact]
    public void Read_ReturnsEveryRecordHeldWhenTheCursorIsZero()
    {
        var store = new DiagnosticLogStore();

        store.Add(TestLogEntries.Create("first"));
        store.Add(TestLogEntries.Create("second"));

        var page = store.Read(after: 0);
        Assert.Equal(["first", "second"], page.Entries.Select((e) => e.Message));
        Assert.Equal(2, page.LastSequence);
        Assert.Equal(0, page.DroppedCount);
    }

    [Fact]
    public void Read_ReturnsOnlyTheRecordsAfterTheCursor()
    {
        var store = new DiagnosticLogStore();
        store.Add(TestLogEntries.Create("first"));
        store.Add(TestLogEntries.Create("second"));
        store.Add(TestLogEntries.Create("third"));

        var page = store.Read(after: 2);

        Assert.Equal("third", Assert.Single(page.Entries).Message);
        Assert.Equal(3, page.LastSequence);
    }

    [Fact]
    public void Read_DoesNotConsumeTheRecords()
    {
        var store = new DiagnosticLogStore();
        store.Add(TestLogEntries.Create("kept"));

        store.Read(after: 0);
        var page = store.Read(after: 0);

        Assert.Equal("kept", Assert.Single(page.Entries).Message);
    }

    [Fact]
    public void Read_ReportsTheRecordsEvictedAfterTheCursor()
    {
        var store = new DiagnosticLogStore(capacity: 2);
        store.Add(TestLogEntries.Create("first"));
        store.Add(TestLogEntries.Create("second"));
        store.Add(TestLogEntries.Create("third"));
        store.Add(TestLogEntries.Create("fourth"));

        var page = store.Read(after: 0);

        Assert.Equal(["third", "fourth"], page.Entries.Select((e) => e.Message));
        Assert.Equal(2, page.DroppedCount);
        Assert.Equal(0, store.Read(after: 2).DroppedCount);
    }

    [Fact]
    public void Read_ReturnsTheNewestRecordsUpToTheMaximumCount()
    {
        var store = new DiagnosticLogStore();
        store.Add(TestLogEntries.Create("first"));
        store.Add(TestLogEntries.Create("second"));
        store.Add(TestLogEntries.Create("third"));

        var page = store.Read(after: 0, maxCount: 2);

        Assert.Equal(["second", "third"], page.Entries.Select((e) => e.Message));
        Assert.Equal(1, page.DroppedCount);
        Assert.Equal(3, page.LastSequence);
    }

    [Fact]
    public void Add_EvictsTheOldestRecordsBeyondTheCharacterBudget()
    {
        var store = new DiagnosticLogStore(characterBudget: 20);
        store.Add(TestLogEntries.Create(new string('a', 10)));
        store.Add(TestLogEntries.Create(new string('b', 10)));
        store.Add(TestLogEntries.Create(new string('c', 10)));

        var page = store.Read(after: 0);

        Assert.Equal([new string('c', 10)], page.Entries.Select((e) => e.Message));
        Assert.Equal(2, page.DroppedCount);
    }

    [Fact]
    public void Add_KeepsTheNewestRecordEvenWhenItExceedsTheCharacterBudget()
    {
        var store = new DiagnosticLogStore(characterBudget: 5);
        store.Add(TestLogEntries.Create("small"));
        store.Add(TestLogEntries.Create(new string('x', 100)));

        var page = store.Read(after: 0);

        Assert.Equal(new string('x', 100), Assert.Single(page.Entries).Message);
    }

    [Fact]
    public void Read_ToleratesANegativeCursor()
    {
        var store = new DiagnosticLogStore();
        store.Add(TestLogEntries.Create("only"));

        var page = store.Read(after: -5);

        Assert.Single(page.Entries);
        Assert.Equal(0, page.DroppedCount);
    }

    [Fact]
    public void Read_ReturnsNothingForACursorBeyondTheLastSequence()
    {
        var store = new DiagnosticLogStore();
        store.Add(TestLogEntries.Create("only"));

        var page = store.Read(after: 10);

        Assert.Empty(page.Entries);
        Assert.Equal(1, page.LastSequence);
        Assert.Equal(0, page.DroppedCount);
    }
}
