using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Bug reports from the dev panel (step 9e): a folder <c>persistentDataPath/bugreports/&lt;timestamp&gt;/</c> with a
    /// screenshot (written at the end of the frame, when the platform allows it), the save JSON, the last
    /// <see cref="LogLines"/> log lines and a state summary (<see cref="GameState.Describe"/> plus fps). The log is kept
    /// from startup in the Editor and dev / QA builds (POCKET_DEV) only.
    /// </summary>
    public static class BugReport
    {
        public const int LogLines = 300;
        static readonly Queue<string> _log = new Queue<string>(LogLines + 1);

        public static string Root => Path.Combine(Application.persistentDataPath, "bugreports");

#if UNITY_EDITOR || POCKET_DEV || DEVELOPMENT_BUILD
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Listen()
        {
            _log.Clear();
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;
        }
#endif

        static void OnLog(string message, string stack, LogType type)
        {
            string line = $"{DateTime.Now:HH:mm:ss.fff} {type}: {message}";
            if (type == LogType.Exception || type == LogType.Error)
            {
                // The first few stack lines say where.
                var lines = stack.Split('\n');
                for (int i = 0; i < lines.Length && i < 4; i++)
                    if (!string.IsNullOrWhiteSpace(lines[i])) line += "\n    " + lines[i].Trim();
            }
            lock (_log)
            {
                _log.Enqueue(line);
                while (_log.Count > LogLines) _log.Dequeue();
            }
        }

        /// <summary>Writes a report; returns its folder name (or null on failure).</summary>
        public static string Write(GameState state, float fps)
        {
            try
            {
                string name = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                string dir = Path.Combine(Root, name);
                Directory.CreateDirectory(dir);
                if (state != null && state.save != null) state.save.Save("bug report");
                if (File.Exists(GameSave.SavePath)) File.Copy(GameSave.SavePath, Path.Combine(dir, "save.json"), true);
                string[] lines;
                lock (_log) lines = _log.ToArray();
                File.WriteAllLines(Path.Combine(dir, "log.txt"), lines);
                var sb = new StringBuilder();
                sb.AppendLine($"Pocket Isle bug report {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Version {Application.version}, {Application.platform}, {SystemInfo.deviceModel}");
                sb.AppendLine($"FPS {fps:0}");
                if (state != null) sb.Append(state.Describe());
                File.WriteAllText(Path.Combine(dir, "state.txt"), sb.ToString());
#if UNITY_ANDROID && !UNITY_EDITOR
                // On Android the path is taken relative to persistentDataPath.
                ScreenCapture.CaptureScreenshot($"bugreports/{name}/screenshot.png");
#else
                ScreenCapture.CaptureScreenshot(Path.Combine(dir, "screenshot.png"));
#endif
                Debug.Log($"[BugReport] Written to {dir}.");
                return name;
            }
            catch (Exception e)
            {
                Debug.LogError($"[BugReport] Failed: {e.Message}");
                return null;
            }
        }
    }
}
