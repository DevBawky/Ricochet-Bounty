using UnityEngine;

[CreateAssetMenu(fileName = "New Stage Data", menuName = "Ricochet Bounty/Stage Data")]
public class StageData : ScriptableObject
{
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
}
