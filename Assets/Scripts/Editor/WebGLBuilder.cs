#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;

public static class WebGLBuilder
{
    [MenuItem("Tools/WebGL/Validate Settings")]
    public static void ValidateWebGLSettings()
    {
        Debug.Log("=== WebGL Settings Validation ===");

        // 1) Scenes in BuildSettings
        var scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();
        if (scenes.Length == 0)
        {
            Debug.LogError("Build Settings에 켜진 씬이 없습니다! Scenes 리스트를 확인하세요.");
        }
        else
        {
            Debug.Log($"Build Settings에 포함된 씬 개수: {scenes.Length}");
            for (int i = 0; i < scenes.Length; i++)
                Debug.Log($"  [{i}] {scenes[i]}");
            if (!scenes[0].EndsWith("LoginScene.unity"))
                Debug.LogWarning("첫 번째 씬이 LoginScene이 아닙니다. WebGL 로드시 자동 로드할 첫 씬이 맞는지 확인하세요.");
        }

        // 2) PlayerSettings for WebGL
        int memSize = PlayerSettings.WebGL.memorySize;
        Debug.Log($"WebGL Memory Size: {memSize}MB (권장: 최소 256MB 이상)");

        string linker = PlayerSettings.WebGL.linkerTarget.ToString();
        Debug.Log($"WebGL Linker Target: {linker} (추천: WebAssembly)");

        bool dataCache = PlayerSettings.WebGL.dataCaching;
        Debug.Log($"WebGL Data Caching: {(dataCache ? "Enabled" : "Disabled")}");

        string compression = PlayerSettings.WebGL.compressionFormat.ToString();
        Debug.Log($"WebGL Compression Format: {compression}");

        // 3) Template
        string template = PlayerSettings.WebGL.template;
        Debug.Log($"WebGL Template: {template}");

        Debug.Log("=== Validation Complete ===");
    }

    [MenuItem("Tools/WebGL/Build")]
    public static void BuildWebGL()
    {
        var scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Debug.LogError("Build Settings에 켜져 있는 씬이 없습니다. Tools→WebGL→Validate Settings로 확인하세요.");
            return;
        }

        string buildPath = "Build/WebGL";
        Debug.Log($"WebGL 빌드 시작 → {buildPath}");

        var opts = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(opts);
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            Debug.Log($"WebGL 빌드 성공! 소요시간: {report.summary.totalTime}");
        else
            Debug.LogError("WebGL 빌드 실패! 로그를 확인하세요.");
    }
}
#endif
