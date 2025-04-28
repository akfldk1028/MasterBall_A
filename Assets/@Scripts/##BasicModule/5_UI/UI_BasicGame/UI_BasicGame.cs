using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Assets.Scripts.Objects;
using UnityEngine.UI;
using VContainer;
using TMPro;
using Unity.Netcode;

public class UI_BasicGameScene : UI_Scene
{
    enum Buttons
    {
      Summon_B,
    }

    enum Texts
    {
        ScoreText_T,
    }

    enum Sliders
    {
    }

    enum Objects
    {
        Waiting,
    }

    // Waiting 오브젝트 참조를 저장할 변수 선언
    GameObject waitingObject;

    [Tooltip("터치를 막을 메인 UI 요소들을 포함하는 CanvasGroup")]
    public CanvasGroup mainUICanvasGroup;

    private CanvasGroup _mainUICanvasGroupComponent;
    private PhysicsPlank _physicsPlank;
    private TMP_Text ScoreText_T;

    // --- PlayerController 참조 추가 ---
    private PlayerController _localPlayerController;
    // --------------------------------

    // BrickGameManager 참조 제거 또는 주석 처리
    // private BrickGameManager _brickGameManager;

    // 이벤트 제거
    // public static event Action OnSummonButtonUIClicked;

    public override bool Init()
    {
        Debug.Log("<color=magenta>[UI_BasicGameScene] Init() called!</color>");

        if (base.Init() == false)
            return false;

        BindButtons(typeof(Buttons));
        BindTexts(typeof(Texts));
        BindSliders(typeof(Sliders));
        BindObjects(typeof(Objects));
        PhysicsBall.OnHitBottom += HandleBallHitBottom;

        // Get CanvasGroup component
        _mainUICanvasGroupComponent = mainUICanvasGroup;

        // Find PhysicsPlank
        _physicsPlank = FindFirstObjectByType<PhysicsPlank>();
        if (_physicsPlank == null)
        {
            Debug.LogError("[UI_BasicGameScene] PhysicsPlank not found in the scene!");
        }

        waitingObject = GetObject((int)Objects.Waiting);
        if (waitingObject == null)
        {
            Debug.LogError("[UI_BasicGameScene] Failed to get 'Waiting' object...", this);
        }
        // BrickGameManager 찾기 제거
        // _brickGameManager = FindObjectOfType<BrickGameManager>();
        // if (_brickGameManager == null)
        // {
        //     Debug.LogWarning("BrickGameManager를 찾을 수 없습니다. 점수가 추가되지 않을 수 있습니다.");
        // }


        // 버튼 바인딩 (메서드 이름 변경, 초기 비활성화)
        Button fireButton = GetButton((int)Buttons.Summon_B);
        if (fireButton != null)
        {
             fireButton.gameObject.BindEvent(OnClickFireButton); // 메서드 이름 변경
             fireButton.interactable = false; // 초기에는 비활성화
        }
        else
        {
             Debug.LogError("[UI_BasicGameScene] Summon_B (Fire) button not found!");
        }

        // 점수 텍스트 참조
        ScoreText_T = GetText((int)Texts.ScoreText_T)?.GetComponent<TMP_Text>();
        if (ScoreText_T == null)
        {
            Debug.LogError("[UI_BasicGameScene] ScoreText_T is null...", this);
        }
        // BrickGameManager 이벤트 구독 제거
        // if (ScoreText_T != null && _brickGameManager != null)
        // {
        //      _brickGameManager.OnScoreChanged += UpdateScoreDisplay;
        //     UpdateScoreDisplay(_brickGameManager.GetCurrentScore()); // 초기 호출 제거
        // }
        // else
        // {
        //     Debug.LogError("[UI_BasicGameScene] BrickGameManager 또는 ScoreText_T가 null이라 이벤트 구독 실패");
        // }

        // --- PlayerController 직접 찾고 구독 --- 
        _localPlayerController = FindObjectOfType<PlayerController>();
        if (_localPlayerController != null)
        {
            Debug.Log("[UI_BasicGameScene] 로컬 PlayerController 찾음! 로컬 점수 이벤트 구독 시작.");
            _localPlayerController.OnLocalScoreChanged += UpdateScoreDisplay;
            // 초기 로컬 점수 UI 업데이트
            UpdateScoreDisplay(_localPlayerController.GetCurrentLocalScore());
            // 발사 버튼 활성화
            if (fireButton != null) fireButton.interactable = true;
        }
        else
        {
            Debug.LogError("[UI_BasicGameScene] Init: 씬에서 PlayerController를 찾을 수 없습니다!");
            // 컨트롤러 못 찾았으니 발사 버튼 비활성화 유지
            if (fireButton != null) fireButton.interactable = false;
        }
        // -------------------------------------

        Refresh();
        return true;
    }

