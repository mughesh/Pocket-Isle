using System;
using System.Collections.Generic;

namespace Diorama
{
    /// <summary>
    /// The lesson chain's logic, no scene objects (EditMode-tested): one lesson at a time, in order. A lesson waits for
    /// its start condition (and delay), teaches until done, invalid or skipped, then the next one comes. Lesson events
    /// (<see cref="Mark"/>) are remembered, so doing something early completes its lesson the moment it comes up.
    /// </summary>
    public class LessonRunner
    {
        public enum Phase { Idle, Waiting, Teaching, Finished }

        public enum Outcome { None, Started, Done, Skipped, Rewound }

        readonly IReadOnlyList<LessonData> _lessons;
        readonly HashSet<string> _eventNames;
        readonly HashSet<string> _events = new HashSet<string>();
        float _held;
        bool _entered;

        public int Index { get; private set; } = -1;
        public Phase State { get; private set; } = Phase.Idle;
        /// <summary>Seconds the current lesson has been teaching.</summary>
        public float Teaching { get; private set; }
        public LessonData Current => Index >= 0 && Index < _lessons.Count ? _lessons[Index] : null;
        public bool IsFinished => State == Phase.Finished;
        public bool IsRunning => State == Phase.Waiting || State == Phase.Teaching;
        /// <summary>Why the last lesson ended (Done, Skipped) or rewound, for logs.</summary>
        public string LastReason { get; private set; }

        /// <param name="eventNames">Names that are lesson events (false until marked) rather than <see cref="GameConditions"/> states.</param>
        public LessonRunner(IReadOnlyList<LessonData> lessons, IEnumerable<string> eventNames)
        {
            _lessons = lessons ?? Array.Empty<LessonData>();
            _eventNames = new HashSet<string>(eventNames ?? Array.Empty<string>());
        }

        public bool IsEvent(string name) => _eventNames.Contains(name);

        /// <summary>Records that something happened (stays true until a rewind clears it).</summary>
        public bool Mark(string name)
        {
            if (!_eventNames.Contains(name)) return false;
            _events.Add(name);
            return true;
        }

        public bool Happened(string name) => _events.Contains(name);

        public bool Holds(string expression) =>
            GameConditions.Check(expression, n => _eventNames.Contains(n) ? _events.Contains(n) : (bool?)null);

        /// <summary>Starts the chain at a lesson (0 = from the top).</summary>
        public void Begin(int index = 0)
        {
            Index = Math.Max(0, index);
            Enter();
        }

        /// <summary>
        /// Picks up a saved lesson. A lesson that can rewind (the battle lessons) restarts at its rewind lesson: the
        /// battle it needs is gone after a reload. Unknown id = from the top.
        /// </summary>
        public void Resume(string id)
        {
            int i = IndexOf(id);
            if (i >= 0 && !string.IsNullOrEmpty(_lessons[i].rewindTo))
            {
                int back = IndexOf(_lessons[i].rewindTo);
                if (back >= 0) i = back;
            }
            Begin(Math.Max(0, i));
        }

        public void Finish()
        {
            Index = _lessons.Count;
            State = Phase.Finished;
        }

        int IndexOf(string id)
        {
            if (string.IsNullOrEmpty(id)) return -1;
            for (int i = 0; i < _lessons.Count; i++)
                if (_lessons[i].id == id) return i;
            return -1;
        }

        void Enter()
        {
            _held = 0f;
            _entered = false;
            Teaching = 0f;
            State = Index >= _lessons.Count ? Phase.Finished : Phase.Waiting;
        }

        void Advance(string reason)
        {
            LastReason = reason;
            Index++;
            Enter();
        }

        /// <summary>Back to an earlier lesson; the events of every lesson from there on are forgotten.</summary>
        public void RewindTo(string id)
        {
            int to = IndexOf(id);
            if (to < 0) to = Math.Max(0, Index);
            for (int i = to; i < _lessons.Count; i++)
            {
                Forget(_lessons[i].doneWhen);
                Forget(_lessons[i].failWhen);
            }
            Index = to;
            Enter();
        }

        void Forget(string expression)
        {
            if (string.IsNullOrEmpty(expression)) return;
            foreach (var part in expression.Split('|', '&'))
                _events.Remove(part.Trim().TrimStart('!').Trim());
        }

        /// <summary>One frame. Started = the lesson begins teaching now (show the line and the ghost).</summary>
        public Outcome Tick(float dt)
        {
            var l = Current;
            if (!IsRunning || l == null) return Outcome.None;
            if (!string.IsNullOrEmpty(l.failWhen) && Holds(l.failWhen))
            {
                LastReason = $"{l.failWhen}: back to {l.rewindTo}";
                RewindTo(l.rewindTo);
                return Outcome.Rewound;
            }
            if (!string.IsNullOrEmpty(l.doneWhen) && Holds(l.doneWhen))
            {
                Advance(State == Phase.Waiting ? "done ahead" : "done");
                return Outcome.Done;
            }
            // Validity counts once the lesson's moment has come (its start held at least once).
            if (_entered && !string.IsNullOrEmpty(l.validWhile) && !Holds(l.validWhile))
            {
                Advance($"{l.validWhile} ended");
                return Outcome.Skipped;
            }
            if (State == Phase.Waiting)
            {
                if (!Holds(l.startWhen))
                {
                    _held = 0f;
                    return Outcome.None;
                }
                _entered = true;
                _held += dt;
                if (_held < l.delay) return Outcome.None;
                State = Phase.Teaching;
                return Outcome.Started;
            }
            Teaching += dt;
            if (l.skipAfter > 0f && Teaching >= l.skipAfter)
            {
                Advance($"skipped after {l.skipAfter:0} s");
                return Outcome.Skipped;
            }
            return Outcome.None;
        }
    }
}
