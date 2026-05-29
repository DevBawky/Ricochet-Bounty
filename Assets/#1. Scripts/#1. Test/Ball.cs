using UnityEngine;

public class Ball : MonoBehaviour
{
    [SerializeField] float _launchForce = 10f;
    [SerializeField] float _lifetime = 1f;

    Rigidbody2D _rigidbody;
    SpriteRenderer _spriteRenderer;

    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        ChangeRandomColor();
        LaunchRandomDirection();
        Destroy(gameObject, _lifetime); //생성된 공을 일정 시간 후에 파괴합니다.
    }

    void ChangeRandomColor()
    {
        _spriteRenderer.color = Random.ColorHSV();
    }

    void LaunchRandomDirection()
    {
        Vector2 direction = Random.insideUnitCircle.normalized; //랜덤한 방향을 생성
        _rigidbody.AddForce(direction *_launchForce, ForceMode2D.Impulse); //랜덤한 방향으로 힘을 가합니다.
    }
}
