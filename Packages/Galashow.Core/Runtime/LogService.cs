using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Galashow.Core
{
    // -----------------------------
    //  Log Level
    // -----------------------------
    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Info  = 2,
        Warn  = 3,
        Error = 4,
        Fatal = 5,
        Off   = 6
    }

    // -----------------------------
    //  Log Event (immutable)
    // -----------------------------
    [Serializable]
    public readonly struct LogEvent
    {
        public readonly long Timestamp;
        public readonly LogLevel Level;
        public readonly string Tag;
        public readonly string Message;
        public readonly string StackTrace;
        public readonly string Exception;

        public LogEvent(long ts, LogLevel level, string tag, string message, string stackTrace, Exception ex)
        {
            Timestamp  = ts;
            Level      = level;
            Tag        = tag ?? string.Empty;
            Message    = message ?? string.Empty;
            StackTrace = stackTrace ?? string.Empty;
            Exception  = ex != null ? ex.GetType().Name + ": " + ex.Message : string.Empty;
        }

        public override string ToString()
        {
            var dt = DateTimeOffset.FromUnixTimeMilliseconds(Timestamp).ToLocalTime().ToString("HH:mm:ss.fff");
            var head = $"[{dt}] [{Level}]" + (string.IsNullOrEmpty(Tag) ? string.Empty : $" [{Tag}]");
            if (!string.IsNullOrEmpty(Exception))
                return $"{head} {Message}\nException: {Exception}\n{StackTrace}";
            if (!string.IsNullOrEmpty(StackTrace))
                return $"{head} {Message}\n{StackTrace}";
            return $"{head} {Message}";
        }
    }

    // -----------------------------
    //  Sink Interface
    // -----------------------------
    public interface ILogSink
    {
        void Write(in LogEvent e, UnityEngine.Object context = null);
    }

    // -----------------------------
    //  Unity Console Sink
    // -----------------------------
    public sealed class UnityConsoleSink : ILogSink
    {
        public bool UseContextObject = true;
        public bool IncludeStackForWarnings = false;

        public void Write(in LogEvent e, UnityEngine.Object context = null)
        {
            var msg = e.ToString();
            var ctx = UseContextObject ? context : null;
            switch (e.Level)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                case LogLevel.Info:
                    if (ctx) Debug.Log(msg, ctx); else Debug.Log(msg);
                    break;
                case LogLevel.Warn:
                    if (ctx) Debug.LogWarning(msg, ctx); else Debug.LogWarning(msg);
                    break;
                case LogLevel.Error:
                case LogLevel.Fatal:
                    if (ctx) Debug.LogError(msg, ctx); else Debug.LogError(msg);
                    break;
            }
        }
    }

    // -----------------------------
    //  In-Memory Ring Buffer Sink
    // -----------------------------
    public sealed class MemoryRingSink : ILogSink
    {
        readonly int _capacity;
        readonly LogEvent[] _buffer;
        int _count;
        int _head; // next write index
        readonly object _lock = new();

        public MemoryRingSink(int capacity = 1024)
        {
            _capacity = Mathf.Max(32, capacity);
            _buffer = new LogEvent[_capacity];
        }

        public void Write(in LogEvent e, UnityEngine.Object _ = null)
        {
            lock (_lock)
            {
                _buffer[_head] = e;
                _head = (_head + 1) % _capacity;
                if (_count < _capacity) _count++;
            }
        }

        public int Count
        {
            get { lock (_lock) return _count; }
        }

        public List<LogEvent> GetRecent(int max)
        {
            lock (_lock)
            {
                var take = Mathf.Clamp(max, 0, _count);
                var list = new List<LogEvent>(take);
                var idx = (_head - 1 + _capacity) % _capacity;
                for (int i = 0; i < take; i++)
                {
                    list.Add(_buffer[idx]);
                    idx = (idx - 1 + _capacity) % _capacity;
                }
                list.Reverse();
                return list;
            }
        }

        public string ExportAsText(int max = 1000)
        {
            var items = GetRecent(max);
            return string.Join("\n", items);
        }
    }

    // -----------------------------
    //  Log Service (Singleton)
    // -----------------------------
    public sealed class LogService : PersistentMonoSingleton<LogService>
    {
        [Header("Config")] [SerializeField]
        private LogLevel _minimumLevel = LogLevel.Info;

        [SerializeField] private bool _useUnityConsole = true;
        [SerializeField] private int _memoryCapacity = 1024;

        public LogLevel MinimumLevel
        {
            get => _minimumLevel;
            set => _minimumLevel = value;
        }

        private readonly List<ILogSink> _sinks = new();
        private MemoryRingSink _memorySink;
        private UnityConsoleSink _unitySink;

        protected override void OnInitializing()
        {
            DontDestroyOnLoad(gameObject);
            _memorySink = new MemoryRingSink(_memoryCapacity);
            _sinks.Add(_memorySink);
            if (_useUnityConsole)
            {
                _unitySink = new UnityConsoleSink();
                _sinks.Add(_unitySink);
            }
        }

        // Allow external layer (e.g., Bridge) to add/remove sinks
        public void AddSink(ILogSink sink)
        {
            if (sink == null) return;
            if (!_sinks.Contains(sink)) _sinks.Add(sink);
        }
        public void RemoveSink(ILogSink sink)
        {
            if (sink == null) return;
            _sinks.Remove(sink);
        }

        // Core logging entry
        public void Log(
            LogLevel level,
            string message,
            string tag = null,
            UnityEngine.Object context = null,
            Exception ex = null,
            string stackTrace = null)
        {
            if (level < _minimumLevel || _minimumLevel == LogLevel.Off) return;
            var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var ev = new LogEvent(ts, level, tag, message, stackTrace, ex);
            foreach (var s in _sinks)
                s.Write(ev, context);
        }

        // Lazy message factory (avoid building strings when filtered out)
        public void Log(LogLevel level,
                        Func<string> messageFactory,
                        string tag = null,
                        UnityEngine.Object context = null,
                        Exception ex = null,
                        string stackTrace = null)
        {
            if (level < _minimumLevel || _minimumLevel == LogLevel.Off) return;
            string msg = null;
            try { msg = messageFactory?.Invoke(); }
            catch (Exception mfEx) { msg = $"<messageFactory threw> {mfEx.Message}"; ex ??= mfEx; }
            Log(level, msg, tag, context, ex, stackTrace);
        }

        // Utilities
        public IReadOnlyList<LogEvent> GetRecent(int max = 200) => _memorySink.GetRecent(max);
        public string DumpText(int max = 1000) => _memorySink.ExportAsText(max);
    }

    // -----------------------------
    //  Static Facade (Ergonomics)
    // -----------------------------
    public static class GLog
    {
        static string CallerToTag(string provided,
            [CallerMemberName] string member = null,
            [CallerFilePath] string file = null)
        {
            if (!string.IsNullOrEmpty(provided)) return provided;
            if (!string.IsNullOrEmpty(file))
            {
                try
                {
                    var name = System.IO.Path.GetFileNameWithoutExtension(file);
                    return string.IsNullOrEmpty(member) ? name : $"{name}.{member}";
                }
                catch { }
            }
            return member ?? "";
        }

        public static void Trace(string msg, string tag = null, UnityEngine.Object ctx = null) =>
            LogService.Instance.Log(LogLevel.Trace, msg, CallerToTag(tag), ctx);
        public static void Debug(string msg, string tag = null, UnityEngine.Object ctx = null) =>
            LogService.Instance.Log(LogLevel.Debug, msg, CallerToTag(tag), ctx);
        public static void Info(string msg, string tag = null, UnityEngine.Object ctx = null) =>
            LogService.Instance.Log(LogLevel.Info, msg, CallerToTag(tag), ctx);
        public static void Warn(string msg, string tag = null, UnityEngine.Object ctx = null) =>
            LogService.Instance.Log(LogLevel.Warn, msg, CallerToTag(tag), ctx);
        public static void Error(string msg, string tag = null, UnityEngine.Object ctx = null, Exception ex = null) =>
            LogService.Instance.Log(LogLevel.Error, msg, CallerToTag(tag), ctx, ex);
        public static void Fatal(string msg, string tag = null, UnityEngine.Object ctx = null, Exception ex = null) =>
            LogService.Instance.Log(LogLevel.Fatal, msg, CallerToTag(tag), ctx, ex);

        // Lazy overloads
        public static void Trace(Func<string> f, string tag = null, UnityEngine.Object ctx = null) =>
            LogService.Instance.Log(LogLevel.Trace, f, CallerToTag(tag), ctx);
        public static void Debug(Func<string> f, string tag = null, UnityEngine.Object ctx = null) =>
            LogService.Instance.Log(LogLevel.Debug, f, CallerToTag(tag), ctx);
        public static void Info(Func<string> f, string tag = null, UnityEngine.Object ctx = null) =>
            LogService.Instance.Log(LogLevel.Info, f, CallerToTag(tag), ctx);
        public static void Warn(Func<string> f, string tag = null, UnityEngine.Object ctx = null) =>
            LogService.Instance.Log(LogLevel.Warn, f, CallerToTag(tag), ctx);
        public static void Error(Func<string> f, string tag = null, UnityEngine.Object ctx = null, Exception ex = null) =>
            LogService.Instance.Log(LogLevel.Error, f, CallerToTag(tag), ctx, ex);
        public static void Fatal(Func<string> f, string tag = null, UnityEngine.Object ctx = null, Exception ex = null) =>
            LogService.Instance.Log(LogLevel.Fatal, f, CallerToTag(tag), ctx, ex);
    }
}
