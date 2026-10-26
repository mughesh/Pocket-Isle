using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Editor only: stops Play when XR was started but the headset never takes the session (Link not active, the headset
    /// asleep or off the head). The OpenXR session then sits in IDLE while the Oculus runtime hands Unity a new app space
    /// every rendered frame (~190 a second), and after about 50 s xrCreateReferenceSpace fails with
    /// XR_ERROR_LIMIT_REACHED and Unity crashes. Every Editor crash since Oct 25 had this pattern; working Link sessions
    /// leave IDLE within a few seconds. Builds are never affected.
    /// </summary>
    [InitializeOnLoad]
    static class XrSessionGuard
    {
        const float GiveUpSeconds = 20f;
        static double _since = -1;

        static XrSessionGuard()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += s => _since = -1;
        }

        static void Tick()
        {
            if (!EditorApplication.isPlaying || EditorApplication.isPaused) return;
            // An XR display was loaded ("OpenXR Display"); flat Play (XR init off) loads none.
            bool xrStarted = !string.IsNullOrEmpty(UnityEngine.XR.XRSettings.loadedDeviceName);
            // Flat Play (XR init off) or a session the headset runs: nothing to do.
            if (!xrStarted || UnityEngine.XR.XRSettings.isDeviceActive || OVRManager.hasVrFocus)
            {
                _since = -1;
                return;
            }
            if (_since < 0) _since = EditorApplication.timeSinceStartup;
            if (EditorApplication.timeSinceStartup - _since < GiveUpSeconds) return;
            Debug.LogError($"[XrSessionGuard] XR started but the headset never took the session in {GiveUpSeconds:0} s " +
                           "(Link not active, or the headset asleep / off the head). Stopping Play before the runtime's app-space " +
                           "loop crashes Unity (XR_ERROR_LIMIT_REACHED). Put the headset on with Link active, then press Play; " +
                           "for a monitor-only check, untick Initialize XR on Startup (Standalone).");
            _since = -1;
            EditorApplication.isPlaying = false;
        }
    }
}
