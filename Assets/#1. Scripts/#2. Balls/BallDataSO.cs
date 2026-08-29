using System.Collections.Generic;
using UnityEngine;

// BallDataSO는 공의 "기본 설정값"을 담는 ScriptableObject입니다.
// 여러 공 오브젝트가 같은 BallDataSO를 함께 참조할 수 있으므로,
// 현재 내구도, 쿨다운, 발동 횟수처럼 플레이 중에 변하는 값은 여기에 저장하지 않습니다.
// 그런 런타임 상태는 BallRuntimeStatus와 BallEffectRuntimeState가 각 공 오브젝트마다 따로 관리합니다.
[CreateAssetMenu(fileName = "새 공 데이터", menuName = "리코셰 바운티/공 데이터")]
public class BallDataSO : ScriptableObject
{
    [Header("저장 식별자")]
    [SerializeField, Tooltip("런 저장 데이터에서 사용하는 고정 ID입니다. 비워 두면 에셋 이름을 사용합니다.")]
    string saveId;

    [Header("상점")]
    [SerializeField, Tooltip("상점 툴팁에 표시할 이름입니다. 비워 두면 에셋 이름을 사용합니다.")]
    string displayName;

    [SerializeField, TextArea(2, 5), Tooltip("상점 툴팁에 표시할 설명입니다. 비워 두면 공 능력치를 바탕으로 요약을 생성합니다.")]
    string description;

    [SerializeField, Tooltip("상점 구매 가격입니다.")]
    int price = 10;

    [SerializeField, Tooltip("상점의 희귀도 가중치에 사용하는 등급입니다.")]
    BallRarity rarity = BallRarity.Common;

    [Header("피해 수치")]
    [Tooltip("이 공이 점수 오브젝트에 닿았을 때 칩을 올릴지, 배수를 올릴지 정합니다.")]
    public DamageValueType ValueType;

    [Tooltip("피해 관리자에 전달할 기본 점수 값입니다.")]
    public float Score = 1f;

    [Header("공 외형")]
    [SerializeField, Tooltip("탄환에 적용할 색상입니다. 생성 시 SpriteRenderer.color에 적용됩니다.")]
    Color ballColor = Color.white;

    [SerializeField, Tooltip("탄환에 적용할 스프라이트입니다. 비워두면 BallSpawner의 기본 Ball Prefab 스프라이트를 그대로 사용합니다.")]
    Sprite ballSprite;

    [Header("공 이동")]
    [SerializeField, Tooltip("공이 발사되거나 이동을 시작할 때 사용할 기본 속도입니다.")]
    float launchSpeed = 10f;

    [SerializeField, Tooltip("탄환이 직선으로 이동할지, 진행 방향을 기준으로 좌우 파동 이동할지 정합니다.")]
    BallMovementType movementType = BallMovementType.Straight;

    [SerializeField, Min(0f), Tooltip("파동 이동의 좌우 흔들림 강도입니다. 전진 방향에 더해지는 수직 방향의 비율로 사용됩니다.")]
    float waveAmplitude = 0.5f;

    [SerializeField, Min(0f), Tooltip("파동 이동이 1초 동안 좌우로 반복되는 횟수입니다.")]
    float waveFrequency = 1f;

    [Header("내구도")]
    [SerializeField, Tooltip("공이 가질 수 있는 최대 내구도입니다. 현재 내구도는 공 오브젝트의 BallRuntimeStatus가 관리합니다.")]
    int maxDurability = 5;

    [SerializeField, Tooltip("공이 벽에 부딪힐 때 감소할 내구도입니다.")]
    int wallHitDurabilityDamage = 1;

    [SerializeField, Tooltip("공이 범퍼나 점수 오브젝트 같은 상호작용 오브젝트에 부딪힐 때 감소할 내구도입니다.")]
    int objectHitDurabilityDamage = 1;

    [Header("시각 효과 프리팹")]
    [SerializeField, Tooltip("공이 벽 또는 점수 오브젝트와 충돌할 때 충돌 지점에 생성할 게임 오브젝트입니다.")]
    GameObject collisionEffectPrefab;

