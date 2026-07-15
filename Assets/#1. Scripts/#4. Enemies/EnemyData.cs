using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy Data", menuName = "Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Save Identity")]
    [SerializeField, Tooltip("Stable ID used by run save data. If empty, the asset name is used.")]
    string saveId;

    [Header("Identity")]
    [SerializeField] string enemyName;

    [Header("Visual")]
    [SerializeField] Sprite enemySprite;

    [Header("Round")]
    [SerializeField] bool isBoss;

    public Sprite EnemySprite
    {
        get
        {
            return enemySprite;
        }
    }

    public string SaveId
    {
        get
        {
            return string.IsNullOrWhiteSpace(saveId) ? name : saveId.Trim();
        }
    }

    public string EnemyName
    {
        get
        {
            return string.IsNullOrWhiteSpace(enemyName) ? name : enemyName;
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
