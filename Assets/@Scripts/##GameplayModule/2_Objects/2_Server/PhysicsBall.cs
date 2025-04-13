using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using VContainer;
using static Define;

namespace Unity.Assets.Scripts.Objects
{
    public class PhysicsBall : PhysicsObject
    {
        #region Ball Properties
        [Header("Ball Properties")]
        [SerializeField] private int ballCount = 1;
        [SerializeField] private float launchForce = 1f;
        [SerializeField] private bool canShoot = true;
        [SerializeField] private float bounceImpactThreshold = 2f; // 튕김 판정 임계값
        
        [Header("Ball Visuals")]
        [SerializeField] private GameObject ballModel;
        [SerializeField] private Material[] ballMaterials;
        [SerializeField] private GameObject ballNumberText;
        private TextMesh numberOfBalls;
        
        [Header("Prediction Line")]
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private LayerMask predictionLayerMask;
        [SerializeField] private float maxRayDistance = 15f;
        [SerializeField] private float reflectionLineLength = 2f;
        private bool showPredictionLine = false;
        
        [Header("Plank Interaction")]
        [SerializeField] private float maxBounceAngle = 75f;
        [SerializeField] private float plankMoveThreshold = 0.1f;
        [SerializeField] private Vector2 launchDirection = Vector2.up;
        [SerializeField] private PhysicsPlank plank; // Assign the GameObject with PhysicsPlank script in Inspector
        private float previousPlankX;
        private bool isReadyForPlankLaunch = false;
        private bool _firstCheckAfterReady = false; // 준비 후 첫 번째 체크 여부 플래그
        private const float SPAWN_OFFSET_Y = 0.05f;
        
        // Ball Behavior
        private bool multipleBalls = false;
        private int ballsShooted = 0;
        private float shootTimer = 0;
        private bool waveEnd = false;
        private float ballStuckTimer = 0;
        private float ballStuckYPos = 0;
        private bool _needsPositionReset = false; // LateUpdate에서 위치 재설정 필요 여부 플래그
        #endregion
        
        #region Network & System Properties
        // 네트워크 변수
        private NetworkVariable<int> _syncedBallCount = new NetworkVariable<int>(1);
        private NetworkVariable<bool> _syncedCanShoot = new NetworkVariable<bool>(true);
        
        // 시스템 변수
        [Inject] private ObjectManager _objectManager;
        private Camera mainCamera;
        private GameObject[] activeBalls;
        #endregion
        
        #region ITargetable Implementation
        // ITargetable 인터페이스 구현
 
        
        #endregion
        

        private Collider2D _plankCollider;
        // private ObjectPlacement _objectPlacement;

        #region Override Methods
        // Awake 메서드 제거
        // protected void Awake()
        // {
        //     // ...
        // }

        // LaunchInDirection 메서드는 rb를 직접 사용하므로 유지
        public void LaunchInDirection(Vector2 direction, float launchForce = 10f)
        {
                rb.linearVelocity = Vector2.zero;
    
                rb.AddForce(direction.normalized * launchForce, ForceMode2D.Impulse);
        }

        public override bool Init()
        {
            if (!base.Init()) // base.Init() 호출 및 결과 확인
                return false;

            // ObjectType 설정 (EObjectType.Ball 이 Define에 없으므로 None으로 임시 설정)
            ObjectType = EObjectType.None; // Define.cs 에 Ball 추가 필요

            lineRenderer = GetComponent<LineRenderer>();

            if (objectCollider == null)
            {
                Debug.LogError("[Ball] Init: Ball Collider (objectCollider) is null!");
                return false;
            }

            // 플랭크 콜라이더는 여전히 필요하면 가져옵니다.
            if (plank != null)
                _plankCollider = plank.GetComponent<BoxCollider2D>();
            else
                Debug.LogWarning("[Ball] Init: Plank is not assigned.");

            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }

            mainCamera = Camera.main;
            Debug.Log($"<color=green>[Ball] Init: Ball 컴포넌트 초기화됨, 볼 개수: {ballCount}</color>");
            return true;
        }
        
