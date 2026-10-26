using Meta.XR.MRUtilityKit;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 9h: room data is optional and silent. MRUK's own startup load is off (it starts Space Setup, the room scan,
    /// when the headset has no scene data). Once the island is placed this loads the room only if the scene permission
    /// was already granted, never asking for it and never requesting a scan. A room = the shadow on the real surface and
    /// the table snap; no room = the island floats (the intended default). Logs which path was taken.
    /// </summary>
    public class RoomSceneLoader : MonoBehaviour
    {
        public IslandPlacer placer;

        bool _tried;

        void Start()
        {
            if (placer == null)
            {
                Debug.LogError("[RoomSceneLoader] No IslandPlacer: room data never loads (the island floats).", this);
                return;
            }
            if (placer.HasPlaced) TryLoad();
            else placer.Placed += TryLoad;
        }

        void OnDestroy()
        {
            if (placer != null) placer.Placed -= TryLoad;
        }

        async void TryLoad()
        {
            if (_tried) return;
            _tried = true;
            try
            {
                var mruk = MRUK.Instance;
                if (mruk == null)
                {
                    Debug.Log("[RoomSceneLoader] No MRUK in the scene: the island floats.");
                    return;
                }
#if UNITY_ANDROID && !UNITY_EDITOR
                if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(OVRPermissionsRequester.ScenePermission))
                {
                    Debug.Log("[RoomSceneLoader] No scene permission (never asked for): the island floats, no shadow or table snap.");
                    return;
                }
#endif
                if (!UnityEngine.XR.XRSettings.isDeviceActive)
                {
                    Debug.Log("[RoomSceneLoader] No XR device (flat Editor): the island floats.");
                    return;
                }
                var result = await mruk.LoadSceneFromDevice(requestSceneCaptureIfNoDataFound: false);
                Debug.Log(result == MRUK.LoadDeviceResult.Success && mruk.GetCurrentRoom() != null
                    ? "[RoomSceneLoader] Room data loaded (no scan asked): shadow and table snap on."
                    : $"[RoomSceneLoader] No room data ({result}), no scan asked: the island floats.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[RoomSceneLoader] Loading the room failed: {e.Message}. The island floats.");
            }
        }
    }
}
