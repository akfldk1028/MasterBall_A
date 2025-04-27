using UnityEngine;
using System;
using TMPro;
using VContainer;

public class BrickGameManager : MonoBehaviour
{
    [Header("게임 설정")]
    [SerializeField] private float initialSpawnDelay = 2f;
    [SerializeField] private float spawnInterval = 5f;
    
    [Header("레벨 설정")]
    [SerializeField] private int maxLevel = 50; // 최대 레벨
    [SerializeField] private int initialLevel = 1; // 초기 레벨
    
    // [Header("점수 설정")] - 주석 처리 또는 제거
    // [Inject] private TextMeshProUGUI _scoreText; // 점수 표시 텍스트 - 제거
    
    [Header("참조")]
    
    // 게임 상태 변수
    private float nextSpawnTime = 0f;
    private float currentSpawnInterval;
    private bool isGameActive = false;
    private int rowsSpawned = 0; // 생성된 행 수 추적
    
    private static int currentScore = 0;
    
    // 이벤트 정의
    public event Action OnGameStart;
    public event Action OnGamePause;
    public event Action OnGameResume;
    public event Action OnGameOver;
    public event Action OnRowSpawn;
    public event Action<int> OnLevelUp; // 레벨업 이벤트 추가
    public event Action<int> OnScoreChanged; // 점수 변경 이벤트 추가
    
    private ObjectPlacement objectPlacer;
    
    // Helper function (or flag) to track if level was increased in the last IncreaseLevel call
    private bool _levelIncreasedLastCall = false; 
    private bool WasLevelIncreasedInLastCall()
    {
        bool result = _levelIncreasedLastCall;
        _levelIncreasedLastCall = false; // Reset flag after checking
        return result;
    }
    
    public void Start()
    {
        StartGame();
    }
    public void Update()
    {
        if (!isGameActive) return;
        
        // 시간 체크하여 새 행 생성 여부 결정
        if (Time.time >= nextSpawnTime)
        {            
            SpawnNewRow();
            
            // 다음 스폰 시간은 AdjustDifficultyByLevel 또는 SpawnNewRow에서 설정됨
            // 여기서는 명시적으로 설정할 필요 없음 (또는 필요시 초기 스폰 시간 로직 검토)
        }
    }
    
    private void SpawnNewRow()
    {
        Debug.Log($"[BrickGameManager] SpawnNewRow 호출");
        if (objectPlacer != null)
        {
            // numberOfRowsToSpawn을 1로 설정하여 한 줄씩만 소환
            Debug.Log($"[BrickGameManager] SpawnNewRow 호출22222");
            objectPlacer.PlaceMultipleRows(1);
            
            // 행 생성 카운터 증가
            rowsSpawned++;
            
            // 레벨업 처리 - 한 줄 생성할 때마다 레벨 증가
            IncreaseLevel();
            
            // 이벤트 발생
            OnRowSpawn?.Invoke();
            
            // 새 블록 웨이브 생성 표시
            CommonVars.newWaveOfBricks = true;

            // 다음 스폰 시간 설정: 레벨업 시 AdjustDifficultyByLevel에서 이미 설정됨.
            // 레벨업이 안 됐을 경우 여기서 설정해야 함.
            if (!WasLevelIncreasedInLastCall()) // 가상의 함수, 실제 레벨업 여부 확인 필요
            {
                 nextSpawnTime = Time.time + currentSpawnInterval;
            }
        }
    }
    
    private void IncreaseLevel()
    {
        _levelIncreasedLastCall = false; // Reset flag at the beginning
        // 최대 레벨 체크
        if (CommonVars.level < maxLevel)
        {
            // 레벨 증가
            CommonVars.level++;
            _levelIncreasedLastCall = true; // Set flag when level increases
            
            // 레벨에 따른 난이도 조정 (스폰 간격 및 다음 스폰 시간 설정)
            AdjustDifficultyByLevel();
            
            // 이벤트 발생
            OnLevelUp?.Invoke(CommonVars.level);
            
            Debug.Log($"레벨 업! 현재 레벨: {CommonVars.level}, 생성된 행 수: {rowsSpawned}");
        }
    }
    
    private void AdjustDifficultyByLevel()
    {
        // 레벨에 따른 난이도 증가 로직
        // 예: 스폰 간격 추가 감소, 블록 이동 속도 증가 등
        
        // 레벨이 올라갈수록 스폰 간격 추가 감소
        float levelFactor = 1f - (0.05f * (CommonVars.level - 1)); // 레벨당 5% 추가 감소
        currentSpawnInterval *= levelFactor;
        currentSpawnInterval = Mathf.Max(currentSpawnInterval, 1.5f);
        
        // 다음 스폰 시간 재설정 (즉시 반영)
        nextSpawnTime = Time.time + currentSpawnInterval;
        Debug.Log($"난이도 조정됨 (레벨업): 새 스폰 간격 {currentSpawnInterval}, 다음 스폰 시간 {nextSpawnTime}"); // 로그 추가
    }
    
    // 게임 시작 메서드
    public void StartGame()
    {
        
        // Debug.Log("[BrickGameManager] StartGame 호출"); 
        objectPlacer = FindFirstObjectByType<ObjectPlacement>();
     
        isGameActive = true;
        currentSpawnInterval = spawnInterval;
        nextSpawnTime = Time.time + initialSpawnDelay;
        
        // 행 카운터 초기화
        rowsSpawned = 0;
        
        // 점수 초기화
        currentScore = 0;
        
        // CommonVars 변수 초기화
        CommonVars.RestartAllVariables();
        
        // 레벨 초기화 - 초기 레벨 설정
        CommonVars.level = initialLevel;
        
        // 초기 행 생성 - 명시적으로 3줄 생성
        objectPlacer.PlaceMultipleRows(3);
        
        // 이벤트 발생
        OnGameStart?.Invoke();
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
    
    // 벽돌이 파괴될 때 호출되는 메서드
    public void AddScore(int waveValue)
    {
        // wave 값만큼 점수 추가
        currentScore += waveValue;
        
        // 이벤트 발생
        OnScoreChanged?.Invoke(currentScore);
        
        Debug.Log($"점수 추가 2@@@@@@@23232@@@@@@@@@@@@@@@22222: +{waveValue}, 현재 점수: {currentScore}");
    }
    
    // 현재 게임 상태 반환 (필요한 경우)
    public bool IsGameActive()
    {
        return isGameActive;
    }
    
    // 현재 난이도 정보 반환 (UI 표시 등에 사용 가능)
    public float GetCurrentSpawnInterval()
    {
        return currentSpawnInterval;
    }
    
    // 현재 레벨 반환
    public int GetCurrentLevel()
    {
        return CommonVars.level;
    }
    
    // 현재 점수 반환
    public int GetCurrentScore()
    {
        return currentScore;
    }
    
    // 생성된 총 행 수 반환
    public int GetRowsSpawned()
    {
        return rowsSpawned;
    }
}