using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
#else
using UnityEngine.UI;
#endif

using Engine.Events;

public class BaseGameUIPanelResultsBase : MonoBehaviour {

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
    public UILabel totalScoreComputed;
    public UILabel totalScore;
	public UILabel totalScores;
	public UILabel totalSpecials;
	public UILabel totalCoins;
    public UILabel totalTime;
    public UILabel totalKills;
#else
    // 2.11: agnostic UIRef handles, bound at runtime by name.
    public Engine.UI.UIRef totalScoreComputed;
    public Engine.UI.UIRef totalScore;
    public Engine.UI.UIRef totalScores;
    public Engine.UI.UIRef totalSpecials;
    public Engine.UI.UIRef totalCoins;
    public Engine.UI.UIRef totalTime;
    public Engine.UI.UIRef totalKills;
#endif

    public virtual void OnEnable() {

        Messenger<string>.AddListener(ButtonEvents.EVENT_BUTTON_CLICK, OnButtonClickEventHandler);
    }

    public virtual void OnDisable() {

        Messenger<string>.RemoveListener(ButtonEvents.EVENT_BUTTON_CLICK, OnButtonClickEventHandler);
    }

	public virtual void Start() {
		Init();
	}
	
	public virtual void Init() {

		loadData();
	}
		 
    public virtual void OnButtonClickEventHandler(string buttonName) {
		//Debug.Log("OnButtonClickEventHandler: " + buttonName);
	}

	public virtual void UpdateDisplay(GamePlayerRuntimeData runtimeData, float timeTotal) {
        
        UIUtil.SetLabelValue(totalTime, FormatUtil.GetFormattedTimeHoursMinutesSecondsMs((double)timeTotal));

        UIUtil.SetLabelValue(totalCoins, runtimeData.coins.ToString("N0", Engine.Game.App.BaseApp.L10n.NumberFormat));
		UIUtil.SetLabelValue(totalScores, runtimeData.scores.ToString("N0", Engine.Game.App.BaseApp.L10n.NumberFormat));
		UIUtil.SetLabelValue(totalScore, runtimeData.score.ToString("N0", Engine.Game.App.BaseApp.L10n.NumberFormat));
        UIUtil.SetLabelValue(totalSpecials, runtimeData.specials.ToString("N0", Engine.Game.App.BaseApp.L10n.NumberFormat));
        UIUtil.SetLabelValue(totalKills, runtimeData.kills.ToString("N0", Engine.Game.App.BaseApp.L10n.NumberFormat));

        double totalScoreValue = runtimeData.totalScoreValue;
        UIUtil.SetLabelValue(totalScoreComputed, totalScoreValue.ToString("N0", Engine.Game.App.BaseApp.L10n.NumberFormat));
    }
	
	public virtual void loadData() {
		//StartCoroutine(loadDataCo());
	}
	
	public virtual IEnumerator loadDataCo() {
		
		yield return new WaitForSeconds(1f);
	}
	
}
