using UnityEngine;
using Unity.Assets.Scripts.Objects;

namespace Unity.Assets.Scripts.Objects // 네임스페이스 지정
{
    // Brick 오브젝트의 동작을 정의합니다.
    // PhysicsObject를 상속받아 기본적인 물리 상호작용 기능을 가집니다.
    public class Brick : PhysicsObject
    {
        // 게임 오버가 발생하는 Y 경계선 (ObjectPlacement와 동일한 값 사용)
        private const float bottomBoundary = -2.3f;
        private bool isGameOverTriggered = false; // 게임 오버 중복 호출 방지

        // PhysicsObject에서 상속받은 Update 또는 FixedUpdate 사용 가능
        // 여기서는 FixedUpdate를 사용하여 물리 업데이트와 동기화
        protected override void FixedUpdate()
        {
            base.FixedUpdate(); // 부모 로직 호출

            // 게임 오버 상태가 아니고, 오브젝트가 경계선 아래로 내려갔는지 확인
            if (!isGameOverTriggered && transform.position.y < bottomBoundary)
            {
                TriggerGameOver();
            }
        }

        private void TriggerGameOver()
        {
            isGameOverTriggered = true;
            Debug.LogError($"[Brick] 게임 오버: 벽돌 {gameObject.name}이 바닥 경계선({bottomBoundary})에 도달했습니다!");

            // TODO: 실제 게임 오버 로직 호출
            // 예: GameManager.Instance.GameOver();

            // 게임 오버를 유발한 벽돌은 파괴하지 않거나, 게임 오버 처리 후 파괴할 수 있음
            // Destroy(gameObject);
        }

        // TODO: 벽돌 고유의 로직 구현
        // 예: 체력, 파괴 시 효과, 점수 등

        // PhysicsObject의 메서드를 오버라이드하여 벽돌의 특정 동작을 구현할 수 있습니다.
        // 예시:
        // protected override void OnCollisionWithBall(Collision2D collision)
        // {
        //     base.OnCollisionWithBall(collision); // 기본 충돌 처리 호출
        //     // 벽돌 체력 감소 로직
        //     // 파괴 조건 확인 및 처리
        // }
    }
} 