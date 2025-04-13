using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Netcode;

namespace Unity.Assets.Scripts.Objects
{
    /// <summary>
    /// 물리 기반 게임 오브젝트의 공통 속성과 기능을 정의하는 기본 클래스
    /// 모든 물리 객체(Ball, Plank, Brick 등)는 이 클래스를 상속받아 구현
    /// </summary>
    public class PhysicsObject : BaseObject
    {
        #region 공통 컴포넌트 및 속성
        [Header("기본 컴포넌트")]
        [SerializeField] protected Rigidbody2D rb; // Rigidbody2D 참조 복원
        [SerializeField] protected Collider2D objectCollider; // 이전 이름 collider2D 또는 objectCollider 유지 (objectCollider로 유지)
        [SerializeField] protected Renderer objectRenderer;
        
        [Header("충돌 속성")]
        [SerializeField] protected PhysicsMaterial2D physicsMaterial;   // 물리 재질
        [SerializeField] protected bool isTrigger = false;              // 트리거 여부
        [SerializeField] protected float bounciness = 0.7f;             // 탄성
        [SerializeField] protected float friction = 0.4f;               // 마찰
        
        // 객체 상태
        protected bool isInitialized = false;     // 초기화 여부
        protected bool isMovable = true;          // 이동 가능 여부
        protected bool isVisible = true;          // 가시성 여부
        
        // 이전 상태 기록 (보간, 변화 감지 등에 사용)
        protected Vector3 previousPosition;
        protected float previousRotation;
        protected Vector2 previousVelocity;
        #endregion
        
        #region 유니티 생명주기 메서드
        protected override void Awake()
        {
            base.Awake(); // 부모 Awake 호출

            // Rigidbody, Collider, Renderer 컴포넌트 가져오기
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (objectCollider == null) objectCollider = GetComponent<Collider2D>();
            if (objectRenderer == null) objectRenderer = GetComponent<Renderer>();

            // Rigidbody 누락 경고 (usePhysics 필드는 제거됨)
            if (rb == null)
            {
                 Debug.LogWarning($"[{name}] PhysicsObject: Rigidbody2D 컴포넌트가 없습니다. 물리 기능을 사용하려면 추가해야 합니다.", this);
            }

            // 초기 상태 기록 (rb 직접 사용)
            previousPosition = transform.position;
            previousRotation = transform.rotation.eulerAngles.z;
            if (rb != null)
                previousVelocity = rb.linearVelocity;
        }
        
        protected virtual void Start()
        {
            InitializePhysics(); // 이름 InitializePhysics 로 복원 또는 InitializeSettings 유지 (InitializePhysics 사용)
        }
        
        protected virtual void OnEnable()
        {
            if (isInitialized)
            {
                // 활성화 시 수행할 작업 (예: 물리 재질 재설정)
                ApplyPhysicsSettings(); // 이름 ApplyPhysicsSettings 로 복원 또는 ApplyColliderSettings 유지 (ApplyPhysicsSettings 사용)
            }
        }
        
        public virtual void Update()
        {
            UpdateState();
        }
        
        protected virtual void FixedUpdate()
        {
            // 서버 또는 스폰되지 않은 객체일 때 물리 업데이트 수행
            if (IsServer || !IsSpawned)
            {
                UpdatePhysics();
            }
        }
        #endregion
        
