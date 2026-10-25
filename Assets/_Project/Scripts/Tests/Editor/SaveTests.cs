using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Diorama.Tests
{
    public class SaveTests
    {
        readonly List<Object> _made = new List<Object>();
        string _dir, _path;
        SpeciesRegistry _registry;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "PocketIsleSaveTests_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            _path = Path.Combine(_dir, SaveGame.FileName);
            _registry = Make<SpeciesRegistry>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _made) Object.DestroyImmediate(o);
            _made.Clear();
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        T Make<T>() where T : ScriptableObject
        {
            var o = ScriptableObject.CreateInstance<T>();
            _made.Add(o);
            return o;
        }

        IslingSpecies Species(string id)
        {
            var s = Make<IslingSpecies>();
            s.name = id;
            s.displayName = id;
            _registry.species.Add(s);
            return s;
        }

        [Test]
        public void RoundTrip_KeepsPartyStorageSaidIdsAndRestoration()
        {
            var kilnpot = Species("Kilnpot");
            var waxle = Species("Waxle");
            var lead = new IslingInstance(kilnpot, 9) { xp = 17, nickname = "Pip" };
            lead.currentHp = 11;
            var stored = new IslingInstance(waxle, 4);
            var data = new SaveData
            {
                party = SaveGame.ToSaved(new[] { lead }),
                storage = SaveGame.ToSaved(new[] { stored }),
                explorerX = 12, explorerY = 7, facingX = 1, facingY = 0,
                hasShrine = true, shrineX = 30, shrineY = 27,
                said = new List<string> { "intro.arrive", "shrine.first" },
                caught = new List<string> { "Kilnpot", "Waxle" },
                walkMode = MoveMode.HoldToWalk.ToString(),
            };
            data.SetRestored("WorldMap_Test", 0.75f);
            SaveGame.Write(_path, data);
            Assert.IsFalse(File.Exists(_path + ".tmp"), "the temp file is renamed into place");

            Assert.AreEqual(SaveGame.ReadResult.Loaded, SaveGame.Read(_path, out var back, out var problem), problem);
            var problems = new List<string>();
            var pods = SaveGame.FromSaved(back.party, _registry, problems);
            var box = SaveGame.FromSaved(back.storage, _registry, problems);
            Assert.AreEqual(0, problems.Count);
            Assert.AreEqual(kilnpot, pods[0].species);
            Assert.AreEqual(9, pods[0].level);
            Assert.AreEqual(17, pods[0].xp);
            Assert.AreEqual(11, pods[0].currentHp);
            Assert.AreEqual("Pip", pods[0].Name);
            Assert.AreEqual(waxle, box[0].species);
            CollectionAssert.AreEqual(new[] { "intro.arrive", "shrine.first" }, back.said);
            Assert.AreEqual(0.75f, back.RestoredFor("WorldMap_Test", 0f));
            Assert.AreEqual(1f, back.RestoredFor("Other", 1f));
            Assert.AreEqual(new Vector2Int(12, 7), new Vector2Int(back.explorerX, back.explorerY));
            Assert.IsTrue(back.hasShrine);
            Assert.AreEqual("HoldToWalk", back.walkMode);

            // A second write replaces the first.
            data.explorerX = 13;
            SaveGame.Write(_path, data);
            SaveGame.Read(_path, out back, out _);
            Assert.AreEqual(13, back.explorerX);
        }

        [Test]
        public void InsideAnInterior_KeepsTheOutdoorReturnTileAndTheInsideTile()
        {
            var data = new SaveData
            {
                explorerX = 33, explorerY = 27, facingX = 1, facingY = 0,
                inside = true, interior = "Lodge", insideX = 3, insideY = 6, insideFacingX = 0, insideFacingY = -1,
            };
            SaveGame.Write(_path, data);
            Assert.AreEqual(SaveGame.ReadResult.Loaded, SaveGame.Read(_path, out var back, out _));
            Assert.IsTrue(back.inside);
            Assert.AreEqual("Lodge", back.interior);
            Assert.AreEqual(new Vector2Int(33, 27), new Vector2Int(back.explorerX, back.explorerY), "outdoor return tile");
            Assert.AreEqual(new Vector2Int(3, 6), new Vector2Int(back.insideX, back.insideY));

            // A step 7 save (no interior fields) reads as outside.
            File.WriteAllText(_path, "{\"version\":1,\"explorerX\":5,\"explorerY\":6}");
            Assert.AreEqual(SaveGame.ReadResult.Loaded, SaveGame.Read(_path, out back, out _));
            Assert.IsFalse(back.inside);
            Assert.AreEqual(5, back.explorerX);
        }

        [Test]
        public void DominantHand_FirstTouchDecidesAndRoundTrips()
        {
            DominantHand.Restore(null);
            Assert.IsFalse(DominantHand.Known);
            Assert.IsFalse(DominantHand.Left, "right until the shard is touched");
            DominantHand.FirstTouch(true);
            DominantHand.FirstTouch(false);
            Assert.IsTrue(DominantHand.Left, "only the first touch counts");
            SaveGame.Write(_path, new SaveData { dominantHand = DominantHand.Saved });
            DominantHand.Restore(null);
            Assert.AreEqual(SaveGame.ReadResult.Loaded, SaveGame.Read(_path, out var back, out _));
            DominantHand.Restore(back.dominantHand);
            Assert.IsTrue(DominantHand.Known && DominantHand.Left);
            DominantHand.Restore(null);
        }

        [Test]
        public void UnknownSpecies_IsSkippedAndReported()
        {
            Species("Kilnpot");
            var saved = new List<SavedIsling> { new SavedIsling { species = "Kilnpot", level = 5, hp = 10 }, new SavedIsling { species = "Gone", level = 5 } };
            var problems = new List<string>();
            var list = SaveGame.FromSaved(saved, _registry, problems);
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(1, problems.Count);
        }

        [Test]
        public void CorruptSave_IsRenamedToBad_AndReadsAsNewGame()
        {
            File.WriteAllText(_path, "{ this is not json");
            Assert.AreEqual(SaveGame.ReadResult.Bad, SaveGame.Read(_path, out var data, out var problem));
            Assert.IsNull(data);
            Assert.IsNotNull(problem);
            Assert.IsFalse(File.Exists(_path));
            Assert.IsTrue(File.Exists(Path.Combine(_dir, SaveGame.BadFileName)));
            Assert.AreEqual(SaveGame.ReadResult.NoSave, SaveGame.Read(_path, out _, out _));
        }

        [Test]
        public void NewerVersion_IsBad()
        {
            File.WriteAllText(_path, JsonUtility.ToJson(new SaveData { version = SaveData.CurrentVersion + 1 }));
            Assert.AreEqual(SaveGame.ReadResult.Bad, SaveGame.Read(_path, out _, out var problem));
            StringAssert.Contains("newer", problem);
        }
    }
}
