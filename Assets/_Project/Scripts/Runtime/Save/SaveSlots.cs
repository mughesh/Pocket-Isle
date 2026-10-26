using System.IO;
using UnityEngine;

namespace Diorama
{
    /// <summary>The two save slots (step 9g): the Full adventure and the judges' Quick Journey. Same <see cref="SaveData"/>.</summary>
    public enum SaveSlot { Adventure = 0, Quick = 1 }

    /// <summary>
    /// Which save file the game reads and writes (step 9g): <c>save.json</c> = Full adventure, <c>quick.json</c> = Quick
    /// Journey. PlayerPrefs keeps the last played slot. At launch the active slot is the last played one when it still
    /// has a save, else whichever slot has one, else the Full adventure (a new player). Any slot existing = a returning
    /// player. <see cref="PendingNew"/> carries a "New journey" pick across the scene reload, so the opening skips the
    /// slot choice it already made.
    /// </summary>
    public static class SaveSlots
    {
        public const string AdventureFile = SaveGame.FileName;
        public const string QuickFile = "quick.json";
        const string LastKey = "pocketisle.lastSlot";

        static bool _picked;
        static SaveSlot _active;

        /// <summary>The slot this run plays (picked once per app run, then changed only by the title).</summary>
        public static SaveSlot Active
        {
            get
            {
                if (!_picked) PickAtLaunch();
                return _active;
            }
        }

        /// <summary>Set by "New journey": the opening plays from the shard in this slot without asking again.</summary>
        public static SaveSlot? PendingNew { get; set; }

        public static string FileOf(SaveSlot slot) => slot == SaveSlot.Quick ? QuickFile : AdventureFile;

        public static string PathOf(SaveSlot slot) => Path.Combine(Application.persistentDataPath, FileOf(slot));

        public static string ActivePath => PathOf(Active);

        public static bool Exists(SaveSlot slot) => File.Exists(PathOf(slot));

        public static bool AnyExists => Exists(SaveSlot.Adventure) || Exists(SaveSlot.Quick);

        /// <summary>The last slot played (PlayerPrefs), or null before any.</summary>
        public static SaveSlot? LastPlayed
        {
            get
            {
                int v = PlayerPrefs.GetInt(LastKey, -1);
                return v == 0 || v == 1 ? (SaveSlot)v : (SaveSlot?)null;
            }
        }

        public static string Label(SaveSlot slot) => slot == SaveSlot.Quick ? "Quick journey" : "Adventure";

        public static SaveSlot Other(SaveSlot slot) => slot == SaveSlot.Quick ? SaveSlot.Adventure : SaveSlot.Quick;

        /// <summary>
        /// The launch rule (pure, tested): the last played slot if it has a save, else the slot that has one (Adventure
        /// first), else Adventure.
        /// </summary>
        public static SaveSlot Pick(bool adventureExists, bool quickExists, SaveSlot? last)
        {
            if (last.HasValue && (last.Value == SaveSlot.Quick ? quickExists : adventureExists)) return last.Value;
            if (adventureExists) return SaveSlot.Adventure;
            return quickExists ? SaveSlot.Quick : SaveSlot.Adventure;
        }

        static void PickAtLaunch()
        {
            _picked = true;
            _active = Pick(Exists(SaveSlot.Adventure), Exists(SaveSlot.Quick), LastPlayed);
            Debug.Log($"[SaveSlots] Launch: {Label(_active)} ({FileOf(_active)}; adventure {(Exists(SaveSlot.Adventure) ? "saved" : "empty")}, " +
                      $"quick {(Exists(SaveSlot.Quick) ? "saved" : "empty")}, last played {(LastPlayed.HasValue ? Label(LastPlayed.Value) : "none")}).");
        }

        /// <summary>Plays this slot from now on and remembers it as the last played one.</summary>
        public static void Choose(SaveSlot slot)
        {
            _picked = true;
            _active = slot;
            PlayerPrefs.SetInt(LastKey, (int)slot);
            PlayerPrefs.Save();
            Debug.Log($"[SaveSlots] Playing {Label(slot)} ({FileOf(slot)}).");
        }

        /// <summary>Deletes a slot's save; the old file is kept as <c>&lt;name&gt;.old.json</c> (one undo for testers).</summary>
        public static void Delete(SaveSlot slot)
        {
            string path = PathOf(slot);
            if (!File.Exists(path)) return;
            string old = Path.ChangeExtension(path, ".old.json");
            File.Copy(path, old, true);
            SaveGame.Delete(path);
            Debug.Log($"[SaveSlots] Deleted {FileOf(slot)} (copy kept as {Path.GetFileName(old)}).");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _picked = false;
            PendingNew = null;
        }
    }
}
