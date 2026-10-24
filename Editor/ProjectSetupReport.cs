using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Larje.ProjectSetup.Editor
{
    public sealed class ProjectSetupReport
    {
        private readonly List<Entry> _entries = new List<Entry>();

        public bool HasErrors => _entries.Any(x => x.Level == EntryLevel.Error);
        public bool HasPending => _entries.Any(x => x.Level == EntryLevel.Pending);
        public IReadOnlyList<Entry> Entries => _entries;

        public void Info(string step, string message)
        {
            _entries.Add(new Entry(step, EntryLevel.Info, message));
        }

        public void Warning(string step, string message)
        {
            _entries.Add(new Entry(step, EntryLevel.Warning, message));
        }

        public void Error(string step, string message)
        {
            _entries.Add(new Entry(step, EntryLevel.Error, message));
        }

        public void Pending(string step, string message)
        {
            _entries.Add(new Entry(step, EntryLevel.Pending, message));
        }

        public string Format()
        {
            StringBuilder builder = new StringBuilder();
            string status = HasErrors ? "FAILED" : HasPending ? "PENDING" : "DONE";
            builder.AppendLine("LARJE_SETUP " + status);
            foreach (Entry entry in _entries)
            {
                builder.AppendLine($"[{entry.Level}] {entry.Step}: {entry.Message}");
            }

            return builder.ToString();
        }

        public void Log()
        {
            if (HasErrors)
            {
                Debug.LogError(Format());
            }
            else
            {
                Debug.Log(Format());
            }
        }

        public void Save(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, Format());
        }

        public enum EntryLevel
        {
            Info,
            Warning,
            Pending,
            Error
        }

        public sealed class Entry
        {
            public string Step { get; }
            public EntryLevel Level { get; }
            public string Message { get; }

            public Entry(string step, EntryLevel level, string message)
            {
                Step = step;
                Level = level;
                Message = message;
            }
        }
    }
}
