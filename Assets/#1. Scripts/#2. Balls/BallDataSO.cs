using System.Collections.Generic;
using UnityEngine;

// BallDataSO는 공의 "기본 설정값"을 담는 ScriptableObject입니다.
// 여러 공 오브젝트가 같은 BallDataSO를 함께 참조할 수 있으므로,
// 현재 내구도, 쿨다운, 발동 횟수처럼 플레이 중에 변하는 값은 여기에 저장하지 않습니다.
// 그런 런타임 상태는 BallRuntimeStatus와 BallEffectRuntimeState가 각 공 오브젝트마다 따로 관리합니다.
[CreateAssetMenu(fileName = "New Ball Data", menuName = "Ball Data")]
public class BallDataSO : ScriptableObject
{
    [Header("Shop")]
    [SerializeField, Tooltip("Shop purchase price.")]
    int price = 10;

    [SerializeField, Tooltip("Rarity used by shop rarity weights.")]
    BallRarity rarity = BallRarity.Common;

    [Header("Damage Value")]
    [Tooltip("이 공이 점수 오브젝트에 닿았을 때 Chips를 올릴지, Multiplier를 올릴지 정합니다.")]
    public DamageValueType ValueType;

    [Tooltip("DamageManager에 전달할 기본 점수 값입니다.")]
    public float Score = 1f;

    [Header("Ball Visual")]
    [SerializeField, Tooltip("탄환에 적용할 색상입니다. 생성 시 SpriteRenderer.color에 적용됩니다.")]
    Color ballColor = Color.white;

    [SerializeField, Tooltip("탄환에 적용할 스프라이트입니다. 비워두면 BallSpawner의 기본 Ball Prefab 스프라이트를 그대로 사용합니다.")]
    Sprite ballSprite;

    [Header("Ball Movement")]
    [SerializeField, Tooltip("공이 발사되거나 이동을 시작할 때 사용할 기본 속도입니다.")]
    float launchSpeed = 10f;

    [SerializeField, Tooltip("탄환이 직선으로 이동할지, 진행 방향을 기준으로 좌우 파동 이동할지 정합니다.")]
    BallMovementType movementType = BallMovementType.Straight;

    [SerializeField, Min(0f), Tooltip("Wave 이동의 좌우 흔들림 강도입니다. 전진 방향에 더해지는 수직 방향의 비율로 사용됩니다.")]
    float waveAmplitude = 0.5f;

    [SerializeField, Min(0f), Tooltip("Wave 이동이 1초 동안 좌우로 반복되는 횟수입니다.")]
    float waveFrequency = 1f;

    [Header("Durability")]
    [SerializeField, Tooltip("공이 가질 수 있는 최대 내구도입니다. 현재 내구도는 공 오브젝트의 BallRuntimeStatus가 관리합니다.")]
    int maxDurability = 5;

    [SerializeField, Tooltip("공이 벽에 부딪힐 때 감소할 내구도입니다.")]
    int wallHitDurabilityDamage = 1;

    [SerializeField, Tooltip("공이 범퍼나 점수 오브젝트 같은 상호작용 오브젝트에 부딪힐 때 감소할 내구도입니다.")]
    int objectHitDurabilityDamage = 1;

    [Header("Effects - Spawn")]
    [SerializeField, Tooltip("공이 생성되었을 때 발동할 효과 목록입니다.")]
    List<BallEffectData> spawnEffects = new List<BallEffectData>();

    [Header("Effects - Object Hit")]
    [SerializeField, Tooltip("공이 범퍼나 점수 오브젝트에 닿았을 때 발동할 효과 목록입니다.")]
    List<BallEffectData> objectHitEffects = new List<BallEffectData>();

    [Header("Effects - Wall Hit")]
    [SerializeField, Tooltip("공이 벽에 닿았을 때 발동할 효과 목록입니다.")]
    List<BallEffectData> wallHitEffects = new List<BallEffectData>();

    [Header("Effects - Destroy")]
    [SerializeField, Tooltip("공이 파괴되기 직전에 발동할 효과 목록입니다.")]
    List<BallEffectData> destroyEffects = new List<BallEffectData>();

    public float LaunchSpeed
    {
        get
        {
            return launchSpeed;
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
