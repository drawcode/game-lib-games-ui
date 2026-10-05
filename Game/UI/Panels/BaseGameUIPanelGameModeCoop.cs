using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Engine.Events;

#if ENABLE_FEATURE_MODE_COOP

public class BaseGameUIPanelGameModeCoop : GameUIPanelBase {

    public static GameUIPanelGameModeCoop Instance;

    public GameObject listItemPrefab;

    public static bool isInst {
        get {
            if(Instance != null) {
                return true;
            }
            return false;
        }
    }

    public override void Awake() {
        base.Awake();
    }

    public override void Start() {
        Init();
    }

    public override void Init() {
        base.Init();

        loadData();
    }

    public override void OnEnable() {

        Messenger<string>.AddListener(ButtonEvents.EVENT_BUTTON_CLICK, OnButtonClickEventHandler);

        Messenger<string>.AddListener(
            UIControllerMessages.uiPanelAnimateIn,
            OnUIControllerPanelAnimateIn);

        Messenger<string>.AddListener(
            UIControllerMessages.uiPanelAnimateOut,
            OnUIControllerPanelAnimateOut);

        Messenger<string, string>.AddListener(
            UIControllerMessages.uiPanelAnimateType,
            OnUIControllerPanelAnimateType);
    }

    public override void OnDisable() {

        Messenger<string>.RemoveListener(ButtonEvents.EVENT_BUTTON_CLICK, OnButtonClickEventHandler);

        Messenger<string>.RemoveListener(
            UIControllerMessages.uiPanelAnimateIn,
            OnUIControllerPanelAnimateIn);

        Messenger<string>.RemoveListener(
            UIControllerMessages.uiPanelAnimateOut,
            OnUIControllerPanelAnimateOut);

        Messenger<string, string>.RemoveListener(
            UIControllerMessages.uiPanelAnimateType,
            OnUIControllerPanelAnimateType);
        // Chain to base so UIPanelBase.OnDisable -> FreeToolkitView runs when this panel is
        // pooled away, else the toolkit view leaks once the panel has one. Phase-3 migration
        // prerequisite (same fix the settings/header/footer bases got in 3A/3B).
        base.OnDisable();
    }

    public override void OnUIControllerPanelAnimateIn(string classNameTo) {
        if(className == classNameTo) {
            AnimateIn();
        }
    }

    public override void OnUIControllerPanelAnimateOut(string classNameTo) {
        if(className == classNameTo) {
            AnimateOut();
        }
    }

    public override void OnUIControllerPanelAnimateType(string classNameTo, string code) {
        if(className == classNameTo) {
            //
        }
    }

    // THE CO-BOT NETWORK ROW — START GAME / STOP GAME / JOIN GAME.
    //
    // Dead since long before the migration (iter 19 audit): the three names have consts in
    // BaseUIButtonNames and no comparison anywhere, so the tiles have always broadcast into
    // nothing. Handled here, on the panel that DRAWS them (rule 90).
    //
    // The session name is the player's profile username, which is the only identity this screen
    // has to offer; Leave() takes none.
    public static string buttonNameGameNetworkStartGame = "ButtonGameNetworkStartGame";
    public static string buttonNameGameNetworkStopGame = "ButtonGameNetworkStopGame";
    public static string buttonNameGameNetworkJoinGame = "ButtonGameNetworkJoinGame";

    // The transport is compiled OUT in this project: Gameverses.GameNetworking dispatches every
    // call behind NETWORK_PHOTON, and the file itself opens with `#define NETWORK_PHOTON_OFF`
    // (NETWORK_USE_UNITY is commented out on the other side), so Create/Join/Leave are empty
    // shells on every platform here. Wiring them straight through would have replaced three dead
    // tiles with three tiles that log and do nothing — so the row is HIDDEN while the transport is
    // absent, and the handler below is what makes it work the moment NETWORK_PHOTON is defined.
    //
    // Toolkit path only: the legacy prefab still draws whatever it drew under the kill switch.
    private void SyncNetworkRowVisibility() {

#if !NETWORK_PHOTON
        if(!isToolkitPanel) {
            return;
        }

        UIUtil.HideObject(UIUtil.ResolveDeep(viewRoot, buttonNameGameNetworkStartGame));
        UIUtil.HideObject(UIUtil.ResolveDeep(viewRoot, buttonNameGameNetworkStopGame));
        UIUtil.HideObject(UIUtil.ResolveDeep(viewRoot, buttonNameGameNetworkJoinGame));
#endif
    }

    public override void BindElements(Engine.UI.UIRef root) {

        base.BindElements(root);

        SyncNetworkRowVisibility();
    }

    public override void OnButtonClickEventHandler(string buttonName) {

        if(UIUtil.IsButtonClicked(buttonNameGameNetworkStartGame, buttonName)) {
            Gameverses.GameNetworking.Create(GameProfiles.Current.username);
        }
        else if(UIUtil.IsButtonClicked(buttonNameGameNetworkJoinGame, buttonName)) {
            Gameverses.GameNetworking.Join(GameProfiles.Current.username);
        }
        else if(UIUtil.IsButtonClicked(buttonNameGameNetworkStopGame, buttonName)) {
            Gameverses.GameNetworking.Leave();
        }
    }

    public override void HandleShow() {
        base.HandleShow();

        buttonDisplayState = UIPanelButtonsDisplayState.None;
        characterDisplayState = UIPanelCharacterDisplayState.CharacterLarge;
        backgroundDisplayState = UIPanelBackgroundDisplayState.PanelBacker;

        // A re-show reuses an already-bound view, so BindElements does not run again.
        SyncNetworkRowVisibility();
    }

    public override void AnimateIn() {

        base.AnimateIn();

        loadData();
    }

    public static void LoadData() {
        if(GameUIPanelGameModeCoop.Instance != null) {
            GameUIPanelGameModeCoop.Instance.loadData();
        }
    }

    public virtual void loadData() {
        StartCoroutine(loadDataCo());
    }

    IEnumerator loadDataCo() {

        yield return new WaitForSeconds(1f);

    }
}
#endif