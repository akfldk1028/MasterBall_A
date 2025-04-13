using UnityEngine;
using Unity.Assets.Scripts.Objects;

namespace Unity.Assets.Scripts.Objects // 네임스페이스 지정
{
    // Star 오브젝트의 동작을 정의합니다.
    // PhysicsObject를 상속받아 기본적인 물리 상호작용 기능을 가집니다.
    public class Star : PhysicsObject
    {
        private const float bottomBoundary = -2.3f;
        private bool isDestroyed = false;

        protected override void FixedUpdate()
        {
            base.FixedUpdate(); // 부모 로직 호출

            if (!isDestroyed && transform.position.y < bottomBoundary)
            {
                isDestroyed = true;
                Debug.Log($"[Star] 별 {gameObject.name}이 바닥 경계선({bottomBoundary})에 도달하여 파괴됩니다.");
                Destroy(gameObject);
            }
        }

        // TODO: 별 고유의 로직 구현
        // 예: 획득 시 효과 (점수 추가, 파워업 등)

        // PhysicsObject의 메서드를 오버라이드하여 별의 특정 동작을 구현할 수 있습니다.
        // 예시:
        // protected override void OnCollisionWithBall(Collision2D collision)
        // {
        //     // 별은 공과 직접 충돌하지 않거나, 충돌 시 바로 획득될 수 있습니다.
        //     // base.OnCollisionWithBall(collision); // 호출하지 않을 수 있음
        //     Debug.Log("별 획득!");
        //     // 획득 효과 처리 (예: ScoreManager.Instance.AddScore(100);)
        //     Destroy(gameObject); // 획득 후 파괴
        // }

        // 또는 패들과 충돌했을 때 획득하도록 구현:
        // private void OnCollisionEnter2D(Collision2D collision)
        // {
        //     if (collision.gameObject.CompareTag("Paddle")) // 패들 태그 확인
        //     {
        //         Debug.Log("별 획득 (패들 충돌)!");
        //         // 획득 효과 처리
        //         Destroy(gameObject);
        //     }
        // }
    }
} 