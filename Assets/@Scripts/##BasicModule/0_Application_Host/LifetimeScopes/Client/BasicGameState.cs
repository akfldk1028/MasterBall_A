// BasicGameState.cs
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static Define;
using VContainer;
using Unity.Assets.Scripts.Resource;
using Unity.Assets.Scripts.Objects;

/// <summary>
/// 게임 상태 변경 이벤트 대리자
/// </summary>
public delegate void OnMoneyUpEventHandler();
public delegate void OnTimerUpEventHandler();
public delegate void OnGameOverEventHandler();
public delegate void OnWaveChangedEventHandler(bool isBossWave);

/// <summary>
/// 기본 게임 상태를 관리하는 클래스
/// </summary>
[RequireComponent(typeof(NetcodeHooks),typeof(NetworkObject))]
public class BasicGameState : GameStateLifetimeScope 
{    
    #region Fields

    [Header("Network")]
    [SerializeField] private NetcodeHooks m_NetcodeHooks;
    private GameStateNetworkHandler _networkHandler;
    private bool _isNetworkReady = false;
    private bool _isServer = false; // 서버인지 여부를 저장
    [SerializeField] private GameObject playerPrefab; // <<-- 플레이어 프리팹 필드 추가
    
    [Header("Game State")]
    private float _timer = 20.0f;

    
    [Header("Game Settings")]

    
    [Header("Optimization")]
    private float _timerUpdateInterval = 0.1f;
    private float _lastTimerUpdate = 0f;

    #endregion

    #region Properties

    /// <summary>
    /// 현재 게임 타이머
    /// </summary>
    public float Timer => _timer;
    

    
    /// <summary>
    /// 현재 게임 상태
    /// </summary>
    public override GameState ActiveState => GameState.BasicGame;

    #endregion
    
    #region Events

 
    public event OnMoneyUpEventHandler OnMoneyUp;

    public event OnTimerUpEventHandler OnTimerUp;

    public event OnGameOverEventHandler OnGameOver;

    public event OnWaveChangedEventHandler OnWaveChanged;
    private SessionManager<SessionPlayerData> _sessionManager;

    #endregion
    
    #region Dependencies

    [Inject] public NetworkManager _networkManager;
    [Inject] public ResourceManager _resourceManager;
    [Inject] private ObjectManager _objectManager;
    [Inject] private MapManager _mapManager;

    private BrickGameManager _brickGameManager;
    [Inject] private ReleaseGameManager _releaseGameManager;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Awake 시 컴포넌트 초기화
    /// </summary>
    public void Awake()
    {            
        base.Awake();
        _brickGameManager = FindObjectOfType<BrickGameManager>();
        if (_brickGameManager == null)
            {
                Debug.LogWarning("BrickGameManager를 찾을 수 없습니다. 점수가 추가되지 않을 수 있습니다.");
            }
        // Ensure this object has its NetworkObject spawned on the server
        // var networkObject = GetComponent<NetworkObject>();
        // if (networkObject != null && !networkObject.IsSpawned && NetworkManager.Singleton.IsServer)
        // {
        //     networkObject.Spawn();
        //     Debug.Log("[BasicGameState] Manual NetworkObject.Spawn() 호출됨");
        // }

        _sessionManager = SessionManager<SessionPlayerData>.Instance;
        LogSessionData();
    }
    /// <summary>
/// 현재 연결된 모든 플레이어의 세션 데이터를 간단히 로그에 출력합니다.
/// </summary>
    public void LogSessionData()
    {
        if (_sessionManager == null)
        {
            Debug.LogError("[BasicGameState] SessionManager가 없습니다.");
            return;
        }

        Debug.Log("===== 세션 데이터 로그 시작 =====");
        
        // NetworkManager를 통해 현재 연결된 모든 클라이언트 ID 가져오기
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            string playerId = _sessionManager.GetPlayerId(clientId);
            if (string.IsNullOrEmpty(playerId))
                continue;
                
            var playerData = _sessionManager.GetPlayerData(playerId);
            if (!playerData.HasValue)
                continue;
                
            var data = playerData.Value;
            
            // 핵심 데이터만 간단히 출력
            Debug.Log($"플레이어[{clientId}]: 이름={data.PlayerName}, HP={data.CurrentHitPoints}, 위치={data.PlayerPosition}");
        }
        
