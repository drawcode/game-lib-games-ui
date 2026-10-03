#define DEV
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using Engine.Events;

public class UIPanelDialogRPGDefense : UIPanelDialogRPGObject {

    // B3: no scene object, so no layout of its own. It inherits the shared panel-dialog-rpg view
    // from UIPanelDialogRPGObject with the static copy only (toolkitStatCode stays empty, there
    // are no defense strings) and no meter (GetToolkitStatValue stays -1); its button names fall
    // back to ButtonRPGDefenseBuyRecharge / -Resume / -Missions. The stale UGUI using went with B10.

    public static UIPanelDialogRPGDefense Instance;

    public override void Awake() {

        if (Instance != null && this != Instance) {
            //There is already a copy of this script running
            //Destroy(gameObject);
            return;
        }

        Instance = this;
        
        panelTypes.Add(UIPanelBaseTypes.typeDialogHUD);
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

            // buy recharge
            LogUtil.Log("Recharge:");
        }
        else if(UIUtil.IsButtonClicked(buttonEarn, buttonName)) {
            HideAll();
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
