#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ScoreDeliveryUISetup
{
    const string CanvasPrefabPath = "Assets/Prefabs/RB_Package/Canvas.prefab";
    const string TravelPrefabFolder = "Assets/#2. Prefabs/UI";
    const string TravelPrefabPath = TravelPrefabFolder + "/UI Value Travel Image.prefab";
    const string BattleScenePath = "Assets/#3. Scenes/SampleScene.unity";

    [MenuItem("Tools/Ricochet Bounty/Configure Score Delivery UI")]
    public static void Configure()
    {
        EnsureFolder("Assets/#2. Prefabs", "UI");
        UIValueTravelImage travelPrefab = CreateOrUpdateTravelPrefab();
        ConfigureCanvasPrefab(travelPrefab);
        ConfigureBattleScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ScoreDeliveryUISetup] Score delivery prefab, Canvas references, and scene references configured.");
    }

    public static void ConfigureFromCommandLine()
    {
        Configure();
    }

    static UIValueTravelImage CreateOrUpdateTravelPrefab()
    {
        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(TravelPrefabPath);
        GameObject instance = prefabAsset != null
            ? PrefabUtility.LoadPrefabContents(TravelPrefabPath)
            : new GameObject("UI Value Travel Image", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

        try
        {
            RectTransform rectTransform = instance.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(22f, 22f);

            Image image = instance.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = Color.white;

            if (instance.GetComponent<UIValueTravelImage>() == null)
            {
                instance.AddComponent<UIValueTravelImage>();
            }

            PrefabUtility.SaveAsPrefabAsset(instance, TravelPrefabPath);
        }
        finally
        {
            if (prefabAsset != null)
            {
                PrefabUtility.UnloadPrefabContents(instance);
            }
            else
            {
                Object.DestroyImmediate(instance);
            }
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(TravelPrefabPath).GetComponent<UIValueTravelImage>();
    }

    static void ConfigureCanvasPrefab(UIValueTravelImage travelPrefab)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(CanvasPrefabPath);
        try
        {
            Canvas canvas = root.GetComponent<Canvas>();
            DamageUI damageUI = root.GetComponentInChildren<DamageUI>(true);
            ScoreDeliveryUI deliveryUI = root.GetComponent<ScoreDeliveryUI>();
            if (deliveryUI == null)
            {
                deliveryUI = root.AddComponent<ScoreDeliveryUI>();
            }

            SerializedObject damageUISerialized = new SerializedObject(damageUI);
            RectTransform chipsTarget = GetTextRect(damageUISerialized, "chipsText");
            RectTransform multiplierTarget = GetTextRect(damageUISerialized, "multiplierText");
            RectTransform damageSource = GetTextRect(damageUISerialized, "finalScoreText");
            Image enemyHpFill = FindNamedComponent<Image>(root.transform, "Enemy_HP_Value");

            SerializedObject serialized = new SerializedObject(deliveryUI);
            serialized.FindProperty("canvas").objectReferenceValue = canvas;
            serialized.FindProperty("travelRoot").objectReferenceValue = canvas.GetComponent<RectTransform>();
            serialized.FindProperty("travelImagePrefab").objectReferenceValue = travelPrefab;
            serialized.FindProperty("damageUI").objectReferenceValue = damageUI;
            serialized.FindProperty("chipsTarget").objectReferenceValue = chipsTarget;
            serialized.FindProperty("multiplierTarget").objectReferenceValue = multiplierTarget;
            serialized.FindProperty("damageSource").objectReferenceValue = damageSource;
            serialized.FindProperty("enemyHpFillImage").objectReferenceValue = enemyHpFill;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, CanvasPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void ConfigureBattleScene()
    {
        Scene scene = EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Single);
        DamageManager damageManager = Object.FindFirstObjectByType<DamageManager>(FindObjectsInactive.Include);
        DamageUI damageUI = Object.FindFirstObjectByType<DamageUI>(FindObjectsInactive.Include);
        ScoreDeliveryUI deliveryUI = Object.FindFirstObjectByType<ScoreDeliveryUI>(FindObjectsInactive.Include);
        RoundManager roundManager = Object.FindFirstObjectByType<RoundManager>(FindObjectsInactive.Include);

        if (damageManager == null || damageUI == null || deliveryUI == null)
        {
            throw new MissingReferenceException("SampleScene requires DamageManager, DamageUI, and ScoreDeliveryUI.");
        }

        SerializedObject managerSerialized = new SerializedObject(damageManager);
        managerSerialized.FindProperty("scoreDeliveryUI").objectReferenceValue = deliveryUI;
        managerSerialized.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject damageUISerialized = new SerializedObject(damageUI);
        damageUISerialized.FindProperty("damageManager").objectReferenceValue = damageManager;
        damageUISerialized.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject deliverySerialized = new SerializedObject(deliveryUI);
        deliverySerialized.FindProperty("worldCamera").objectReferenceValue = Camera.main;
        if (roundManager != null)
        {
            SerializedObject roundSerialized = new SerializedObject(roundManager);
            deliverySerialized.FindProperty("enemyHpText").objectReferenceValue =
                roundSerialized.FindProperty("targetEnemyHpText").objectReferenceValue;
        }
        deliverySerialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static RectTransform GetTextRect(SerializedObject owner, string propertyName)
    {
        Component component = owner.FindProperty(propertyName).objectReferenceValue as Component;
        return component != null ? component.GetComponent<RectTransform>() : null;
    }

    static T FindNamedComponent<T>(Transform root, string objectName) where T : Component
    {
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == objectName && transforms[i].TryGetComponent(out T component))
            {
                return component;
            }
        }

        return null;
    }

    static void EnsureFolder(string parent, string name)
    {
        string path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