        Debug.Log("===== 세션 데이터 로그 종료 =====");
    }



    public void StartGame()
    {

        // 세션이 시작되었음을 알림
        _sessionManager.OnSessionStarted();
        
        // 필요한 초기화 작업
        InitializeState();
        _brickGameManager.StartGame(); 
        _releaseGameManager.StartGame();
    }

    private void SpawnInitialBallAndPlank()
    {
        // Vector3 position = new Vector3(125.6288f, 3.746667f, 0f);
        // _objectManager.Spawn<PhysicsBall>(position: position ,templateID: 201000, prefabName: "PhysicsBall", isNetworkObject: false);
        Debug.Log("[BasicGameState] 서버에서 초기 Ball 및 Plank 스폰 시작");
        // _objectManager 와 _resourceManager (주입받은) 사용하여 스폰
        // ... 스폰 로직 ...
    }



    // 게임 종료 시 호출
    public void EndGame()
    {
        // 세션이 종료되었음을 알림
        _sessionManager.OnSessionEnded();
    }
    public void OnPlayerConnected(ulong clientId, string playerId)
    {
        // 플레이어 세션 데이터 설정
        SessionPlayerData playerData = new SessionPlayerData(
            clientId, 
            $"Player_{clientId}", 
            new NetworkGuid(),
            100, // 초기 체력
            true, // 연결됨
            false // 캐릭터 생성 안됨
        );
        
        _sessionManager.SetupConnectingPlayerSessionData(clientId, playerId, playerData);
        
        // 필요한 추가 처리
        SyncGameStateToPlayer(clientId);
    }

    private void SyncGameStateToPlayer(ulong clientId)
    {
        if (_networkHandler == null) return;
        
        // 특정 플레이어에게만 상태 동기화
        // _networkHandler.SyncStateToClientRpc(
        //     _timer,
        //     _wave,
        //     _money,
        //     _monsterCount,
        //     _isBossWave,
        //     new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } } }
        // );
    }

    public void OnPlayerDisconnected(ulong clientId)
    {
        _sessionManager.DisconnectClient(clientId);
    }
    /// <summary>
    /// 게임 상태 초기화
    /// </summary>
    public void Initialize()
    {
        Debug.Log("[BasicGameState] 초기화 시작");
        CheckDependencyInjection();
        SpawnInitialBallAndPlank();

        if (m_NetcodeHooks != null)
        {
            m_NetcodeHooks.OnNetworkSpawnHook += OnNetworkSpawn;
            m_NetcodeHooks.OnNetworkDespawnHook += OnNetworkDespawn;
            Debug.Log("[BasicGameState] 네트워크 이벤트 등록 완료");
        }
        else
        {
            Debug.LogError("[BasicGameState] NetcodeHooks가 null입니다!");
        }


        Debug.Log("[BasicGameState] 초기화 완료");
    }
    
    /// <summary>
    /// 프레임마다 서버 상태 업데이트
    /// </summary>
    private void Update()
    {
        if (_isNetworkReady && _isServer)
        {
            UpdateServerState();
        }
    }
    
    /// <summary>
    /// 객체 파괴 시 이벤트 해제
    /// </summary>
    protected override void OnDestroy()
    {
        // 서버인 경우 클라이언트 연결 콜백 해제
        if (_networkManager != null && _isServer) // _isServer 플래그 사용
        {
            _networkManager.OnClientConnectedCallback -= HandleClientConnected;
            Debug.Log("[BasicGameState] 클라이언트 연결 콜백 해제 완료 (OnDestroy)");
        }

        if (m_NetcodeHooks != null)
        {
            m_NetcodeHooks.OnNetworkSpawnHook -= OnNetworkSpawn;
            m_NetcodeHooks.OnNetworkDespawnHook -= OnNetworkDespawn;
            Debug.Log("[BasicGameState] 네트워크 이벤트 해제 완료");
        }
        
        base.OnDestroy();
    }
    
    #endregion

    #region Network Callbacks
    
    /// <summary>
    /// 네트워크 스폰 시 호출
    /// </summary>
    public void OnNetworkSpawn()
    {
        Debug.Log("[BasicGameState] OnNetworkSpawn 호출됨");


        _isServer = NetworkManager.Singleton.IsServer;
        _isNetworkReady = true;

        Debug.Log($"[BasicGameState] 네트워크 준비 완료. 서버 여부: {_isServer}");

        if (_isServer)
        {
            // 서버 초기화 로직 (예: 초기 상태 동기화)
            InitializeState(); // 서버에서만 게임 상태 초기화
            SyncInitialStateToClients();
            


            _networkManager.OnClientConnectedCallback += HandleClientConnected;
            Debug.Log("[BasicGameState] 클라이언트 연결 콜백 구독 완료 (OnNetworkSpawn)");

             HandleClientConnected(NetworkManager.Singleton.LocalClientId);
        }
    }
    
    /// <summary>
    /// 네트워크 디스폰 시 호출
    /// </summary>
    private void OnNetworkDespawn()
    {
        Debug.Log("[BasicGameState] OnNetworkDespawn 호출됨");
        _isNetworkReady = false;

        // 서버인 경우 클라이언트 연결 콜백 해제 (OnDestroy에서도 하지만 안전을 위해 추가)
        if (_networkManager != null && _isServer)
        {
            _networkManager.OnClientConnectedCallback -= HandleClientConnected;
             Debug.Log("[BasicGameState] 클라이언트 연결 콜백 해제 완료 (OnNetworkDespawn)");
        }
    }
    
    #endregion

    #region Server Methods
    
    /// <summary>
    /// 상태 초기화
    /// </summary>
    private void InitializeState()
    {
        _timer = 20.0f;
        // _wave = 1;
        // _money = 50;
        // _monsterCount = 0;
        // _isBossWave = false;
        // Debug.Log("[BasicGameState] 상태 초기화 완료");
    }
    
    /// <summary>
    /// 서버 상태 업데이트
    /// </summary>
    private void UpdateServerState()
    {
        UpdateTimer();
    }
    
    /// <summary>
    /// 타이머 업데이트
    /// </summary>
    private void UpdateTimer()
    {

        
        if (_timer > 0)
        {
            _timer -= Time.deltaTime;
            _timer = Mathf.Max(_timer, 0); // 음수 방지
            
            // 주기적으로 클라이언트에 타이머 동기화
            if (Time.time - _lastTimerUpdate >= _timerUpdateInterval)
            {
                _lastTimerUpdate = Time.time;
            }
            
            // 타이머가 0이 되면 새 웨이브 시작
            if (_timer <= 0)
            {
                // StartNextWave();
            }
        }
    }
    

    /// <summary>
    /// 초기 상태를 클라이언트에 동기화
    /// </summary>
    private void SyncInitialStateToClients()
    {
        if (_networkHandler == null) return;
        
        // _networkHandler.SyncInitialStateClientRpc(
        //     _timer,
        //     _wave,
        //     _money,
        //     _monsterCount,
        //     _isBossWave
        // );
    }
    
    #endregion

    #region Public Methods
    
    /// <summary>
    /// 게임 리소스 불러오기
    /// </summary>
    public void Load()
    {
        // 게임 리소스 로드 로직 구현
    }
    
    /// <summary>
    /// 돈 획득 메서드
    /// </summary>
    /// <param name="value">획득할 금액</param>
    /// <param name="type">호스트 타입</param>
    public void GetMoney(int value, HostType type = HostType.All)
    {
        if (!IsServerReady() || _networkHandler == null) return;
        
     
    }
    

    

    
    /// <summary>
    /// 게임 오버 이벤트 발생
    /// </summary>
    public void OnGameOverEvent()
    {
        Debug.Log("[BasicGameState] 게임 오버 이벤트 발생");
        Time.timeScale = 0.0f;
        OnGameOver?.Invoke();
    }
    
    #endregion
    
    #region Client State Update Methods
    
    /// <summary>
    /// 클라이언트 타이머 업데이트
    /// </summary>
    public void UpdateClientTimer(float timer)
    {
        if (!_isServer) // 클라이언트만 값 업데이트
        {
            _timer = timer;
            OnTimerUp?.Invoke();
        }
    }
    

    
    /// <summary>
    /// 클라이언트 돈 업데이트
    /// </summary>
    public void UpdateClientMoney(int money)
    {
        if (!_isServer) // 클라이언트만 값 업데이트
        {
            // _money = money;
            OnMoneyUp?.Invoke();
        }
    }
    
    /// <summary>
    /// 클라이언트 몬스터 수 업데이트
    /// </summary>
    public void UpdateClientMonsterCount(int count)
    {
        if (!_isServer) // 클라이언트만 값 업데이트
        {
            // _monsterCount = count;
        }
    }
    
    /// <summary>
    /// 클라이언트 초기 상태 동기화
    /// </summary>
    public void SyncClientInitialState(float timer, int wave, int money, int monsterCount, bool isBossWave)
    {
        if (!_isServer) // 클라이언트만 값 업데이트
        {
            _timer = timer;
            // _wave = wave;
            // _money = money;
            // _monsterCount = monsterCount;
            // _isBossWave = isBossWave;
            
            OnTimerUp?.Invoke();
            OnMoneyUp?.Invoke();
            
            Debug.Log("[BasicGameState] 초기 상태 동기화 완료");
        }
    }
    
    #endregion

    #region Helper Methods
    
    /// <summary>
    /// 의존성 주입 확인
    /// </summary>
    private void CheckDependencyInjection()
    {
        if (_resourceManager != null)
        {
            Debug.Log("[BasicGameState] ResourceManager 주입 성공");
        }
        else
        {
            Debug.LogError("[BasicGameState] ResourceManager 주입 실패");
        }
        
        if (_objectManager != null)
        {
            Debug.Log("[BasicGameState] ObjectManager 주입 성공");
        }
        else
        {
            Debug.LogError("[BasicGameState] ObjectManager 주입 실패");
        }
        
        if (_mapManager != null)
        {
            Debug.Log("[BasicGameState] MapManager 주입 성공");
        }
        else
        {
            Debug.LogError("[BasicGameState] MapManager 주입 실패");
        }
        _brickGameManager = FindObjectOfType<BrickGameManager>();
        if (_brickGameManager == null)
        {
            Debug.LogWarning("BrickGameManager를 찾을 수 없습니다. 점수가 추가되지 않을 수 있습니다.");
        }

        Debug.Log($"[BasicGameState] ID: {GetInstanceID()}, 이름: {gameObject.name}");
    }
    
    /// <summary>
    /// 서버 준비 상태 확인
    /// </summary>
    private bool IsServerReady()
    {
        return _isNetworkReady && _isServer;
    }
    
    #endregion

    // 클라이언트 연결 시 플레이어 스폰 처리 메서드 <<-- 추가
    private void HandleClientConnected(ulong clientId)
    {
        Debug.Log($"[BasicGameState] Client connected: {clientId}. Spawning player...");

        if (playerPrefab == null)
        {
            Debug.LogError("[BasicGameState] Player Prefab is not assigned!", this);
            return;
        }

        // TODO: 플레이어 스폰 위치 결정 (필요 시 로직 추가)
        Vector3 spawnPosition = Vector3.zero; // 예시 위치
        Quaternion spawnRotation = Quaternion.identity;

        GameObject playerInstance = Instantiate(playerPrefab, spawnPosition, spawnRotation);
        NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();

        if (networkObject == null)
        {
            Debug.LogError("[BasicGameState] Player Prefab does not have a NetworkObject component!", playerPrefab);
            Destroy(playerInstance);
            return;
        }

        // 플레이어 객체로 스폰하고 소유권 부여
        networkObject.SpawnAsPlayerObject(clientId);

        Debug.Log($"[BasicGameState] Player spawned for client {clientId}. NetworkObjectId: {networkObject.NetworkObjectId}");

        // (선택 사항) 스폰 후 플레이어 데이터 초기화 또는 색상 설정 등
        // PlayerController playerController = playerInstance.GetComponent<PlayerController>();
        // if (playerController != null)
        // {
        //     // 예: playerController.PlayerColor.Value = GetColorForPlayer(clientId);
        //     // 예: SessionManager에서 플레이어 데이터 가져와서 설정
        // }
    }
}


