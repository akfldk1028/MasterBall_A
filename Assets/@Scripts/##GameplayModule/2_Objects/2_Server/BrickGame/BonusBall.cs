using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Services.Matchmaker.Models;

namespace Unity.Assets.Scripts.Objects
{
    public class BonusBall : PhysicsObject
    {
        [Header("BonusBall Settings")]
        [SerializeField] private int ballsToAdd = 1; // 추가할 공의 개수
        [SerializeField] private GameObject ballPrefab; // 생성할 공 프리팹
        [SerializeField] private AudioClip collectSound; // 효과음
        [SerializeField] private GameObject collectEffect; // 수집 이펙트
        [SerializeField] private float launchDelay = 0.1f; // 발사 간격
        [SerializeField] private float launchForce = 10f; // 발사 힘
        [SerializeField] private Vector2 baseDirection = Vector2.up; // 기본 발사 방향
        
        private bool isCollected = false;
        private PhysicsPlank plank; // 플랭크 참조
        
        private void Start()
        {
    
            
            // 플랭크 찾기
            plank = FindObjectOfType<PhysicsPlank>();
            if (plank == null)
            {
                Debug.LogWarning("플랭크를 찾을 수 없습니다. 보너스 공 생성에 문제가 있을 수 있습니다.");
            }
        }
        
        // // 충돌 처리 (PhysicsObject 상속)
        // protected override void HandleCollision(Collision2D collision)
        // {
        //     base.HandleCollision(collision);
            
        //     // !!!중요!!! - 객체의 유형 로깅 (디버깅용)
        //     Debug.Log($"<color=yellow>[BonusBall] 충돌 감지: {collision.gameObject.name}, Tag: {collision.gameObject.tag}</color>");
            
        //     // isCollected 체크가 필요함
        //     if (isCollected) return;
            
        //     // 공과 충돌 감지 - 여기서 태그 또는 컴포넌트로 확인
        //     if (collision.gameObject.CompareTag("Ball") || 
        //         collision.gameObject.GetComponent<PhysicsBall>() != null)
        //     {
        //         HandleTriggerCollision(collision.gameObject);
        //     }
        // }
        
        // OnCollisionEnter2D 직접 구현 (PhysicsObject의 OnCollisionEnter2D보다 먼저 실행됨)
        protected override void OnTriggerEnter2D(Collider2D collision)
        {
            // 디버그용 로그 - 모든 충돌 출력
            Debug.Log($"<color=cyan>[BonusBall] OnCollisionEnter2D: {collision.gameObject.name}, Tag: {collision.gameObject.tag}</color>");
            
            // isCollected 체크가 필요함
            if (isCollected) return;
            
            // 공과 충돌 감지 - 여기서 태그 또는 컴포넌트로 확인
            if (collision.gameObject.CompareTag("Ball") || 
                collision.gameObject.GetComponent<PhysicsBall>() != null)
            {
                HandleTriggerCollision(collision.gameObject);
            }
        }
        
        // 공과 충돌 시 처리
        private void HandleTriggerCollision(GameObject ballObject)
        {
            // 이미 처리됐으면 중복 처리 방지
            if (isCollected) return;
            
            isCollected = true;
            
            Debug.Log($"<color=green>[{gameObject.name}] BonusBall 충돌 처리 시작! 공 {ballsToAdd}개 추가 발사 예정</color>");
            
            // 효과음 재생
            if (collectSound != null)
            {
                AudioSource.PlayClipAtPoint(collectSound, transform.position);
            }
            
            // 이펙트 생성
            if (collectEffect != null)
            {
                Instantiate(collectEffect, transform.position, Quaternion.identity);
            }
            
            // 통계 업데이트
            PlayerPrefs.SetInt("numberOfBallsCollected", PlayerPrefs.GetInt("numberOfBallsCollected") + 1);
            
            // 즉시 추가 공 생성 및 발사
            SpawnAndLaunchNewBalls();
            
            // 오브젝트 제거 - 코루틴으로 약간 지연시켜 처리
            StartCoroutine(DestroyAfterDelay(0.1f));
        }
        // 약간의 지연 후에 오브젝트 제거 (이펙트 및 사운드가 재생될 시간 확보)
        private IEnumerator DestroyAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            Destroy(gameObject);
        }
        
