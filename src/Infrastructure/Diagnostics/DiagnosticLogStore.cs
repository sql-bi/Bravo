using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Sqlbi.Bravo.Infrastructure.Diagnostics;

/// <summary>
/// Keeps the most recent log records in memory for the diagnostics pane, oldest evicted first.
/// </summary>
/// <remarks>
/// Readers keep their own cursor: reading does not change the store, so a reload of the pane reads the same records.
/// The store is bounded both by the number of records and by their text, because a single record can carry a
/// serialized payload of several megabytes.
/// </remarks>
public sealed class DiagnosticLogStore
{
    public const int DefaultCapacity = 1000;
    public const long DefaultCharacterBudget = 8 * 1024 * 1024;

    private readonly Queue<Slot> _slots = new();
    private readonly Lock _lock = new();
    private readonly int _capacity;
    private readonly long _characterBudget;
    private long _lastSequence;
    private long _characters;

    public DiagnosticLogStore(int capacity = DefaultCapacity, long characterBudget = DefaultCharacterBudget)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(characterBudget);

        _capacity = capacity;
        _characterBudget = characterBudget;
    }

    public void Add(DiagnosticLogEntry entry)
    {
        var size = CountCharacters(entry);

        lock (_lock)
        {
            _lastSequence++;
            _slots.Enqueue(new Slot(_lastSequence, entry, size));
            _characters += size;

            // The newest record is kept even when it alone exceeds the budget.
            while (_slots.Count > _capacity || (_characters > _characterBudget && _slots.Count > 1))
            {
                var evicted = _slots.Dequeue();
                _characters -= evicted.Size;
            }
        }
    }

    /// <summary>
    /// Reads the records added after <paramref name="after"/>, at most the newest <paramref name="maxCount"/> of them.
    /// </summary>
    public DiagnosticLogPage Read(long after, int maxCount = int.MaxValue)
    {
        var cursor = Math.Max(after, 0);
        var limit = Math.Max(maxCount, 0);

        lock (_lock)
        {
            var firstHeld = _slots.Count > 0 ? _slots.Peek().Sequence : _lastSequence + 1;
            var evictedCount = Math.Max(firstHeld - (cursor + 1), 0);
            var heldCount = Math.Max(_lastSequence - Math.Max(cursor, firstHeld - 1), 0);
            var skippedCount = Math.Max(heldCount - limit, 0);

            var entries = _slots
                .Where((slot) => slot.Sequence > cursor)
                .Skip((int)skippedCount)
                .Select((slot) => slot.Entry)
                .ToArray();

            return new DiagnosticLogPage(entries, _lastSequence, evictedCount + skippedCount);
        }
    }

    private static long CountCharacters(DiagnosticLogEntry entry)
    {
        long count = entry.Category.Length + entry.Message.Length + (entry.Template?.Length ?? 0);

        count += CountCharacters(entry.Properties);

        if (entry.Exception is not null)
        {
            foreach (var exception in entry.Exception)
            {
                count += exception.Type.Length + exception.Message.Length + (exception.StackTrace?.Length ?? 0);
                count += CountCharacters(exception.Details);
            }
        }

        return count;
    }

    private static long CountCharacters(IReadOnlyDictionary<string, string?>? values)
    {
        if (values is null)
        {
            return 0;
        }

        long count = 0;
        foreach (var (key, value) in values)
        {
            count += key.Length + (value?.Length ?? 0);
        }

        return count;
    }

    private readonly record struct Slot(long Sequence, DiagnosticLogEntry Entry, long Size);
}
