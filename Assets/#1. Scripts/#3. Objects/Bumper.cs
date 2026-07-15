using UnityEngine;
using UnityEngine.Events;

// Bumper는 충돌 판정을 반드시 사용해야 하므로 Collider2D가 필요합니다.
// 기본 사용 방식은 Trigger가 아니라 Collision 방식입니다.
[RequireComponent(typeof(Collider2D))]
public class Bumper : MonoBehaviour
{
    [Header("Ball Check")]
    [SerializeField] bool useTagCheck = true; // true이면 ballTag와 같은 태그를 가진 오브젝트만 반응합니다.
    [SerializeField] string ballTag = "Ball"; // Ball 오브젝트에 붙일 태그 이름입니다.

    [Header("Bounce")]
    [SerializeField] float speedBoostMultiplier = 1.5f; // 충돌 시 현재 속도를 몇 배로 키울지 정합니다.
    [SerializeField] float minBounceSpeed = 8f; // 공이 너무 느릴 때도 최소한 이 속도 이상으로 튕기게 합니다.
    [SerializeField] float maxBallSpeed = 20f; // 공이 너무 빨라져서 제어하기 어려워지지 않도록 최대 속도를 제한합니다.
    [SerializeField] float pushOutDistance = 0.05f; // 충돌 후 공을 살짝 밀어내서 범퍼 안에 끼는 상황을 줄입니다.

    [Header("Visual Feedback")]
    [SerializeField] float scalePunchAmount = 0.15f; // 충돌 순간 범퍼가 원래 크기보다 얼마나 커질지 정합니다.
    [SerializeField] float resetSpeed = 12f; // 커진 범퍼가 원래 크기로 돌아오는 속도입니다.

    [Header("Events")]
    [SerializeField] UnityEvent onBumperHit; // 사운드나 이펙트를 연결하기 위한 이벤트입니다. 점수 처리는 연결하지 않습니다.

    Vector3 originalScale;

    public Vector3 OriginalScale => originalScale;

    void Awake()
    {
        // 충돌 피드백 후 다시 돌아올 기준 크기가 필요하므로 시작할 때 원래 scale을 저장합니다.
        originalScale = transform.localScale;

        // 이 스크립트는 OnCollisionEnter2D를 사용합니다.
        // 따라서 Collider2D의 Is Trigger는 꺼져 있어야 정상적으로 충돌 이벤트가 들어옵니다.
        Collider2D bumperCollider = GetComponent<Collider2D>();
        if (bumperCollider != null && bumperCollider.isTrigger)
        {
            Debug.LogWarning($"{name} Bumper Collider2D의 Is Trigger가 켜져 있습니다. Bumper는 Collision 방식 사용을 권장합니다.", this);
        }
    }

    void Update()
    {
        // 충돌 순간 커진 범퍼를 매 프레임 originalScale로 부드럽게 되돌립니다.
        // 이렇게 하면 별도의 애니메이션 없이도 간단한 타격 피드백을 만들 수 있습니다.
        transform.localScale = Vector3.Lerp(transform.localScale, originalScale, resetSpeed * Time.deltaTime);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // 1. 충돌한 오브젝트가 범퍼 반응 대상인지 확인합니다.
        if (!IsValidBall(collision))
        {
            return;
        }

        // 2. 특정 BallController나 BallMovement에 의존하지 않고 Rigidbody2D만 가져옵니다.
        Rigidbody2D ballRigidbody = GetBallRigidbody(collision);
        if (ballRigidbody == null)
        {
            return;
        }

        // 3. 범퍼 중심에서 공 위치로 향하는 방향을 계산합니다.
        // 이 방향으로 속도를 주면 공이 범퍼 바깥쪽으로 튕겨나갑니다.
        Vector2 bounceDirection = CalculateBounceDirection(collision, ballRigidbody);

        // 4. 계산된 방향과 속도 제한값을 사용해서 공을 튕겨냅니다.
        BounceBall(ballRigidbody, bounceDirection);

        // 5. 범퍼의 시각적 피드백을 재생합니다.
        PlayHitFeedback();

        // 점수 시스템은 이 스크립트에서 절대 처리하지 않습니다.
        // ScoreManager, UI 점수 이동, BallController.OnHitObject 같은 호출은 연결하지 않습니다.
        // 필요한 사운드나 이펙트만 Inspector에서 onBumperHit에 연결합니다.
        onBumperHit.Invoke();
    }

