using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommonVars : MonoBehaviour
{
    // 싱글톤 인스턴스
    private static CommonVars _instance;
    public static CommonVars Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("CommonVars");
                _instance = go.AddComponent<CommonVars>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // SessionManager 참조
    private SessionManager<SessionPlayerData> _sessionManager;
    // GameState 참조
    private BasicGameState _gameState;

    // 세션 데이터가 아직 설정되지 않았을 때 사용할 로컬 변수들
    // (세션이 초기화되기 전 또는 싱글 플레이어 모드에서 사용)
    private static int _level = 1;
    private static int _numberOfBalls = 1;
    private static int _newBalls = 1;
    private static int _ballHitBottom = 0;
    private static bool _lastBallHitBottom = false;
    private static bool _startMovingTowardsMainBall = false;
    private static int _ballsReachedDistance = 0;
    private static bool _firstBallHitBottomCollider = false;
    private static float _firstBallHitXPos = 0;
    private static bool _canContinue = true;
    private static bool _newWaveOfBricks = false;
    private static float _speedUpTimer = 0;

    // 현재 세션이 활성화되어 있는지 여부
    private bool _isSessionActive = false;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // SessionManager 참조 가져오기
        _sessionManager = SessionManager<SessionPlayerData>.Instance;
        
        // 디버그 로그
        Debug.Log("[CommonVars] 초기화됨");
    }

    private void Start()
    {
        // GameState 찾기 (다른 객체가 생성된 후)
        _gameState = FindObjectOfType<BasicGameState>();
        if (_gameState != null)
        {
            Debug.Log("[CommonVars] GameState 연결됨");
            _isSessionActive = true;
        }
    }

    // 정적 프로퍼티 - 세션 데이터가 있으면 세션에서, 없으면 로컬 변수에서 값 가져옴
    public static int level
    {
        get
        {
            if (IsUsingSessionData())
            {
                // 세션에서 값 가져오기 시도
                // 여기서는 세션 데이터가 아직 설계되지 않았으므로 로컬 변수 사용
                return _level;
            }
            return _level;
        }
        set
        {
            _level = value;
            // 세션 데이터 사용 중이면 세션에도 값 설정
            // 미구현 - 세션 데이터 구조에 맞게 수정 필요
        }
    }

    public static int numberOfBalls
    {
        get { return _numberOfBalls; }
        set { _numberOfBalls = value; }
    }

    public static int newBalls
    {
        get { return _newBalls; }
        set { _newBalls = value; }
    }

    public static int ballHitBottom
    {
        get { return _ballHitBottom; }
        set { _ballHitBottom = value; }
    }

    public static bool lastBallHitBottom
    {
        get { return _lastBallHitBottom; }
        set { _lastBallHitBottom = value; }
    }

    public static bool startMovingTowardsMainBall
    {
        get { return _startMovingTowardsMainBall; }
        set { _startMovingTowardsMainBall = value; }
    }

    public static int ballsReachedDistance
    {
        get { return _ballsReachedDistance; }
        set { _ballsReachedDistance = value; }
    }

    public static bool firstBallHitBottomCollider
    {
        get { return _firstBallHitBottomCollider; }
        set { _firstBallHitBottomCollider = value; }
    }

    public static float firstBallHitXPos
    {
        get { return _firstBallHitXPos; }
        set { _firstBallHitXPos = value; }
    }

    public static bool canContinue
    {
        get { return _canContinue; }
        set { _canContinue = value; }
    }

    public static bool newWaveOfBricks
    {
        get { return _newWaveOfBricks; }
        set { _newWaveOfBricks = value; }
    }

    public static float speedUpTimer
    {
        get { return _speedUpTimer; }
        set { _speedUpTimer = value; }
    }

    // 세션 데이터를 사용하는지 여부 확인
    private static bool IsUsingSessionData()
    {
        return Instance._isSessionActive && Instance._sessionManager != null;
    }

    public static void RestartAllVariables()
    {
        _level = 1;
        _numberOfBalls = 1;
        _newBalls = 1;
        _ballHitBottom = 0;
        _lastBallHitBottom = false;
        _startMovingTowardsMainBall = false;
        _ballsReachedDistance = 0;
        _firstBallHitBottomCollider = false;
        _firstBallHitXPos = 0;
        _canContinue = true;
        _newWaveOfBricks = false;
        _speedUpTimer = 0;
        
        Debug.Log("[CommonVars] 모든 변수가 초기화되었습니다.");
    }

    // 향후 세션 데이터 연동 구현을 위한 준비
    // SessionPlayerData에 커스텀 데이터 필드를 추가하고 해당 필드를 활용하여 구현
    // 현재는 로컬 변수만 사용하는 간단한 버전으로 구현
}