        // 새로운 공 생성 및 발사 메서드
        private void SpawnAndLaunchNewBalls()
        {
            // 서버 환경에서는 NetworkManager를 통해 처리 (멀티플레이어 지원)
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                for (int i = 0; i < ballsToAdd; i++)
                {
                    // 서버에서 네트워크 공 생성 (순차적 지연 발사)
                    StartCoroutine(SpawnBallWithDelay(i * launchDelay, true));
                }
            }
            // 로컬 테스트 또는 싱글플레이어 환경
            else
            {
                for (int i = 0; i < ballsToAdd; i++)
                {
                    // 로컬에서 공 생성 (순차적 지연 발사)
                    StartCoroutine(SpawnBallWithDelay(i * launchDelay, false));
                }
            }
        }
        
        // 지연 발사 코루틴
        private IEnumerator SpawnBallWithDelay(float delay, bool isServer)
        {
            yield return new WaitForSeconds(delay);
            
            // 공 생성 및 발사
            if (isServer)
            {
                SpawnBallOnServer();
            }
            else
            {
                SpawnBallLocally();
            }
        }
        
        // 서버에서 네트워크 공 생성
        private void SpawnBallOnServer()
        {
            // 저장된 PhysicsBall 프리팹 사용, 또는 프리팹을 참조로 가지고 있어야 함
            if (ballPrefab == null)
            {
                // 레퍼런스가 없으면 기존 공에서 프리팹 정보 얻기 시도
                var existingBall = FindObjectOfType<PhysicsBall>();
                if (existingBall != null)
                {
                    CreateAndLaunchBall(existingBall.gameObject, true);
                }
                else
                {
                    Debug.LogError("기존 공을 찾을 수 없어 새 공을 생성할 수 없습니다.");
                }
            }
            else
            {
                // 프리팹이 있으면 직접 사용
                CreateAndLaunchBall(ballPrefab, true);
            }
        }
        
        // 로컬에서 공 생성 (싱글플레이어 또는 테스트용)
        private void SpawnBallLocally()
        {
            // 기존 공 찾기
            var existingBall = FindObjectOfType<PhysicsBall>();
            
            if (existingBall != null)
            {
                CreateAndLaunchBall(existingBall.gameObject, false);
            }
            else
            {
                Debug.LogError("기존 공을 찾을 수 없어 새 공을 생성할 수 없습니다.");
            }
        }
        
        // 실제 공 생성 및 발사 로직 (코드 중복 제거)
        private void CreateAndLaunchBall(GameObject ballTemplate, bool isServer)
        {
            // 1. 위치 계산 (공통 유틸리티 사용)
            Vector3 spawnPosition;
            
            // 템플릿 공의 콜라이더 가져오기
            var templateBallCollider = ballTemplate.GetComponent<Collider2D>();
            
            // 공통 유틸리티를 사용하여 위치 계산
            spawnPosition = BallPositionUtility.GetLaunchPosition(plank, templateBallCollider, ballTemplate.transform);
            
            // 2. 공 생성
            GameObject newBallObj = Instantiate(ballTemplate, spawnPosition, Quaternion.identity);
            
            // 태그 설정 확인
            if (newBallObj.tag == "Untagged" || string.IsNullOrEmpty(newBallObj.tag))
            {
                newBallObj.tag = "Ball";
            }
            
            // 3. 네트워크 스폰 (서버인 경우)
            if (isServer && newBallObj.GetComponent<NetworkObject>() != null)
            {
                newBallObj.GetComponent<NetworkObject>().Spawn();
            }
            
            // 4. PhysicsBall 컴포넌트 초기화 및 발사
            PhysicsBall newBall = newBallObj.GetComponent<PhysicsBall>();
            if (newBall != null)
            {
                newBall.Init(); // 초기화
                
                // 중요: 기존 공의 파워업 상태를 새 공에 복사
                PhysicsBall templateBall = ballTemplate.GetComponent<PhysicsBall>();
                if (templateBall != null)
                {
                    newBall.ColorBallByPower();
                }
                    
                // 공통 유틸리티를 사용하여 무작위 방향 생성
                Vector2 launchDir = BallPositionUtility.GetRandomizedLaunchDirection(baseDirection);
                Debug.Log($"보너스 공 발사 방향: {launchDir}");
                Debug.Log($"보너스 공 발사 방향: {launchDir}");
                Debug.Log($"보너스 공 발사 방향: {launchDir}");
                Debug.Log($"보너스 공 발사 방향: {launchDir}");
                Debug.Log($"보너스 공 발사 방향: {launchDir}");
                Debug.Log($"보너스 공 발사 방향: {launchDir}");
                Debug.Log($"보너스 공 발사 방향: {launchDir}");
                Debug.Log($"보너스 공 발사 방향: {launchDir}");
                // 발사
                newBall.LaunchBall(launchDir);
                
                Debug.Log($"<color=yellow>[{gameObject.name}] 보너스 공이 발사되었습니다. 방향: {launchDir}, 공격력: {newBall.AttackPower}</color>");
            }
            else
            {
                Debug.LogError($"생성된 공 오브젝트에 PhysicsBall 컴포넌트가 없습니다: {newBallObj.name}");
            }
        }
    }
}