    [SerializeField, Tooltip("내구도가 0 이하가 되었을 때 공 위치에 생성할 게임 오브젝트입니다.")]
    GameObject durabilityDepletedEffectPrefab;

    [SerializeField, Min(0f), Tooltip("생성된 시각 효과가 파괴되기까지의 시간입니다. 프리팹이 수명을 직접 관리하면 0으로 설정합니다.")]
    float visualEffectLifetime = 1f;

    [Header("효과 - 생성")]
    [SerializeField, Tooltip("공이 생성되었을 때 발동할 효과 목록입니다.")]
    List<BallEffectData> spawnEffects = new List<BallEffectData>();

    [Header("효과 - 오브젝트 충돌")]
    [SerializeField, Tooltip("공이 범퍼나 점수 오브젝트에 닿았을 때 발동할 효과 목록입니다.")]
    List<BallEffectData> objectHitEffects = new List<BallEffectData>();

    [Header("효과 - 벽 충돌")]
    [SerializeField, Tooltip("공이 벽에 닿았을 때 발동할 효과 목록입니다.")]
    List<BallEffectData> wallHitEffects = new List<BallEffectData>();

    [Header("효과 - 파괴")]
    [SerializeField, Tooltip("공이 파괴되기 직전에 발동할 효과 목록입니다.")]
    List<BallEffectData> destroyEffects = new List<BallEffectData>();

    public float LaunchSpeed
    {
        get
        {
            return launchSpeed;
        }
    }

    public string SaveId
    {
        get
        {
            return string.IsNullOrWhiteSpace(saveId) ? name : saveId.Trim();
        }
    }

    public BallMovementType MovementType
    {
        get
        {
            return movementType;
        }
    }

    public float WaveAmplitude
    {
        get
        {
            return Mathf.Max(0f, waveAmplitude);
        }
    }

    public float WaveFrequency
    {
        get
        {
            return Mathf.Max(0f, waveFrequency);
        }
    }

    public int Price
    {
        get
        {
            return Mathf.Max(0, price);
        }
    }

    public BallRarity Rarity
    {
        get
        {
            return rarity;
        }
    }

    public Color BallColor
    {
        get
        {
            return ballColor;
        }
    }

    public Sprite BallSprite
    {
        get
        {
            return ballSprite;
        }
    }

    public int MaxDurability
    {
        get
        {
            return maxDurability;
        }
    }

    public int WallHitDurabilityDamage
    {
        get
        {
            return wallHitDurabilityDamage;
        }
    }

    public int ObjectHitDurabilityDamage
    {
        get
        {
            return objectHitDurabilityDamage;
        }
    }

    public string DisplayName
    {
        get
        {
            return string.IsNullOrWhiteSpace(displayName) ? name : displayName.Trim();
        }
    }

    public string Description
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(description))
            {
                return description.Trim();
            }

            return $"{GetLocalizedRarity()} · {GetLocalizedValueType()}\n기본 수치 {Score:0.##} · 내구도 {MaxDurability}";
        }
    }

    string GetLocalizedRarity()
    {
        switch (rarity)
        {
            case BallRarity.Rare:
                return "희귀";
            case BallRarity.Epic:
                return "영웅";
            case BallRarity.Legendary:
                return "전설";
            default:
                return "일반";
        }
    }

    string GetLocalizedValueType()
    {
        return ValueType == DamageValueType.Multiplier ? "배수" : "칩";
    }

    public GameObject CollisionEffectPrefab
    {
        get
        {
            return collisionEffectPrefab;
        }
    }

    public GameObject DurabilityDepletedEffectPrefab
    {
        get
        {
            return durabilityDepletedEffectPrefab;
        }
    }

    public float VisualEffectLifetime
    {
        get
        {
            return Mathf.Max(0f, visualEffectLifetime);
        }
    }

    public List<BallEffectData> SpawnEffects
    {
        get
        {
            return spawnEffects;
        }
    }

    public List<BallEffectData> ObjectHitEffects
    {
        get
        {
            return objectHitEffects;
        }
    }

    public List<BallEffectData> WallHitEffects
    {
        get
        {
            return wallHitEffects;
        }
    }

    public List<BallEffectData> DestroyEffects
    {
        get
        {
            return destroyEffects;
        }
    }
}
