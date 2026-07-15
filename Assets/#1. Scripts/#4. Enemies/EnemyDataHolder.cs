using UnityEngine;
using UnityEngine.UI;

public class EnemyDataHolder : MonoBehaviour
{
    [Header("Enemy Data")]
    [SerializeField] EnemyData enemyData;
    [SerializeField] int currentHealth;

    [Header("UI")]
    [SerializeField] Image healthBarImage;

    bool isDead;
    int maximumHealth;

    public bool IsDead
    {
        get
        {
            return isDead || currentHealth <= 0;
        }
    }

    public int CurrentHealth
    {
        get
        {
            return currentHealth;
        }
    }

    public void Initialize(EnemyData selectedEnemyData, int enemyHealth)
    {
        enemyData = selectedEnemyData;
        maximumHealth = Mathf.Max(1, enemyHealth);

        if (enemyData == null)
        {
            Debug.LogWarning("[EnemyDataHolder] EnemyData가 연결되어 있지 않아 체력을 초기화할 수 없습니다.", this);
            currentHealth = 0;
            isDead = true;
            UpdateHealthBar();
            return;
        }

        currentHealth = maximumHealth;
        isDead = currentHealth <= 0;
        UpdateHealthBar();

        Debug.Log($"[EnemyDataHolder] 적 체력 초기화 완료. maxHealth: {maximumHealth}, currentHealth: {currentHealth}, IsDead: {IsDead}", this);
    }

    public void TakeDamage(int damage)
    {
        if (IsDead)
        {
            Debug.Log("[EnemyDataHolder] 이미 사망한 적이므로 대미지 적용을 무시합니다.", this);
            return;
        }

        if (enemyData == null)
        {
            Debug.LogWarning("[EnemyDataHolder] EnemyData가 없어 대미지를 적용할 수 없습니다.", this);
            return;
        }

        int safeDamage = Mathf.Max(0, damage);
        currentHealth = Mathf.Max(0, currentHealth - safeDamage);
        UpdateHealthBar();

        Debug.Log($"[EnemyDataHolder] 대미지 적용: -{safeDamage}, currentHealth: {currentHealth}, fillAmount: {GetHealthFillAmount()}", this);

        if (currentHealth <= 0)
        {
            Die();
            Debug.Log($"[EnemyDataHolder] TakeDamage 후 사망 확인. IsDead: {IsDead}", this);
            return;
        }

        Debug.Log($"[EnemyDataHolder] TakeDamage 후 생존 확인. IsDead: {IsDead}", this);
    }

    void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        Debug.Log("[EnemyDataHolder] 적 체력이 0 이하입니다. EnemyData.OnDeath를 호출합니다.", this);
        enemyData.OnDeath();
    }

    void UpdateHealthBar()
    {
        if (healthBarImage == null)
        {
            Debug.LogWarning("[EnemyDataHolder] healthBarImage가 연결되어 있지 않아 체력바를 갱신할 수 없습니다.", this);
            return;
        }

        healthBarImage.fillAmount = GetHealthFillAmount();
    }

    float GetHealthFillAmount()
    {
        if (enemyData == null || maximumHealth <= 0)
        {
            return 0f;
        }

        return Mathf.Clamp01((float)currentHealth / maximumHealth);
    }
}
