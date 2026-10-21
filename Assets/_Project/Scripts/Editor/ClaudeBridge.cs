using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Lets Claude Code drive the open Editor without window focus. Claude writes one command per line to
    /// Temp/ClaudeBridge/cmd.txt; the bridge runs them and appends results to Temp/ClaudeBridge/log.txt.
    /// Commands: "refresh" (import changed files, recompile), "menu:Some/Menu/Path" (run a menu item).
    /// Temp/ is not in git and is wiped when Unity closes.
    /// </summary>
    [InitializeOnLoad]
    static class ClaudeBridge
    {
        const string Dir = "Temp/ClaudeBridge";
        const string CommandFile = Dir + "/cmd.txt";
        const string LogFile = Dir + "/log.txt";
        static double _next;

        static ClaudeBridge()
        {
            Directory.CreateDirectory(Dir);
            EditorApplication.update += Poll;
            Log("ready (domain reloaded)");
        }

        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 0.5;
            if (!File.Exists(CommandFile) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;

            string[] lines;
            try
            {
                lines = File.ReadAllLines(CommandFile);
                File.Delete(CommandFile);
            }
            catch (IOException)
            {
                return; // still being written
            }

            foreach (var raw in lines)
            {
                var cmd = raw.Trim();
                if (cmd.Length == 0) continue;
                try
                {
                    if (cmd == "refresh")
                    {
                        AssetDatabase.Refresh();
                        Log("refresh: ok");
                    }
                    else if (cmd.StartsWith("menu:", StringComparison.Ordinal))
                    {
                        var path = cmd.Substring(5);
                        bool ok = EditorApplication.ExecuteMenuItem(path);
                        Log($"{cmd}: {(ok ? "ok" : "menu item not found")}");
                    }
                    else
                    {
                        Log($"{cmd}: unknown command");
                    }
                }
                catch (Exception e)
                {
                    Log($"{cmd}: exception {e.GetType().Name}: {e.Message}");
                }
            }
        }

        static void Log(string message)
        {
            try { File.AppendAllText(LogFile, $"{DateTime.Now:HH:mm:ss} {message}\n"); }
            catch (IOException) { }
        }
    }
}
