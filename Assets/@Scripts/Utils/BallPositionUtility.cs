using UnityEngine;
using Unity.Assets.Scripts.Objects;


    public static class BallPositionUtility
    {
        private const float SPAWN_OFFSET_Y = 0.05f; // 동일한 Y 오프셋 상수 사용

        /// <summary>
        /// 플랭크 위의 적절한 발사 위치를 계산합니다.
        /// </summary>
        /// <param name="plank">플랭크 컴포넌트</param>
        /// <param name="ballCollider">볼의 콜라이더</param>
        /// <param name="ballTransform">볼의 트랜스폼</param>
        /// <returns>계산된 발사 위치</returns>
        public static Vector3 GetLaunchPosition(PhysicsPlank plank, Collider2D ballCollider, Transform ballTransform)
        {
            if (plank == null) 
            {
                Debug.LogWarning("플랭크 참조가 없습니다. 기본 위치를 반환합니다.");
                return ballTransform.position;
            }

            // 플랭크 콜라이더 가져오기
            var plankCollider = plank.GetComponent<BoxCollider2D>();
            if (plankCollider == null)
            {
                Debug.LogWarning("플랭크 콜라이더가 없습니다. 기본 위치를 반환합니다.");
                return ballTransform.position;
            }

            // 플랭크의 현재 위치
            Vector3 plankPos = plank.transform.position;
            
            // 플랭크의 X 위치를 그대로 사용
            float posX = plankPos.x;
            
            // Y 위치 계산
            Vector3 plankScale = plank.transform.localScale;
            float plankHalfHeight = (plankScale.y * plankCollider.bounds.size.y / plankScale.y * 0.5f);
            float plankTopY = plankPos.y + plankHalfHeight;
            
            float ballRadiusY = 0;
            if (ballCollider != null)
            {
                ballRadiusY = (ballTransform.localScale.y * ballCollider.bounds.size.y / ballTransform.localScale.y * 0.5f);
            }
            else
            {
                // 콜라이더가 없으면 기본값 사용
                ballRadiusY = 0.5f * ballTransform.localScale.y;
            }
            
            float posY = plankTopY + ballRadiusY + SPAWN_OFFSET_Y;
            
            // Z 위치는 플랭크의 Z 위치 사용
            return new Vector3(posX, posY, plankPos.z);
        }
        
        /// <summary>
        /// 기존 공의 발사 방향에 약간의 무작위성을 추가합니다.
        /// </summary>
        /// <param name="baseDirection">기본 발사 방향</param>
        /// <param name="randomAngleRange">무작위 각도 범위 (양쪽으로 +/-)</param>
        /// <returns>무작위화된 발사 방향</returns>
        public static Vector2 GetRandomizedLaunchDirection(Vector2 baseDirection, float randomAngleRange = 15f)
        {
            // 기본 방향 정규화
            baseDirection = baseDirection.normalized;
            
            // 랜덤 각도 계산 (양쪽으로 지정된 범위 내에서)
            float randomAngle = Random.Range(-randomAngleRange, randomAngleRange);
            
            // 각도 적용하여 회전된 방향 반환
            return Quaternion.Euler(0, 0, randomAngle) * baseDirection;
        }
    }
