using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using VContainer;
using static Define;
using Unity.Assets.Scripts.Data;

namespace Unity.Assets.Scripts.Objects
{
    public enum EBallState { None, Ready, Launching, Moving }

    public class PhysicsBall : PhysicsObject
    {
        // Static event for hitting the bottom
        public static event Action OnHitBottom;

        #region Fields
        [Header("Ball Properties")]
        [SerializeField] private int ballCount = 1;
        
        [Header("Ball Visuals")]
        [SerializeField] private Material[] ballMaterials;
        
        [Header("Plank Interaction")]
        [SerializeField] private float plankMoveThreshold = 0.1f;
        [SerializeField] private Vector2 launchDirection = Vector2.up;
        [SerializeField] private PhysicsPlank plank;
        private float previousPlankX;
        
        // Ball Behavior
        private int ballsShooted = 0;
        
        // Power Up - Static shared values
        private static int currentPower = 1;
        private static float powerTimer = 0f;
        [SerializeField] private Material normalMaterial, poweredMaterial;
        
        // Network variables
        private int _syncedBallCount = 1;
        private EBallState _syncedState = EBallState.None;
        
        // References
        [Inject] private ObjectManager _objectManager;
        private Camera mainCamera;
        
        // Ball stats
        public CreatureStat BaseRadius = new CreatureStat(0);
        public CreatureStat BaseSpeed = new CreatureStat(0);
        public CreatureStat BaseDamage = new CreatureStat(0);
        public CreatureStat launchForce = new CreatureStat(10);
        [SerializeField] private float maxBounceAngle = 75f;
        
        // Properties
        public static int SharedPower => currentPower;
        public static float SharedPowerTimer => powerTimer;
        public int AttackPower { get; private set; } = 1;
        
        // State
        private EBallState _currentState = EBallState.None;
        public EBallState CurrentState {
            get => _currentState;
            protected set {
                if (_currentState == value) return;
                
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[{gameObject.name}] State Change: {_currentState} -> {value}");
                #endif
                
                _currentState = value;
                // if (IsServer && IsSpawned)
                _syncedState = _currentState;
                
                // 상태 진입 처리
                switch (value) {
                    case EBallState.Ready:
                        SetBallPositionAbovePlank();
                        previousPlankX = plank != null ? plank.transform.position.x : transform.position.x;
                        if (rb != null) rb.isKinematic = true;
                        break;
                    case EBallState.Launching:
                        ballsShooted = 1;
                        break;
                    case EBallState.Moving:
                        // 이동 상태 시작
                        break;
                }
            }
        }
        #endregion

        #region Unity Lifecycle
        public override bool Init()
        {
            if (!base.Init()) return false;
            
            ObjectType = EObjectType.ball;
            plank = GameObject.FindGameObjectWithTag("Plank").GetComponent<PhysicsPlank>();
            mainCamera = Camera.main;
            
            Debug.Log($"<color=green>[Ball] Init: Ball 컴포넌트 초기화됨, 볼 개수: {ballCount}</color>");
            return true;
        }
        
        public override void SetInfo(int templateID)
        {
            base.SetInfo(templateID);
            Data.BallData ballData = DataLoader.instance.BallDic[templateID];
            
            // Stats from data
            BaseRadius = new CreatureStat(ballData.BaseRadius);
            BaseSpeed = new CreatureStat(ballData.BaseSpeed);
            BaseDamage = new CreatureStat(ballData.BaseDamage);
            launchForce = new CreatureStat(ballData.LaunchForce);
            maxBounceAngle = ballData.maxBounceAngle;
            
            CurrentState = EBallState.None;
            
                    ResetBallToReadyState();



        }
        
        protected virtual void Update()
        {
            // 공격력 상태 업데이트
            UpdatePowerStatus();
        }
        
        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            
            
            // 상태에 따른 행동 처리
            switch (CurrentState) {
                case EBallState.Ready:
                    if (CommonVars.canContinue && plank != null) {
                        float currentPlankX = plank.transform.position.x;
                        
                        // 플랭크 위에 위치 유지
                        if (Mathf.Abs(currentPlankX - transform.position.x) > 0.01f || 
                            Mathf.Abs(transform.position.y - GetPlankRelativeSpawnY()) > 0.01f) {
                            SetBallPositionAbovePlank();
                        }
                        
                        // 플랭크 이동 감지 시 발사
                        if (Mathf.Abs(currentPlankX - previousPlankX) >= plankMoveThreshold) {
                            LaunchBall(launchDirection);
                        } else {
                            previousPlankX = currentPlankX;
                        }
                    }
                    break;
                
                case EBallState.Launching:
                    if (ballCount > ballsShooted + 1) {
                        LaunchBall(launchDirection);
                        ballsShooted++;
                    }
                    break;
                
                case EBallState.Moving:
                    if (rb != null && !rb.isKinematic) {
                        LimitBallSpeed();
                    }
                    break;
                
                case EBallState.None:
                    CurrentState = EBallState.Ready;
                    break;
            }
        }
        #endregion
        