        #region Unity Lifecycle Methods
        // Start 메서드 추가
        protected virtual void Start() // virtual로 선언하여 혹시 모를 자식 클래스 오버라이드 허용
        {

            SetBallPositionAbovePlank();

            isReadyForPlankLaunch = true; // 발사 준비 완료


            _firstCheckAfterReady = true; // 첫 프레임 델타 체크 방지 플래그 활성화
            previousPlankX = (plank != null) ? plank.transform.position.x : transform.position.x; // 초기 X 위치 저장
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                _syncedBallCount.Value = ballCount;
                _syncedCanShoot.Value = canShoot;
            }

            if (IsClient)
            {
                _syncedBallCount.OnValueChanged += OnBallCountChanged;
                _syncedCanShoot.OnValueChanged += OnCanShootChanged;
            }

        }
        #endregion
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            
            if (IsClient)
            {
                _syncedBallCount.OnValueChanged -= OnBallCountChanged;
                _syncedCanShoot.OnValueChanged -= OnCanShootChanged;
            }
        }
        
        public override void Update()
        {
            base.Update();
            
            // 게임 상태 체크
            // if (Vars.canContinue && ballNumberText != null && numberOfBalls != null)
            // {
            //     // 공 개수 텍스트 위치 업데이트
            //     UpdateBallNumberTextPosition();
            // }
            
            // 플랭크 이동 감지 및 자동 발사 로직
            HandlePlankLaunch();

            HandleMultipleBallLaunch();
            
            // 웨이브 상태 체크 및 리셋 - 천천히 이동 로직 제거
            // HandleWaveReset(); // 천천히 이동 로직 제거 (BottomBoundary에서 바로 처리)
        }
        
        protected override void FixedUpdate()
        {
            base.FixedUpdate(); // 부모 FixedUpdate 호출 (서버 측 UpdatePhysics 호출)
            
            // 볼의 물리적 움직임 업데이트 (기존 로직 유지)
            if (IsServer && !canShoot && rb != null && rb.linearVelocity.magnitude > 0)
            {
                if (rb.linearVelocity.magnitude < 5f)
                {
                    rb.linearVelocity = rb.linearVelocity.normalized * 10f;
                }
            }
        }
        
  
        
        // public override void OnDamaged(BaseObject attacker, SkillBase skill)
        // {
            // base.OnDamaged(attacker, skill);
            
            // 공 데미지 처리 (필요시 구현)
        // }
        #endregion
        
        #region Ball Specific Methods
        // 볼 발사 메서드
        public void LaunchBall(Vector2 direction)
        {

   
            if (rb != null)
            {
                Launch(direction.normalized, launchForce, ForceMode2D.Impulse); // PhysicsObject의 Launch 메서드 활용

                canShoot = false;
                isReadyForPlankLaunch = false; // <-- The change
                _firstCheckAfterReady = false;

                if (IsServer && IsSpawned)
                {
                    _syncedCanShoot.Value = false;
                }

                // 여러 개 공 발사 준비
                if (ballCount > 1)
                {
                    multipleBalls = true;
                    ballsShooted = 0;
                    shootTimer = 0f;
                }
                else
                {
                    multipleBalls = false;
                }
            }
        }
        
        // 볼 개수 설정 메서드
        public void SetBallCount(int count)
        {
             // 서버 또는 스폰되지 않은 경우에만 개수 설정 가능
            if (IsServer || !IsSpawned)
            {
                ballCount = Mathf.Max(1, count);
                 // 스폰된 서버 객체일 때만 NetworkVariable 동기화
                if(IsServer && IsSpawned)
                {
                     _syncedBallCount.Value = ballCount;
                }
            }
        }
        

        

        // 플랭크 위로 볼 위치 설정
        private void SetBallPositionAbovePlank()
        {
            if (plank != null)
            {
                // Debug.Log($"[SetBallPositionAbovePlank Frame {Time.frameCount}] Function Called.");
                if (_plankCollider != null)
                {
                    // 플랭크의 현재 위치 및 크기 정보 가져오기
                    Vector3 currentPlankPosition = plank.transform.position;
                    Vector3 plankScale = plank.transform.localScale;
                    Debug.Log($"[SetBallPositionAbovePlank Frame {Time.frameCount}] Current Plank Position = {currentPlankPosition}, Scale = {plankScale}");

                    // X 좌표: 플랭크의 정중앙 위치 (위치는 이미 중앙이지만 명시적으로 표현)
                    float centerX = currentPlankPosition.x;
                    previousPlankX = centerX;  // 발사 로직용 저장

                    // Y 좌표 계산
                    float plankHalfHeight = (plankScale.y * 0.5f);
                    float plankTopY = currentPlankPosition.y + plankHalfHeight;
                    float ballRadiusY = (transform.localScale.y * 0.5f);
                    float spawnY = plankTopY + ballRadiusY + SPAWN_OFFSET_Y;

                    // 최종 위치 설정 (X: 플랭크 중앙, Y: 플랭크 상단 + 볼 반지름 + 오프셋, Z: 플랭크와 동일)
                    Vector3 finalPos = new Vector3(centerX, spawnY, currentPlankPosition.z);
                    transform.position = finalPos;

                    // 속도 초기화 (추가)
                    if (rb != null)
                    {
                        rb.linearVelocity = Vector2.zero;
                        rb.angularVelocity = 0f;
                    }

                    Debug.Log($"[SetBallPositionAbovePlank Frame {Time.frameCount}] Ball position set to: {finalPos}");
                }
                else { Debug.LogWarning("[Ball SetBallPositionAbovePlank] Plank Collider is null"); }
            }
            else { Debug.LogWarning("[Ball SetBallPositionAbovePlank] Plank reference is null"); }
        }
        
   
        
        // 벽돌/벽 충돌 처리
        private void HandleBrickOrWallCollision(Collision2D collision)
        {
            if (rb == null) return;
            
            // 충돌한 표면의 법선 벡터 가져오기
            Vector2 normal = collision.contacts[0].normal;
            
            // 현재 속도에 기반한 반사 방향 계산
            Vector2 reflectDir = Vector2.Reflect(rb.linearVelocity.normalized, normal);
            
            // 현재 속력 유지
            float currentSpeed = rb.linearVelocity.magnitude;
            
            // 속력이 너무 느리면 기본 속도로 설정 (Stuck 방지)
            if (currentSpeed < 5f) currentSpeed = 10f;
            
            if (collision.gameObject.CompareTag("Brick"))
            {
                // 벽돌일 경우 약간의 랜덤 방향 추가
                float randomAngle = UnityEngine.Random.Range(-5f, 5f) * Mathf.Deg2Rad;
                reflectDir = new Vector2(
                    reflectDir.x * Mathf.Cos(randomAngle) - reflectDir.y * Mathf.Sin(randomAngle),
                    reflectDir.x * Mathf.Sin(randomAngle) + reflectDir.y * Mathf.Cos(randomAngle)
                );
                
                // 벽돌에 데미지 적용 (필요시)
                Brick brick = collision.gameObject.GetComponent<Brick>();
                if (brick != null)
                {
                    // brick.OnHit();
                }
            }
            
            // 새 속도 적용
            rb.linearVelocity = reflectDir.normalized * currentSpeed;
        }
        
        // 플랭크 이동 감지 및 자동 발사 처리
        private void HandlePlankLaunch()
        {
            if (canShoot && plank != null && CommonVars.canContinue) // 기본 조건
            {
                if (isReadyForPlankLaunch) // 발사 준비 되었는가? (Check 1)
                {
                    float currentPlankX = plank.transform.position.x;

                    // 발사 준비 상태일 때 항상 공의 위치를 플랭크 위로 유지
                    if (Mathf.Abs(currentPlankX - transform.position.x) > 0.01f)
                    {
                        SetBallPositionAbovePlank();
                    }

                    if (_firstCheckAfterReady) // 첫 번째 체크인가? (Check 2)
                    {
                        previousPlankX = currentPlankX;
                        _firstCheckAfterReady = false; // 첫 번째 체크 완료
                        return; // 첫 프레임은 델타 체크 안 함
                    }

                    // 두 번째 프레임 이후: 이동량(Delta) 체크
                    float deltaX = Mathf.Abs(currentPlankX - previousPlankX);

                    if (deltaX > plankMoveThreshold) // 이동량이 임계값보다 큰가? (Check 3)
                    {
                        LaunchBall(launchDirection.normalized); // 발사! (내부에서 isReady=false 설정됨)
                    }

                    previousPlankX = currentPlankX; // 다음 프레임 비교를 위해 현재 X 저장
                }
            }
        }
        
        // 멀티 볼 발사 처리
        private void HandleMultipleBallLaunch()
        {
            if (!multipleBalls) return;
            
            shootTimer += Time.deltaTime;
            if (shootTimer >= 0.1f)
            {
                shootTimer = 0;
                if (ballCount > ballsShooted + 1)
                {
                    // 추가 볼 찾기 및 발사
                    activeBalls = GameObject.FindGameObjectsWithTag("ball");
                    if (ballsShooted < activeBalls.Length && activeBalls[ballsShooted] != null)
                    {
                        PhysicsBall ballComponent = activeBalls[ballsShooted].GetComponent<PhysicsBall>();
                        if (ballComponent != null && ballComponent != this)
                        {
                            ballComponent.LaunchBall(launchDirection.normalized);
                        }
                        else
                        {
                            Rigidbody2D ballRb = activeBalls[ballsShooted].GetComponent<Rigidbody2D>();
                            if (ballRb != null)
                            {
                                ballRb.linearVelocity = Vector2.zero;
                                ballRb.AddForce(launchDirection.normalized * launchForce);
                            }
                        }
                        ballsShooted++;
                    }
                }
                else
                {
                    multipleBalls = false;
                    ballsShooted = 0;
                }
            }
        }
        
        // 웨이브 리셋 처리
        private void HandleWaveReset()
        {

        }
        
        // 웨이브 리셋
        private void ResetWave()
        {
            CommonVars.ballsReachedDistance = 0;
            CommonVars.canContinue = true;
            CommonVars.startMovingTowardsMainBall = false;
            CommonVars.firstBallHitBottomCollider = false;
            CommonVars.newWaveOfBricks = true;
        }
        
        // 플랭크 기준 스폰 Y 좌표 계산
        private float GetPlankRelativeSpawnY()
        {
            if (plank != null && _plankCollider != null)
            {
                float plankHalfHeight = (plank.transform.localScale.y * 0.5f);
                float plankTopY = plank.transform.position.y + plankHalfHeight;
                float ballRadiusY = (transform.localScale.y * 0.5f);
                
                return plankTopY + ballRadiusY + SPAWN_OFFSET_Y;
            }
            
            Debug.LogWarning("[Ball] GetPlankRelativeSpawnY: Cannot calculate position, returning current Y.");
            return transform.position.y;
        }
        #endregion
        
        #region Network Callbacks
        // 네트워크 볼 개수 변경 콜백
        private void OnBallCountChanged(int previousValue, int newValue){ ballCount = newValue;}
        
        
        // 네트워크 발사 가능 상태 변경 콜백
        private void OnCanShootChanged(bool previousValue, bool newValue)
        {
            canShoot = newValue;

            // 발사 가능 상태가 되면 볼 위치 재설정
            if (canShoot && !previousValue)
            {
                // SetBallPositionAbovePlank(); // 직접 호출 제거
                 _needsPositionReset = true; // 플래그 설정
                 Debug.Log($"[OnCanShootChanged Frame {Time.frameCount}] Setting _needsPositionReset = true."); // 플래그 설정 로그 추가

                isReadyForPlankLaunch = true;
                _firstCheckAfterReady = true;
            }
        }
        #endregion
        

        protected override void HandleCollision(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Plank"))
            {
                if (canShoot || isReadyForPlankLaunch)
                {
                     // Debug.LogWarning($"[Ball HandleCollision Frame {Time.frameCount}] IGNORING initial plank collision while ready. (canShoot={canShoot}, isReady={isReadyForPlankLaunch})"); // 로그 제거
                     return;
                }

                // 공이 이미 발사된 상태에서는 정상적으로 플랭크와 충돌하여 튕깁니다.
                PhysicsPlank plankComponent = collision.gameObject.GetComponent<PhysicsPlank>();
                if (plankComponent != null)
                {
                    Vector2 bounceVelocity = plankComponent.CalculateBounceVelocity(rb, collision);
                    if (rb != null)
                    {
                         rb.linearVelocity = bounceVelocity;
                    }
                }
            }
            else if (collision.gameObject.CompareTag("Brick") || collision.gameObject.CompareTag("Wall"))
            {
                // 벽돌/벽 충돌은 공이 발사된 후에만 처리
                if (!canShoot && !isReadyForPlankLaunch && rb.linearVelocity.magnitude > 0.1f)
                {
                     HandleBrickOrWallCollision(collision);
                }
            }
        }



        #region Triggers
        // 트리거 충돌 이벤트 (바닥 경계, 아이템 등)
        protected override void HandleTrigger(Collider2D other)
        {
            if (other.gameObject.CompareTag("BottomBoundary"))
            {
                Debug.Log($"[Ball HandleTrigger Frame {Time.frameCount}] Hit BottomBoundary. FORCING immediate position reset.");
                
                // 물리 완전 초기화
                if (rb != null)
                {
                    // 물리엔진 일시 정지 (위치 강제 변경을 위해)
                    bool wasKinematic = rb.isKinematic;
                    rb.isKinematic = true;
                    
                    // 모든 물리 상태 초기화
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                    
                    // 즉시 플랭크 중앙으로 강제 이동
                    if (plank != null)
                    {
                        // 플랭크 위치 가져오기
                        Vector3 plankPos = plank.transform.position;
                        Vector3 plankScale = plank.transform.localScale;
                        
                        // 플랭크 중앙 위 위치 계산
                        float plankHalfHeight = plankScale.y * 0.5f;
                        float plankTopY = plankPos.y + plankHalfHeight;
                        float ballRadiusY = transform.localScale.y * 0.5f;
                        float spawnY = plankTopY + ballRadiusY + SPAWN_OFFSET_Y;
                        
                        // 강제로 위치 설정
                        transform.position = new Vector3(plankPos.x, spawnY, plankPos.z);
                        Debug.Log($"[Ball FORCE RESET] 위치 강제 변경: {transform.position}, 플랭크 위치: {plankPos}");
                        
                        // 이전 플랭크 위치 업데이트 (발사 로직용)
                        previousPlankX = plankPos.x;
                    }
                    else
                    {
                        Debug.LogError("[Ball] HandleTrigger: plank reference is null!");
                    }
                    
                    // 물리엔진 상태 복원
                    rb.isKinematic = wasKinematic;
                }
                
                // 상태 초기화
                canShoot = true;
                isReadyForPlankLaunch = true;
                _firstCheckAfterReady = true;

                // 공통 변수 리셋
                ResetWave();
                
                // 서버 오브젝트일 경우 네트워크 변수도 업데이트
                if (IsServer && IsSpawned)
                {
                    _syncedCanShoot.Value = true;
                }
            }
            else if (other.gameObject.CompareTag("star"))
            {
                if (other.gameObject != null)
                {
                    Destroy(other.gameObject);
                }
            }
            else if (other.gameObject.CompareTag("newBall"))
            {
                 // 서버 또는 스폰되지 않은 경우에만 볼 개수 증가 및 동기화
                 if (IsServer || !IsSpawned)
                {
                    ballCount++;
                     // 스폰된 서버 객체일 때만 NetworkVariable 동기화
                    if(IsServer && IsSpawned)
                    {
                        _syncedBallCount.Value = ballCount;
                    }
                }
                if (other.gameObject != null)
                {
                    Destroy(other.gameObject);
                }
            }
        }
        #endregion

        protected override void OnDrawGizmos() // PhysicsObject 에 이미 있다면 override 키워드 사용
        {
            base.OnDrawGizmos(); // 부모 기즈모 호출 (필요시)

#if UNITY_EDITOR
            if (plank != null && _plankCollider != null)
            {
                 // 스폰 위치 계산 (SetBallPositionAbovePlank 로직 재사용 또는 복사)
                 float plankHalfHeight = (plank.transform.localScale.y * 0.5f);
                 float plankTopY = plank.transform.position.y + plankHalfHeight;
                 float ballRadiusY = (transform.localScale.y * 0.5f);
                 float spawnY = plankTopY + ballRadiusY + SPAWN_OFFSET_Y;
                 float spawnX = plank.transform.position.x; // 현재 플랭크 X 기준

                 Vector3 spawnPos = new Vector3(spawnX, spawnY, plank.transform.position.z);

                 // 계산된 스폰 위치에 와이어 스피어 그리기
                 Gizmos.color = Color.yellow;
                 Gizmos.DrawWireSphere(spawnPos, ballRadiusY); // 공 반지름 크기로 그림
            }
#endif
        }

        protected virtual void LateUpdate() // virtual 추가
        {
            if (_needsPositionReset)
            {
                Debug.Log($"[Ball LateUpdate Frame {Time.frameCount}] Resetting position based on flag.");
                SetBallPositionAbovePlank(); // LateUpdate에서 위치 설정
                _needsPositionReset = false; // 플래그 해제
            }
        }
    }
}