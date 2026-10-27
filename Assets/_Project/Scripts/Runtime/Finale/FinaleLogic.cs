using System;
using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>The finale's beats in order (saved as the puzzle <see cref="FinaleLogic.StageId"/>'s stage).</summary>
    public enum FinaleStep { None = 0, Wake = 1, Understanding = 2, Goodbye = 3, EyesOpen = 4, HallOfFame = 5, Credits = 6, Spark = 7, Done = 8 }

    /// <summary>
    /// Step 13, the pure part of the statue's whispers, the finale's state machine and the post-game (EditMode-tested):
    /// which lines a shard locking in says, how far the statue has woken, where a finale resumes after quitting, where
    /// an Isling was met, a realm's roster and how much of it is caught, and the Keeper rematch's level.
    /// </summary>
    public static class FinaleLogic
    {
        /// <summary>Saved progress (stage = the <see cref="FinaleStep"/> reached).</summary>
        public const string StageId = "finale";
        /// <summary>Saved once the Spark has said hello (free play).</summary>
        public const string DoneId = "finale.done";
        public const string StillIsle = "the Still Isle";

        /// <summary>"shard.fire" -> "fire".</summary>
        public static string Element(string shardId) =>
            string.IsNullOrEmpty(shardId) ? "" : shardId.StartsWith("shard.") ? shardId.Substring(6) : shardId;

        /// <summary>
        /// A shard locks in with <paramref name="placed"/> shards in the statue now: the Heart's memory of that element,
        /// then the Spirit's reaction (1 and 2; the third starts the finale instead).
        /// </summary>
        public static List<string> WhisperLines(string shardId, int placed)
        {
            var lines = new List<string>();
            string e = Element(shardId);
            if (e.Length > 0) lines.Add("heart.dream." + e);
            if (placed == 1 || placed == 2) lines.Add("spirit.dream." + placed);
            return lines;
        }

        /// <summary>The statue's wake: 0 asleep, 1 eyes glow faintly, 2 breathing and swaying moss, 3 awake (the Heart).</summary>
        public static int StatueStage(int placed) => Mathf.Clamp(placed, 0, 3);

        /// <summary>
        /// Where the finale starts when the hub shows: nothing before all three shards; from the wake when it never
        /// started; at the goodbye when the player quit in the middle (the Spirit asks again); done stays done.
        /// </summary>
        public static FinaleStep ResumeStep(int placed, int savedStage, bool done)
        {
            if (done) return FinaleStep.Done;
            if (placed < 3) return FinaleStep.None;
            return savedStage <= (int)FinaleStep.None ? FinaleStep.Wake : FinaleStep.Goodbye;
        }

        /// <summary>The next beat after <paramref name="step"/> (the goodbye only moves on when the Spirit is in the socket).</summary>
        public static FinaleStep Next(FinaleStep step) => step >= FinaleStep.Done ? FinaleStep.Done : step + 1;

        /// <summary>Walking, wild battles and the menu's Title wait while these play.</summary>
        public static bool Locks(FinaleStep step) => step > FinaleStep.None && step < FinaleStep.Done;

        // ---- where each friend was met ----

        /// <summary>The name shown on a Hall of Fame card for a map's realm id.</summary>
        public static string RealmName(string realm)
        {
            switch ((realm ?? "").ToLowerInvariant())
            {
                case "cinderwood": return "Cinderwood";
                case "galewick": return "Galewick";
                case "lilymere": return "Lilymere";
                default: return StillIsle;
            }
        }

        /// <summary>
        /// Old saves (no "met in"): the first realm whose roster has the species, else the Still Isle (the starters and
        /// gifts). <paramref name="rosters"/> = realm name -> roster.
        /// </summary>
        public static string GuessMetIn(IslingSpecies species, IEnumerable<KeyValuePair<string, HashSet<IslingSpecies>>> rosters)
        {
            if (species != null && rosters != null)
                foreach (var kv in rosters)
                    if (kv.Value != null && kv.Value.Contains(species)) return kv.Key;
            return StillIsle;
        }

        /// <summary>A Hall of Fame card: "Kilnpot · Lv 18 · met in Cinderwood".</summary>
        public static string Card(string name, int level, string metIn) => $"{name} · Lv {level}\nmet in {(string.IsNullOrEmpty(metIn) ? StillIsle : metIn)}";

        // ---- collection ----

        /// <summary>
        /// A realm's roster: every species in its grass and water tables (zones too) and what they evolve into; no Keepers.
        /// </summary>
        public static HashSet<IslingSpecies> Roster(WorldMap map)
        {
            var set = new HashSet<IslingSpecies>();
            if (map == null) return set;
            void Table(EncounterTable t)
            {
                if (t == null || t.entries == null) return;
                foreach (var e in t.entries)
                    for (var s = e.species; s != null && !s.keeper && set.Add(s); s = s.evolvesTo) { }
            }
            Table(map.grassTable);
            Table(map.waterTable);
            if (map.zoneLegend != null)
                foreach (var z in map.zoneLegend) Table(z.grassTable);
            return set;
        }

        /// <summary>Caught of a roster (species ids as the save keeps them).</summary>
        public static int CaughtOf(HashSet<IslingSpecies> roster, ICollection<string> caught)
        {
            int n = 0;
            if (roster == null || caught == null) return 0;
            foreach (var s in roster)
                if (caught.Contains(SpeciesRegistry.IdOf(s))) n++;
            return n;
        }

        /// <summary>"Cinderwood 9/12 · Galewick 8/11 · Lilymere 5/11".</summary>
        public static string CollectionLine(IList<(string realm, int caught, int total)> realms)
        {
            var parts = new List<string>();
            foreach (var r in realms) parts.Add($"{r.realm} {r.caught}/{r.total}");
            return string.Join(" · ", parts);
        }

        /// <summary>A friendly Keeper rematch: five levels over the last fight.</summary>
        public static int RematchLevel(int lastLevel) => Mathf.Max(1, lastLevel) + 5;
    }

    /// <summary>
    /// Step 13, the goodbye in the palm (pure, EditMode-tested with a synthetic palm). Hold an open hand out palm up:
    /// the Spirit lands on it and rides there. Bring it within <see cref="socketRadius"/> of the Heart's forehead socket
    /// and it floats off into it. Look + pinch the Heart = it goes by itself after <see cref="gazeDelay"/>. The palm
    /// turning over or the hand lost for <see cref="loseSeconds"/>: it floats back to wait in front of the Heart. After
    /// <see cref="waitSeconds"/> with nothing happening it says the wait line (never forces it).
    /// </summary>
    [Serializable]
    public class PalmHandoff
    {
        public enum State { Waiting, OnPalm, ToSocket }

        [Tooltip("Palm normal · up at least this (palm up).")]
        public float upDot = 0.55f;
        [Tooltip("Palm up this long before it lands (s).")]
        public float landSeconds = 0.35f;
        [Tooltip("Carried this close to the socket (m, real world at island scale 1): it floats in.")]
        public float socketRadius = 0.06f;
        [Tooltip("Palm down or untracked this long while carried: it floats back (s).")]
        public float loseSeconds = 0.6f;
        [Tooltip("Nothing happening this long: the wait line (s).")]
        public float waitSeconds = 30f;
        [Tooltip("Look + pinch the Heart: it goes by itself after this (s).")]
        public float gazeDelay = 1.5f;

        public State Now { get; private set; }
        float _up, _lost, _idle, _gaze = -1f;

        public void Reset()
        {
            Now = State.Waiting;
            _up = _lost = _idle = 0f;
            _gaze = -1f;
        }

        /// <summary>
        /// One frame. <paramref name="palmUp"/> = palm normal · up; <paramref name="open"/> = not a fist;
        /// <paramref name="scale"/> = island scale (the socket radius grows with the island). Returns a line to say or
        /// null ("spirit.finale.hand" on landing, "spirit.finale.wait" when idle, "spirit.finale.3" as it leaves).
        /// </summary>
        public string Step(float dt, bool tracked, float palmUp, bool open, Vector3 palm, Vector3 socket, float scale, bool gazePinch)
        {
            if (Now == State.ToSocket) return null;
            bool up = tracked && open && palmUp >= upDot;
            if (gazePinch && _gaze < 0f) _gaze = 0f;
            if (_gaze >= 0f)
            {
                _gaze += dt;
                if (_gaze >= gazeDelay)
                {
                    Now = State.ToSocket;
                    return "spirit.finale.3";
                }
            }
            _idle += dt;
            if (Now == State.Waiting)
            {
                _up = up ? _up + dt : 0f;
                if (_up >= landSeconds)
                {
                    Now = State.OnPalm;
                    _lost = 0f;
                    _idle = 0f;
                    return "spirit.finale.hand";
                }
            }
            else if (Now == State.OnPalm)
            {
                if ((palm - socket).magnitude <= socketRadius * Mathf.Max(1f, scale) && tracked)
                {
                    Now = State.ToSocket;
                    return "spirit.finale.3";
                }
                _lost = up ? 0f : _lost + dt;
                if (_lost >= loseSeconds)
                {
                    Now = State.Waiting;
                    _up = 0f;
                }
            }
            if (_idle >= waitSeconds)
            {
                _idle = 0f;
                return "spirit.finale.wait";
            }
            return null;
        }
    }
}
