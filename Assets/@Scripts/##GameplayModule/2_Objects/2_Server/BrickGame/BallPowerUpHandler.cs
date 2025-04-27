using UnityEngine;


    /// <summary>
    /// 모든 PhysicsBall 인스턴스가 공유하는 파워업 상태 및 타이머를 관리합니다.
    /// </summary>
    public static class BallPowerUpHandler
    {
        private static int currentPower = 1; // 모든 공이 공유하는 공격력
        private static float powerTimer = 0f; // 모든 공이 공유하는 타이머

        // 정적 변수 접근자
        public static int SharedPower => currentPower;
        public static float SharedPowerTimer => powerTimer;

        /// <summary>
        /// 모든 공의 파워업 상태를 설정합니다.
        /// </summary>
        /// <param name="amount">증가시킬 공격력 양</param>
        /// <param name="duration">파워업 지속 시간(초)</param>
        public static void SharedPowerUp(int amount, float duration)
        {
            Debug.Log($"SharedPowerUp called! amount: {amount}, duration: {duration}");
            currentPower += amount;
            // 기존 타이머와 새 지속 시간 중 더 긴 쪽을 선택
            powerTimer = Mathf.Max(powerTimer, duration); 
            
            Debug.Log($"<color=green>[BallPowerUpHandler] 모든 공 공격력 증가: {currentPower}, 남은 시간: {powerTimer}초</color>");
        }

        /// <summary>
        /// 파워업 타이머를 매 프레임 업데이트합니다. 
        /// 이 메서드는 BasicGameState 같은 중앙 관리자에서 매 프레임 호출되어야 합니다.
        /// </summary>
        public static void StaticUpdateTimer()
        {
            if (powerTimer > 0)
            {
                powerTimer -= Time.deltaTime;

                if (powerTimer <= 0)
                {
                    powerTimer = 0;
                    currentPower = 1; // 기본값으로 리셋
                    
                    Debug.Log("<color=yellow>[BallPowerUpHandler] 모든 공 공격력 효과 종료</color>");
                }
            }
        }
    }
