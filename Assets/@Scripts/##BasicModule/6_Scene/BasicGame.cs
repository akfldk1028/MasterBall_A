using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Assets.Scripts.Scene;
using VContainer;
using Unity.Assets.Scripts.Resource;
using Object = UnityEngine.Object;
using Unity.Netcode;
using Unity.Assets.Scripts.UI;
using Unity.Assets.Scripts.Objects;
using VContainer.Unity;
using System;
using static Define;


namespace Unity.Assets.Scripts.Scene
{
public class BasicGameScene : BaseScene
{
    // [Inject] public MapSpawnerFacade _mapSpawnerFacade;
	// [Inject] private ObjectManagerFacade _objectManagerFacade;
	// [Inject] private ServerMonster _serverMonster; // MonoBehaviour는 이런 방식으로 주입받을 수 없습니다.
	
    // VContainer.IObjectResolver 추가
    [Inject] private VContainer.IObjectResolver _container;
	// greenslime 몬스터 ID
// BasicGameState.cs:
// 게임의 핵심 상태 (점수, 시간, 레벨 등)를 소유하고 관리합니다.
// 네트워크 동기화가 필요한 상태는 NetworkVariable로 관리합니다.
// 게임의 시작, 종료, 핵심 오브젝트 스폰 등 게임 흐름을 제어합니다.
// UI_BasicGame.cs:
// BasicGame 씬 내의 모든 UI 요소 (점수 텍스트, 시간 텍스트, 버튼 등)를 참조하고 제어합니다.
// BasicGameState를 주입(Inject) 받아 필요한 상태 값을 가져옵니다.
// BasicGameState의 NetworkVariable 변경 사항이나 관련 이벤트를 구독(Subscribe)합니다.
// 상태 변경 알림을 받으면 UI 요소를 업데이트하여 사용자에게 보여줍니다.
// UI 버튼 클릭 등 사용자 입력을 처리하고, 필요하면 BasicGameState나 다른 서비스의 메서드를 호출합니다. (예: 일시정지 버튼 -> _gameState.PauseGameRequest())
// BasicGameScene.cs:
// 역할이 줄어듭니다. 주로 씬 자체의 설정 (배경음악, 카메라, 씬 전용 효과 등) 및 씬 생명주기 관련 초기화/정리 작업을 담당할 수 있습니다.
// UI_BasicGame이나 BasicGameState를 직접 제어할 필요가 없을 수도 있습니다. (필요하다면 주입받아 사용할 수는 있습니다.)


    private bool _isInitialized = false;
	
    private bool _isEventsSubscribed = false;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        SceneType = EScene.BasicGame;
 
        return true;
    }

    public override void Clear()
    {


    }

    private void OnEnable()
    {
        // 이벤트 구독은 Init에서만 수행
    }

    private void OnDisable()
    {
        // 이벤트 해제는 Clear에서만 수행
    }



}

}