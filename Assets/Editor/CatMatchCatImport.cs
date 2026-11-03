#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>Builds the Cat-Match-AR model prefab with controllable source animations.</summary>
[InitializeOnLoad]
static class CatMatchCatImport
{
    const string ModelPath = "Assets/CatCafe/Models/CatMatch/Cat Lite.fbx";
    const string TexturePath = "Assets/CatCafe/Models/CatMatch/Textures/Cats/Tex_Cat_OrangeTabby.png";
    const string Folder = "Assets/CatCafe/Resources/Cats";
    const string MaterialPath = Folder + "/CatMatchCat.mat";
    const string ControllerPath = Folder + "/CatMatchCat.controller";
    const string PrefabPath = Folder + "/CatMatchCat.prefab";

    static CatMatchCatImport() => EditorApplication.delayCall += Import;

    static void Import()
    {
        if (!File.Exists(ModelPath) || !File.Exists(TexturePath)) return;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)) return;
        Directory.CreateDirectory(Folder);

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (!material)
        {
            material = new Material(shader) { name = "Cat-Match low-poly tabby" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);

        var clips = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__"))
            .ToDictionary(c => c.name, c => c);
        if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath))
            AssetDatabase.DeleteAsset(ControllerPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Walking", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Play", AnimatorControllerParameterType.Trigger);
        var machine = controller.layers[0].stateMachine;
        var idle = machine.AddState("Idle_A");
        machine.defaultState = idle;
        idle.motion = clips["Idle_A"];
        var walk = machine.AddState("Walk"); walk.motion = clips["Walk"];
        var run = machine.AddState("Run"); run.motion = clips["Run"];
        var idleWalk = idle.AddTransition(walk); idleWalk.hasExitTime = false;
        idleWalk.AddCondition(AnimatorConditionMode.If, 0, "Walking");
        var walkIdle = walk.AddTransition(idle); walkIdle.hasExitTime = false;
        walkIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "Walking");
        var anyRun = machine.AddAnyStateTransition(run); anyRun.hasExitTime = false;
        anyRun.AddCondition(AnimatorConditionMode.If, 0, "Play");
        var runIdle = run.AddTransition(idle); runIdle.hasExitTime = true; runIdle.exitTime = .92f;
        runIdle.duration = .12f;

        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        var instance = Object.Instantiate(model);
        instance.name = "CatMatchCat";
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;
        foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            renderer.sharedMaterial = material;
        var animator = instance.GetComponent<Animator>();
        if (!animator) animator = instance.AddComponent<Animator>();
        animator.avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
        Object.DestroyImmediate(instance);
        AssetDatabase.SaveAssets();
        Debug.Log("Imported Cat-Match-AR low-poly cat and generated idle/walk/play animation prefab.");
    }
}
#endif
