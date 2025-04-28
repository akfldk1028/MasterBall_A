using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic; // List 사용 위해 추가
using System.Linq; // Linq 사용 위해 추가
using VContainer; // Inject 사용 위해 추가 (만약 VContainer로 의존성 주입한다면)
using Unity.Assets.Scripts.Objects; // Brick 네임스페이스 사용을 위해 추가
using System; // Action 사용을 위해 추가

public class PlayerController : NetworkBehaviour
{
    // --- 로컬 점수 및 이벤트 추가 ---
    private int localScore = 0;
    public event Action<int> OnLocalScoreChanged; // UI 업데이트용
    // 로컬 점수 초기값 확인 등을 위한 Getter (선택 사항)
    public int GetCurrentLocalScore() => localScore;
    // -----------------------------

    // BulletCount 제거 또는 주석 처리
    // public NetworkVariable<int> BulletCount = new NetworkVariable<int>(10, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<Color> PlayerColor = new NetworkVariable<Color>(Color.white, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // --------------------------
    
    // --- 서버 측 참조 ---
    // VContainer 등으로 주입받는 것을 권장
    private ReleaseGameManager _serverReleaseGameManager;
    // [Inject] private BrickGameManager _brickGameManager; // 점수 확인이 이제 PlayerScore로 대체되므로 필요 없을 수 있음

    private List<Cannon> _myCannons = new List<Cannon>(); // 플레이어 소유 캐논 리스트 (서버에서만 관리)
    private int _nextCannonIndex = 0; // 순환용 인덱스
    // --------------------

    // --- 로컬 플레이어 스폰 이벤트 제거 ---
    // public static event Action<PlayerController> OnLocalPlayerSpawned;
    // ------------------------------------

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // --- 진단 로그 추가 ---
        Debug.Log($"[{nameof(PlayerController)}] OnNetworkSpawn called on client {NetworkManager.Singleton?.LocalClientId}. Object ID: {NetworkObjectId}, Owner: {OwnerClientId}. IsOwner: {IsOwner}, IsClient: {IsClient}, IsServer: {IsServer}");
        // --------------------

        // 서버에서만 필요한 초기화 수행
        if (IsServer)
        {
            // VContainer 사용하지 않는 경우 FindObjectOfType 사용
            // _serverReleaseGameManager = FindObjectOfType<ReleaseGameManager>();
            // _brickGameManager = FindObjectOfType<BrickGameManager>(); // 필요하다면 유지

            // 필요한 참조 확인 로그는 유지
            // if (_brickGameManager == null) Debug.LogError($"[{nameof(PlayerController)}] 서버에서 BrickGameManager를 찾을 수 없습니다.", this);

            _serverReleaseGameManager = FindObjectOfType<ReleaseGameManager>();

            _myCannons = FindObjectsOfType<Cannon>().Where(cannon => cannon.OwnerClientId == OwnerClientId).ToList();
            if (_myCannons.Count > 0) Debug.Log($"[{nameof(PlayerController)}] 플레이어 {OwnerClientId}가 {_myCannons.Count}개의 캐논을 찾았습니다.");
            else Debug.LogWarning($"[{nameof(PlayerController)}] 플레이어 {OwnerClientId}의 캐논을 찾을 수 없습니다.", this);

            // 스폰 시 초기 점수 설정 (예시) - 게임 시작 로직에 따라 위치 조정 필요
            // PlayerScore.Value = 0; // 필요하다면 여기서 초기화

            // PlayerColor.Value = GetColorForPlayer(OwnerClientId);
        }

        // 클라이언트(Owner)에서 UI 이벤트 구독 등
        if (IsOwner) // 이 IsOwner 체크는 PlayerController 위치 때문에 의미 없을 수 있음
        {
             Debug.Log($"[{nameof(PlayerController)}] Local player ({OwnerClientId}) 스폰됨. <<-- IsOwner is TRUE");
             // --- 이벤트 발생 제거 ---
             // OnLocalPlayerSpawned?.Invoke(this);
             // ------------------------

             // 로컬 점수 초기값 확인 (필요시)
             Debug.Log($"[{nameof(PlayerController)}] 초기 로컬 점수 확인 (IsOwner): {localScore}");
        }
        else
        {
            // 다른 클라이언트의 플레이어 객체에 대한 설정 (예: 이름표 표시)
            // ConfigureRemotePlayer();
        }

        // 모든 클라이언트(소유자 포함)에서 필요한 공통 설정
        // ConfigureCommonVisuals();

