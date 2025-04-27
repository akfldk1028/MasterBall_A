using VContainer;
using VContainer.Unity;
using Unity.Assets.Scripts.Scene;
using Unity.Assets.Scripts.UI;
using UnityEngine;
using Unity.Assets.Scripts.Resource;
using System;
using Unity.Assets.Scripts.Objects;
using Unity.Netcode;
using Unity.Assets.Scripts.Data;

public class BasicGameLifetimeScope : LifetimeScope
{
    [Inject] private DebugClassFacade _debugClassFacade;

    protected override void Configure(IContainerBuilder builder)
    {
        _debugClassFacade?.LogInfo(GetType().Name, "BasicGameLifetimeScope Configure 시작");
   
        base.Configure(builder);

        ResourceManager resourceManager = null;
        resourceManager = Parent.Container.Resolve<ResourceManager>();
        builder.RegisterInstance(resourceManager);
        UIManager _uiManager = null;
        _uiManager = Parent.Container.Resolve<UIManager>();
        builder.RegisterInstance(_uiManager);
        ObjectManager _objectManager = null;
        _objectManager = Parent.Container.Resolve<ObjectManager>();
        builder.RegisterInstance(_objectManager);
        NetUtils _netUtils = null;
        _netUtils = Parent.Container.Resolve<NetUtils>();
        builder.RegisterInstance(_netUtils);

        // BrickGameManager _brickGameManager = null;
        // _brickGameManager = Parent.Container.Resolve<BrickGameManager>();
        // builder.RegisterInstance(_brickGameManager);

        builder.RegisterComponentInHierarchy<BrickGameManager>();
               
        ReleaseGameManager _releaseGameManager = null;
        _releaseGameManager = Parent.Container.Resolve<ReleaseGameManager>();
        builder.RegisterInstance(_releaseGameManager);
 




        // 기본 매니저 등록
        builder.Register<MapManager>(Lifetime.Singleton);

        // 씬 관련 컴포넌트 등록
        _debugClassFacade?.LogInfo(GetType().Name, "BasicGameScene 등록 시도");
        builder.RegisterComponentInHierarchy<BasicGameScene>();
       
        _debugClassFacade?.LogInfo(GetType().Name, "UI_BasicGame 등록 시도");
        builder.RegisterComponentInHierarchy<UI_BasicGameScene>();

  
        // 컨테이너 빌드 후 초기화를 수행하는 콜백 등록
        builder.RegisterBuildCallback(container => {
            try {
                Debug.Log("[BasicGameLifetimeScope] BasicGameState Prefab 스폰 시작");
                BasicGameState _basicGameState = null;
                if (NetworkManager.Singleton.IsServer)
                {
                    // Prefab 인스턴스화
                    resourceManager.Load<GameObject>("BasicGameLifetimeScope_Server");
                    GameObject go = resourceManager.Instantiate("BasicGameLifetimeScope_Server");

                    _basicGameState = Parent.Container.Resolve<BasicGameState>();
                    builder.RegisterInstance(_basicGameState);
                        
                    // DI 컨테이너로 의존성 주입
                    container.InjectGameObject(go);
                    Debug.Log("[BasicGameLifetimeScope] Prefab DI 의존성 주입 완료");
                    
                    // 네트워크 스폰
                    var netObj = go.GetComponent<NetworkObject>();
                    if (netObj != null)
                    {
                        netObj.Spawn();
                        Debug.Log("[BasicGameLifetimeScope] BasicGameState Prefab Spawn() 호출됨");
                    }
                    
                    // 초기화 호출
                    var basicGameState = go.GetComponent<BasicGameState>();
                    if (basicGameState != null)
                    {
                        basicGameState.Initialize();
                        basicGameState.OnNetworkSpawn();
                        Debug.Log("[BasicGameLifetimeScope] BasicGameState.Initialize() 호출됨");
                    }
                }
            }
            catch (Exception ex) {
                Debug.LogError($"[BasicGameLifetimeScope] 초기화 중 오류 발생: {ex.Message}\n{ex.StackTrace}");
            }
        });

        _debugClassFacade?.LogInfo(GetType().Name, "BasicGameLifetimeScope Configure 완료");
    }
}