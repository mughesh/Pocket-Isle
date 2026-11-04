using System;
using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 14h: the Isling Journal's rules (pure, EditMode-tested). Each Isling is unseen, seen (met in battle) or caught
    /// (caught, or evolved into). The game holds the two id sets (the save's <c>caught</c> and <c>seen</c>); everything
    /// that counts Islings (the Journal, the toast, the Heart's collection card) reads them through here.
    /// 46 Islings: 20 Fire, 14 Water, 12 Wind; the Keepers and the Lantern Spirit don't count.
    /// </summary>
    public static class Journal
    {
        public enum State { Unseen, Seen, Caught }

        public static readonly Element[] Elements = { Element.Fire, Element.Water, Element.Wind };

        public const string SpiritId = "LanternSpirit", FirstLine = "journal.first";

        /// <summary>The game's caught / seen id sets (GameSave sets these). Empty until a game is loaded.</summary>
        public static Func<ICollection<string>> CaughtIds = () => Array.Empty<string>();
        public static Func<ICollection<string>> SeenIds = () => Array.Empty<string>();
        /// <summary>The player's Islings (pods and the lodge), for "Lv N of your best one".</summary>
        public static Func<IEnumerable<IslingInstance>> Owned = () => Array.Empty<IslingInstance>();

        /// <summary>A new Journal entry was caught (a first catch or a new evolved form): the toast and its chime.</summary>
        public static event Action<IslingSpecies> NewEntry;

        public static void RaiseNewEntry(IslingSpecies s)
        {
            if (InJournal(s)) NewEntry?.Invoke(s);
        }

        /// <summary>Counts in the Journal: not a Keeper, not the Lantern Spirit.</summary>
        public static bool InJournal(IslingSpecies s) => s != null && !s.keeper && s.name != SpiritId && s.element != Element.Neutral;

        /// <summary>
        /// Journal order: Fire, Water, Wind; inside each, evolution lines together (base first), lines in the order given.
        /// </summary>
        public static List<IslingSpecies> Order(IEnumerable<IslingSpecies> all)
        {
            var list = new List<IslingSpecies>();
            var pool = new List<IslingSpecies>();
            var evolved = new HashSet<IslingSpecies>();
            foreach (var s in all)
                if (InJournal(s) && !pool.Contains(s))
                {
                    pool.Add(s);
                    if (s.evolvesTo != null) evolved.Add(s.evolvesTo);
                }
            foreach (var e in Elements)
            {
                foreach (var s in pool)
                {
                    if (s.element != e || evolved.Contains(s)) continue;
                    for (var f = s; f != null && InJournal(f) && !list.Contains(f) && f.element == e; f = f.evolvesTo) list.Add(f);
                }
                // A form whose base is another element (or an odd chain) goes to its own element's end.
                foreach (var s in pool)
                    if (s.element == e && !list.Contains(s)) list.Add(s);
            }
            return list;
        }

        public static State StateOf(IslingSpecies s, ICollection<string> caught, ICollection<string> seen)
        {
            string id = SpeciesRegistry.IdOf(s);
            if (caught != null && caught.Contains(id)) return State.Caught;
            if (seen != null && seen.Contains(id)) return State.Seen;
            return State.Unseen;
        }

        public static State StateOf(IslingSpecies s) => StateOf(s, CaughtIds(), SeenIds());

        /// <summary>Caught Islings among <paramref name="entries"/> (of one element, or all with Neutral).</summary>
        public static int Found(IList<JournalData.Entry> entries, ICollection<string> caught, Element element = Element.Neutral)
        {
            int n = 0;
            if (entries == null || caught == null) return 0;
            foreach (var e in entries)
                if ((element == Element.Neutral || e.species.element == element) && caught.Contains(SpeciesRegistry.IdOf(e.species))) n++;
            return n;
        }

        public static int Total(IList<JournalData.Entry> entries, Element element = Element.Neutral)
        {
            int n = 0;
            if (entries == null) return 0;
            foreach (var e in entries)
                if (element == Element.Neutral || e.species.element == element) n++;
            return n;
        }

        /// <summary>The level of the player's best Isling of this species, or 0.</summary>
        public static int BestLevel(IslingSpecies s, IEnumerable<IslingInstance> owned)
        {
            int best = 0;
            if (owned != null)
                foreach (var c in owned)
                    if (c != null && c.species == s && c.level > best) best = c.level;
            return best;
        }

        /// <summary>The name line under a portrait.</summary>
        public static string NameLine(JournalData.Entry e, State state) => state == State.Unseen ? "???" : e.species.displayName;

        /// <summary>
        /// The line under the name: caught "Lv N" (its best one; "Caught" if none is with the player now), seen "Seen",
        /// unseen: where it lives or "Rare"; an evolved form "Evolves from {base}" once the base is caught, else "???".
        /// </summary>
        public static string StatusLine(JournalData.Entry e, State state, int bestLevel, ICollection<string> caught)
        {
            switch (state)
            {
                case State.Caught: return bestLevel > 0 ? $"Lv {bestLevel}" : "Caught";
                case State.Seen: return "Seen";
            }
            if (e.from != null)
                return caught != null && caught.Contains(SpeciesRegistry.IdOf(e.from)) ? $"Evolves from {e.from.displayName}" : "???";
            return string.IsNullOrEmpty(e.hint) ? "A starter" : e.hint;
        }

        /// <summary>The toast: "New Isling! Toastling · 13 / 46".</summary>
        public static string ToastLine(string name, int found, int total) => $"New Isling! {name} · {found} / {total}";

        /// <summary>
        /// Old saves (no Journal yet): every Isling in the pods and the lodge counts as caught; nothing more is seen.
        /// Returns how many ids it added.
        /// </summary>
        public static int Migrate(ICollection<string> caught, IEnumerable<IslingInstance> owned)
        {
            int n = 0;
            if (caught == null || owned == null) return 0;
            foreach (var c in owned)
            {
                if (c == null || c.species == null) continue;
                string id = SpeciesRegistry.IdOf(c.species);
                if (caught.Contains(id)) continue;
                caught.Add(id);
                n++;
            }
            return n;
        }

        /// <summary>
        /// Where it lives, from the encounter tables in order (realm name, place name, table): the first place it shows up
        /// as a common, "Rare" if it only ever shows up as a rare, null if nowhere (an evolved form).
        /// </summary>
        public static string Hint(IslingSpecies s, IList<(string realm, string place, EncounterTable table)> tables)
        {
            bool rare = false;
            if (tables == null) return null;
            foreach (var (realm, place, table) in tables)
            {
                if (table == null || table.entries == null) continue;
                foreach (var e in table.entries)
                {
                    if (e.species != s) continue;
                    if (e.rare)
                    {
                        rare = true;
                        continue;
                    }
                    return string.IsNullOrEmpty(place) ? realm : $"{realm}, {place}";
                }
            }
            return rare ? "Rare" : null;
        }
    }
}
