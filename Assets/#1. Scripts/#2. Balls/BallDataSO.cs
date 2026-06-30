using UnityEngine;

// BallDataSO는 공이 참조하는 데이터 에셋입니다.
// 공 오브젝트에 직접 붙는 MonoBehaviour가 아니라, Project 창에서 생성하는 ScriptableObject입니다.
// ScriptableObject를 사용하면 여러 공이 같은 데이터를 공유할 수 있어서
// 같은 성능의 공을 여러 개 만들 때 값을 한 곳에서 관리하기 쉽습니다.
[CreateAssetMenu(fileName = "New Ball Data", menuName = "Ball Data")]
public class BallDataSO : ScriptableObject
{
    [Header("Damage Value")]
    public DamageValueType ValueType; // 이 공이 Chips를 올릴지, Multiplier를 올릴지 정합니다.
    public float Score = 1f; // 공이 오브젝트에 닿았을 때 DamageManager에 더할 값입니다.
}