        // 점수 변경 시 UI 업데이트 콜백 등록 (모든 클라이언트에서 실행, UI 업데이트는 콜백 내부에서 처리)
        // PlayerScore.OnValueChanged += HandleScoreChanged; // UI_BasicGameScene에서 처리하므로 여기선 주석 처리
        // 초기 점수 설정 (서버에서 이미 설정했으므로 여기선 필요 없을 수 있음)
        // UpdateScoreDisplay(PlayerScore.Value); // UI_BasicGameScene에서 처리하므로 여기선 주석 처리
    }

    public override void OnNetworkDespawn()
    {
         // BulletCount 구독 해제 제거
         // if (IsOwner)
         // {
             // BulletCount.OnValueChanged -= HandleBulletCountChanged;
         // }

         // PlayerScore 구독 해제 제거 (UI_BasicGameScene에서 처리)
         // if (IsOwner)
         // {
         //     PlayerScore.OnValueChanged -= HandleScoreChanged;
         // }
         base.OnNetworkDespawn();
    }

    // UI 점수 업데이트용 콜백 제거 (UI_BasicGameScene에서 직접 구독)
    // private void HandleScoreChanged(int previousValue, int newValue)
    // {
    //     if (!IsOwner) return;
    //     Debug.Log($"점수 변경 감지됨: {previousValue} -> {newValue}");
    //     // UIManager.Instance.UpdateScoreText(newValue);
    // }

    // --- UI에서 호출할 발사 요청 메서드 (수정됨) ---
    public void RequestFireCannon()
    {
        if (NetUtils.IsClientCheck(OwnerClientId)) return; // 자신의 객체만 제어

        int bulletsToFire = localScore; // 로컬 점수 기준으로 발사할 총알 수 결정
        Debug.Log($"[{nameof(PlayerController)}] 발사 요청: 로컬 점수 {bulletsToFire} 확인.");

        if (bulletsToFire <= 0)
        {
            Debug.Log($"[{nameof(PlayerController)}] 발사할 점수(총알)가 없습니다.");
            return;
        }

        // 서버에 총알 발사 요청 (로컬 점수 개수 전달)
        FireBulletsServerRpc(bulletsToFire);

        // 발사 후 로컬 점수 초기화
        localScore = 0;
        // 로컬 점수 변경 이벤트 발생 (UI 업데이트용)
        OnLocalScoreChanged?.Invoke(localScore);
        Debug.Log($"[{nameof(PlayerController)}] 로컬 점수 초기화됨: {localScore}");
    }
    // ------------------------------------------

    // --- 새 발사 요청 ServerRpc ---
    [ServerRpc]
    private void FireBulletsServerRpc(int bulletCount, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;

        ulong clientId = rpcParams.Receive.SenderClientId; // 요청 보낸 클라이언트 ID
        Debug.Log($"[{nameof(PlayerController)}] 서버: 플레이어 {clientId}로부터 {bulletCount}발 발사 요청 받음.");

        // ReleaseGameManager 참조 확인 (OnNetworkSpawn에서 이미 찾았어야 함)
        if (_serverReleaseGameManager == null)
        {
            Debug.LogError($"[{nameof(PlayerController)}] 서버: ReleaseGameManager 참조가 없습니다. 발사 불가.", this);
            return;
        }

        // 캐논 재확인 (게임 도중 캐논 상태가 변할 수 있으므로)
         _myCannons = FindObjectsOfType<Cannon>().Where(c => c.OwnerClientId == clientId).ToList(); // clientId 사용
        if (_myCannons.Count == 0)
        {
            Debug.LogWarning($"[{nameof(PlayerController)}] 서버: 플레이어 {clientId}에게 발사할 캐논이 없습니다.", this);
            return;
        }

        // 클라이언트가 요청한 수 만큼 루프하여 발사
        for (int i = 0; i < bulletCount; i++)
        {
            // 캐논 순환 선택
            var cannon = _myCannons[i % _myCannons.Count];
            Transform firePoint = cannon.firePoint ?? cannon.turretBarrel;
            if (firePoint == null)
            {
                Debug.LogError($"[{nameof(PlayerController)}] 서버: 캐논 '{cannon.name}'의 발사 지점을 찾을 수 없습니다.", cannon);
                continue; // 다음 총알 발사 시도
            }
            // ReleaseGameManager를 통해 총알 발사 (이 총알은 네트워크 객체여야 함)
            _serverReleaseGameManager.ServerFireBullet(firePoint.position, firePoint.rotation, clientId, PlayerColor.Value); // clientId 전달
        }
        Debug.Log($"[{nameof(PlayerController)}] 서버: 플레이어 {clientId} {bulletCount}발 발사 완료.");
    }
    // -----------------------------

    // --- 로컬 점수 추가 및 이벤트 발생 메서드 ---
    public void AddLocalScore(int amount)
    {
        Debug.Log($"[{nameof(PlayerController)}] 로컬 점수 추가 요청: {amount}");
        Debug.Log($"[{nameof(PlayerController)}] 로컬 점수 추가 요청: {NetworkManager.Singleton.LocalClientId}");
        // if (!NetUtils.IsClientCheck(NetworkManager.Singleton.LocalClientId))
        // {
        //     Debug.LogError($"[{nameof(PlayerController)}] 로컬 점수 추가 요청: {amount} 실패 - 자신의 객체가 아님");
        //     return;
        // }
        if (amount <= 0) return;

        localScore += amount;
        Debug.Log($"[{nameof(PlayerController)}] 로컬 점수 +{amount}. 현재: {localScore}");
        OnLocalScoreChanged?.Invoke(localScore); // UI 업데이트 이벤트 발생
    }
    // -----------------------------------------

    // --- 벽돌 타격 보고 ServerRpc 제거 ---
    // [ServerRpc] private void ReportBrickHitServerRpc(...) ...
    // ------------------------------------

    // --- 기존 BulletCount 관련 로직 제거 ---
    // ...
    // ------------------------------------
}
