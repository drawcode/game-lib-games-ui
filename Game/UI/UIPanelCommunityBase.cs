#define DEV
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using Engine.Events;
using Engine.UI;

public class UIPanelCommunityBase : UIPanelBase {

    // ----------------------------------------------------------------------------------------
    // TOOLKIT (B6 -- the community panels; see UIPanelCommunityToolkit for the hosting model)

    UIPanelCommunityToolkit _toolkit;

    public UIPanelCommunityToolkit toolkit {
        get {
            if(_toolkit == null) {
                _toolkit = new UIPanelCommunityToolkit(this);
            }
            return _toolkit;
        }
    }

    // Above the chrome band: the legacy CommunityCamera (depth 53) draws over the UI (10) and
    // dialog (15) cameras. UIPanelCommunityBackground sits one below, under the dialog cards.
    public override int toolkitSortOrder {
        get {
            return UILayers.overlay;
        }
    }

    // Scene-resident (the UICommunity prefab instance at the GameSceneDynamic root, enabled at
    // scene load), and the cards it carries are shown by code paths that never show the panel
    // itself -- so the view has to exist before the first card does.
    public override bool toolkitPreloadView {
        get {
            return true;
        }
    }

    // The CARDS slide (UIPanelCommunityToolkit.SetCard), never the view: the panel-level
    // AnimateIn/AnimateOut a few callers fire (HideAll -> AnimateOut on Broadcast/Camera) must not
    // move or hide the full-screen layer every card lives on.
    protected override void ShowToolkitViewSlide() {
    }

    protected override void HideToolkitViewSlide() {
    }

    // UIPanelBase.ShowPanel shows the root outright; here the draw gate decides (LateUpdate).
    public override void ShowPanel() {

        if(isToolkitPanel) {
            toolkit.InvalidateRoot();
            return;
        }

        base.ShowPanel();
    }

    public override void HidePanel() {

        if(isToolkitPanel) {
            return;
        }

        base.HidePanel();
    }

    protected override void SuppressLegacyView() {
        // NOT base: there is no panelContainer; the panel's own children are what the view replaces.
        toolkit.ReassertSuppression();
    }

    protected override void FreeToolkitView() {

        toolkit.RestoreSuppressed();

        base.FreeToolkitView();
    }

    public override void BindElements(UIRef root) {

        base.BindElements(root);

        if(!isToolkitPanel) {
            return;
        }

        toolkit.Bind();
    }

    // LateUpdate, not Update: the subclasses already override Update.
    public virtual void LateUpdate() {

        if(!isToolkitPanel) {
            return;
        }

        toolkit.ReassertSuppression();
        toolkit.UpdateRoot();
    }

    // ----------------------------------------------------------------------------------------

    public override void Awake() {
        base.Awake();
    }

    public override void Init() {
        base.Init();

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
        base.OnButtonClickEventHandler(buttonName);
    }

    public virtual void showDialog() {

#if USE_GAME_LIB_GAMEVERSES
        GameCommunity.HideGameCommunity();

        UIPanelCommunityBackground.ShowBackground();
#endif

        if(GameController.Instance.gameRunningState == GameRunningState.RUNNING) {
            GameController.GameRunningStateOverlay();
        }
    }

    public virtual void hideDialog() {

#if USE_GAME_LIB_GAMEVERSES
        UIPanelCommunityBackground.HideBackground();
#endif

        if(GameController.Instance.gameRunningState != GameRunningState.RUNNING
           && GameController.Instance.gameState == GameStateGlobal.GameOverlay) {
            GameController.GameRunningStateRun();
        }
    }

    public override void AnimateOut() {
        base.AnimateOut();
    }

    public virtual void Update() {
        //base.Update();
    }
}