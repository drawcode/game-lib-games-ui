#define DEV
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Engine.Events;

public class UIPanelModeTypeCollection : UIPanelModeTypeBase {

    public static UIPanelModeTypeCollection Instance;

    // B5: GamePanelModeTypeCollection carries the SAME four quiz cards as GamePanelModeTypeChoiceQuiz
    // (names, text, colours, geometry), so it loads the same view. This panel drives none of them --
    // no label fields, no card tweens -- and in the scene they stay parked off-screen, so a show
    // draws an EMPTY overlay, exactly as the legacy one does. The view root and its cards are
    // non-picking, so that empty overlay does not swallow taps. Nothing calls ShowDefault today.
    public override string toolkitViewKey {
        get {
            return BaseUIPanel.panelModeTypeChoice;
        }
    }

    protected override string[][] toolkitModeColorTargets {
        get {
            return UIPanelModeTypeViews.quizModeColorTargets;
        }
    }

    public override void Awake() {
        base.Awake();

        if(Instance != null && this != Instance) {
            //There is already a copy of this script running
            //Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public static bool isInst {
        get {
            if(Instance != null) {
                return true;
            }
            return false;
        }
    }

    public override void Init() {
        base.Init();

        loadData();
    }

    public override void Start() {
        Init();
    }

    public override void OnEnable() {
        base.OnEnable();
    }

    public override void OnDisable() {
        base.OnDisable();
    }

    public override void OnButtonClickEventHandler(string buttonName) {

    }

    public static void ShowDefault() {
        if(isInst) {
            Instance.showDefault();
        }
    }

    public static void HideAll() {
        if(isInst) {
            Instance.hideAll();
        }
    }
    
    public void showDefault() {

        ShowCamera();

        AnimateIn();
        loadData();
    }
    
    public void hideAll() {

        AnimateOut();

        HideCamera(.5f);
    }

    public static void LoadData() {
        if(Instance != null) {
            Instance.loadData();
        }
    }

    public void loadData() {
        StartCoroutine(loadDataCo());
    }

    IEnumerator loadDataCo() {
        yield return new WaitForSeconds(1f);
    }
}