    private float _elapsedTime = 0.0f;
    private float _updateInterval = 1.0f;
    private void Update()
    {
        _elapsedTime += Time.deltaTime;

        // UpdateScoreDisplay(BrickGameManager.GetCurrentScore());

        // if (_elapsedTime >= _updateInterval)
        // {
        //     float fps = 1.0f / Time.deltaTime;
        //     float ms = Time.deltaTime * 1000.0f;
        //     string text = string.Format("{0:N1} FPS ({1:N1}ms)", fps, ms);
        //     GetText((int)Texts.GoldCountText).text = text;

        //     _elapsedTime = 0;
        // }
    }
    
    public void SetInfo(/* BrickGameManager brickGameManager - 제거 */)
    {
        Refresh();
    }

    void Refresh()
    {
        if (_init == false)
            return;
    }

    // OnClickSummonButton -> OnClickFireButton 이름 변경 및 로직 수정
    void OnClickFireButton(PointerEventData evt)
    {
        Debug.Log("[UI_BasicGameScene] Fire Button clicked.");

        if (_localPlayerController != null)
        {
            // 로컬 플레이어 컨트롤러에 발사 요청
            _localPlayerController.RequestFireCannon();
             Debug.Log("[UI_BasicGameScene] RequestFireCannon() 호출함.");
        }
        else
        {
            Debug.LogError("[UI_BasicGameScene] Fire Button 클릭 시 로컬 PlayerController 참조가 없습니다! 이벤트가 아직 발생하지 않았거나 핸들러에 문제가 있을 수 있습니다.");
            // 이벤트 기반에서는 재시도 코루틴이 의미 없음
        }
    }

    // UpdateScoreDisplay 파라미터 및 로직 수정
    private void UpdateScoreDisplay(int newScore)
    {
        Debug.Log($"[UI_BasicGameScene] UpdateScoreDisplay 호출됨. 새 점수: {newScore}");

        if (ScoreText_T == null)
        {
            Debug.LogError("[UI_BasicGameScene] UpdateScoreDisplay 내부에서 ScoreText_T가 null입니다!");
            return;
        }

        ScoreText_T.text = newScore.ToString(); // 새 점수로 업데이트
    }

    private void OnDestroy()
    {
        Debug.Log("<color=red>[UI_BasicGameScene] OnDestroy called! Unsubscribing from events.</color>");

        // 이벤트 구독 해제
        PhysicsBall.OnHitBottom -= HandleBallHitBottom;
        // --- 정적 이벤트 구독 해제 제거 ---
        // PlayerController.OnLocalPlayerSpawned -= HandleLocalPlayerSpawned;
        // --------------------------------

        // PlayerController 로컬 점수 이벤트 구독 해제 (유지)
        if (_localPlayerController != null)
        {
            _localPlayerController.OnLocalScoreChanged -= UpdateScoreDisplay;
        }
    }

    private void HandleBallHitBottom()
    {
        StartCoroutine(ShowWaitingAndBlockInputCoroutine());
    }

    private IEnumerator ShowWaitingAndBlockInputCoroutine()
    {
        Debug.Log("<color=green>ShowWaitingAndBlockInputCoroutine</color>");
        
        // 메인 UI 터치 및 플랭크 이동 비활성화
        if (_mainUICanvasGroupComponent != null)
        {
            _mainUICanvasGroupComponent.interactable = false;
            Debug.Log("[UI_BasicGameScene] Main UI interaction disabled.");
        }
        _physicsPlank.CanMove = false;

        
        // waitingObject 활성화
        if (waitingObject != null)
        {
            _mainUICanvasGroupComponent.interactable = false;

            Debug.Log("[UI_BasicGameScene] Ball hit bottom. Showing Waiting UI for 3 seconds.");
            waitingObject.SetActive(true); // Waiting UI 활성화

            yield return new WaitForSeconds(3f); // 3초 대기

            waitingObject.SetActive(false); // Waiting UI 비활성화
            Debug.Log("[UI_BasicGameScene] Hiding Waiting UI after 3 seconds.");
        }
        
        // 메인 UI 터치 및 플랭크 이동 다시 활성화
        if (_mainUICanvasGroupComponent != null)
        {
            _mainUICanvasGroupComponent.interactable = true;
            Debug.Log("[UI_BasicGameScene] Main UI interaction enabled.");
        }
        if (_physicsPlank != null)
        {
            _physicsPlank.CanMove = true;
            Debug.Log("[UI_BasicGameScene] Plank movement enabled.");
        }
    }
}