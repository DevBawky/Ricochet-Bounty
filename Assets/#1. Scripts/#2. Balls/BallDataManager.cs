using UnityEngine;

// BallDataManager는 요청서의 BallDataHolder와 같은 역할을 하는 기존 컴포넌트입니다.
// 실제 공 프리팹에 붙어서 "이 공이 어떤 BallDataSO를 사용할지"를 들고 있습니다.
// BallDataSO 자체에는 현재 내구도나 쿨다운 같은 런타임 값을 저장하지 않습니다.
public class BallDataManager : MonoBehaviour
{
    [SerializeField] BallDataSO ballData;

    public BallDataSO BallData
    {
        get
        {
            return ballData;
        }
    }

    public bool ValidateData()
    {
        if (ballData != null)
        {
            return true;
        }

        Debug.LogWarning($"[BallDataManager] {name}에 BallDataSO가 연결되어 있지 않습니다.", this);
        return false;
    }
}
