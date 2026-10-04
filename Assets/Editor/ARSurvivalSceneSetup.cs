using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.SceneManagement;

public static class ARSurvivalSceneSetup
{
    const string SetupCompletedKey = "ARSurvivalSceneSetupCompleted";
    const string CustomPlanePath = "Assets/Prefabs/ARCustomNamePlane.prefab";
    const string AudioFolder = "Assets/Audio/GeneratedSFX";

    [InitializeOnLoadMethod]
    static void RegisterOpenSceneSetup()
    {
        EditorSceneManager.activeSceneChangedInEditMode += (previous, current) => TryPrepareActiveScene();
        EditorApplication.delayCall += TryPrepareActiveScene;
    }

    static void TryPrepareActiveScene()
    {
        if (SessionState.GetBool(SetupCompletedKey, false)) return;
        SessionState.SetBool(SetupCompletedKey, true);
        PrepareScene();
    }

    [MenuItem("Tools/AR Survival/Prepare ARMain Scene")]
    public static void PrepareScene()
    {
        const string scenePath = "Assets/Scenes/ARMain.unity";
        Scene previousScene = EditorSceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath(scenePath);
        bool openedAdditively = !scene.IsValid() || !scene.isLoaded;
        if (openedAdditively) scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("Open Assets/Scenes/ARMain.unity before preparing the AR Survival scene.");
            return;
        }

        SceneManager.SetActiveScene(scene);
        SetupWorld(scene);
        SetupAR(scene);
        SetupAudio(scene);
        SetupCanvas(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        if (previousScene.IsValid() && previousScene.isLoaded) SceneManager.SetActiveScene(previousScene);
        if (openedAdditively) EditorSceneManager.CloseScene(scene, true);
        Debug.Log("AR Survival scene setup is complete. Review the scene, then build for Android.");
    }

