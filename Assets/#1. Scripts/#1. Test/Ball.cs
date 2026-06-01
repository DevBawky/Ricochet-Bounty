using UnityEngine;

public class Ball : MonoBehaviour
{
    [SerializeField] float _launchForce = 10f;
    [SerializeField] float _pushOutDistance = 0.01f;
    [SerializeField] int _maxCollisionCount = 5;

    Rigidbody2D _rigidbody;
    SpriteRenderer _spriteRenderer;
    Vector2 _lastVelocity;
    int _collisionCount;

    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();

        _rigidbody.freezeRotation = true;
    }

    void Start()
    {
        ChangeRandomColor();
        LaunchRandomDirection();
    }

    void FixedUpdate()
    {
        _lastVelocity = _rigidbody.linearVelocity;
    }

    void ChangeRandomColor()
    {
        _spriteRenderer.color = Random.ColorHSV();
    }

    void LaunchRandomDirection()
    {
        Vector2 direction = Random.insideUnitCircle.normalized;
        _rigidbody.AddForce(direction * _launchForce, ForceMode2D.Impulse);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Wall"))
        {
            return;
        }

        _collisionCount++;

        if (_collisionCount == _maxCollisionCount)
        {
            Destroy(gameObject);
            return;
        }

        if (_lastVelocity == Vector2.zero)
        {
            return;
        }

        Vector2 bestNormal = FindBestWallNormal(collision);
        float movingIntoWallAmount = Vector2.Dot(_lastVelocity, bestNormal);

        if (movingIntoWallAmount >= 0f)
        {
            return;
        }

        Vector2 reflectedVelocity = Vector2.Reflect(_lastVelocity, bestNormal);
        _rigidbody.linearVelocity = reflectedVelocity;

        Vector2 pushedPosition = _rigidbody.position + bestNormal * _pushOutDistance;
        _rigidbody.position = pushedPosition;
    }

    Vector2 FindBestWallNormal(Collision2D collision)
    {
        Vector2 moveDirection = _lastVelocity.normalized;
        Vector2 bestNormal = collision.GetContact(0).normal;
        float bestDot = Vector2.Dot(moveDirection, bestNormal);

        for (int i = 1; i < collision.contactCount; i++)
        {
            Vector2 normal = collision.GetContact(i).normal;
            float dot = Vector2.Dot(moveDirection, normal);

            if (dot < bestDot)
            {
                bestDot = dot;
                bestNormal = normal;
            }
        }

        return bestNormal;
    }
}
