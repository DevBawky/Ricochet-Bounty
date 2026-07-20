using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class KillableDamageHighlightController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] DamageManager damageManager;
    [SerializeField] RoundManager roundManager;
    [SerializeField] StateManager stateManager;

    [Header("Existing Effect Images")]
    [SerializeField] Image chipsEffect;
    [SerializeField] Image multiplierEffect;

    [Header("Independent Material Templates")]
    [SerializeField] Material chipsMaterialTemplate;
    [SerializeField] Material multiplierMaterialTemplate;

    [Header("Alpha Transition")]
    [SerializeField, Min(0f)] float alphaFadeDuration = 0.5f;

    Material originalChipsMaterial;
    Material originalMultiplierMaterial;
    Material chipsMaterialInstance;
    Material multiplierMaterialInstance;
    int lastObservedChips = int.MinValue;
    int lastObservedEnemyHp = int.MinValue;
    int lastObservedMaximumEnemyHp = int.MinValue;
    float lastObservedMultiplier = float.NaN;
    GameState lastObservedGameState = (GameState)(-1);
    Coroutine alphaFadeCoroutine;
    float currentAlpha;
    bool targetVisible;
    bool damageAppliedForCurrentScore;

    void Awake()
    {
        FindMissingReferences();
        CreateMaterialInstances();
        HideImmediately();
    }

    void OnEnable()
    {
        FindMissingReferences();
        if (damageManager != null)
        {
            damageManager.OnDamageValueChanged.AddListener(RefreshHighlight);
            damageManager.OnScoreReset.AddListener(OnScoreReset);
        }

        if (roundManager != null)
        {
            roundManager.EnemyHealthChanged += OnEnemyHealthChanged;
        }

        if (stateManager != null)
        {
            stateManager.StateChanged += OnGameStateChanged;
        }

        RefreshHighlight();
    }

    void OnDisable()
    {
        if (damageManager != null)
        {
            damageManager.OnDamageValueChanged.RemoveListener(RefreshHighlight);
            damageManager.OnScoreReset.RemoveListener(OnScoreReset);
        }

        if (roundManager != null)
        {
            roundManager.EnemyHealthChanged -= OnEnemyHealthChanged;
        }

        if (stateManager != null)
        {
            stateManager.StateChanged -= OnGameStateChanged;
        }
    }

    void OnDestroy()
    {
        if (alphaFadeCoroutine != null)
        {
            StopCoroutine(alphaFadeCoroutine);
        }

        RestoreMaterial(chipsEffect, originalChipsMaterial, chipsMaterialInstance);
        RestoreMaterial(multiplierEffect, originalMultiplierMaterial, multiplierMaterialInstance);
    }

    void LateUpdate()
    {
        int chips = damageManager != null ? damageManager.CurrentChips : 0;
        float multiplier = damageManager != null ? damageManager.CurrentMultiplier : 0f;
        int enemyHp = roundManager != null ? roundManager.CurrentEnemyHp : 0;
        int maximumEnemyHp = roundManager != null ? roundManager.MaximumEnemyHp : 0;
        GameState gameState = stateManager != null ? stateManager.CurrentState : (GameState)(-1);

        if (chips == lastObservedChips &&
            Mathf.Approximately(multiplier, lastObservedMultiplier) &&
            enemyHp == lastObservedEnemyHp &&
            maximumEnemyHp == lastObservedMaximumEnemyHp &&
            gameState == lastObservedGameState)
        {
            return;
        }

        bool enemyHealthChanged = enemyHp != lastObservedEnemyHp || maximumEnemyHp != lastObservedMaximumEnemyHp;
        lastObservedChips = chips;
        lastObservedMultiplier = multiplier;
        lastObservedGameState = gameState;

        if (enemyHealthChanged)
        {
            EvaluateEnemyHealthChange(enemyHp, maximumEnemyHp);
        }
        else
        {
            RefreshHighlight();
        }
    }

    public void RefreshHighlight()
    {
        bool isBattle = stateManager != null && stateManager.CurrentState == GameState.Battle;
        int enemyHp = roundManager != null ? roundManager.CurrentEnemyHp : 0;
        int maximumEnemyHp = roundManager != null ? roundManager.MaximumEnemyHp : 0;
        int expectedDamage = damageManager != null
            ? Mathf.RoundToInt(damageManager.CurrentChips * damageManager.CurrentMultiplier)
            : 0;
        bool shouldShow = !damageAppliedForCurrentScore &&
            isBattle &&
            enemyHp > 0 &&
            maximumEnemyHp > 0 &&
            expectedDamage >= maximumEnemyHp;
        SetVisibleAnimated(shouldShow);
    }

    public void HideImmediately()
    {
        if (alphaFadeCoroutine != null)
        {
            StopCoroutine(alphaFadeCoroutine);
            alphaFadeCoroutine = null;
        }

        targetVisible = false;
        SetMaterialAlpha(0f);
        SetEffectObjectsActive(false);
    }

    public void BeginScoringSequence()
    {
        damageAppliedForCurrentScore = false;
        SetVisibleAnimated(false);
    }

    void OnGameStateChanged(GameState previousState, GameState nextState)
    {
        if (nextState != GameState.Battle || previousState != GameState.Battle)
        {
            SetVisibleAnimated(false);
            return;
        }

        RefreshHighlight();
    }

    void OnScoreReset()
    {
        damageAppliedForCurrentScore = false;
        SetVisibleAnimated(false);
    }

    void OnEnemyHealthChanged()
    {
        int enemyHp = roundManager != null ? roundManager.CurrentEnemyHp : 0;
        int maximumEnemyHp = roundManager != null ? roundManager.MaximumEnemyHp : 0;
        EvaluateEnemyHealthChange(enemyHp, maximumEnemyHp);
    }

    void EvaluateEnemyHealthChange(int enemyHp, int maximumEnemyHp)
    {
        bool sameEnemyHealthPool = lastObservedMaximumEnemyHp > 0 && maximumEnemyHp == lastObservedMaximumEnemyHp;
        bool tookDamage = sameEnemyHealthPool && lastObservedEnemyHp > 0 && enemyHp < lastObservedEnemyHp;
        bool resetToFullHealth = maximumEnemyHp > 0 && enemyHp >= maximumEnemyHp;

        lastObservedEnemyHp = enemyHp;
        lastObservedMaximumEnemyHp = maximumEnemyHp;

        if (tookDamage)
        {
            damageAppliedForCurrentScore = true;
            SetVisibleAnimated(false);
            return;
        }

        if (resetToFullHealth)
        {
            damageAppliedForCurrentScore = false;
        }

        RefreshHighlight();
    }

    void SetVisibleAnimated(bool visible)
    {
        if (targetVisible == visible)
        {
            return;
        }

        targetVisible = visible;
        if (visible)
        {
            SetEffectObjectsActive(true);
        }

        if (alphaFadeCoroutine != null)
        {
            StopCoroutine(alphaFadeCoroutine);
        }

        alphaFadeCoroutine = StartCoroutine(FadeAlphaRoutine(visible ? 1f : 0f));
    }

    IEnumerator FadeAlphaRoutine(float targetAlpha)
    {
        float startAlpha = currentAlpha;
        float duration = Mathf.Max(0f, alphaFadeDuration);
        if (duration <= 0f)
        {
            SetMaterialAlpha(targetAlpha);
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                SetMaterialAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
                yield return null;
            }

            SetMaterialAlpha(targetAlpha);
        }

        if (!targetVisible && Mathf.Approximately(targetAlpha, 0f))
        {
            SetEffectObjectsActive(false);
        }

        alphaFadeCoroutine = null;
    }

    void SetMaterialAlpha(float alpha)
    {
        currentAlpha = Mathf.Clamp01(alpha);
        if (chipsMaterialInstance != null)
        {
            chipsMaterialInstance.SetFloat("_Alpha", currentAlpha);
        }

        if (multiplierMaterialInstance != null)
        {
            multiplierMaterialInstance.SetFloat("_Alpha", currentAlpha);
        }
    }

    void SetEffectObjectsActive(bool active)
    {
        if (chipsEffect != null && chipsEffect.gameObject.activeSelf != active)
        {
            chipsEffect.gameObject.SetActive(active);
        }

        if (multiplierEffect != null && multiplierEffect.gameObject.activeSelf != active)
        {
            multiplierEffect.gameObject.SetActive(active);
        }
    }

    void CreateMaterialInstances()
    {
        if (chipsEffect != null)
        {
            originalChipsMaterial = chipsEffect.material;
            Material source = chipsMaterialTemplate != null ? chipsMaterialTemplate : originalChipsMaterial;
            if (source != null)
            {
                chipsMaterialInstance = new Material(source) { name = source.name + " (Chips Runtime)" };
                chipsEffect.material = chipsMaterialInstance;
            }
        }

        if (multiplierEffect != null)
        {
            originalMultiplierMaterial = multiplierEffect.material;
            Material source = multiplierMaterialTemplate != null ? multiplierMaterialTemplate : originalMultiplierMaterial;
            if (source != null)
            {
                multiplierMaterialInstance = new Material(source) { name = source.name + " (Multiplier Runtime)" };
                multiplierEffect.material = multiplierMaterialInstance;
            }
        }
    }

    void RestoreMaterial(Image image, Material original, Material runtime)
    {
        if (image != null && image.material == runtime)
        {
            image.material = original;
        }

        if (runtime != null)
        {
            Destroy(runtime);
        }
    }

    void FindMissingReferences()
    {
        damageManager ??= FindFirstObjectByType<DamageManager>(FindObjectsInactive.Include);
        roundManager ??= FindFirstObjectByType<RoundManager>(FindObjectsInactive.Include);
        stateManager ??= FindFirstObjectByType<StateManager>(FindObjectsInactive.Include);
    }
}
