using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Galashow.Core
{
    public class TaskRunner : PersistentMonoSingleton<TaskRunner>
    {
        int _seq;
        readonly Dictionary<int, Coroutine> _running = new();
        readonly Dictionary<object, List<int>> _byOwner = new();

        public TaskHandle NextFrame(Action action, object owner = null) =>
            DelayFrames(1, action, owner);

        public TaskHandle Delay(float seconds, Action action, object owner = null, bool unscaled = false)
        {
            return StartTask(W_Delay(seconds, action, unscaled), owner);
        }

        public TaskHandle DelayFrames(int frames, Action action, object owner = null)
        {
            return StartTask(W_DelayFrames(frames, action), owner);
        }

        public TaskHandle Every(float interval, Action tick, object owner = null, float? duration = null, bool unscaled = false)
        {
            return StartTask(W_Every(interval, tick, duration, unscaled), owner);
        }

        public TaskHandle Until(Func<bool> predicate, Action onDone, object owner = null)
        {
            return StartTask(W_Until(predicate, onDone), owner);
        }

        public void Cancel(ITaskHandle handle)
        {
            if (handle is not TaskHandle h || !_running.TryGetValue(h.Id, out var co)) return;
            StopCoroutine(co);
            _running.Remove(h.Id);
            RemoveFromOwner(h.Id);
            h.Active = false;
        }

        public void CancelAll(object owner)
        {
            if (owner == null) return;
            if (!_byOwner.TryGetValue(owner, out var list)) return;
            foreach (var id in list)
            {
                if (_running.TryGetValue(id, out var co)) StopCoroutine(co);
                _running.Remove(id);
            }
            _byOwner.Remove(owner);
        }

        TaskHandle StartTask(IEnumerator routine, object owner)
        {
            var h = new TaskHandle { Id = ++_seq, Runner = this, Active = true };
            var co = StartCoroutine(W_Wrap(h, routine));
            _running[h.Id] = co;
            if (owner != null)
            {
                if (!_byOwner.TryGetValue(owner, out var list)) _byOwner[owner] = list = new List<int>(4);
                list.Add(h.Id);
            }
            return h;
        }

        IEnumerator W_Wrap(TaskHandle h, IEnumerator inner)
        {
            yield return inner;
            _running.Remove(h.Id);
            RemoveFromOwner(h.Id);
            h.Active = false;
        }

        void RemoveFromOwner(int id)
        {
            foreach (var kv in _byOwner)
            {
                if (kv.Value.Remove(id))
                {
                    if (kv.Value.Count == 0) _byOwner.Remove(kv.Key);
                    break;
                }
            }
        }

        static IEnumerator W_Delay(float seconds, Action action, bool unscaled)
        {
            var end = (unscaled ? Time.unscaledTime : Time.time) + Mathf.Max(0f, seconds);
            while ((unscaled ? Time.unscaledTime : Time.time) < end) yield return null;
            action?.Invoke();
        }

        static IEnumerator W_DelayFrames(int frames, Action action)
        {
            for (int i = 0; i < Mathf.Max(0, frames); i++) yield return null;
            action?.Invoke();
        }

        static IEnumerator W_Every(float interval, Action tick, float? duration, bool unscaled)
        {
            float elapsed = 0f, acc = 0f;
            while (!duration.HasValue || elapsed < duration.Value)
            {
                yield return null;
                var dt = unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
                elapsed += dt; acc += dt;
                if (acc >= interval)
                {
                    tick?.Invoke();
                    acc = 0f;
                }
            }
        }

        static IEnumerator W_Until(Func<bool> predicate, Action onDone)
        {
            while (!predicate()) yield return null;
            onDone?.Invoke();
        }
    }
}
