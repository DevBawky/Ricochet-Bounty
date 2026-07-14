using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy Data", menuName = "Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] string enemyName;

    [Header("Visual")]
    [SerializeField] Sprite enemySprite;

    [Header("Stats")]
    [SerializeField] int maxHealth = 10;

    [Header("Round")]
    [SerializeField] bool isBoss;

    public Sprite EnemySprite
    {
        get
        {
            return enemySprite;
        }
    }

    public string EnemyName
    {
        get
        {
            return string.IsNullOrWhiteSpace(enemyName) ? name : enemyName;
        }
    }

    public int MaxHealth
    {
        get
        {
            return maxHealth;
        }
    }

    public bool IsBoss
    {
        get
        {
            return isBoss;
        }
    }

    public void OnDeath()
    {
        Debug.Log($"[EnemyData] 사망 처리: {EnemyName}");

        // TODO: 이후 보상 지급, 사망 연출, 다음 진행 분기 등을 필요한 지점에서 확장합니다.
    }
}
