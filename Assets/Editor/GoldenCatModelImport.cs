#if UNITY_EDITOR
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityGLTF;

/// <summary>Builds the checked-in CC0 GLB into a prefab for runtime use.</summary>
[InitializeOnLoad]
static class GoldenCatModelImport
{
    const string Source = "Assets/CatCafe/Models/GoldenShorthair/GoldenShorthair.glb";
    const string OutputFolder = "Assets/CatCafe/Resources/Cats";
    const string PrefabPath = OutputFolder + "/GoldenCat.prefab";

    static GLTFEditorImporter importer;
    static FieldInfo isDoneField;

    static GoldenCatModelImport() => EditorApplication.delayCall += BeginImport;

    static void BeginImport()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) || !File.Exists(Source)) return;
        Directory.CreateDirectory(OutputFolder);
        importer = new GLTFEditorImporter();
        isDoneField = typeof(GLTFEditorImporter).GetField("_isDone", BindingFlags.Instance | BindingFlags.NonPublic);
        importer.setupForPath(Path.GetFullPath(Source), OutputFolder, "GoldenCat", false);
        importer.Load(true);
        EditorApplication.update += ContinueImport;
    }

    static void ContinueImport()
    {
        if (importer == null) return;
        importer.Update();
        if (isDoneField == null || !(bool)isDoneField.GetValue(importer)) return;
        EditorApplication.update -= ContinueImport;
        importer = null;
        AssetDatabase.Refresh();
        Debug.Log("Imported CC0 golden shorthair companion prefab.");
    }
}
#endif
