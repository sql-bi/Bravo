using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Bravo.Api.Diagnostics;

/// <summary>
/// The last records of the log of the host, numbered in the order they were written, which the diagnostics pane
/// of the page reads.
/// </summary>
public sealed class LogBuffer
{
    /// <summary>
    /// How many records are kept: more than a run writes, unless something goes wrong again and again.
    /// </summary>
    public const int DefaultCapacity = 1000;

    private readonly Lock _gate = new();
    private readonly Queue<LogRecord> _records;
    private readonly int _capacity;
    private int _lastNumber;

    public LogBuffer(int capacity = DefaultCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);

        _records = new Queue<LogRecord>(capacity);
        _capacity = capacity;
    }

    /// <summary>
    /// Adds a record, which takes the place of the oldest one when the buffer is full.
    /// </summary>
    /// <param name="create">Creates the record from the number it gets.</param>
    /// <returns>The record that was added.</returns>
    public LogRecord Append(Func<int, LogRecord> create)
    {
        ArgumentNullException.ThrowIfNull(create);

        lock (_gate)
        {
            _lastNumber++;
            if (_records.Count == _capacity)
            {
                _records.Dequeue();
            }

            var record = create(_lastNumber);
            _records.Enqueue(record);

            return record;
        }
    }

    /// <summary>
    /// Reads the records after <paramref name="after"/>.
    /// </summary>
    /// <param name="after">The number of the last record that the caller has read, or 0 for all that are kept.</param>
    public LogBatch Read(int after)
    {
        lock (_gate)
        {
            return new LogBatch
            {
                Records = [.. _records.Where(record => record.Number > after)],
                Next = _lastNumber,
            };
        }
    }
}
