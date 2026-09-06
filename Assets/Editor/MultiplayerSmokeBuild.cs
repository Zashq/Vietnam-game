using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class MultiplayerSmokeBuild
{
    [InitializeOnLoadMethod]
    private static void CheckRequest()
    {
        EditorApplication.delayCall += () =>
        {
            const string request = "Temp/BuildMultiplayerSmoke.request";
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(request);
            try
            {
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { MultiplayerArenaSetup.ScenePath },
                    locationPathName = "Builds/MultiplayerSmoke/VietnamMultiplayer.exe",
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.Development
                });
                File.WriteAllText("Temp/MultiplayerSmokeBuild.result", report.summary.result + " errors=" + report.summary.totalErrors);
            }
            catch (Exception exception) { File.WriteAllText("Temp/MultiplayerSmokeBuild.result", exception.ToString()); Debug.LogException(exception); }
        };
    }
}
