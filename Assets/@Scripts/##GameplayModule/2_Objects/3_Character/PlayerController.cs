using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic; // List 사용 위해 추가
using System.Linq; // Linq 사용 위해 추가

public class PlayerController : NetworkBehaviour
{
    // --- 네트워크 상태 변수 ---
    // 초기 총알 개수 (예: 10개). 게임 시작 시 또는 점수 획득 시 업데이트 필요
    public NetworkVariable<int> BulletCount = new NetworkVariable<int>(10, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // 플레이어 색상 (스폰 시 또는 다른 로직으로 설정 필요)
    public NetworkVariable<Color> PlayerColor = new NetworkVariable<Color>(Color.white, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // --------------------------
    
    // --- 서버 측 참조 ---
    private ReleaseGameManager _serverReleaseGameManager;
    private BrickGameManager _brickGameManager; // 현재 점수로 발사 횟수 계산용
    private List<Cannon> _myCannons = new List<Cannon>(); // 플레이어 소유 캐논 리스트 (서버에서만 관리)
    private int _nextCannonIndex = 0; // 순환용 인덱스
    // --------------------

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // 서버에서만 필요한 초기화 수행
        if (IsServer)
        {
            _serverReleaseGameManager = FindObjectOfType<ReleaseGameManager>();
            _brickGameManager = FindObjectOfType<BrickGameManager>(); // 점수 관리 매니저 조회
            if (_serverReleaseGameManager == null)
            {
                Debug.LogError($"[{nameof(PlayerController)}] 서버에서 ReleaseGameManager를 찾을 수 없습니다.", this);
            }
            if (_brickGameManager == null)
            {
                Debug.LogError($"[{nameof(PlayerController)}] 서버에서 BrickGameManager를 찾을 수 없습니다.", this);
            }
            _myCannons = FindObjectsOfType<Cannon>().Where(cannon => cannon.OwnerClientId == OwnerClientId).ToList();
            if (_myCannons.Count == 0)
            {
                Debug.LogWarning($"[{nameof(PlayerController)}] 플레이어 {OwnerClientId}의 캐논을 찾을 수 없습니다.", this);
            }
            else
            {
                Debug.Log($"[{nameof(PlayerController)}] 플레이어 {OwnerClientId}가 {_myCannons.Count}개의 캐논을 찾았습니다.");
            }

            // PlayerColor.Value = GetColorForPlayer(OwnerClientId);
        }

        // 클라이언트(Owner)에서 UI 이벤트 구독
        if (IsOwner)
        {
            // 총알 개수 UI가 없으므로 관련 이벤트 구독 제거됨
            // BulletCount.OnValueChanged += HandleBulletCountChanged;

            Debug.Log($"[{nameof(PlayerController)}] Local player ({OwnerClientId}) SUCCESSFULLY subscribed to UI summon button click.");
        }
    }

    public override void OnNetworkDespawn()
    {
         base.OnNetworkDespawn();

         // 클라이언트(Owner)에서 UI 이벤트 구독 해제
         if (IsOwner)
         {
             // 총알 개수 UI 관련 이벤트 구독 해제 제거됨
             // BulletCount.OnValueChanged -= HandleBulletCountChanged;

             Debug.Log($"[{nameof(PlayerController)}] Local player ({OwnerClientId}) unsubscribed from UI summon button click.");
         }
    }

    // --- 발사 요청 ServerRpc ---
    [ServerRpc]
    public void RequestFireMyBulletsServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;

        // 현재 점수만큼 총알 발사 요청
        int bulletsToFire = (_brickGameManager != null) ? _brickGameManager.GetCurrentScore() : 0;
        Debug.Log($"[{nameof(PlayerController)}] 서버: 발사 요청 받음. 플레이어 {OwnerClientId}, 발사 횟수(점수): {bulletsToFire}");
 
        if (_serverReleaseGameManager == null)
        {
            Debug.LogError($"[{nameof(PlayerController)}] 서버: ReleaseGameManager 참조가 없습니다. 발사 불가.", this);
            return;
        }

        _myCannons = FindObjectsOfType<Cannon>().Where(c => c.OwnerClientId == OwnerClientId).ToList();
        if (_myCannons.Count == 0)
        {
            Debug.LogWarning($"[{nameof(PlayerController)}] 서버: 플레이어 {OwnerClientId}에게 발사할 캐논이 없습니다.", this);
            return;
        }

        if (bulletsToFire <= 0)
        {
            Debug.Log($"[{nameof(PlayerController)}] 서버: 플레이어 {OwnerClientId}는 발사할 점수가 없습니다.");
            return;
        }
        
        // score 만큼 루프하여 발사
        for (int i = 0; i < bulletsToFire; i++)
        {
            var cannon = _myCannons[i % _myCannons.Count];
            Transform firePoint = cannon.firePoint ?? cannon.turretBarrel;
            if (firePoint == null)
            {
                Debug.LogError($"[{nameof(PlayerController)}] 서버: 캐논 '{cannon.name}'의 발사 지점을 찾을 수 없습니다.", cannon);
                continue;
            }
            _serverReleaseGameManager.ServerFireBullet(firePoint.position, firePoint.rotation, OwnerClientId, PlayerColor.Value);
        }
        Debug.Log($"[{nameof(PlayerController)}] 서버: 플레이어 {OwnerClientId} {bulletsToFire}발 발사 완료.");
    }

    // --- (선택사항) 총알 없음 피드백용 ClientRpc ---
    // [ClientRpc]
    // private void NotifyNoAmmoClientRpc(ClientRpcParams clientRpcParams = default)
    // {
    //     if (!IsOwner) return; // 자신에게 온 Rpc만 처리
    //     Debug.Log("총알이 부족합니다!");
    //     // TODO: UI에 "총알 부족" 메시지 표시
    // }

    // --- (선택사항) 점수 획득 시 총알 개수 업데이트 메서드 ---
    // 서버에서 호출되어야 함
    public void ServerAddBullets(int amount)
    {
        if (!IsServer) return;
        BulletCount.Value += amount;
         Debug.Log($"[{nameof(PlayerController)}] 서버: 플레이어 {OwnerClientId} 총알 {amount}개 추가. 현재: {BulletCount.Value}");
    }
}
