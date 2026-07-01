using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy Data", menuName = "Enemy Data")]
public class EnemyData : ScriptableObject
{
    [SerializeField] int maxHealth = 10;

    public int MaxHealth
    {
        get
        {
            return maxHealth;
        }
    }

    public void OnDeath()
    {
        Debug.Log($"[EnemyData] 적 사망: {name}");

        // TODO: 이후 보상 지급, 사망 연출, 다음 스테이지 전환 등을 이 지점에서 확장합니다.
    }
}
