using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Desktopirates.Editor
{
    public static class DesktopiratesBuild
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("desktopirates/Prepare Project")]
        public static void PrepareProject()
        {
            Directory.CreateDirectory("Assets/Scenes");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            PlayerSettings.companyName = "desktopirates";
            PlayerSettings.productName = "desktopirates";
            PlayerSettings.applicationIdentifier = "com.desktopirates.game";
            PlayerSettings.defaultScreenWidth = 720;
            PlayerSettings.defaultScreenHeight = 760;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = false;
            PlayerSettings.runInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });

            Directory.CreateDirectory("Assets/Resources");
            CreateOrUpdateMaterial("Assets/Resources/OceanMaterial.mat", Shader.Find("Desktopirates/Ocean"));
            CreateOrUpdateMaterial("Assets/Resources/StandardMaterial.mat", Shader.Find("Standard"));

            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 0;
            AssetDatabase.SaveAssets();
            Debug.Log("desktopirates project prepared.");
        }

        [MenuItem("desktopirates/Build Windows")]
        public static void BuildWindows()
        {
            PrepareProject();
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string output = Path.Combine(projectRoot, "Builds", "Windows", "desktopirates.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"desktopirates build failed: {report.summary.result}");

            Debug.Log($"desktopirates build succeeded: {output} ({report.summary.totalSize} bytes)");
        }

        private static void CreateOrUpdateMaterial(string path, Shader shader)
        {
            if (shader == null) throw new BuildFailedException($"Required shader was not found for {path}");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
                EditorUtility.SetDirty(material);
            }
        }
    }
}
