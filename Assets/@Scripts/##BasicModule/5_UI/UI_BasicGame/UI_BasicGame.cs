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

    private BrickGameManager _brickGameManager;

    // Summon 버튼 클릭 시 발생하는 정적 이벤트 (복원)
    public static event Action OnSummonButtonUIClicked;
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
        _brickGameManager = FindObjectOfType<BrickGameManager>();
        if (_brickGameManager == null)
        {
            Debug.LogWarning("BrickGameManager를 찾을 수 없습니다. 점수가 추가되지 않을 수 있습니다.");
        }


        // 버튼 바인딩 (초기 비활성화 로직 제거)
        Button summonButton = GetButton((int)Buttons.Summon_B);
        if (summonButton != null)
        {
             summonButton.gameObject.BindEvent(OnClickSummonButton);
             // summonButton.interactable = false; // 제거
        }
        else
        {
             Debug.LogError("[UI_BasicGameScene] Summon_B button not found!");
        }

        // 점수 텍스트 참조 및 이벤트 구독
        ScoreText_T = GetText((int)Texts.ScoreText_T)?.GetComponent<TMP_Text>();
        if (ScoreText_T == null)
        {
            Debug.LogError("[UI_BasicGameScene] ScoreText_T is null...", this);
        }
        // if(_brickGameManager == null)
        // {
        //     Debug.LogError("[UI_BasicGameScene] BrickGameManager is null...", this);
        // }
        if (ScoreText_T != null)
        {
             _brickGameManager.OnScoreChanged += UpdateScoreDisplay;
            UpdateScoreDisplay(_brickGameManager.GetCurrentScore());
        }
        else
        {
            Debug.LogError("[UI_BasicGameScene] BrickGameManager 또는 ScoreText_T가 null이라 이벤트 구독 실패");
        }

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
    
    public void SetInfo(BrickGameManager brickGameManager)
    {
        _brickGameManager = brickGameManager;
        Refresh();
    }

    void Refresh()
    {
        if (_init == false)
            return;
    }

    void OnClickSummonButton(PointerEventData evt)
    {
        Debug.Log("[UI_BasicGameScene] OnClickSummonButton method entered.");

        // 정적 이벤트 발생시키기
        OnSummonButtonUIClicked?.Invoke();
        Debug.Log("[UI_BasicGameScene] OnSummonButtonUIClicked event invoked.");
    }

    private void UpdateScoreDisplay(int newScore)
    {
        if (ScoreText_T != null)
        {
            ScoreText_T.text = newScore.ToString();
            Debug.Log($"[UI_BasicGameScene] Updating Score Display (ScoreText_T): {newScore}");
        }
    }

    private void OnDestroy()
    {
        Debug.Log("<color=red>[UI_BasicGameScene] OnDestroy called! Unsubscribing from events.</color>");

        // 이벤트 구독 해제
        PhysicsBall.OnHitBottom -= HandleBallHitBottom;

        // BrickGameManager 이벤트 구독 해제 (유지)
        if (ScoreText_T != null)
        {
            _brickGameManager.OnScoreChanged -= UpdateScoreDisplay;
        }

        // PlayerController 이벤트 구독 해제 부분 없음 (제거됨)
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