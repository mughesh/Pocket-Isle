using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Diorama.Tests
{
    /// <summary>
    /// Step 8: the Still Isle and Cinderwood maps (gates, landmarks, reachability), gate travel in the save, the v1 save
    /// migration and per-realm restoration. Needs Set Up MR_Main (8 hub) to have written the maps.
    /// </summary>
    public class HubTests
    {
        const string HubPath = "Assets/_Project/Data/WorldMap_StillIsle.asset";
        const string CinderwoodPath = "Assets/_Project/Data/WorldMap_Cinderwood.asset";
        const int FarSteps = 400;

        static WorldMap Load(string path)
        {
            var map = AssetDatabase.LoadAssetAtPath<WorldMap>(path);
            if (map == null) Assert.Inconclusive($"{path} missing (run Tools > Pocket Isle > Set Up MR_Main (8 hub)).");
            return map;
        }

        static GateLink Gate(WorldMap map, string id)
        {
            foreach (var g in map.gates)
                if (g.id == id) return g;
            Assert.Fail($"{map.name} has no '{id}' gate.");
            return default;
        }

        [Test]
        public void Hub_EveryGateFrontAndLandmarkIsReachableFromTheStart()
        {
            var hub = Load(HubPath);
            Assert.IsTrue(hub.CanWalk(hub.startTile), "start tile walkable");
            foreach (var g in hub.gates)
            {
                Assert.IsFalse(hub.CanWalk(g.tile), $"{g.id} gate blocks");
                Assert.IsTrue(hub.CanWalk(g.Front(0)) || hub.CanWalk(g.Front(1)), $"{g.id} gate has a walkable front");
                var front = hub.CanWalk(g.Front(0)) ? g.Front(0) : g.Front(1);
                Assert.IsTrue(ExplorerPath.CanReach(hub, hub.startTile, front, FarSteps), $"{g.id} gate reachable");
            }
            foreach (var name in new[] { "firstGrass", "pond" })
            {
                Assert.IsTrue(hub.TryGetLandmark(name, out var t), name);
                Assert.IsTrue(ExplorerPath.CanReach(hub, hub.startTile, t, FarSteps), $"{name} reachable");
            }
            Assert.AreEqual(IslandMap.TallGrass, hub.At(hub.TryGetLandmark("firstGrass", out var grass) ? grass : default), "first grass is tall grass");
            Assert.IsTrue(hub.TryGetLandmark("pond", out var pond));
            Assert.AreEqual(IslandMap.Water, hub.At(pond + Vector2Int.down), "the pond spot faces water (up)");
        }

        [Test]
        public void Hub_FirstGrassIsCloseToTheStart()
        {
            var hub = Load(HubPath);
            Assert.IsTrue(hub.TryGetLandmark("firstGrass", out var grass));
            var path = new List<Vector2Int>();
            Assert.IsTrue(ExplorerPath.Find(hub, hub.startTile, grass, path, FarSteps));
            Assert.LessOrEqual(path.Count, 25, "about 10 s of walking (0.4 s per tile)");
        }

        [Test]
        public void Gates_FireLeadsToCinderwoodAndHomeLeadsBack_WaterAndWindSealed()
        {
            var hub = Load(HubPath);
            var cw = Load(CinderwoodPath);
            var fire = Gate(hub, "fire");
            Assert.IsTrue(fire.Open);
            Assert.AreEqual(cw, fire.to);
            Assert.IsTrue(cw.CanWalk(fire.arrive), "arrival in Cinderwood is walkable");
            var home = Gate(cw, "home");
            Assert.IsTrue(home.Open);
            Assert.AreEqual(hub, home.to);
            Assert.IsTrue(hub.CanWalk(home.arrive), "arrival in the hub is walkable");
            Assert.IsTrue(home.arrive == fire.Front(0) || home.arrive == fire.Front(1), "you come back in front of the fire gate");
            Assert.IsTrue(fire.arrive == home.Front(0) || fire.arrive == home.Front(1), "you arrive in front of Cinderwood's return gate");
            Assert.IsFalse(Gate(hub, "water").Open);
            Assert.IsFalse(Gate(hub, "wind").Open);
            Assert.IsTrue(ExplorerPath.CanReach(cw, cw.startTile, fire.arrive, FarSteps), "return gate reachable from Cinderwood's start");
        }

        [Test]
        public void Maps_HaveTheirOwnEncounterTablesAndRealmIds()
        {
            var hub = Load(HubPath);
            var cw = Load(CinderwoodPath);
            Assert.IsNotNull(hub.grassTable);
            Assert.IsNotNull(hub.waterTable);
            Assert.IsNotNull(cw.grassTable);
            Assert.AreNotEqual(hub.grassTable, cw.grassTable);
            Assert.AreEqual("stillisle", hub.realm);
            Assert.AreEqual("cinderwood", cw.realm);
            Assert.AreEqual(0f, hub.theme.embers, "no embers on the calm hub");
        }

        [Test]
        public void Save_KeepsTheMapTheShrineMapAndTheFirstEncounter()
        {
            var data = new SaveData
            {
                map = "WorldMap_Cinderwood", explorerX = 30, explorerY = 27,
                hasShrine = true, shrineX = 10, shrineY = 20, shrineMap = "WorldMap_StillIsle",
                firstEncounterDone = true,
                inside = true, interior = "Lodge", insideX = 3, insideY = 6,
            };
            data.SetRestored("WorldMap_StillIsle", 1f);
            data.SetRestored("WorldMap_Cinderwood", 0.25f);
            var back = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(data));
            Assert.AreEqual("WorldMap_Cinderwood", back.map, "the lodge returns to the map you came from");
            Assert.AreEqual("WorldMap_StillIsle", back.shrineMap);
            Assert.IsTrue(back.firstEncounterDone);
            Assert.IsTrue(back.inside);
            Assert.AreEqual(0.25f, back.RestoredFor("WorldMap_Cinderwood", 1f));
            Assert.AreEqual(1f, back.RestoredFor("WorldMap_StillIsle", 0f));
        }

        [Test]
        public void NewGameOrReturning_DecidedByTheSaveFile()
        {
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "PocketIsleHub_" + System.Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(dir);
            string path = System.IO.Path.Combine(dir, SaveGame.FileName);
            try
            {
                Assert.AreEqual(SaveGame.ReadResult.NoSave, SaveGame.Read(path, out _, out _), "no save: the opening plays");
                SaveGame.Write(path, new SaveData { map = "WorldMap_StillIsle", explorerX = 15, explorerY = 22 });
                Assert.AreEqual(SaveGame.ReadResult.Loaded, SaveGame.Read(path, out var data, out _), "a save: returning player, no opening");
                Assert.AreEqual("WorldMap_StillIsle", data.map);
                Assert.IsFalse(data.firstEncounterDone, "a new save keeps the scripted first encounter ahead");
            }
            finally
            {
                System.IO.Directory.Delete(dir, true);
            }
        }

        [Test]
        public void Migrate_V1SaveMovesToCinderwood()
        {
            var v1 = new SaveData { version = 1, explorerX = 28, explorerY = 27, hasShrine = true, shrineX = 34, shrineY = 27 };
            v1.SetRestored(SaveGame.LegacyRealm, 0.5f);
            SaveGame.Migrate(v1);
            Assert.AreEqual(SaveData.CurrentVersion, v1.version);
            Assert.AreEqual(SaveGame.LegacyMap, v1.map);
            Assert.AreEqual(SaveGame.LegacyMap, v1.shrineMap);
            Assert.AreEqual(0.5f, v1.RestoredFor(SaveGame.LegacyMap, 0f));
            Assert.IsTrue(v1.firstEncounterDone, "old players skip the scripted first encounter");
        }

        [Test]
        public void Migrate_LeavesCurrentSavesAlone()
        {
            var v2 = new SaveData { map = "WorldMap_StillIsle", firstEncounterDone = false };
            SaveGame.Migrate(v2);
            Assert.AreEqual("WorldMap_StillIsle", v2.map);
            Assert.IsFalse(v2.firstEncounterDone);
        }

        [Test]
        public void RealmRestoration_RemembersEachRealmAndStartsNewOnesFromTheirTheme()
        {
            var go = new GameObject("realm");
            var realm = go.AddComponent<RealmRestoration>();
            var theme = ScriptableObject.CreateInstance<RealmTheme>();
            theme.startRestored = 0f;
            var a = ScriptableObject.CreateInstance<WorldMap>();
            a.name = "A";
            a.theme = theme;
            var b = ScriptableObject.CreateInstance<WorldMap>();
            b.name = "B";
            try
            {
                Assert.AreEqual(0f, realm.Recall(a), "first visit: the theme's start value");
                Assert.AreEqual(1f, realm.Recall(b), "no theme: restored");
                realm.Remember(a, 0.6f);
                Assert.AreEqual(0.6f, realm.Recall(a), 1e-5f);
                realm.Forget();
                Assert.AreEqual(0f, realm.Recall(a));
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(theme);
                Object.DestroyImmediate(a);
                Object.DestroyImmediate(b);
            }
        }
    }
}
