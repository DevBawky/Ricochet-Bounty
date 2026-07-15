using UnityEngine;

[CreateAssetMenu(fileName = "New Stage Data", menuName = "Ricochet Bounty/Stage Data")]
public class StageData : ScriptableObject
{
    const int MinimumEnemyHealth = 1;

    [Header("Enemy Health")]
    [SerializeField, Min(MinimumEnemyHealth)] int[] enemyHealthByWave = new int[6]
    {
        MinimumEnemyHealth,
        MinimumEnemyHealth,
        MinimumEnemyHealth,
        MinimumEnemyHealth,
        MinimumEnemyHealth,
        MinimumEnemyHealth
    };

    [Header("Normal Waves")]
    [SerializeField] EnemyData[] enemyCandidates;
    [SerializeField] GameObject[] battleGridCandidates;

    [Header("Boss Wave")]
    [SerializeField] EnemyData bossEnemy;
    [SerializeField] GameObject bossBattleGrid;

    public EnemyData[] EnemyCandidates
    {
        get
        {
            return enemyCandidates;
        }
    }

    public EnemyData BossEnemy
    {
        get
        {
            return bossEnemy;
        }
    }

    public GameObject[] BattleGridCandidates
    {
        get
        {
            return battleGridCandidates;
        }
    }

    public GameObject BossBattleGrid
    {
        get
        {
            return bossBattleGrid;
        }
    }

    public bool TryGetEnemyHealth(int waveIndex, out int enemyHealth)
    {
        enemyHealth = MinimumEnemyHealth;

        if (enemyHealthByWave == null || enemyHealthByWave.Length <= 0)
        {
            Debug.LogWarning($"[StageData] {name}의 Enemy Health By Wave 배열이 비어 있습니다. Wave {waveIndex + 1}에는 최소 체력 {MinimumEnemyHealth}을 사용합니다.", this);
            return false;
        }

        if (waveIndex < 0 || waveIndex >= enemyHealthByWave.Length)
        {
            Debug.LogWarning($"[StageData] {name}의 Wave {waveIndex + 1} 체력이 없습니다. 배열 길이: {enemyHealthByWave.Length}. 최소 체력 {MinimumEnemyHealth}을 사용합니다.", this);
            return false;
        }

        if (enemyHealthByWave[waveIndex] <= 0)
        {
            Debug.LogWarning($"[StageData] {name}의 Wave {waveIndex + 1} 체력이 {enemyHealthByWave[waveIndex]}입니다. 최소 체력 {MinimumEnemyHealth}을 사용합니다.", this);
            return false;
        }

        enemyHealth = enemyHealthByWave[waveIndex];
        return true;
    }

    void OnValidate()
    {
        if (enemyHealthByWave == null)
        {
            return;
        }

        for (int i = 0; i < enemyHealthByWave.Length; i++)
        {
            enemyHealthByWave[i] = Mathf.Max(MinimumEnemyHealth, enemyHealthByWave[i]);
        }
    }
}
