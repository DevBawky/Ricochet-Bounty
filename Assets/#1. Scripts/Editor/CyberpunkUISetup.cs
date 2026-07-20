#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class CyberpunkUISetup
{
    const string CanvasPath = "Assets/Prefabs/RB_Package/Canvas.prefab";
    const string TravelPrefabPath = "Assets/#2. Prefabs/UI/UI Value Travel Image.prefab";
    const string MaterialFolder = "Assets/Prefabs/RB_Package/Materials";

    [MenuItem("Tools/Ricochet Bounty/Apply Cyberpunk UI Setup")]
    public static void Apply()
    {
        Material energy = GetOrCreateMaterial(
            MaterialFolder + "/M_CyberEnergyProjectile.mat",
            "UI/Cyber Energy Projectile");
        Material chips = GetOrCreateMaterial(
            MaterialFolder + "/M_CyberElectricChips.mat",
            "UI/Cyber Electric Overheat Border");
        Material multiplier = GetOrCreateMaterial(
            MaterialFolder + "/M_CyberElectricMultiplier.mat",
            "UI/Cyber Electric Overheat Border");
        Material dissolve = GetOrCreateMaterial(
            MaterialFolder + "/M_CyberPanelDissolve.mat",
            "UI/Cyber Panel Dissolve");

        ConfigureMaterialDefaults(energy, chips, multiplier, dissolve);
        ConfigureTravelPrefab(energy);
        ConfigureCanvasPrefab(chips, multiplier, dissolve);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CyberpunkUISetup] Shaders, materials, score travel, kill highlight, and panel dissolve are configured.");
    }

    static Material GetOrCreateMaterial(string path, string shaderName)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find(shaderName);
        if (shader == null)
        {
            throw new System.InvalidOperationException("Shader not found: " + shaderName);
        }

        if (material == null)
        {
            material = new Material(shader) { name = System.IO.Path.GetFileNameWithoutExtension(path) };
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = shader;
            EditorUtility.SetDirty(material);
        }

        return material;
    }

    static void ConfigureMaterialDefaults(Material energy, Material chips, Material multiplier, Material dissolve)
    {
        energy.SetColor("_CoreColor", new Color(2.2f, 3.2f, 4.5f, 1f));
        energy.SetColor("_RingColor", new Color(0.05f, 1.7f, 4f, 1f));
        energy.SetFloat("_GlowWidth", 0.16f);
        energy.SetFloat("_GlowIntensity", 2.2f);

        chips.SetColor("_ElectricColor", new Color(4f, 1.45f, 0.05f, 1f));
        chips.SetColor("_HotColor", new Color(4f, 0.15f, 0.02f, 1f));
        chips.SetFloat("_BorderWidth", 0.12f);

        multiplier.SetColor("_ElectricColor", new Color(0.05f, 2.4f, 4f, 1f));
        multiplier.SetColor("_HotColor", new Color(0.8f, 0.2f, 4f, 1f));
        multiplier.SetFloat("_BorderWidth", 0.12f);

        dissolve.SetColor("_EdgeColor", new Color(0.05f, 1.8f, 4f, 1f));
        dissolve.SetFloat("_DissolveAmount", 0f);

        EditorUtility.SetDirty(energy);
        EditorUtility.SetDirty(chips);
        EditorUtility.SetDirty(multiplier);
        EditorUtility.SetDirty(dissolve);
    }

    static void ConfigureTravelPrefab(Material energy)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(TravelPrefabPath);
        try
        {
            Image image = root.GetComponent<Image>();
            if (image == null)
            {
                throw new System.InvalidOperationException("UI Value Travel Image prefab has no Image component.");
            }

            image.material = energy;
            EditorUtility.SetDirty(image);
            PrefabUtility.SaveAsPrefabAsset(root, TravelPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void ConfigureCanvasPrefab(Material chips, Material multiplier, Material dissolve)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(CanvasPath);
        try
        {
            Transform playerPanel = FindDescendant(root.transform, "Player Panel");
            Image chipsImage = FindImage(root.transform, "Effect_Chips");
            Image multiplierImage = FindImage(root.transform, "Effect_Multiplier");

            if (chipsImage != null)
            {
                chipsImage.material = chips;
                AddMarker(chipsImage.gameObject);
            }

            if (multiplierImage != null)
            {
                multiplierImage.material = multiplier;
                AddMarker(multiplierImage.gameObject);
            }

            KillableDamageHighlightController highlight = GetOrAdd<KillableDamageHighlightController>(root);
            var highlightSo = new SerializedObject(highlight);
            highlightSo.FindProperty("chipsEffect").objectReferenceValue = chipsImage;
            highlightSo.FindProperty("multiplierEffect").objectReferenceValue = multiplierImage;
            highlightSo.FindProperty("chipsMaterialTemplate").objectReferenceValue = chips;
            highlightSo.FindProperty("multiplierMaterialTemplate").objectReferenceValue = multiplier;
            highlightSo.ApplyModifiedPropertiesWithoutUndo();

            CyberPanelDissolveController dissolveController = GetOrAdd<CyberPanelDissolveController>(root);
            var dissolveSo = new SerializedObject(dissolveController);
            dissolveSo.FindProperty("dissolveMaterialTemplate").objectReferenceValue = dissolve;
            dissolveSo.FindProperty("playerPanel").objectReferenceValue = playerPanel != null ? playerPanel.gameObject : null;

            string[] names = { "MainMenu", "Round Select", "Battle", "Result", "Shop", "Event", "Treasure", "GameOver", "Game Over", "Clear" };
            var panels = new List<GameObject>();
            for (int i = 0; i < names.Length; i++)
            {
                Transform panel = FindDescendant(root.transform, names[i]);
                if (panel != null && !panels.Contains(panel.gameObject))
                {
                    panels.Add(panel.gameObject);
                }
            }

            SerializedProperty statePanels = dissolveSo.FindProperty("statePanels");
            statePanels.arraySize = panels.Count;
            for (int i = 0; i < panels.Count; i++)
            {
                statePanels.GetArrayElementAtIndex(i).objectReferenceValue = panels[i];
            }
            dissolveSo.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(highlight);
            EditorUtility.SetDirty(dissolveController);
            PrefabUtility.SaveAsPrefabAsset(root, CanvasPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static T GetOrAdd<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }

    static void AddMarker(GameObject gameObject)
    {
        if (gameObject.GetComponent<CyberDissolveOptOut>() == null)
        {
            gameObject.AddComponent<CyberDissolveOptOut>();
        }
    }

    static Image FindImage(Transform root, string name)
    {
        Transform target = FindDescendant(root, name);
        return target != null ? target.GetComponent<Image>() : null;
    }

    static Transform FindDescendant(Transform root, string name)
    {
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name == name)
            {
                return all[i];
            }
        }

        return null;
    }
}
#endif