    // 충돌한 오브젝트가 범퍼 반응 대상인지 확인합니다.
    bool IsValidBall(Collision2D collision)
    {
        if (collision == null)
        {
            return false;
        }

        // useTagCheck가 true이면 지정한 태그의 오브젝트만 Ball로 인정합니다.
        if (useTagCheck)
        {
            return collision.gameObject.CompareTag(ballTag);
        }

        // useTagCheck가 false이면 Rigidbody2D가 있는 오브젝트라면 반응할 수 있습니다.
        return GetBallRigidbody(collision) != null;
    }

    // Collision2D에서 Rigidbody2D를 찾습니다.
    // 먼저 collision.rigidbody를 사용하고, 없으면 부모 오브젝트까지 확인합니다.
    Rigidbody2D GetBallRigidbody(Collision2D collision)
    {
        if (collision.rigidbody != null)
        {
            return collision.rigidbody;
        }

        return collision.gameObject.GetComponentInParent<Rigidbody2D>();
    }

    // 범퍼가 공을 어느 방향으로 튕길지 계산합니다.
    Vector2 CalculateBounceDirection(Collision2D collision, Rigidbody2D ballRigidbody)
    {
        Vector2 bumperPosition = transform.position;
        Vector2 ballPosition = ballRigidbody.position;

        // 기본 방향은 "범퍼 위치에서 공 위치로 향하는 방향"입니다.
        // 이 방향을 사용하면 공이 범퍼 중심에서 바깥쪽으로 밀려납니다.
        Vector2 bounceDirection = ballPosition - bumperPosition;

        if (IsSafeDirection(bounceDirection))
        {
            return bounceDirection.normalized;
        }

        // 공과 범퍼의 위치가 거의 같으면 방향을 정하기 어렵습니다.
        // 이때는 공이 현재 움직이던 방향을 대신 사용합니다.
        Vector2 currentMoveDirection = ballRigidbody.linearVelocity;
        if (IsSafeDirection(currentMoveDirection))
        {
            return currentMoveDirection.normalized;
        }

        // 공의 현재 이동 방향도 너무 작으면 충돌 contact normal을 fallback으로 사용합니다.
        // contact normal은 충돌 지점에서 밀려나야 하는 방향을 알려줍니다.
        if (collision.contactCount > 0)
        {
            Vector2 contactNormal = collision.GetContact(0).normal;
            if (IsSafeDirection(contactNormal))
            {
                return contactNormal.normalized;
            }
        }

        // 모든 방향 계산이 실패하는 예외 상황에서도 zero vector나 NaN을 피하기 위해 기본 방향을 사용합니다.
        return Vector2.up;
    }

    // 방향 벡터가 너무 작거나 잘못된 값인지 확인합니다.
    bool IsSafeDirection(Vector2 direction)
    {
        if (float.IsNaN(direction.x) || float.IsNaN(direction.y))
        {
            return false;
        }

        return direction.sqrMagnitude > 0.0001f;
    }

    // 계산된 방향으로 공의 속도를 변경하고, 범퍼에 끼지 않도록 살짝 밀어냅니다.
    void BounceBall(Rigidbody2D ballRigidbody, Vector2 bounceDirection)
    {
        float currentSpeed = ballRigidbody.linearVelocity.magnitude;

        // 현재 속도에 배율을 곱해 더 강하게 튕기게 합니다.
        float boostedSpeed = currentSpeed * speedBoostMultiplier;

        // minBounceSpeed는 공이 거의 멈춘 상태에서도 충분히 튕기게 하기 위해 필요합니다.
        // maxBallSpeed는 속도가 무한히 커져 게임이 제어 불가능해지는 것을 막기 위해 필요합니다.
        float finalSpeed = Mathf.Clamp(boostedSpeed, minBounceSpeed, maxBallSpeed);

        ballRigidbody.linearVelocity = bounceDirection * finalSpeed;

        // 충돌 직후 공이 범퍼와 겹쳐 있으면 같은 충돌이 반복되거나 끼어 보일 수 있습니다.
        // 그래서 Rigidbody2D.position을 사용해 튕겨나갈 방향으로 아주 조금 이동시킵니다.
        ballRigidbody.position += bounceDirection * pushOutDistance;
    }

    // 범퍼가 맞았을 때 순간적으로 커지는 간단한 시각 피드백입니다.
    void PlayHitFeedback()
    {
        transform.localScale = originalScale * (1f + scalePunchAmount);
    }
}