        #region Network Callbacks
        // public override void OnNetworkSpawn()
        // {
        //     base.OnNetworkSpawn();
            
        //     if (IsServer) {
        //         _syncedBallCount.Value = ballCount;
        //         _syncedState.Value = CurrentState;
        //     }
            
        //     if (IsClient) {
        //         _syncedBallCount.OnValueChanged += (_, newValue) => ballCount = newValue;
        //         _syncedState.OnValueChanged += (_, newValue) => { if (!IsServer) CurrentState = newValue; };
        //         CurrentState = _syncedState.Value;
        //     }
        // }
        
        // public override void OnNetworkDespawn()
        // {
        //     base.OnNetworkDespawn();
            
        //     if (IsClient) {
        //         _syncedBallCount.OnValueChanged -= (_, newValue) => ballCount = newValue;
        //         _syncedState.OnValueChanged -= (_, newValue) => { if (!IsServer) CurrentState = newValue; };
        //     }
        // }
        #endregion
        
        #region Physics Methods
        // 볼 속도 제한
        private void LimitBallSpeed()
        {
            float currentSpeed = rb.linearVelocity.magnitude;
            
            // 속도 제한 적용
            if (currentSpeed > 0 && currentSpeed < 5f) {
                rb.linearVelocity = rb.linearVelocity.normalized * 10f;
            } else if (currentSpeed > 20f) {
                rb.linearVelocity = rb.linearVelocity.normalized * 20f;
                
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[{gameObject.name}] Ball speed capped at 20 (was {currentSpeed:F1})");
                #endif
            }
        }
        

