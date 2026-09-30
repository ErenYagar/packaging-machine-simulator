using System;
using System.Collections.Generic;

namespace WaferSaw
{
    public sealed class LogEntry
    {
        public readonly string Time, Message;
        public readonly LogLevel Level;
        public LogEntry(float seconds, string message, LogLevel level)
        { Time = new DateTime(2026, 1, 1, 14, 3, 0).AddSeconds(seconds).ToString("HH:mm:ss"); Message = message; Level = level; }
    }

    public sealed class EventLogger
    {
        private readonly List<LogEntry> entries = new List<LogEntry>();
        public IReadOnlyList<LogEntry> Entries => entries;
        public int Revision { get; private set; }
        public void Write(float seconds, string message, LogLevel level = LogLevel.Info)
        {
            entries.Add(new LogEntry(seconds, message, level));
            if (entries.Count > 250) entries.RemoveAt(0);
            Revision++;
        }
        public void Reset() { entries.Clear(); Revision++; }
    }
}
