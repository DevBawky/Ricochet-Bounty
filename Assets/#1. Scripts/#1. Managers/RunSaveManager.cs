using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class RunSaveManager : MonoBehaviour
{
    const string SaveFileName = "current-run.json";
    const string BackupFileName = "current-run.backup.json";

    public static RunSaveManager Instance { get; private set; }

    [Header("Managers")]
    [SerializeField] StateManager stateManager;
    [SerializeField] RoundManager roundManager;
    [SerializeField] PlayerBallDeck playerBallDeck;
    [SerializeField] GoldManager goldManager;
    [SerializeField] PlayerUpgradeManager playerUpgradeManager;
    [SerializeField] DamageManager damageManager;
    [SerializeField] ShotRuntimeContext shotRuntimeContext;
    [SerializeField] RoundSelectManager roundSelectManager;
    [SerializeField] ShopManager shopManager;
    [SerializeField] EventManager eventManager;
    [SerializeField] TreasureManager treasureManager;
    [SerializeField] ResultPayOutUI resultPayOutUI;

    [Header("Main Menu - Load Game")]
    [SerializeField] GameObject loadGamePanel;
    [SerializeField] Button yesButton;
    [SerializeField] Button noButton;

    [Header("ID Catalog")]
    [SerializeField, Tooltip("Register every BallDataSO that can be owned during a run.")]
    List<BallDataSO> ballCatalog = new List<BallDataSO>();

    bool hasActiveRun;
    bool suppressAutoSave;
    bool saveRequested;
    string pendingSaveReason;

    public string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);
    string BackupFilePath => Path.Combine(Application.persistentDataPath, BackupFileName);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("[RunSaveManager] More than one RunSaveManager exists. Disable the duplicate component.", this);
            enabled = false;
            return;
        }

        Instance = this;
        FindMissingReferences();
        BindButtons();
        SetLoadPanelActive(false);
        ValidateBallCatalog();
    }

    void OnEnable()
    {
        FindMissingReferences();
        SubscribeToRunChanges();
    }

    void OnDisable()
    {
        UnsubscribeFromRunChanges();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void LateUpdate()
    {
        if (!saveRequested)
        {
            return;
        }

        string reason = pendingSaveReason;
        saveRequested = false;
        pendingSaveReason = string.Empty;
        SaveCurrentRun(reason);
    }

    public void OnClickPlayGame()
    {
        FindMissingReferences();
        if (stateManager == null || stateManager.CurrentState != GameState.MainMenu)
        {
            Debug.LogWarning("[RunSaveManager] Play Game can only be handled from MainMenu.", this);
            return;
        }

        if (HasAnySaveFile())
        {
            if (loadGamePanel == null)
            {
                Debug.LogError("[RunSaveManager] A save exists, but the Load Game panel is not connected. MainMenu will remain active.", this);
                return;
            }

            SetLoadPanelActive(true);
            return;
        }

        StartNewRunAndSave();
    }

    public void OnClickYes()
    {
        if (!TryLoadCurrentRun())
        {
            SetLoadPanelActive(true);
        }
    }

    public void OnClickNo()
    {
        DeleteSaveFiles();
        StartNewRunAndSave();
    }

    public void StartNewRunAndSave()
    {
        FindMissingReferences();
        if (!HasRequiredManagers(out string error))
        {
            Debug.LogError($"[RunSaveManager] New run could not start. {error}", this);
            stateManager?.ReturnToMainMenuAfterLoadFailure();
            return;
        }

        suppressAutoSave = true;
        saveRequested = false;
        pendingSaveReason = string.Empty;
        try
        {
            stateManager.ReturnToMainMenuAfterLoadFailure();
            playerUpgradeManager.ResetRunData();
            roundManager.StartNewRun();
            playerBallDeck.ResetDeck();
            goldManager.ResetGold();
            damageManager.ResetScore();
            shotRuntimeContext.ResetShot();
            shopManager?.ResetRuntimeState();
            eventManager?.ResetRuntimeState();
            treasureManager?.ResetRuntimeState();
            resultPayOutUI?.ResetRuntimeState();
            stateManager.ChangeState(GameState.RoundSelect);
            hasActiveRun = true;
            SetLoadPanelActive(false);
        }
        catch (Exception exception)
        {
            hasActiveRun = false;
            Debug.LogError($"[RunSaveManager] New run initialization failed. MainMenu will remain active.\n{exception}", this);
            stateManager.ReturnToMainMenuAfterLoadFailure();
            return;
        }
        finally
        {
            suppressAutoSave = false;
        }

        SaveCurrentRun("New run started");
    }

    public void RequestAutoSave(string reason)
    {
        if (suppressAutoSave || !hasActiveRun)
        {
            return;
        }

        saveRequested = true;
        pendingSaveReason = string.IsNullOrWhiteSpace(reason) ? "Run data changed" : reason;
    }

    public bool SaveCurrentRun(string reason)
    {
        if (suppressAutoSave || !hasActiveRun)
        {
            return false;
        }

        FindMissingReferences();
        if (!HasRequiredManagers(out string error))
        {
            Debug.LogError($"[RunSaveManager] Save failed. {error}", this);
            return false;
        }

        if (stateManager.CurrentState == GameState.MainMenu)
        {
            return false;
        }

        if (!IsSafeBattleCheckpoint())
        {
            Debug.Log($"[RunSaveManager] Save skipped during {stateManager.CurrentBattleState}. The pre-fire checkpoint remains valid. Reason: {reason}", this);
            return false;
        }

        try
        {
            RunSaveData data = CaptureRunData();
            string json = JsonUtility.ToJson(data, true);
            WriteSaveAtomically(json);
            Debug.Log($"[RunSaveManager] Run saved. Reason: {reason}. Path: {SaveFilePath}", this);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[RunSaveManager] Save failed. Existing checkpoint was preserved when possible.\n{exception}", this);
            return false;
        }
    }

    public bool TryLoadCurrentRun()
    {
        FindMissingReferences();
        if (!HasRequiredManagers(out string managerError))
        {
            Debug.LogError($"[RunSaveManager] Load failed. {managerError}", this);
            stateManager?.ReturnToMainMenuAfterLoadFailure();
            return false;
        }

        if (!TryReadSaveData(out RunSaveData data, out string readError))
        {
            Debug.LogError($"[RunSaveManager] Load failed. {readError} MainMenu will remain active.", this);
            stateManager.ReturnToMainMenuAfterLoadFailure();
            return false;
        }

        if (!TryResolveSaveData(data, out ResolvedRunData resolved, out string validationError))
        {
            Debug.LogError($"[RunSaveManager] Load validation failed. {validationError} MainMenu will remain active.", this);
            stateManager.ReturnToMainMenuAfterLoadFailure();
            return false;
        }

        suppressAutoSave = true;
        saveRequested = false;
        pendingSaveReason = string.Empty;
        try
        {
            stateManager.ReturnToMainMenuAfterLoadFailure();
            playerUpgradeManager.RestoreSaveData(data.upgrades);
            if (!playerBallDeck.RestoreDeck(resolved.ownedBalls, resolved.drawPile, resolved.discardPile, resolved.currentCylinder))
            {
                throw new InvalidOperationException("PlayerBallDeck rejected the validated save data.");
            }

            goldManager.RestoreGold(data.gold);
            damageManager.RestoreScore(data.damage.chips, data.damage.multiplier);
            shotRuntimeContext.RestoreSaveData(data.shot);
            roundManager.RestoreRunState(
                data.stageIndex,
                data.waveIndex,
                data.waveType,
                data.currentPlayerLife,
                data.lastBattleRemainingLife,
                resolved.enemy,
                resolved.battleGrid,
                data.maximumEnemyHp,
                data.currentEnemyHp,
                data.nonBattleWaveCompletionLocked);

            if (data.gameState == GameState.Battle)
            {
                roundManager.SpawnSelectedBattleGrid(false);
                if (!roundManager.RestoreBattleGridObjects(data.battleGridObjects))
                {
                    throw new InvalidOperationException("Battle Grid objects could not be restored.");
                }
            }

            stateManager.RestoreLoadedState(data.gameState, data.battleState);
            roundSelectManager?.RestoreSaveData(data.roundSelect);
            shopManager?.RestoreSaveData(data.shop, resolved.shopSlotBalls);
            eventManager?.RestoreSaveData(data.eventState);
            treasureManager?.RestoreSaveData(data.treasure, resolved.treasureBall);
            resultPayOutUI?.RestoreSaveData(data.result);
            hasActiveRun = true;
            SetLoadPanelActive(false);
            Debug.Log($"[RunSaveManager] Run loaded successfully. State: {data.gameState}, Stage: {data.stageIndex + 1}, Wave: {data.waveIndex + 1}", this);
        }
        catch (Exception exception)
        {
            hasActiveRun = false;
            Debug.LogError($"[RunSaveManager] Load application failed. MainMenu will remain active.\n{exception}", this);
            stateManager.ReturnToMainMenuAfterLoadFailure();
            return false;
        }
        finally
        {
            suppressAutoSave = false;
        }

        SaveCurrentRun("Loaded checkpoint normalized");
        return true;
    }

    public void DeleteSaveFiles()
    {
        DeleteFileIfExists(SaveFilePath);
        DeleteFileIfExists(BackupFilePath);
        hasActiveRun = false;
        Debug.Log($"[RunSaveManager] Run save deleted. Path: {SaveFilePath}", this);
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveCurrentRun("Application paused");
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            SaveCurrentRun("Application focus lost");
        }
    }

    void OnApplicationQuit()
    {
        SaveCurrentRun("Application quit");
    }

    RunSaveData CaptureRunData()
    {
        RunSaveData data = new RunSaveData
        {
            savedAtUtc = DateTime.UtcNow.ToString("O"),
            gameState = stateManager.CurrentState,
            battleState = stateManager.CurrentBattleState,
            waveType = roundManager.CurrentWaveType,
            stageIndex = roundManager.Progress.CurrentStageIndex,
            waveIndex = roundManager.Progress.CurrentWaveIndex,
            currentPlayerLife = roundManager.CurrentPlayerLife,
            lastBattleRemainingLife = roundManager.LastBattleRemainingLife,
            gold = goldManager.CurrentGold,
            enemyId = roundManager.SelectedEnemyData != null ? roundManager.SelectedEnemyData.SaveId : string.Empty,
            maximumEnemyHp = roundManager.MaximumEnemyHp,
            currentEnemyHp = roundManager.CurrentEnemyHp,
            battleGridId = roundManager.SelectedBattleGridPrefab != null ? roundManager.SelectedBattleGridPrefab.name : string.Empty,
            nonBattleWaveCompletionLocked = roundManager.NonBattleWaveCompletionLocked,
            upgrades = playerUpgradeManager.CaptureSaveData(),
            damage = new DamageSaveData
            {
                chips = damageManager.CurrentChips,
                multiplier = damageManager.CurrentMultiplier
            },
            shot = shotRuntimeContext.CaptureSaveData(),
            roundSelect = roundSelectManager != null ? roundSelectManager.CaptureSaveData() : new RoundSelectSaveData(),
            shop = shopManager != null ? shopManager.CaptureSaveData() : new ShopSaveData(),
            eventState = eventManager != null ? eventManager.CaptureSaveData() : new EventSaveData(),
            treasure = treasureManager != null ? treasureManager.CaptureSaveData() : new TreasureSaveData(),
            result = resultPayOutUI != null ? resultPayOutUI.CaptureSaveData() : new ResultSaveData()
        };

        data.battleGridObjects = stateManager.CurrentState == GameState.Battle
            ? roundManager.CaptureBattleGridObjects()
            : new List<BattleGridObjectSaveData>();
        data.deck.ownedBalls = GetBallIds(playerBallDeck.OwnedBalls);
        data.deck.drawPile = GetBallIds(playerBallDeck.DrawPile);
        data.deck.discardPile = GetBallIds(playerBallDeck.DiscardPile);
        data.deck.currentCylinder = GetBallIds(playerBallDeck.CurrentCylinder);
        return data;
    }

    bool TryResolveSaveData(RunSaveData data, out ResolvedRunData resolved, out string error)
    {
        resolved = new ResolvedRunData();
        error = string.Empty;

        if (data == null || data.fileType != RunSaveData.ExpectedFileType || data.version != RunSaveData.CurrentVersion)
        {
            error = "The JSON file type or version is not supported.";
            return false;
        }

        if (data.deck == null || data.upgrades == null || data.damage == null || data.shot == null ||
            data.roundSelect == null || data.shop == null || data.eventState == null || data.treasure == null ||
            data.result == null || data.battleGridObjects == null)
        {
            error = "The save is missing one or more required data sections.";
            return false;
        }

        if (!Enum.IsDefined(typeof(GameState), data.gameState) || data.gameState == GameState.MainMenu ||
            !Enum.IsDefined(typeof(BattleState), data.battleState) || !Enum.IsDefined(typeof(WaveType), data.waveType))
        {
            error = "The save contains an invalid game, battle, or wave state.";
            return false;
        }

        if (data.stageIndex < 0 || data.stageIndex >= roundManager.Progress.MaxStageCount ||
            data.waveIndex < 0 || data.waveIndex >= roundManager.Progress.WaveCountPerStage ||
            data.gold < 0 || data.currentPlayerLife < 0 || data.currentPlayerLife > roundManager.MaxPlayerLife ||
            data.lastBattleRemainingLife < 0 || data.lastBattleRemainingLife > roundManager.MaxPlayerLife ||
            data.maximumEnemyHp < 0 || data.currentEnemyHp < 0 || data.damage.chips < 0 ||
            data.damage.multiplier < 0f || float.IsNaN(data.damage.multiplier) || float.IsInfinity(data.damage.multiplier))
        {
            error = "The save contains an out-of-range progress, life, gold, or enemy value.";
            return false;
        }

        if (data.upgrades.ballHpLevel < 0 || data.upgrades.ballHpLevel > PlayerUpgradeManager.MaxUpgradeLevel ||
            data.upgrades.ballDefenseLevel < 0 || data.upgrades.ballDefenseLevel > PlayerUpgradeManager.MaxUpgradeLevel ||
            data.upgrades.scoreBoostLevel < 0 || data.upgrades.scoreBoostLevel > PlayerUpgradeManager.MaxUpgradeLevel ||
            data.upgrades.currentDeleteCost < 0 || data.shop.currentRefreshCost < 0 || data.shop.nextRefreshCost < 0)
        {
            error = "The save contains an invalid upgrade or shop cost value.";
            return false;
        }

        if (!TryResolveBallList(data.deck.ownedBalls, resolved.ownedBalls, out error) ||
            !TryResolveBallList(data.deck.drawPile, resolved.drawPile, out error) ||
            !TryResolveBallList(data.deck.discardPile, resolved.discardPile, out error) ||
            !TryResolveBallList(data.deck.currentCylinder, resolved.currentCylinder, out error))
        {
            return false;
        }

        if (resolved.ownedBalls.Count < PlayerBallDeck.MinimumOwnedBallCount ||
            resolved.ownedBalls.Count > PlayerBallDeck.MaxOwnedBallCount)
        {
            error = "The saved owned ball count is outside the allowed range.";
            return false;
        }

        if (!HasMatchingDeckContents(data.deck))
        {
            error = "Owned balls do not match drawPile + discardPile + currentCylinder.";
            return false;
        }

        if (data.gameState == GameState.Battle)
        {
            if (data.waveType != WaveType.Battle && data.waveType != WaveType.Boss)
            {
                error = "Battle GameState has an incompatible WaveType.";
                return false;
            }

            resolved.enemy = roundManager.FindEnemyById(data.enemyId);
            resolved.battleGrid = roundManager.FindBattleGridById(data.battleGridId);
            if (resolved.enemy == null || resolved.battleGrid == null)
            {
                error = $"Enemy ID '{data.enemyId}' or Battle Grid ID '{data.battleGridId}' was not found.";
                return false;
            }

            if (data.maximumEnemyHp <= 0 || data.currentEnemyHp <= 0 || data.currentEnemyHp > data.maximumEnemyHp)
            {
                error = "Battle enemy HP is invalid.";
                return false;
            }

            if (!roundManager.CanRestoreBattleGridObjects(resolved.battleGrid, data.battleGridObjects, out error))
            {
                return false;
            }
        }

        else if ((data.gameState == GameState.Event && data.waveType != WaveType.Event) ||
            (data.gameState == GameState.Treasure && data.waveType != WaveType.Treasure))
        {
            error = $"{data.gameState} GameState has an incompatible WaveType.";
            return false;
        }

        if (data.shop != null && data.shop.slots != null)
        {
            for (int i = 0; i < data.shop.slots.Count; i++)
            {
                string ballId = data.shop.slots[i] != null ? data.shop.slots[i].ballId : string.Empty;
                BallDataSO ball = string.IsNullOrWhiteSpace(ballId) ? null : FindBallById(ballId);
                if (!string.IsNullOrWhiteSpace(ballId) && ball == null)
                {
                    error = $"Shop Ball ID '{ballId}' was not found.";
                    return false;
                }
                resolved.shopSlotBalls.Add(ball);
            }
        }

        string treasureBallId = data.treasure != null ? data.treasure.resultBallId : string.Empty;
        if (!string.IsNullOrWhiteSpace(treasureBallId))
        {
            resolved.treasureBall = FindBallById(treasureBallId);
            if (resolved.treasureBall == null)
            {
                error = $"Treasure Ball ID '{treasureBallId}' was not found.";
                return false;
            }
        }

        else if (data.gameState == GameState.Treasure && data.treasure.treasureOpened)
        {
            error = "The opened Treasure has no result Ball ID.";
            return false;
        }

        if (data.gameState == GameState.Event && data.eventState != null && data.eventState.eventId >= 0 &&
            (eventManager == null || !eventManager.HasEventId(data.eventState.eventId)))
        {
            error = $"Event ID '{data.eventState.eventId}' was not found.";
            return false;
        }

        return true;
    }

    bool TryResolveBallList(IList<string> ids, List<BallDataSO> target, out string error)
    {
        error = string.Empty;
        if (ids == null)
        {
            error = "A saved deck pile is null.";
            return false;
        }

        for (int i = 0; i < ids.Count; i++)
        {
            BallDataSO ball = FindBallById(ids[i]);
            if (ball == null)
            {
                error = $"Ball ID '{ids[i]}' was not found.";
                return false;
            }
            target.Add(ball);
        }

        return true;
    }

    BallDataSO FindBallById(string ballId)
    {
        if (string.IsNullOrWhiteSpace(ballId))
        {
            return null;
        }

        for (int i = 0; i < ballCatalog.Count; i++)
        {
            if (ballCatalog[i] != null && ballCatalog[i].SaveId == ballId)
            {
                return ballCatalog[i];
            }
        }

        if (playerBallDeck != null && playerBallDeck.StartingDeck != null)
        {
            for (int i = 0; i < playerBallDeck.StartingDeck.Count; i++)
            {
                BallDataSO ball = playerBallDeck.StartingDeck[i];
                if (ball != null && ball.SaveId == ballId)
                {
                    return ball;
                }
            }
        }

        return null;
    }

    static List<string> GetBallIds(IReadOnlyList<BallDataSO> balls)
    {
        List<string> result = new List<string>();
        if (balls == null)
        {
            return result;
        }

        for (int i = 0; i < balls.Count; i++)
        {
            if (balls[i] == null)
            {
                throw new InvalidOperationException($"Deck contains a null BallDataSO at index {i}.");
            }
            result.Add(balls[i].SaveId);
        }
        return result;
    }

    static bool HasMatchingDeckContents(BallDeckSaveData deck)
    {
        Dictionary<string, int> counts = new Dictionary<string, int>();
        AddCounts(counts, deck.ownedBalls, 1);
        AddCounts(counts, deck.drawPile, -1);
        AddCounts(counts, deck.discardPile, -1);
        AddCounts(counts, deck.currentCylinder, -1);
        foreach (KeyValuePair<string, int> entry in counts)
        {
            if (entry.Value != 0)
            {
                return false;
            }
        }
        return true;
    }

    static void AddCounts(Dictionary<string, int> counts, IList<string> ids, int amount)
    {
        if (ids == null)
        {
            return;
        }

        for (int i = 0; i < ids.Count; i++)
        {
            counts.TryGetValue(ids[i], out int value);
            counts[ids[i]] = value + amount;
        }
    }

    bool TryReadSaveData(out RunSaveData data, out string error)
    {
        if (TryReadSaveFile(SaveFilePath, out data, out error))
        {
            return true;
        }

        string primaryError = error;
        if (TryReadSaveFile(BackupFilePath, out data, out error))
        {
            Debug.LogWarning($"[RunSaveManager] Primary save could not be read. Backup was used. Primary error: {primaryError}", this);
            return true;
        }

        error = $"Primary: {primaryError} Backup: {error}";
        return false;
    }

    static bool TryReadSaveFile(string path, out RunSaveData data, out string error)
    {
        data = null;
        error = string.Empty;
        if (!File.Exists(path))
        {
            error = $"File does not exist: {path}.";
            return false;
        }

        try
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json))
            {
                error = $"File is empty: {path}.";
                return false;
            }
            data = JsonUtility.FromJson<RunSaveData>(json);
            if (data == null)
            {
                error = $"JSON root is invalid: {path}.";
                return false;
            }
            return true;
        }
        catch (Exception exception)
        {
            error = $"{path}: {exception.Message}";
            return false;
        }
    }

    void WriteSaveAtomically(string json)
    {
        string directory = Application.persistentDataPath;
        Directory.CreateDirectory(directory);
        string temporaryPath = SaveFilePath + ".tmp";
        File.WriteAllText(temporaryPath, json, new UTF8Encoding(false));

        if (File.Exists(SaveFilePath))
        {
            try
            {
                File.Replace(temporaryPath, SaveFilePath, BackupFilePath);
                return;
            }
            catch (PlatformNotSupportedException)
            {
                // Fall through to the portable copy path.
            }
            catch (IOException)
            {
                // Fall through when the platform cannot atomically replace this file.
            }

            if (TryReadSaveFile(SaveFilePath, out _, out _))
            {
                File.Copy(SaveFilePath, BackupFilePath, true);
            }
        }

        File.Copy(temporaryPath, SaveFilePath, true);
        File.Delete(temporaryPath);
    }

    bool IsSafeBattleCheckpoint()
    {
        if (stateManager.CurrentState != GameState.Battle)
        {
            return true;
        }

        BattleState battleState = stateManager.CurrentBattleState;
        return battleState == BattleState.WaitingForPlayerInput || battleState == BattleState.BattleStart ||
            battleState == BattleState.TurnStart;
    }

    bool HasRequiredManagers(out string error)
    {
        error = string.Empty;
        if (stateManager == null || roundManager == null || playerBallDeck == null || goldManager == null ||
            playerUpgradeManager == null || damageManager == null || shotRuntimeContext == null)
        {
            error = "StateManager, RoundManager, PlayerBallDeck, GoldManager, PlayerUpgradeManager, DamageManager, and ShotRuntimeContext must be connected.";
            return false;
        }
        return true;
    }

    bool HasAnySaveFile()
    {
        return File.Exists(SaveFilePath) || File.Exists(BackupFilePath);
    }

    void SetLoadPanelActive(bool active)
    {
        if (loadGamePanel != null)
        {
            loadGamePanel.SetActive(active);
        }
    }

    void BindButtons()
    {
        if (yesButton != null)
        {
            yesButton.onClick.RemoveListener(OnClickYes);
            yesButton.onClick.AddListener(OnClickYes);
        }

        if (noButton != null)
        {
            noButton.onClick.RemoveListener(OnClickNo);
            noButton.onClick.AddListener(OnClickNo);
        }
    }

    void SubscribeToRunChanges()
    {
        if (goldManager != null)
        {
            goldManager.OnGoldChanged.RemoveListener(OnGoldChanged);
            goldManager.OnGoldChanged.AddListener(OnGoldChanged);
        }
        if (playerBallDeck != null)
        {
            playerBallDeck.OnDeckChanged.RemoveListener(OnDeckChanged);
            playerBallDeck.OnDeckChanged.AddListener(OnDeckChanged);
        }
        if (playerUpgradeManager != null)
        {
            playerUpgradeManager.OnUpgradesChanged.RemoveListener(OnUpgradesChanged);
            playerUpgradeManager.OnUpgradesChanged.AddListener(OnUpgradesChanged);
        }
    }

    void UnsubscribeFromRunChanges()
    {
        goldManager?.OnGoldChanged.RemoveListener(OnGoldChanged);
        playerBallDeck?.OnDeckChanged.RemoveListener(OnDeckChanged);
        playerUpgradeManager?.OnUpgradesChanged.RemoveListener(OnUpgradesChanged);
    }

    void OnGoldChanged() => RequestAutoSave("Gold changed");
    void OnDeckChanged() => RequestAutoSave("Deck changed");
    void OnUpgradesChanged() => RequestAutoSave("Player upgrade changed");

    void FindMissingReferences()
    {
        if (stateManager == null) stateManager = FindFirstObjectByType<StateManager>(FindObjectsInactive.Include);
        if (roundManager == null) roundManager = FindFirstObjectByType<RoundManager>(FindObjectsInactive.Include);
        if (playerBallDeck == null) playerBallDeck = FindFirstObjectByType<PlayerBallDeck>(FindObjectsInactive.Include);
        if (goldManager == null) goldManager = FindFirstObjectByType<GoldManager>(FindObjectsInactive.Include);
        if (playerUpgradeManager == null) playerUpgradeManager = FindFirstObjectByType<PlayerUpgradeManager>(FindObjectsInactive.Include);
        if (damageManager == null) damageManager = FindFirstObjectByType<DamageManager>(FindObjectsInactive.Include);
        if (shotRuntimeContext == null) shotRuntimeContext = FindFirstObjectByType<ShotRuntimeContext>(FindObjectsInactive.Include);
        if (roundSelectManager == null) roundSelectManager = FindFirstObjectByType<RoundSelectManager>(FindObjectsInactive.Include);
        if (shopManager == null) shopManager = FindFirstObjectByType<ShopManager>(FindObjectsInactive.Include);
        if (eventManager == null) eventManager = FindFirstObjectByType<EventManager>(FindObjectsInactive.Include);
        if (treasureManager == null) treasureManager = FindFirstObjectByType<TreasureManager>(FindObjectsInactive.Include);
        if (resultPayOutUI == null) resultPayOutUI = FindFirstObjectByType<ResultPayOutUI>(FindObjectsInactive.Include);
    }

    void ValidateBallCatalog()
    {
        HashSet<string> ids = new HashSet<string>();
        for (int i = 0; i < ballCatalog.Count; i++)
        {
            BallDataSO ball = ballCatalog[i];
            if (ball == null)
            {
                Debug.LogWarning($"[RunSaveManager] Ball Catalog entry {i} is empty.", this);
                continue;
            }
            if (!ids.Add(ball.SaveId))
            {
                Debug.LogError($"[RunSaveManager] Duplicate Ball Save ID: '{ball.SaveId}'.", ball);
            }
        }
    }

    static void DeleteFileIfExists(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception exception)
        {
            Debug.LogError($"[RunSaveManager] Could not delete save file '{path}'. {exception.Message}");
        }
    }

    class ResolvedRunData
    {
        public readonly List<BallDataSO> ownedBalls = new List<BallDataSO>();
        public readonly List<BallDataSO> drawPile = new List<BallDataSO>();
        public readonly List<BallDataSO> discardPile = new List<BallDataSO>();
        public readonly List<BallDataSO> currentCylinder = new List<BallDataSO>();
        public readonly List<BallDataSO> shopSlotBalls = new List<BallDataSO>();
        public EnemyData enemy;
        public GameObject battleGrid;
        public BallDataSO treasureBall;
    }
}
