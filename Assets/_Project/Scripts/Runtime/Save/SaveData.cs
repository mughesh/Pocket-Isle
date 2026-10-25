using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Diorama
{
    /// <summary>One creature in the save: species id (<see cref="SpeciesRegistry"/>), level, XP, HP, nickname.</summary>
    [Serializable]
    public class SavedIsling
    {
        public string species;
        public int level;
        public int xp;
        public int hp;
        public string nickname;
    }

    /// <summary>How restored one realm is (realm id = its world map asset name).</summary>
    [Serializable]
    public class SavedRealm
    {
        public string realm;
        public float restored;
    }

    /// <summary>The whole save file (JSON). Bump <see cref="CurrentVersion"/> when the layout changes.</summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 2;

        public int version = CurrentVersion;
        public string savedAt;
        public List<SavedIsling> party = new List<SavedIsling>();
        public List<SavedIsling> storage = new List<SavedIsling>();
        /// <summary>The outdoor map (world map asset name) the explorer is on (step 8: the hub or a realm).</summary>
        public string map;
        public int explorerX, explorerY;
        public int facingX, facingY = -1;
        public bool hasShrine;
        public int shrineX, shrineY;
        /// <summary>The map the last shrine is on (losing everything returns there).</summary>
        public string shrineMap;
        /// <summary>The scripted first encounter (step 8) has happened.</summary>
        public bool firstEncounterDone;
        public List<SavedRealm> realms = new List<SavedRealm>();
        public List<string> said = new List<string>();
        public List<string> caught = new List<string>();
        /// <summary>Species met in battle (step 7c: "New!" only the first time).</summary>
        public List<string> seen = new List<string>();
        public string walkMode;

        [Header("Inside an interior (the explorer fields above are then the outdoor return tile)")]
        public bool inside;
        public string interior;
        public int insideX, insideY;
        public int insideFacingX, insideFacingY = -1;

        public float RestoredFor(string realm, float fallback)
        {
            foreach (var r in realms)
                if (r.realm == realm) return r.restored;
            return fallback;
        }

        public void SetRestored(string realm, float restored)
        {
            foreach (var r in realms)
                if (r.realm == realm)
                {
                    r.restored = restored;
                    return;
                }
            realms.Add(new SavedRealm { realm = realm, restored = restored });
        }
    }

    /// <summary>
    /// Save file IO and the creature conversions, no scene objects (EditMode-tested). Writes go to a temp file that
    /// then replaces the save, so a crash mid-write never leaves half a save. An unreadable or newer-version file is
    /// renamed to save.bad.json.
    /// </summary>
    public static class SaveGame
    {
        public const string FileName = "save.json";
        public const string BadFileName = "save.bad.json";

        public enum ReadResult { NoSave, Loaded, Bad }

        public static string DefaultPath => Path.Combine(Application.persistentDataPath, FileName);

        public static void Write(string path, SaveData data)
        {
            data.version = SaveData.CurrentVersion;
            data.savedAt = DateTime.UtcNow.ToString("o");
            string json = JsonUtility.ToJson(data, true);
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, json);
            if (File.Exists(path)) File.Replace(tmp, path, null);
            else File.Move(tmp, path);
        }

        /// <summary>Reads the save. Bad = unreadable or from a newer version: it is renamed to save.bad.json.</summary>
        public static ReadResult Read(string path, out SaveData data, out string problem)
        {
            data = null;
            problem = null;
            if (!File.Exists(path)) return ReadResult.NoSave;
            try
            {
                data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                if (data == null) problem = "empty file";
                else if (data.version <= 0) problem = "no version";
                else if (data.version > SaveData.CurrentVersion) problem = $"version {data.version} is newer than {SaveData.CurrentVersion}";
            }
            catch (Exception e)
            {
                problem = e.Message;
            }
            if (problem == null)
            {
                Migrate(data);
                return ReadResult.Loaded;
            }
            data = null;
            string bad = Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, BadFileName);
            if (File.Exists(bad)) File.Delete(bad);
            File.Move(path, bad);
            return ReadResult.Bad;
        }

        /// <summary>Version 1 saves were all made on the test map (step 8 renamed it Cinderwood).</summary>
        public const string LegacyRealm = "WorldMap_Test";
        public const string LegacyMap = "WorldMap_Cinderwood";

        /// <summary>
        /// Brings an older save up to <see cref="SaveData.CurrentVersion"/>. v1 -> v2: the map, the shrine's map and the
        /// realm ids become Cinderwood (renamed test map), and the scripted first encounter counts as done.
        /// </summary>
        public static void Migrate(SaveData data)
        {
            if (data == null || data.version >= SaveData.CurrentVersion) return;
            if (data.version < 2)
            {
                if (string.IsNullOrEmpty(data.map)) data.map = LegacyMap;
                if (data.hasShrine && string.IsNullOrEmpty(data.shrineMap)) data.shrineMap = data.map;
                foreach (var r in data.realms)
                    if (r.realm == LegacyRealm) r.realm = LegacyMap;
                data.firstEncounterDone = true;
            }
            data.version = SaveData.CurrentVersion;
        }

        public static void Delete(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }

        public static SavedIsling ToSaved(IslingInstance c) => new SavedIsling
        {
            species = SpeciesRegistry.IdOf(c.species), level = c.level, xp = c.xp, hp = c.currentHp, nickname = c.nickname,
        };

        /// <summary>Back to a creature; null (with the reason) for a species the registry doesn't know.</summary>
        public static IslingInstance FromSaved(SavedIsling s, SpeciesRegistry registry, out string problem)
        {
            problem = null;
            var species = registry != null && s != null ? registry.Find(s.species) : null;
            if (species == null)
            {
                problem = $"unknown species '{(s != null ? s.species : "null")}'";
                return null;
            }
            var c = new IslingInstance(species, Mathf.Max(1, s.level)) { xp = Mathf.Max(0, s.xp), nickname = s.nickname };
            c.currentHp = Mathf.Clamp(s.hp, 0, c.MaxHp);
            return c;
        }

        public static List<SavedIsling> ToSaved(IEnumerable<IslingInstance> list)
        {
            var result = new List<SavedIsling>();
            foreach (var c in list)
                if (c != null && c.species != null) result.Add(ToSaved(c));
            return result;
        }

        /// <summary>Creatures the registry knows; the unknown ones are reported in <paramref name="problems"/> and skipped.</summary>
        public static List<IslingInstance> FromSaved(IEnumerable<SavedIsling> list, SpeciesRegistry registry, List<string> problems)
        {
            var result = new List<IslingInstance>();
            if (list == null) return result;
            foreach (var s in list)
            {
                var c = FromSaved(s, registry, out var problem);
                if (c != null) result.Add(c);
                else problems?.Add(problem);
            }
            return result;
        }
    }
}
