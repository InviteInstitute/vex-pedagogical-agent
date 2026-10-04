using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Headless WebGL build for CI / batchmode:
//   Unity -batchmode -nographics -quit -projectPath <proj> -buildTarget WebGL \
//         -executeMethod BuildScript.BuildWebGL -logFile -
// -buildTarget WebGL matters: opened for another target, the editor compiles scripts
// without UNITY_WEBGL and the build fails with "script class layout is incompatible"
// (uLipSync has WebGL-only fields); switching target inside this method is too late.
// Outputs to <proj>/Builds/WebGL/ (git-ignored). Install it where the avatar page
// loads it with webgl/install_build.sh unity/Builds/WebGL, the same step as a build
// made from File > Build Settings.
public static class BuildScript
{
    static readonly string[] Scenes = { "Assets/Scenes/QuestionScene.unity" };

    public static void BuildWebGL()
    {
        // Brotli with decompression fallback: about 17MB for students instead of ~40MB
        // uncompressed, and a plain static server (nginx, python -m http.server) can
        // serve it because the loader decompresses in the browser.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.decompressionFallback = true;

        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

        string projectDir = Directory.GetParent(Application.dataPath).FullName;         // .../unity
        string outDir = Path.Combine(projectDir, "Builds", "WebGL");

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