    static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null) return component;
        }
        return null;
    }

    static GameObject FindNamed(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name) return root;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child.gameObject;
        }
        return null;
    }

    static void SetupWorld(Scene scene)
    {
        GameObject environment = FindNamed(scene, "GameEnvironment") ?? FindNamed(scene, "BarnEnvironment");
        GameObject world = FindNamed(scene, "GameWorldRoot");
        if (world == null)
        {
            world = new GameObject("GameWorldRoot");
            if (environment != null)
            {
                world.transform.SetPositionAndRotation(environment.transform.position, environment.transform.rotation);
                world.transform.localScale = environment.transform.localScale;
                environment.transform.SetParent(world.transform, true);
                environment.name = "BarnEnvironment";
            }
        }

        foreach (EnemyBase enemy in world.GetComponentsInChildren<EnemyBase>(true))
            UnityEngine.Object.DestroyImmediate(enemy.gameObject);

        FixEnemyPrefab("Assets/Prefabs/Prefabs 2/enemy.prefab", true);
        FixEnemyPrefab("Assets/Prefabs/Prefabs 2/shooter enemy.prefab", false);

        EnemySpawner spawner = FindInScene<EnemySpawner>(scene);
        if (spawner != null)
        {
            spawner.transform.SetParent(world.transform, false);
            spawner.transform.localPosition = Vector3.zero;
            spawner.transform.localRotation = Quaternion.identity;
            spawner.transform.localScale = Vector3.one;
        }

        GameManager gameManager = FindInScene<GameManager>(scene);
        if (gameManager != null) gameManager.enemySpawner = spawner;

        Camera sceneCamera = FindInScene<Camera>(scene);
        PlayerController player = FindInScene<PlayerController>(scene);
        GameObject firePoint = FindNamed(scene, "FirePoint2");
        if (sceneCamera != null && player != null && firePoint != null)
        {
            firePoint.transform.SetParent(sceneCamera.transform, false);
            firePoint.transform.localPosition = new Vector3(0f, -0.1f, 0.5f);
            firePoint.transform.localRotation = Quaternion.identity;
            player.firePoint = firePoint.transform;
        }
        if (environment != null)
        {
            var renderers = environment.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                Vector3 groundPoint = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                float localFloor = world.transform.InverseTransformPoint(groundPoint).y;
                environment.transform.localPosition -= Vector3.up * localFloor;
            }
        }
        world.SetActive(false);
    }

    static void FixEnemyPrefab(string path, bool isMelee)
    {
        GameObject prefab = PrefabUtility.LoadPrefabContents(path);
        if (prefab == null) return;
        prefab.tag = "Enemy";
        if (isMelee)
        {
            MeleeEnemy melee = prefab.GetComponent<MeleeEnemy>();
            Animator animator = prefab.GetComponentInChildren<Animator>(true);
            RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Prefabs/MeleeEnemyAnimator.controller");
            if (melee != null && animator != null)
            {
                animator.runtimeAnimatorController = controller;
                melee.animator = animator;
            }
        }
        PrefabUtility.SaveAsPrefabAsset(prefab, path);
        PrefabUtility.UnloadPrefabContents(prefab);
    }

    static void SetupAR(Scene scene)
    {
        ARPlaneManager planeManager = FindInScene<ARPlaneManager>(scene);
        ARRaycastManager raycastManager = FindInScene<ARRaycastManager>(scene);
        if (planeManager == null)
        {
            Debug.LogError("ARPlaneManager was not found on the XR Origin. Plane detection cannot run.");
            return;
        }
        if (raycastManager == null)
            raycastManager = planeManager.gameObject.GetComponent<ARRaycastManager>() ?? planeManager.gameObject.AddComponent<ARRaycastManager>();

        GameObject world = FindNamed(scene, "GameWorldRoot");
        ARPlacementController placement = raycastManager.GetComponent<ARPlacementController>();
        if (placement == null) placement = raycastManager.gameObject.AddComponent<ARPlacementController>();
        placement.raycastManager = raycastManager;
        placement.planeManager = planeManager;
        placement.gameWorldRoot = world;

        EnsureCustomPlanePrefab();
        var customPlane = AssetDatabase.LoadAssetAtPath<GameObject>(CustomPlanePath);
        planeManager.planePrefab = customPlane;
        planeManager.requestedDetectionMode = UnityEngine.XR.ARSubsystems.PlaneDetectionMode.Horizontal;
        PrefabUtility.RecordPrefabInstancePropertyModifications(planeManager);
        EditorUtility.SetDirty(planeManager);
    }

    static void EnsureCustomPlanePrefab()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
        GameObject source = new GameObject("ARCustomNamePlane");
        source.AddComponent<ARPlane>();
        var visualizer = source.AddComponent<CustomPlaneVisualizer>();
        visualizer.planeMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/My-Name.mat");
        PrefabUtility.SaveAsPrefabAsset(source, CustomPlanePath);
        UnityEngine.Object.DestroyImmediate(source);
    }

    static void SetupAudio(Scene scene)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Audio")) AssetDatabase.CreateFolder("Assets", "Audio");
        if (!AssetDatabase.IsValidFolder(AudioFolder)) AssetDatabase.CreateFolder("Assets/Audio", "GeneratedSFX");
        string[] names = { "PlayerShoot", "PlayerDeath", "EnemySpawn", "EnemyShoot", "MeleeAttack" };
        float[] starts = { 900f, 260f, 180f, 680f, 120f };
        float[] ends = { 300f, 65f, 430f, 380f, 55f };
        float[] durations = { 0.14f, 0.72f, 0.35f, 0.16f, 0.22f };
        for (int i = 0; i < names.Length; i++) WriteTone(AudioFolder + "/" + names[i] + ".wav", starts[i], ends[i], durations[i], i);
        AssetDatabase.Refresh();

        AudioManager audio = FindInScene<AudioManager>(scene);
        if (audio == null) return;
        audio.playerShootClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioFolder + "/PlayerShoot.wav");
        audio.playerDeathClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioFolder + "/PlayerDeath.wav");
        audio.enemySpawnClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioFolder + "/EnemySpawn.wav");
        audio.enemyShootClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioFolder + "/EnemyShoot.wav");
        audio.meleeDamageClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioFolder + "/MeleeAttack.wav");
        EditorUtility.SetDirty(audio);
    }

    static void WriteTone(string path, float startHz, float endHz, float seconds, int style)
    {
        if (File.Exists(path)) return;
        const int sampleRate = 22050;
        int samples = Mathf.CeilToInt(sampleRate * seconds);
        byte[] data = new byte[44 + samples * 2];
        Action<int, int> writeInt = (offset, value) => BitConverter.GetBytes(value).CopyTo(data, offset);
        data[0] = (byte)'R'; data[1] = (byte)'I'; data[2] = (byte)'F'; data[3] = (byte)'F';
        writeInt(4, data.Length - 8);
        data[8] = (byte)'W'; data[9] = (byte)'A'; data[10] = (byte)'V'; data[11] = (byte)'E';
        data[12] = (byte)'f'; data[13] = (byte)'m'; data[14] = (byte)'t'; data[15] = (byte)' ';
        writeInt(16, 16); BitConverter.GetBytes((short)1).CopyTo(data, 20);
        BitConverter.GetBytes((short)1).CopyTo(data, 22); writeInt(24, sampleRate);
        writeInt(28, sampleRate * 2); BitConverter.GetBytes((short)2).CopyTo(data, 32);
        BitConverter.GetBytes((short)16).CopyTo(data, 34);
        data[36] = (byte)'d'; data[37] = (byte)'a'; data[38] = (byte)'t'; data[39] = (byte)'a';
        writeInt(40, samples * 2);
        double phase = 0;
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)samples;
            float hz = Mathf.Lerp(startHz, endHz, t);
            phase += 2.0 * Math.PI * hz / sampleRate;
            float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t));
            float wave = (float)Math.Sin(phase);
            if (style == 2 || style == 4) wave = 0.7f * wave + 0.3f * Mathf.Sign((float)Math.Sin(phase * 0.51));
            if (style == 1) envelope = Mathf.Pow(1f - t, 1.7f);
            short value = (short)(Mathf.Clamp(wave * envelope * 0.55f, -1f, 1f) * short.MaxValue);
            BitConverter.GetBytes(value).CopyTo(data, 44 + i * 2);
        }
        File.WriteAllBytes(path, data);
    }

    static void SetupCanvas(Scene scene)
    {
        Canvas canvas = FindInScene<Canvas>(scene);
        if (canvas == null) return;
        canvas.transform.localScale = Vector3.one;
        var rect = canvas.transform as RectTransform;
        if (rect != null) rect.localScale = Vector3.one;
        CanvasScalerSetup(canvas);
        UIManager ui = canvas.GetComponent<UIManager>();
        if (ui != null)
        {
            ui.BuildUI();
            EditorUtility.SetDirty(ui);
        }
    }

    static void CanvasScalerSetup(Canvas canvas)
    {
        var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
        if (scaler == null) return;
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        EditorUtility.SetDirty(scaler);
    }
}
