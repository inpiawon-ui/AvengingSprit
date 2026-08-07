using System;
using System.Collections.Generic;

namespace GameFramework.Core.Module.Timer
{
    public sealed class TimerManager : ITimerManager
    {
        private readonly List<TimerEntry> _entries     = new();
        private readonly List<TimerEntry> _toAdd       = new();   // Tick 중 추가 버퍼
        private int                       _nextId      = 1;
        private bool                      _isTicking;

        // ──────────────────────────────────────────────
        // ITimerManager
        // ──────────────────────────────────────────────

        public TimerHandle Delay(float seconds, Action onComplete) =>
            AddEntry(seconds, onComplete, 1);

        public TimerHandle Interval(float seconds, Action onTick, int count = -1) =>
            AddEntry(seconds, onTick, count == 0 ? 1 : count);

        public void Cancel(TimerHandle handle)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                if (e.Id == handle.Id) { e.IsCancelled = true; _entries[i] = e; return; }
            }
            // Tick 중 추가된 항목도 탐색
            for (int i = 0; i < _toAdd.Count; i++)
            {
                var e = _toAdd[i];
                if (e.Id == handle.Id) { e.IsCancelled = true; _toAdd[i] = e; return; }
            }
        }

        public void CancelAll()
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i]; e.IsCancelled = true; _entries[i] = e;
            }
            _toAdd.Clear();
        }

        public void Pause(TimerHandle handle)  => SetPaused(handle.Id, true);
        public void Resume(TimerHandle handle) => SetPaused(handle.Id, false);

        public void PauseAll()
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i]; e.IsPaused = true; _entries[i] = e;
            }
        }

        public void ResumeAll()
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i]; e.IsPaused = false; _entries[i] = e;
            }
        }

        // ──────────────────────────────────────────────
        // Tick (GameBootstrapper에서 매 프레임 호출)
        // ──────────────────────────────────────────────

        public void Tick(float deltaTime)
        {
            _isTicking = true;

            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                var e = _entries[i];
                if (e.IsCancelled) { _entries.RemoveAt(i); continue; }
                if (e.IsPaused)    continue;

                e.Elapsed += deltaTime;
                if (e.Elapsed >= e.Duration)
                {
                    e.Elapsed -= e.Duration;
                    e.Callback?.Invoke();

                    if (e.RemainCount > 0)
                    {
                        e.RemainCount--;
                        if (e.RemainCount == 0) { _entries.RemoveAt(i); continue; }
                    }
                }
                _entries[i] = e;
            }

            _isTicking = false;

            // 버퍼링된 항목 플러시
            if (_toAdd.Count > 0)
            {
                _entries.AddRange(_toAdd);
                _toAdd.Clear();
            }
        }

        // ──────────────────────────────────────────────
        // 내부
        // ──────────────────────────────────────────────

        private TimerHandle AddEntry(float duration, Action callback, int count)
        {
            int id = _nextId++;
            var entry = new TimerEntry
            {
                Id          = id,
                Elapsed     = 0f,
                Duration    = duration,
                RemainCount = count,
                IsPaused    = false,
                IsCancelled = false,
                Callback    = callback,
            };

            if (_isTicking) _toAdd.Add(entry);
            else            _entries.Add(entry);

            return new TimerHandle(id);
        }

        private void SetPaused(int id, bool paused)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                if (e.Id == id) { e.IsPaused = paused; _entries[i] = e; return; }
            }
        }
    }
}
