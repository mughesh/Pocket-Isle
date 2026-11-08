namespace Diorama.Audio.EditorTools
{
    /// <summary>Step 23 bridge: bakes the re-voiced knots and Galewick's restoration breeze, then syncs the cues.</summary>
    public static class Step23Audio
    {
        public static void Bake() => Step18Audio.Bake("knot.pull", "keeper.knot.snap", "restore.breeze");
    }
}
