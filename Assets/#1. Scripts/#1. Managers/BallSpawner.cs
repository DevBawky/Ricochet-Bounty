using UnityEngine;

public class BallSpawner : MonoBehaviour
{
    [SerializeField] GameObject[] _ballPool;

    Camera _mainCamera;

    void Awake()
    {
        _mainCamera = Camera.main;
    }

    void Update()
    {
        // 마우스 왼쪽 버튼을 클릭하면 공을 생성합니다.
        if (Input.GetMouseButtonDown(0))
        {
            SpawnBall();
        }
    }

    void SpawnBall()
    {
        GameObject ballPrefab = GetRandomBallPrefab();
        if (ballPrefab == null)
        {
            return;
        }

        // ScreenToWorldPoint는 화면 좌표인 마우스 위치를 월드 좌표로 바꿔 줍니다.
        // Vector2에 담으면 z 값은 자동으로 버려져서 2D 위치로 사용할 수 있습니다.
        Vector2 mousePosition = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Instantiate(ballPrefab, mousePosition, Quaternion.identity);
    }

    GameObject GetRandomBallPrefab()
    {
        // Inspector의 _ballPool 배열에 공 프리팹을 하나 이상 넣어야 랜덤 선택이 가능합니다.
        if (_ballPool == null || _ballPool.Length == 0)
        {
            Debug.LogWarning("[BallSpawner] 공 풀이 비어 있습니다. Inspector에서 _ballPool에 공 프리팹을 넣어 주세요.", this);
            return null;
        }

        // 0부터 배열 길이 바로 전까지의 숫자 중 하나를 랜덤으로 뽑습니다.
        // Random.Range(int, int)의 두 번째 값은 포함되지 않으므로 Length를 그대로 넣으면 됩니다.
        int randomIndex = Random.Range(0, _ballPool.Length);
        GameObject ballPrefab = _ballPool[randomIndex];

        if (ballPrefab == null)
        {
            Debug.LogWarning($"[BallSpawner] _ballPool의 {randomIndex}번 칸이 비어 있습니다.", this);
            return null;
        }

        return ballPrefab;
    }
}
