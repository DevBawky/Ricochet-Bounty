using UnityEngine;

public class BallSpawner : MonoBehaviour
{
    [SerializeField] GameObject _ballPrefab;
    Camera _mainCamera;

    void Awake()
    {
        _mainCamera = Camera.main;
    }

    void Update()
    {
        //마우스 클릭을 감지하여 SpawnBall 함수를 호출
        if (Input.GetMouseButtonDown(0))
        {
            SpawnBall();
        }
    }

    void SpawnBall()
    {
        Vector2 mousePosition = _mainCamera.ScreenToWorldPoint(Input.mousePosition); //Vector2를 사용하면 자동으로 z축이 0으로 설정됩니다.
        Instantiate(_ballPrefab, mousePosition, Quaternion.identity);
    }

}
