using UnityEngine;
using System;
using System.Collections;
using TMPro;
using System.Collections.Generic;
using VContainer;
using Unity.Netcode; // Netcode 네임스페이스 추가

public class ReleaseGameManager : MonoBehaviour // NetworkBehaviour 상속 불필요
{
    // --- 총알 발사 관련 변수 추가 ---
    [Header("발사 설정")]
    [SerializeField] private GameObject bulletPrefab; // 총알 프리팹 (NetworkObject 필요)
    [SerializeField] private float bulletSpeed = 20f; // 총알 속도
    // [SerializeField] private float bulletFireInterval = 0.1f; // 더 이상 사용 안 함 (개별 스폰)
    // [SerializeField] private float bulletSpreadAngle = 5f; // 필요 시 발사 로직 내에서 사용
    // -------------------------------

    private bool isGameActive = false;
    private int _latestScore = 0; // 최신 점수 저장
    
    // --- Cannon 참조 변경 ---
    private Cannon[] _cannons; // 캐논 배열 (IsometricGridGenerator에서 가져옴)
    // ----------------------
    
    // 이벤트 정의
    public event Action OnGameStart;
    public event Action OnGamePause;
    public event Action OnGameResume;
    public event Action OnGameOver;
    public event Action OnRowSpawn;
    public event Action OnSummonButtonClicked;
    

    [Inject] private BrickGameManager _brickGameManager;
    // private void Awake()
    // {
       // 초기화 로직
    // }
    
    // private void Start()
    // {
    //     isGameActive = true;
    //     OnGameStart?.Invoke();

    //     // --- BrickGameManager 찾기 ---
    //     _brickGameManager = FindObjectOfType<BrickGameManager>();
    //     if (_brickGameManager != null)
    //     {
    //         _latestScore = _brickGameManager.GetCurrentScore();
    //         _brickGameManager.OnScoreChanged += HandleScoreChange;
    //         Debug.Log($"<color=green>[ReleaseGameManager] Initial Score: {_latestScore}</color> ");
    //     }
    //     else
    //     {
    //          Debug.LogError($"<color=red>[ReleaseGameManager] BrickGameManager를 찾을 수 없습니다!</color> ");
    //     }
        
    //     // --- IsometricGridGenerator에서 캐논 배열 가져오기 ---
    //     if (IsometricGridGenerator.Instance != null)
    //     {
    //         _cannons = IsometricGridGenerator.Instance.GetAllCannons();
    //         Debug.Log($"<color=cyan>[ReleaseGameManager] IsometricGridGenerator에서 {(_cannons != null ? _cannons.Length : 0)}개의 캐논을 가져왔습니다.</color>");
    //     }
    //     else
    //     {
    //         // 그리드 생성기를 찾을 수 없는 경우 대체 방법으로 FindObjectsOfType 사용
    //         _cannons = FindObjectsOfType<Cannon>();
    //         Debug.LogWarning($"<color=yellow>[ReleaseGameManager] IsometricGridGenerator를 찾을 수 없어 직접 캐논을 검색: {(_cannons != null ? _cannons.Length : 0)}개 발견</color>");
    //     }
        
    //     // --- UI 버튼 이벤트 구독 ---
    //     UI_BasicGameScene.OnSummonButtonUIClicked += HandleSummonButtonClickFromUI;
    //     Debug.Log("<color=cyan>[ReleaseGameManager] 소환 버튼 클릭 이벤트 구독 완료</color>");
    //     // -------------------------
    // }


    public void StartGame()
    {
        isGameActive = true;
        OnGameStart?.Invoke();
        // _latestScore = _brickGameManager.GetCurrentScore();
        // _brickGameManager.OnScoreChanged += HandleScoreChange;

        if (IsometricGridGenerator.Instance != null)
        {
            _cannons = IsometricGridGenerator.Instance.GetAllCannons();
            Debug.Log($"<color=cyan>[ReleaseGameManager] IsometricGridGenerator에서 {(_cannons != null ? _cannons.Length : 0)}개의 캐논을 가져왔습니다.</color>");
        }
        else
        {
            // 그리드 생성기를 찾을 수 없는 경우 대체 방법으로 FindObjectsOfType 사용
            _cannons = FindObjectsOfType<Cannon>();
            Debug.LogWarning($"<color=yellow>[ReleaseGameManager] IsometricGridGenerator를 찾을 수 없어 직접 캐논을 검색: {(_cannons != null ? _cannons.Length : 0)}개 발견</color>");
        }
         // UI_BasicGameScene.OnSummonButtonUIClicked += HandleSummonButtonClickFromUI; // 제거
    }   


