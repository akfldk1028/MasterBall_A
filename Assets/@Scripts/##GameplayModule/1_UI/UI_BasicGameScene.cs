using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class UI_BasicGameScene : UI_Scene
{
    enum Buttons
    {
      Summon_B,
    }

    enum Texts
    {
        LevelText,
        BattlePowerText,
        GoldCountText,
        DiaCountText,
        MeatCountText,
        WoodCountText,
        MineralCountText,
    }

    enum Sliders
    {
        MeatSlider,
        WoodSlider,
        MineralSlider,
    }

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        BindButtons(typeof(Buttons));
        BindTexts(typeof(Texts));
        BindSliders(typeof(Sliders));

        GetButton((int)Buttons.Summon_B).gameObject.BindEvent(OnClickSummonButton);
     
        Refresh();
        
        return true;
    }

    private float _elapsedTime = 0.0f;
    private float _updateInterval = 1.0f;

    private void Update()
    {
        _elapsedTime += Time.deltaTime;

        // if (_elapsedTime >= _updateInterval)
        // {
        //     float fps = 1.0f / Time.deltaTime;
        //     float ms = Time.deltaTime * 1000.0f;
        //     string text = string.Format("{0:N1} FPS ({1:N1}ms)", fps, ms);
        //     GetText((int)Texts.GoldCountText).text = text;

        //     _elapsedTime = 0;
        // }
    }
    
    public void SetInfo()
    {
        Refresh();
    }

    void Refresh()
    {
        if (_init == false)
            return;
    }
    public static event Action OnSummonButtonUIClicked;

    void OnClickSummonButton(PointerEventData evt)
    {
        Debug.Log("<color=green>OnOnClickSummonButton</color>");
        Debug.Log("<color=green>OnOnClickSummonButton</color>");
        Debug.Log("<color=green>OnOnClickSummonButton</color>");
        Debug.Log("<color=green>OnOnClickSummonButton</color>");
        Debug.Log("<color=green>OnOnClickSummonButton</color>");
        Debug.Log("<color=green>OnOnClickSummonButton</color>");
        Debug.Log("<color=green>OnOnClickSummonButton</color>");
        Debug.Log("<color=green>OnOnClickSummonButton</color>");
        OnSummonButtonUIClicked?.Invoke();

        // -------------------------------
    }

    void OnClickDiaPlusButton(PointerEventData evt)
    {
        Debug.Log("OnClickDiaPlusButton");
    }

    void OnClickHeroesListButton(PointerEventData evt)
    {
		Debug.Log("OnClickHeroesListButton");
	}

    void OnClickSetHeroesButton(PointerEventData evt)
    {
		Debug.Log("OnClickSetHeroesButton");
	}

    void OnClickSettingButton(PointerEventData evt)
    {
		Debug.Log("OnClickSettingButton");
	}

    void OnClickInventoryButton(PointerEventData evt)
    {
        Debug.Log("OnClickInventoryButton");
    }

    void OnClickWorldMapButton(PointerEventData evt)
    {
        Debug.Log("OnClickWorldMapButton");
    }

    void OnClickQuestButton(PointerEventData evt)
    {
        Debug.Log("OnClickQuestButton");
    }

    void OnClickChallengeButton(PointerEventData evt)
    {
        Debug.Log("OnOnClickChallengeButton");
    }

    void OnClickCampButton(PointerEventData evt)
    {
        Debug.Log("OnClickCampButton");
    }

    void OnClickPortalButton(PointerEventData evt)
    {
        Debug.Log("OnClickPortalButton");
	}

    void OnClickCheatButton(PointerEventData evt)
    {
		Debug.Log("OnClickCheatButton");
	}

}