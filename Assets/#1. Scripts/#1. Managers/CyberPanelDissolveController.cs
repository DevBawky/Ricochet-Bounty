using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CyberPanelDissolveController : MonoBehaviour
{
    [Header("Material")]
    [SerializeField] Material dissolveMaterialTemplate;

    [Header("Existing Panels")]
    [SerializeField] GameObject playerPanel;
    [SerializeField] GameObject[] statePanels;

    [Header("Timing")]
    [SerializeField, Min(0.01f)] float dissolveOutDuration = 0.22f;
    [SerializeField, Min(0.01f)] float dissolveInDuration = 0.3f;
    [SerializeField] bool useUnscaledTime = true;

    readonly Dictionary<GameObject, PanelCache> caches = new Dictionary<GameObject, PanelCache>();
    Coroutine transitionCoroutine;
    bool initialized;

    public bool IsTransitioning => transitionCoroutine != null;

    sealed class GraphicCache
    {
        public Graphic Graphic;
        public TMP_Text TmpText;
        public Material OriginalMaterial;
        public Material RuntimeMaterial;
        public bool OriginalRaycastTarget;
    }

    sealed class PanelCache
    {
        public GameObject Panel;
        public readonly List<GraphicCache> Graphics = new List<GraphicCache>();
    }

    void Awake()
    {
        CacheConfiguredPanels();
    }

    void OnDestroy()
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        foreach (PanelCache panel in caches.Values)
        {
            for (int i = 0; i < panel.Graphics.Count; i++)
            {
                GraphicCache entry = panel.Graphics[i];
                if (entry.Graphic != null)
                {
                    if (entry.TmpText != null)
                    {
                        entry.TmpText.fontSharedMaterial = entry.OriginalMaterial;
                    }
                    else if (entry.Graphic.material == entry.RuntimeMaterial)
                    {
                        entry.Graphic.material = entry.OriginalMaterial;
                    }

                    entry.Graphic.raycastTarget = entry.OriginalRaycastTarget;
                }

                if (entry.RuntimeMaterial != null)
                {
                    Destroy(entry.RuntimeMaterial);
                }
            }
        }
    }

    public void TransitionTo(GameObject primaryPanel, bool showPlayerPanel)
    {
        CachePanel(primaryPanel);
        if (showPlayerPanel)
        {
            CachePanel(playerPanel);
        }

        if (!initialized)
        {
            ApplyImmediate(primaryPanel, showPlayerPanel);
            initialized = true;
            return;
        }

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        transitionCoroutine = StartCoroutine(TransitionRoutine(primaryPanel, showPlayerPanel));
    }

    public void ApplyImmediate(GameObject primaryPanel, bool showPlayerPanel)
    {
        foreach (PanelCache cache in caches.Values)
        {
            bool visible = cache.Panel == primaryPanel || (cache.Panel == playerPanel && showPlayerPanel);
            SetAmount(cache, visible ? 0f : 1f);
            cache.Panel.SetActive(visible);
            SetRaycasts(cache, visible);
        }

        initialized = true;
    }

    IEnumerator TransitionRoutine(GameObject primaryPanel, bool showPlayerPanel)
    {
        var outgoing = new List<PanelCache>();
        var incoming = new List<PanelCache>();

        foreach (PanelCache cache in caches.Values)
        {
            bool wanted = cache.Panel == primaryPanel || (cache.Panel == playerPanel && showPlayerPanel);
            if (cache.Panel.activeSelf && !wanted)
            {
                outgoing.Add(cache);
                SetRaycasts(cache, false);
            }
            else if (wanted && !cache.Panel.activeSelf)
            {
                cache.Panel.SetActive(true);
                SetAmount(cache, 1f);
                SetRaycasts(cache, false);
                incoming.Add(cache);
            }
            else if (wanted)
            {
                SetRaycasts(cache, false);
            }
        }

        yield return Animate(outgoing, 0f, 1f, dissolveOutDuration);
        for (int i = 0; i < outgoing.Count; i++)
        {
            outgoing[i].Panel.SetActive(false);
        }

        yield return Animate(incoming, 1f, 0f, dissolveInDuration);

        foreach (PanelCache cache in caches.Values)
        {
            bool wanted = cache.Panel == primaryPanel || (cache.Panel == playerPanel && showPlayerPanel);
            if (wanted)
            {
                SetAmount(cache, 0f);
                SetRaycasts(cache, true);
            }
        }

        transitionCoroutine = null;
    }

    IEnumerator Animate(List<PanelCache> panels, float from, float to, float duration)
    {
        if (panels.Count == 0)
        {
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            float amount = Mathf.Lerp(from, to, t);
            for (int i = 0; i < panels.Count; i++)
            {
                SetAmount(panels[i], amount);
            }

            yield return null;
        }

        for (int i = 0; i < panels.Count; i++)
        {
            SetAmount(panels[i], to);
        }
    }

    void CacheConfiguredPanels()
    {
        CachePanel(playerPanel);
        if (statePanels == null)
        {
            return;
        }

        for (int i = 0; i < statePanels.Length; i++)
        {
            CachePanel(statePanels[i]);
        }
    }

    void CachePanel(GameObject panel)
    {
        if (panel == null || caches.ContainsKey(panel) || dissolveMaterialTemplate == null)
        {
            return;
        }

        var cache = new PanelCache { Panel = panel };
        Graphic[] graphics = panel.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];
            if (graphic == null ||
                HasOptOutMarker(graphic.transform, panel.transform) ||
                UsesProtectedBattleBackgroundMaterial(graphic))
            {
                continue;
            }

            TMP_Text tmpText = graphic as TMP_Text;
            Material original = tmpText != null ? tmpText.fontSharedMaterial : graphic.material;
            Material runtime = new Material(dissolveMaterialTemplate)
            {
                name = dissolveMaterialTemplate.name + " (" + graphic.name + ")"
            };

            if (tmpText != null)
            {
                runtime.SetFloat("_UseSDF", 1f);
                if (original != null && original.mainTexture != null)
                {
                    runtime.mainTexture = original.mainTexture;
                }
                tmpText.fontSharedMaterial = runtime;
            }
            else
            {
                graphic.material = runtime;
            }

            cache.Graphics.Add(new GraphicCache
            {
                Graphic = graphic,
                TmpText = tmpText,
                OriginalMaterial = original,
                RuntimeMaterial = runtime,
                OriginalRaycastTarget = graphic.raycastTarget
            });
        }

        caches.Add(panel, cache);
    }

    static bool HasOptOutMarker(Transform current, Transform panelRoot)
    {
        while (current != null)
        {
            if (current.GetComponent<CyberDissolveOptOut>() != null)
            {
                return true;
            }

            if (current == panelRoot)
            {
                break;
            }

            current = current.parent;
        }

        return false;
    }

    static bool UsesProtectedBattleBackgroundMaterial(Graphic graphic)
    {
        if (graphic.name != "BG" || graphic.material == null)
        {
            return false;
        }

        string materialName = graphic.material.name;
        return materialName.StartsWith("M_Grid") || materialName.StartsWith("M_Enemy Grid");
    }

    static void SetAmount(PanelCache panel, float amount)
    {
        for (int i = 0; i < panel.Graphics.Count; i++)
        {
            Material material = panel.Graphics[i].RuntimeMaterial;
            if (material != null)
            {
                material.SetFloat("_DissolveAmount", amount);
            }
        }
    }

    static void SetRaycasts(PanelCache panel, bool enabled)
    {
        for (int i = 0; i < panel.Graphics.Count; i++)
        {
            GraphicCache entry = panel.Graphics[i];
            if (entry.Graphic != null)
            {
                entry.Graphic.raycastTarget = enabled && entry.OriginalRaycastTarget;
            }
        }
    }
}
