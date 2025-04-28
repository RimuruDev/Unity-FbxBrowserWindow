#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Object = UnityEngine.Object;

public sealed class FbxBrowserWindow : EditorWindow
{
    private const string ExpFilter = ".fbx";
    private const string ModelFilter = "t:Model";

    private Vector2 scrollPosition;
    private readonly List<bool> selected = new();
    private readonly List<string> fbxPaths = new();

    private bool generateLightmapUVs = true;
    private float hardAngle = 60f;
    private float angleDistortion = 8f;
    private float areaDistortion = 15f;
    private float packMargin = 4f;

    [MenuItem("Tools/FBX Browser")]
    private static void ShowWindow()
    {
        var window = GetWindow<FbxBrowserWindow>();
        window.titleContent = new GUIContent("FBX Browser");
        window.Refresh();
    }

    private void OnEnable() =>
        Refresh();

    private void Refresh()
    {
        var guids = AssetDatabase.FindAssets(ModelFilter);
        fbxPaths.Clear();
        selected.Clear();

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(ExpFilter, StringComparison.OrdinalIgnoreCase))
            {
                fbxPaths.Add(path);
                selected.Add(false);
            }
        }
    }

    private void OnGUI()
    {
        DrawToolbar();
        DrawList();
        DrawBatchSettings();
    }

    private void DrawToolbar()
    {
        using var h = new GUILayout.HorizontalScope(EditorStyles.toolbar);

        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
            Refresh();

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Select All", EditorStyles.toolbarButton))
        {
            for (var i = 0; i < selected.Count; i++)
                selected[i] = true;
        }

        if (GUILayout.Button("Deselect All", EditorStyles.toolbarButton))
        {
            for (var i = 0; i < selected.Count; i++)
                selected[i] = false;
        }
    }

    private void DrawList()
    {
        using var scroll = new GUILayout.ScrollViewScope(scrollPosition);
        scrollPosition = scroll.scrollPosition;

        for (var i = 0; i < fbxPaths.Count; i++)
        {
            using var h = new GUILayout.HorizontalScope();

            selected[i] = GUILayout.Toggle(selected[i], GUIContent.none, GUILayout.Width(20));

            var obj = AssetDatabase.LoadAssetAtPath<Object>(fbxPaths[i]);

            if (!GUILayout.Button(obj.name, EditorStyles.linkLabel))
                continue;

            EditorGUIUtility.PingObject(obj);
            Selection.activeObject = obj;
        }

        if (GUILayout.Button("Select In Project"))
            Selection.objects = fbxPaths.Where((_, i) => selected[i]).Select(AssetDatabase.LoadAssetAtPath<Object>).ToArray();
    }

    private void DrawBatchSettings()
    {
        GUILayout.Space(10);
        GUILayout.Label("Batch Settings", EditorStyles.boldLabel);

        generateLightmapUVs = GUILayout.Toggle(generateLightmapUVs, "Generate Lightmap UVs");

        using (new GUILayout.VerticalScope("box"))
        {
            hardAngle = EditorGUILayout.Slider("Hard Angle", hardAngle, 0f, 180f);
            angleDistortion = EditorGUILayout.Slider("Angle Distortion", angleDistortion, 1f, 75f);
            areaDistortion = EditorGUILayout.Slider("Area Distortion", areaDistortion, 1f, 100f);
            packMargin = EditorGUILayout.Slider("Pack Margin", packMargin, 1f, 64f);
        }

        if (GUILayout.Button("Apply Lightmap UV Settings To Selected"))
        {
            ApplyBatch(importer =>
            {
                importer.generateSecondaryUV = generateLightmapUVs;
                importer.secondaryUVHardAngle = hardAngle;
                importer.secondaryUVAngleDistortion = angleDistortion;
                importer.secondaryUVAreaDistortion = areaDistortion;
                importer.secondaryUVPackMargin = packMargin;
            });
        }

        GUILayout.Space(10);

        using var h = new GUILayout.HorizontalScope();

        if (GUILayout.Button("Toggle Read/Write"))
            ApplyBatch(i => i.isReadable = !i.isReadable);

        if (GUILayout.Button("Toggle Optimize Mesh Polygons"))
            ApplyBatch(i => i.optimizeMeshPolygons = !i.optimizeMeshPolygons);
    }

    private void ApplyBatch(Action<ModelImporter> mutator)
    {
        var paths = fbxPaths.Where((_, i) => selected[i]).ToList();

        if (paths.Count == 0)
        {
            EditorUtility.DisplayDialog("FBX Browser", "No FBX selected.", "OK");
            return;
        }

        try
        {
            AssetDatabase.StartAssetEditing();

            foreach (var importer in paths.Select(path => (ModelImporter)AssetImporter.GetAtPath(path)))
            {
                mutator(importer);
                importer.SaveAndReimport();
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }
    }
}
#endif