        protected override void OnStuck()
        {
            base.OnStuck();
            
            if (CurrentState == EBallState.Moving && rb != null && !rb.isKinematic) 
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[{gameObject.name}] 공이 움직임 패턴에 갇혔습니다.");
                #endif
                
                // 공의 현재 상태 분석
                Vector2 velocity = rb.linearVelocity;
                float speed = velocity.magnitude;
                
                // 떠다니는 상태 검사 (Y축으로만 움직임)
                bool isFloating = Mathf.Abs(velocity.y) > 1f && Mathf.Abs(velocity.x) < 0.5f;
                // 수평 갇힘 검사 (X축으로만 약하게 움직임)
                bool isHorizontalTrapped = Mathf.Abs(velocity.y) < 0.5f && Mathf.Abs(velocity.x) < 2f;
                // 느린 움직임 검사
                bool isMovingSlow = speed < 3f;
                
                // 기존 속도의 크기 보존 (너무 느려지는 것 방지)
                float targetSpeed = Mathf.Max(speed, 10f); // 최소 10의 속도 유지
                
                // 상태에 따른 처리
                if (isFloating)
                {
                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogWarning($"[{gameObject.name}] 공이 상하로만 떠다니고 있습니다.");
                    #endif
                    
                    // 현재 Y 방향의 반대 방향으로 약간의 힘 적용
                    float dirX = (UnityEngine.Random.value > 0.5f) ? 1f : -1f; // 랜덤 좌우 방향
                    Vector2 unstuckForce = new Vector2(dirX, velocity.y > 0 ? -0.2f : 0.2f).normalized;
                    
                    // 기존 속도 크기를 유지하면서 방향만 바꿈
                    rb.linearVelocity = unstuckForce * targetSpeed;
                }
                else if (isHorizontalTrapped)
                {
                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogWarning($"[{gameObject.name}] 공이 수평으로 갇혀 있습니다.");
                    #endif
                    
                    // 수직 방향 성분 추가
                    Vector2 unstuckForce = new Vector2(velocity.x, velocity.x > 0 ? 0.5f : -0.5f).normalized;
                    
                    // 기존 속도 크기를 유지하면서 방향만 바꿈
                    rb.linearVelocity = unstuckForce * targetSpeed;
                }
                else if (isMovingSlow)
                {
                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogWarning($"[{gameObject.name}] 공이 너무 느리게 움직이고 있습니다.");
                    #endif
                    
                    // 방향은 유지하고 속도만 증가
                    rb.linearVelocity = rb.linearVelocity.normalized * targetSpeed;
                }
                else
                {
                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogWarning($"[{gameObject.name}] 공이 불명확한 패턴으로 갇혔습니다.");
                    #endif
                    
                    // 약간의 랜덤 방향 변화 적용
                    Vector2 randomDir = UnityEngine.Random.insideUnitCircle.normalized * 0.3f;
                    Vector2 newDir = (velocity.normalized + randomDir).normalized;
                    
                    // 기존 속도 크기를 유지하면서 방향만 약간 바꿈
                    rb.linearVelocity = newDir * targetSpeed;
                }
                
                // LimitBallSpeed 호출하여 공 속도가 게임의 범위 내에 유지되도록 함
                LimitBallSpeed();
            }
        }
                
        protected override void HandleCollision(Collision2D collision)
        {
            // 서버에서만 충돌 로직 처리

   
            // 벽돌 또는 벽과의 충돌 처리
            if (collision.gameObject.CompareTag("Brick") || collision.gameObject.CompareTag("Wall"))
            {
                HandleBrickOrWallCollision(collision);
            }
            // 플랭크와의 충돌 처리
            else if (collision.gameObject.CompareTag("Plank"))
            {
                HandlePlankCollision(collision);
            }
            else
            {
                // 예상치 못한 충돌 처리 (예: 다른 공과의 충돌)
                Debug.LogWarning($"[PhysicsBall] Unexpected collision with {collision.gameObject.name}");
            }
        }
        
        private void HandleBrickOrWallCollision(Collision2D collision)
        {
            if (rb == null) return;
            
            // 충돌 법선 벡터로 반사 방향 계산
            Vector2 normal = collision.contacts[0].normal;
            Vector2 reflectDir = Vector2.Reflect(rb.linearVelocity.normalized, normal);
            float currentSpeed = Mathf.Clamp(rb.linearVelocity.magnitude, 10f, 12f);
            
            // 벽 충돌 특별 처리 (벽 타기 방지)
            if (collision.gameObject.CompareTag("Wall")) {
                float dotProduct = Vector2.Dot(reflectDir, normal);
                if (Mathf.Abs(dotProduct) < 0.1f) {
                    reflectDir = (reflectDir + normal * 0.2f).normalized;
                }
            }
            // 벽돌 충돌 처리
            else if (collision.gameObject.CompareTag("Brick")) {
                // 약간의 랜덤 각도 추가
                float randomAngle = UnityEngine.Random.Range(-2f, 2f) * Mathf.Deg2Rad;
                reflectDir = new Vector2(
                    reflectDir.x * Mathf.Cos(randomAngle) - reflectDir.y * Mathf.Sin(randomAngle),
                    reflectDir.x * Mathf.Sin(randomAngle) + reflectDir.y * Mathf.Cos(randomAngle)
                );
                
                // 벽돌에 데미지 적용 (필요시)
                // Brick brick = collision.gameObject.GetComponent<Brick>();
                // if (brick != null) brick.OnHit();
            }
            
            // 새 속도 적용
            rb.linearVelocity = reflectDir.normalized * currentSpeed;
            transform.position += (Vector3)(reflectDir.normalized * 0.01f);
        }
        
        private void HandlePlankCollision(Collision2D collision)
        {
            if (rb == null || plank == null) return;
            
            // 플랭크 위치 기준 충돌 위치의 상대적 거리 계산
            Vector2 contactPoint = collision.GetContact(0).point;
            Vector2 plankCenter = plank.transform.position;
            float plankWidth = plank.GetComponent<BoxCollider2D>().size.x * plank.transform.localScale.x;
            
            // 상대적 위치 (-1 ~ 1)로 정규화
            float normalizedDifference = Mathf.Clamp((contactPoint.x - plankCenter.x) / (plankWidth / 2f), -1f, 1f);
            
            // 각도 계산 및 반사 방향 결정
            float angle = normalizedDifference * maxBounceAngle;
            Vector2 bounceDirection = Quaternion.Euler(0f, 0f, -angle) * Vector2.up;
            
            // 속도 계산 (5~20 범위 내에서 약간 증가)
            float bounceSpeed = Mathf.Clamp(rb.linearVelocity.magnitude * 1.02f, 5f, 20f);
            
            // 새 속도 적용
            rb.linearVelocity = bounceDirection.normalized * bounceSpeed;
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[PhysicsBall] Plank collision. Angle: {angle:F1}, Speed: {bounceSpeed:F1}");
            #endif
        }
        
        protected override void HandleTrigger(Collider2D other)
        {
            
            if (other.CompareTag("BottomBoundary")) {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[{gameObject.name}] Ball hit BottomBoundary. Current ball count: {ballCount}");
                #endif
                
                if (IsLastBall()) {
                    ResetBallToReadyState();
                } else {
                    Destroy(gameObject);
                }
            }
        }
        #endregion
        
        #region Ball Methods
        public void LaunchBall(Vector2 direction)
        {
            if (rb == null) return;
            
            // 발사 전 Kinematic 해제
            rb.isKinematic = false;
            
            // 방향과 힘으로 발사
            Launch(direction.normalized, launchForce.Value, ForceMode2D.Impulse);
            
            // 상태 전환
            CurrentState = (ballCount > 1) ? EBallState.Launching : EBallState.Moving;
        }
        
        public void SetBallCount(int count)
        {
            
            ballCount = Mathf.Max(1, count);
            _syncedBallCount = ballCount;
        }
        
        private void ResetBallToReadyState()
        {
            // 속도 초기화
            if (rb != null) {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
            
            // 상태 변경
            CurrentState = EBallState.Ready;
            
            // 게임 변수 초기화
            ResetWaveVariables();
            OnHitBottom?.Invoke(); // static 이벤트 호출

        }
        
        private void ResetWaveVariables()
        {
            powerTimer = 0;
            currentPower = 1;
            CommonVars.ballsReachedDistance = 0;
            CommonVars.canContinue = true;
            CommonVars.startMovingTowardsMainBall = false;
            CommonVars.firstBallHitBottomCollider = false;
            CommonVars.newWaveOfBricks = true;
        }
        
        private bool IsLastBall()
        {
            int movingBalls = 0;
            foreach (PhysicsBall ball in FindObjectsOfType<PhysicsBall>()) {
                if (ball.CurrentState == EBallState.Moving || ball.CurrentState == EBallState.Launching) {
                    movingBalls++;
                }
            }
            return movingBalls <= 1;
        }
        #endregion
        
        #region Power Up Methods
        private void UpdatePowerStatus()
        {
            ColorBallByPower();
            
            // 첫 번째 공이거나 유일한 공만 타이머 처리
            if (gameObject.name.Contains("ball") || FindObjectsOfType<PhysicsBall>().Length == 1) {
                if (powerTimer > 0) {
                    powerTimer -= Time.deltaTime;
                    
                    if (powerTimer <= 0) {
                        powerTimer = 0;
                        currentPower = 1;
                        Debug.Log("<color=yellow>[PhysicsBall] 모든 공 공격력 효과 종료</color>");
                    }
                }
            }
        }
        
        public void ColorBallByPower()
        {
            if (objectRenderer == null) return;
            
            // 파워 레벨에 따른 색상 계산
            Color targetColor;
            
            if (currentPower <= 1) {
                targetColor = Color.white;
            } else if (currentPower <= 3) {
                // 2~3 레벨: 노란색 → 빨간색
                float t = (currentPower - 1) / 2f;
                targetColor = new Color(1, 1 - t, 0);
            } else if (currentPower <= 5) {
                // 4~5 레벨: 빨간색 → 보라색
                float t = (currentPower - 3) / 2f;
                targetColor = new Color(1, 0, t);
            } else {
                // 6+ 레벨: 보라색 → 파란색
                float t = Mathf.Min((currentPower - 5) / 3f, 1f);
                targetColor = new Color(Mathf.Max(1 - t, 0), 0, 1);
            }
            
            // 색상 점진적 변경
            if (objectRenderer.material.HasProperty("_Color")) {
                objectRenderer.material.color = Color.Lerp(
                    objectRenderer.material.color, 
                    targetColor, 
                    Time.deltaTime * 3f
                );
            }
        }
        
        public static void SharedPowerUp(int amount, float duration)
        {
            currentPower += amount;
            powerTimer = Mathf.Max(powerTimer, duration);
            Debug.Log($"<color=green>[PhysicsBall] 공격력 증가: {currentPower}, 시간: {powerTimer}초</color>");
        }
        
        public void PowerUp(int amount, float duration)
        {
            SharedPowerUp(amount, duration);
        }
        #endregion
        
        #region Utility Methods
        private void SetBallPositionAbovePlank()
        {
            if (plank == null) return;
            
            // 플랭크 위 위치 계산
            Vector3 newPosition = BallPositionUtility.GetLaunchPosition(plank, objectCollider, transform);
            transform.position = newPosition;
            
            // 속도 초기화
            if (rb != null) {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }
        
        private float GetPlankRelativeSpawnY()
        {
            if (plank == null) return transform.position.y;
            return BallPositionUtility.GetLaunchPosition(plank, objectCollider, transform).y;
        }
        
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, $"State: {CurrentState}");
            
            if (CurrentState == EBallState.Ready) {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, transform.position + (Vector3)launchDirection * 2f);
            }
            #endif
        }
        #endregion
    }
}
