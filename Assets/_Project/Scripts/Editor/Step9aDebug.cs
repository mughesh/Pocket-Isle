using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>Claude bridge helpers for step 9a (edit mode). Shots land in Logs/BattleShots.</summary>
    static class Step9aDebug
    {
        /// <summary>Every 9a prop and both trees on a throwaway 14 x 14 map: a seated shot and a low close one.</summary>
        public static void PropGallery()
        {
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            if (island == null || island.world == null) return;
            var old = island.world;
            var map = ScriptableObject.CreateInstance<WorldMap>();
            map.name = "Gallery";
            map.tileSize = old.tileSize;
            map.capHeight = old.capHeight;
            map.theme = PropKit.Theme("Cinderwood");
            const int n = 14;
            var g = new char[n, n];
            var p = new char[n, n];
            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                {
                    g[r, c] = r == 0 || c == 0 || r == n - 1 || c == n - 1 ? IslandMap.Blocked : IslandMap.Grass;
                    p[r, c] = IslandMap.Empty;
                }
            var legend = new List<PropLegend>();
            void Key(char k, string recipe, float yaw = 0f, bool turned = false) =>
                legend.Add(new PropLegend { key = k.ToString(), recipe = PropKit.Load(recipe), yaw = yaw, turned = turned });
            Key('c', "CliffWall");
            Key('C', "CliffWall", 180f);
            Key('d', "ForgeDoor");
            Key('h', "ClayHut");
            Key('f', "Fence");
            Key('F', "Fence", 0f, true);
            Key('q', "FenceCorner", 90f);
            Key('s', "Signpost");
            Key('w', "Woodpile");
            Key('l', "LeafPile");
            Key('r', "RuinedKiln");
            Key('p', "PotShards");
            Key('e', "EmberVent");
            Key('k', "Kiln");
            for (int c = 1; c <= 3; c++) p[2, c] = c % 2 == 0 ? 'C' : 'c';
            p[2, 4] = 'd';
            for (int c = 7; c <= 12; c++) p[2, c] = c % 2 == 0 ? 'C' : 'c';
            p[5, 2] = 'h';
            p[6, 5] = 'k';
            for (int c = 7; c <= 9; c++) p[6, c] = 'f';
            p[6, 10] = 'q';
            p[7, 10] = 'F';
            p[6, 12] = 's';
            p[9, 2] = 'w';
            p[9, 3] = 'l';
            p[9, 5] = 'r';
            p[9, 6] = 'p';
            p[9, 7] = 'e';
            for (int r = 9; r <= 11; r++)
                for (int c = 9; c <= 12; c++) g[r, c] = IslandMap.Tree;
            var layout = new StringBuilder();
            var props = new StringBuilder();
            for (int r = 0; r < n; r++)
            {
                for (int c = 0; c < n; c++)
                {
                    layout.Append(g[r, c]);
                    props.Append(p[r, c]);
                }
                layout.Append('\n');
                props.Append('\n');
            }
            map.SetLayout(layout.ToString());
            map.propLegend = legend;
            map.SetProps(props.ToString());

            // Hide every scene renderer (explorer, hands, rings, bubbles): the island draws without renderers.
            // forceRenderingOff is not serialized, so the scene stays clean.
            var hidden = new List<Renderer>();
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                if (r.enabled && !r.forceRenderingOff)
                {
                    r.forceRenderingOff = true;
                    hidden.Add(r);
                }
            try
            {
                island.world = map;
                island.Rebuild();
                island.SetView(island.CenteredView, island.CenteredView, 1f);
                var t = island.transform;
                var eye = t.position + t.rotation * new Vector3(0f, 0.5f, 0.42f);
                BattleDebug.Render("Logs/BattleShots/9a_gallery.png", eye, Quaternion.LookRotation(t.position + t.rotation * new Vector3(0f, 0f, 0.02f) - eye, Vector3.up));
                var look = t.position + t.rotation * new Vector3(0f, 0.06f, -0.08f);
                eye = t.position + t.rotation * new Vector3(0.05f, 0.16f, 0.36f);
                BattleDebug.Render("Logs/BattleShots/9a_gallery_low.png", eye, Quaternion.LookRotation(look - eye, Vector3.up));
                look = t.position + t.rotation * new Vector3(0.02f, 0.02f, 0.06f);
                eye = t.position + t.rotation * new Vector3(0.02f, 0.13f, 0.3f);
                BattleDebug.Render("Logs/BattleShots/9a_gallery_small.png", eye, Quaternion.LookRotation(look - eye, Vector3.up));
            }
            finally
            {
                foreach (var r in hidden)
                    if (r != null) r.forceRenderingOff = false;
                island.world = old;
                island.Rebuild();
                Object.DestroyImmediate(map);
            }
            Debug.Log("[Step9aDebug] Prop gallery in Logs/BattleShots/9a_gallery*.png");
        }
    }
}
