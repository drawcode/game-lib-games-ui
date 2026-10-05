#define DEV
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using Engine.Events;

public class UIPanelDialogRPGEnergy : UIPanelDialogRPGObject {

    public static UIPanelDialogRPGEnergy Instance;

    public override void Awake() {

        if (Instance != null && this != Instance) {
            //There is already a copy of this script running
            //Destroy(gameObject);
            return;
        }

        Instance = this;
        
        panelTypes.Add(UIPanelBaseTypes.typeDialogHUD);
    }

    // TOOLKIT (B3): the shared panel-dialog-rpg view, filled with this stat. Same read as the
    // UIGameRPGEnergy meter in the scene tree (rounded to 2 places, default 1).
    public override string toolkitStatCode {
        get {
            return "energy";
        }
    }

    public override double GetToolkitStatValue() {
        return Math.Round(GameProfileCharacters.currentProgress.GetGamePlayerProgressEnergy(1), 2);
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

        //loadData();
    }

    public override void Start() {
        Init();
    }

    // EVENTS
 
    public override void OnEnable() {

        base.OnEnable();
    }
    
    public override void OnDisable() {

        base.OnDisable();
    }

    public override void OnButtonClickEventHandler(string buttonName) {
        if(UIUtil.IsButtonClicked(buttonBuyRecharge, buttonName)) {
            GameStoreController.Purchase("rpg-recharge-full-1", 1);
        }
        else if(UIUtil.IsButtonClicked(buttonEarn, buttonName)) {
            GameController.QuitGame();
#if ENABLE_FEATURE_GAME_MODE
            GameUIController.ShowGameMode();
#endif
        }
        else if(UIUtil.IsButtonClicked(buttonResume, buttonName)) {
            HideAll();
            GameController.ResumeGame();
        }
    }

    // SHOW/LOAD

    public static void ShowDefault() {
        if(isInst) {
            Instance.AnimateIn();
        }
    }

    public static void HideAll() {
        if(isInst) {
            Instance.AnimateOut();
        }
    }

    public static void LoadData() {
        if(Instance != null) {
            Instance.loadData();
        }
    }

    public void loadData() {

    }

    public override void AnimateIn() {
        base.AnimateIn();

    }

    public override void AnimateOut() {
        base.AnimateOut();

    }

    public override void Update() {
        base.Update();
    }
 
}