    private void OnDestroy()
    {
        // --- 이벤트 구독 해지 ---
        if (_brickGameManager != null)
        {
            // _brickGameManager.OnScoreChanged -= HandleScoreChange;
            Debug.Log("[ReleaseGameManager] Unsubscribed from OnScoreChanged event.");
        }

        // UI_BasicGameScene.OnSummonButtonUIClicked -= HandleSummonButtonClickFromUI; // 제거
        // -----------------------
    }
    
    private void Update()
    {
        if (!isGameActive) return;
    }
    

    
    public void PauseGame()
    {
        isGameActive = false;
        OnGamePause?.Invoke();
    }
    
    public void ResumeGame()
    {
        isGameActive = true;
        OnGameResume?.Invoke();
    }
    
    public void GameOver()
    {
        isGameActive = false;
        OnGameOver?.Invoke();
    }
    
    // --- 버튼 클릭 이벤트 처리 (제거 또는 주석 처리) ---
    // private void HandleSummonButtonClickFromUI()
    // {
    //     Debug.Log($"<color=cyan>[ReleaseGameManager] 소환 버튼 클릭됨! 현재 점수: {_latestScore}</color>");
    //     // 총알 발사 시작
    //     StartFireBullets();
    // }

    // --- 기존 총알 발사 로직 (제거 또는 주석 처리) ---
    // private void StartFireBullets() { /* ... */ }
    // private IEnumerator FireBulletsFromCannonCoroutine(Cannon cannon, int ammo) { /* ... */ }

    // --- 서버 전용 총알 발사 메서드 추가 ---
    public void ServerFireBullet(Vector3 position, Quaternion rotation, ulong ownerClientId, Color ownerColor)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogError("ServerFireBullet는 서버에서만 호출되어야 합니다!");
            return;
        }

        if (bulletPrefab == null)
        {
             Debug.LogError("Bullet Prefab이 ReleaseGameManager에 할당되지 않았습니다.");
             return;
        }

        GameObject bullet = Instantiate(bulletPrefab, position, rotation);
        NetworkObject networkObject = bullet.GetComponent<NetworkObject>();

        if (networkObject != null)
        {
            // 총알 스크립트 가져오기 (존재한다고 가정)
            CannonBullet bulletScript = bullet.GetComponent<CannonBullet>();
            if (bulletScript != null)
            {
                // 서버에서 소유자 정보 설정 (클라이언트에 동기화될 수 있도록 NetworkVariable 사용 필요)
                // 예시: bulletScript.OwnerClientId.Value = ownerClientId;
                // 예시: bulletScript.OwnerColor.Value = ownerColor;
                // 중요: CannonBullet 스크립트에 OwnerClientId, OwnerColor 등의 NetworkVariable이 정의되어 있어야 함
                //       클라이언트에서도 이 정보를 읽을 수 있도록 ReadPermission 설정 필요

                // 임시로 직접 값 설정 (CannonBullet 내부에서 NetworkVariable 처리 필요)
                bulletScript.ownerPlayerID = (int)ownerClientId; // ulong을 int로 변환 (ID 체계 확인 필요)
                bulletScript.ownerColor = ownerColor;
                // bulletScript.SetOwner(...) // 이 메서드가 NetworkVariable을 설정하도록 수정 필요
            }
            else
            {
                 Debug.LogWarning("Bullet Prefab에 CannonBullet 스크립트가 없습니다. 소유자 정보 설정 불가.");
            }

            // 네트워크에 총알 스폰 (모든 클라이언트에 생성)
            networkObject.Spawn(true);
            Debug.Log($"총알 스폰됨! 서버에서 생성. 위치: {position}");

            // 발사 로직 (스폰 후 즉시 적용)
             if (bulletScript != null)
             {
                 bulletScript.Fire(bullet.transform.forward, bulletSpeed); // 스폰된 객체의 방향 사용
             }
             else
             {
                // CannonBullet 스크립트가 없으면 Rigidbody 사용
                Rigidbody rb = bullet.GetComponent<Rigidbody>();
                if (rb != null)
                {
                   rb.linearVelocity = bullet.transform.forward * bulletSpeed;
                }
             }
        }
        else
        {
            Debug.LogError("Bullet Prefab에 NetworkObject 컴포넌트가 없습니다!");
            Destroy(bullet); // 스폰 실패 시 생성된 게임 오브젝트 제거
        }
    }
    // -------------------------------------------

    // --- 점수 변경 처리 메서드 ---
    private void HandleScoreChange(int newScore)
    {
        _latestScore = newScore;
        Debug.Log($"[ReleaseGameManager] Score Updated: {_latestScore}");
    }
    // ---------------------------

    // 현재 게임 상태 반환 (필요한 경우)
    public bool IsGameActive()
    {
        return isGameActive;
    }
    

}