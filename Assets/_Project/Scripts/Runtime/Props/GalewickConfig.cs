using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 10a: whether the hub's wind gate opens to Galewick. In the Editor it follows <see cref="openInEditor"/>; in
    /// QA and release builds it stays sealed until <see cref="open"/> is ticked (after step 10d passes), so a tester's build
    /// stays on finished content. The dev panel's Go to reaches Galewick either way. Data/Galewick/GalewickConfig.asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Pocket Isle/Galewick Config", fileName = "GalewickConfig")]
    public class GalewickConfig : ScriptableObject
    {
        [Tooltip("Builds: the wind gate opens. Leave off until Galewick is finished (step 10d) and Mugesh says so.")]
        public bool open;
        [Tooltip("Editor Play: the wind gate opens.")]
        public bool openInEditor = true;

        /// <summary>The gate opens right now (Editor or build rule).</summary>
        public bool IsOpen => Application.isEditor ? openInEditor : open;
    }
}
