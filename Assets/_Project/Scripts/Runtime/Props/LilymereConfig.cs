using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 11a: whether the hub's well (water) gate opens to Lilymere, by the same rule as <see cref="GalewickConfig"/>:
    /// open in the Editor (<see cref="GalewickConfig.openInEditor"/>), sealed in builds until <see cref="GalewickConfig.open"/>
    /// is ticked once Lilymere is finished. Data/Lilymere/LilymereConfig.asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Pocket Isle/Lilymere Config", fileName = "LilymereConfig")]
    public class LilymereConfig : GalewickConfig
    {
    }
}
