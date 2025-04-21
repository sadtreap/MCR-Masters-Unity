using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEditor.Build.Reporting;

public class WebGLBuilder
{
    [MenuItem("Build/Build WebGL")]
    public static void BuildWebGL()
    {
        string buildPath = "Builds/WebGL";

        // 빌드 폴더 없으면 생성
        if (!Directory.Exists(buildPath))
            Directory.CreateDirectory(buildPath);

        // 씬 리스트
        string[] scenes = new[]
        {
            "Assets/Scenes/LoginScene.unity",
            "Assets/Scenes/LobbyScene.unity",
            "Assets/Scenes/RoomListScene.unity",
            "Assets/Scenes/RoomScene.unity",
            "Assets/Scenes/GameScene.unity",
        };

        // 빌드 설정
        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        // 빌드 수행
        BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("✅ WebGL 빌드 성공! 출력 경로: " + summary.outputPath);
        }
        else
        {
            Debug.LogError("❌ WebGL 빌드 실패!");
        }
    }
}
