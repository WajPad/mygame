using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Poziva se iz GitHub Actions (game-ci): -executeMethod BuildScript.BuildWindows
public static class BuildScript
{
    const string ScenePath = "Assets/Scenes/Main.unity";

    static readonly string[] AlwaysIncludedShaders =
    {
        "Standard",
        "Nature/Terrain/Diffuse",
        "Unlit/Color",
        "Skybox/Procedural",
        "Sprites/Default",
        "Hidden/Internal-Colored",
    };

    [MenuItem("Forest/Napravi scenu")]
    public static void CreateScene()
    {
        Directory.CreateDirectory("Assets/Scenes");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Prazan teren (da Unity ukljuci terrain shadere u build). Pravi teren generise igra u runtime-u.
        var td = new TerrainData { heightmapResolution = 33 };
        td.size = new Vector3(10f, 1f, 10f);
        AssetDatabase.CreateAsset(td, "Assets/Scenes/PlaceholderTerrain.asset");
        var terrainGo = Terrain.CreateTerrainGameObject(td);
        terrainGo.name = "Terrain";

        var game = new GameObject("Game");
        game.AddComponent<GameBootstrap>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
    }

    static void ConfigurePlayer()
    {
        PlayerSettings.companyName = "Forest Survival";
        PlayerSettings.productName = "Forest Survival";
        PlayerSettings.colorSpace = ColorSpace.Gamma;
        PlayerSettings.defaultIsNativeResolution = true;
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.runInBackground = true;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Standalone, ManagedStrippingLevel.Disabled);
        foreach (var s in AlwaysIncludedShaders) AddAlwaysIncludedShader(s);
    }

    static void AddAlwaysIncludedShader(string shaderName)
    {
        var shader = Shader.Find(shaderName);
        if (shader == null) return;

        var all = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
        if (all == null || all.Length == 0) { Debug.LogWarning("GraphicsSettings nije nadjen"); return; }
        var graphicsSettings = all[0];

        var so = new SerializedObject(graphicsSettings);
        var arr = so.FindProperty("m_AlwaysIncludedShaders");
        for (int i = 0; i < arr.arraySize; i++)
            if (arr.GetArrayElementAtIndex(i).objectReferenceValue == shader) return;

        arr.InsertArrayElementAtIndex(arr.arraySize);
        arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = shader;
        so.ApplyModifiedProperties();
    }

    static string GetArg(string name, string fallback)
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "-" + name) return args[i + 1];
        return fallback;
    }

    public static void BuildWindows()
    {
        CreateScene();
        ConfigurePlayer();

        string outDir = GetArg("customBuildPath", "build/StandaloneWindows64");
        string name = GetArg("customBuildName", "ForestSurvival");
        if (outDir.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            outDir = Path.GetDirectoryName(outDir);
        Directory.CreateDirectory(outDir);

        var options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = Path.Combine(outDir, name + ".exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };

        var report = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;
        Debug.Log("Build rezultat: " + summary.result + " (" + summary.totalSize + " bajtova)");

        if (summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }
}
