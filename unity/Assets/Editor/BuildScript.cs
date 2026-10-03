using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Headless WebGL build for CI / batchmode:
//   Unity -batchmode -nographics -quit -projectPath <proj> \
//         -executeMethod BuildScript.BuildWebGL -logFile -
// Outputs to <repo>/webgl/build_out/ai-conversation-agent/ ; the Build/ subfolder
// there is what the embed page (webgl/index.html) loads.
public static class BuildScript
{
    static readonly string[] Scenes = { "Assets/Scenes/QuestionScene.unity" };
    const string BuildLeafName = "ai-conversation-agent"; // must match BUILD_NAME in webgl/index.html

    public static void BuildWebGL()
    {
        // Serve-anywhere: no compression so a plain static server (python -m http.server) works.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = true;

        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

        string projectDir = Directory.GetParent(Application.dataPath).FullName;         // .../unity
        string repoDir = Directory.GetParent(projectDir).FullName;                      // .../vex-pedagogical-agent
        string outDir = Path.Combine(repoDir, "webgl", "build_out", BuildLeafName);

        BuildReport report = BuildPipeline.BuildPlayer(
            Scenes, outDir, BuildTarget.WebGL, BuildOptions.None);

        BuildSummary summary = report.summary;
        Debug.Log($"WebGL build {summary.result}: {summary.totalSize} bytes -> {outDir}");
        if (summary.result != BuildResult.Succeeded)
        {
            EditorApplication.Exit(1);
        }
    }
}