        #region 네트워크 메서드
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
        }
        
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
        }
        #endregion
        
        #region 초기화 및 설정
        /// <summary>
        /// 물리 및 콜라이더 설정 초기화
        /// </summary>
        protected virtual void InitializePhysics() // 이름 복원
        {
            // Rigidbody 설정 적용 로직 제거 (인스펙터에서 설정 가정)
            ApplyPhysicsSettings(); // 콜라이더 설정 적용
            isInitialized = true;
        }
        
        /// <summary>
        /// 콜라이더 및 물리 재질 설정 적용
        /// </summary>
        protected virtual void ApplyPhysicsSettings() // 이름 복원 및 내용 수정
        {
            // Rigidbody 설정 적용 로직 제거
            // rb.isKinematic = isKinematic;
            // rb.mass = mass;
            // ... 등등 제거

            // 콜라이더 및 물리 재질 설정은 유지
            if (objectCollider != null)
            {
                if (physicsMaterial == null)
                {
                    physicsMaterial = new PhysicsMaterial2D($"{name}_PhysicsMaterial");
                    physicsMaterial.bounciness = bounciness;
                    physicsMaterial.friction = friction;
                }
                objectCollider.sharedMaterial = physicsMaterial;
                objectCollider.isTrigger = isTrigger;
            }
        }
        
        /// <summary>
        /// 물리 업데이트 로직 (하위 클래스에서 구현)
        /// </summary>
        protected virtual void UpdatePhysics()
        {
            // 자식 클래스에서 오버라이드하여 사용
        }
        
        /// <summary>
        /// 상태 업데이트 및 이전 상태 기록 (rb 직접 사용)
        /// </summary>
        protected virtual void UpdateState()
        {
            if (Time.frameCount % 5 == 0)
            {
                previousPosition = transform.position;
                previousRotation = transform.rotation.eulerAngles.z;
                if (rb != null && !rb.isKinematic) // rb 직접 사용
                    previousVelocity = rb.linearVelocity;
            }
        }
        #endregion
        
        #region 공통 기능 메서드 (Rigidbody 직접 사용)
        /// <summary>
        /// 객체에 힘 적용 (rb 직접 사용)
        /// </summary>
        public virtual void ApplyForce(Vector2 force, ForceMode2D forceMode = ForceMode2D.Force)
        {
            if (rb == null) return;

            // 서버이거나 스폰되지 않은 경우에만 물리력 적용
            if (IsServer || !IsSpawned)
            {
                rb.AddForce(force, forceMode);
            }
        }
        
        /// <summary>
        /// 객체에 토크 적용 (rb 직접 사용)
        /// </summary>
        public virtual void ApplyTorque(float torque, ForceMode2D forceMode = ForceMode2D.Force)
        {
             if (rb == null) return;

            // 서버이거나 스폰되지 않은 경우에만 토크 적용
            if (IsServer || !IsSpawned)
            {
                 rb.AddTorque(torque, forceMode);
            }
        }
        
        /// <summary>
        /// 객체 발사 (rb 직접 사용)
        /// </summary>
        public virtual void Launch(Vector2 direction, float force, ForceMode2D forceMode = ForceMode2D.Impulse)
        {
             if (rb == null) return;

            // 서버이거나 스폰되지 않은 경우에만 발사 로직 수행
            if (IsServer || !IsSpawned)
            {
                 rb.linearVelocity = Vector2.zero; // 기존 속도 초기화
                 rb.AddForce(direction.normalized * force, forceMode);
            }
        }
        
        /// <summary>
        /// 객체 이동 (Kinematic Body 또는 Transform 직접 제어 - rb 직접 사용)
        /// </summary>
        public virtual void Move(Vector3 position)
        {
            if (rb != null && rb.isKinematic) // rb 직접 사용
            {
                rb.MovePosition(position);
            }
            else
            {
                transform.position = position;
                 // if (IsServer) transform.position = position;
            }
        }
        
        /// <summary>
        /// 객체 회전 (Kinematic Body 또는 Transform 직접 제어 - rb 직접 사용)
        /// </summary>
        public virtual void Rotate(float zRotation)
        {
             if (rb != null && rb.isKinematic) // rb 직접 사용
            {
                 rb.MoveRotation(zRotation);
            }
            else
            {
                transform.rotation = Quaternion.Euler(0, 0, zRotation);
                // if (IsServer) transform.rotation = Quaternion.Euler(0, 0, zRotation);
            }
        }
        
        /// <summary>
        /// 객체 가시성 설정
        /// </summary>
        public virtual void SetVisibility(bool visible)
        {
            if (objectRenderer != null)
            {
                objectRenderer.enabled = visible;
                isVisible = visible;
            }
        }
        
        /// <summary>
        /// 객체 이동 가능 여부 설정 (Kinematic 설정 사용 - rb 직접 사용)
        /// </summary>
        public virtual void SetMovable(bool movable)
        {
             if (rb != null)
            {
                rb.isKinematic = !movable; // movable == true -> non-kinematic
                isMovable = movable;
                if (rb.isKinematic) // Kinematic으로 바뀔 때 속도 초기화
                {
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }
            }
        }
        
        /// <summary>
        /// 객체 위치 설정 (보간 사용)
        /// </summary>
        public virtual void MoveToPosition(Vector3 targetPosition, float speed)
        {
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
                return;
                
            // 보간을 사용하여 부드럽게 이동
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
        }
        
        /// <summary>
        /// 지정된 대상을 향해 바라보기
        /// </summary>
        public virtual void LookAt(Vector3 targetPosition)
        {
            Vector2 direction = (Vector2)(targetPosition - transform.position);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            Rotate(angle);
        }
        #endregion
        
        #region 유틸리티 메서드
        /// <summary>
        /// 지정된 레이어의 가장 가까운 대상 찾기
        /// </summary>
        public virtual GameObject FindNearestTarget(float radius, LayerMask layerMask)
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, radius, layerMask);
            
            GameObject nearestTarget = null;
            float minDistanceSqr = Mathf.Infinity;
            
            foreach (Collider2D hitCollider in colliders)
            {
                // 자기 자신은 제외
                if (hitCollider.gameObject == gameObject) continue;
                
                Vector3 directionToTarget = hitCollider.transform.position - transform.position;
                float dSqrToTarget = directionToTarget.sqrMagnitude;
                if (dSqrToTarget < minDistanceSqr)
                {
                    minDistanceSqr = dSqrToTarget;
                    nearestTarget = hitCollider.gameObject;
                }
            }
            
            return nearestTarget;
        }
        
        /// <summary>
        /// 중첩 감지 및 해결
        /// </summary>
        protected virtual bool ResolveOverlap(LayerMask collisionLayers, float pushForce = 1f)
        {
            if (objectCollider == null) return false;
            
            Collider2D[] overlaps = new Collider2D[5];
            int count = Physics2D.OverlapCollider(objectCollider, new ContactFilter2D { layerMask = collisionLayers, useLayerMask = true }, overlaps);
            
            if (count > 0)
            {
                bool resolved = false;
                for (int i = 0; i < count; i++)
                {
                    if (overlaps[i] == objectCollider) continue;
                    
                    Vector2 direction = (transform.position - overlaps[i].transform.position).normalized;
                    if (rb != null && !rb.isKinematic) // rb 직접 사용
                    {
                        rb.AddForce(direction * pushForce, ForceMode2D.Impulse);
                        resolved = true;
                    }
                    else
                    {
                        // Kinematic이거나 Rigidbody 없으면 Transform으로 밀어내기 시도 (네트워크 주의)
                        // transform.position += (Vector3)direction * 0.01f;
                    }
                }
                return resolved;
            }
            
            return false;
        }
        
        /// <summary>
        /// 디버그 정보 표시 (UNITY_EDITOR에서만 활성화)
        /// </summary>
        protected virtual void OnDrawGizmos()
        {
            #if UNITY_EDITOR
            if (objectCollider != null)
            {
                Gizmos.color = new Color(0, 1, 0, 0.3f);
                if (objectCollider is CircleCollider2D)
                {
                    CircleCollider2D circle = objectCollider as CircleCollider2D;
                    Gizmos.DrawSphere(transform.position + (Vector3)circle.offset, circle.radius);
                }
                else if (objectCollider is BoxCollider2D)
                {
                    BoxCollider2D box = objectCollider as BoxCollider2D;
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawCube(box.offset, box.size);
                }
            }
            #endif
        }
        #endregion
        
        #region 충돌 처리
        protected virtual void OnCollisionEnter2D(Collision2D collision)
        {
            // 충돌 처리는 서버 또는 스폰되지 않은 경우에만 위임
            if (IsServer || !IsSpawned)
            {
                 HandleCollision(collision);
            }
        }
        
        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            // 트리거 처리는 서버 또는 스폰되지 않은 경우에만 위임
             if (IsServer || !IsSpawned)
            {
                HandleTrigger(other);
            }
        }
        
        protected virtual void HandleCollision(Collision2D collision){}
        
        protected virtual void HandleTrigger(Collider2D other){}        
        #endregion
    }
}