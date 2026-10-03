#define DEV
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using Engine.Events;
using Engine.Utility;

public class UIPanelCommunityShare : UIPanelCommunityBase {

    public static UIPanelCommunityShare Instance;

    public GameObject containerShares;
    public GameObject containerActionTools;
    public GameObject containerActionAppRate;

    // ----------------------------------------------------------------------------------------
    // TOOLKIT (B6)

    // REACHABLE on non-web builds (Context.isWeb is UNITY_WEBPLAYER only, so false everywhere
    // else): the app-rate badge from BaseGameUIPanelMain.AnimateInDelayed (GameCommunity
    // .ShowActionAppRate), the share card from BaseGameUIPanelResults.AnimateIn (GameCommunity
    // .ShowSharesCenter), and the action tools bar from InitPlatform, one second after boot. Every
    // other screen's UIPanelBase.HandleHide sends HideActionAppRate + HideSharesCenter. All three
    // land on the show/hide methods below, which mirror onto the view's cards.
    public override string toolkitViewKey {
        get {
            return BaseUIPanel.panelCommunityShare;
        }
    }

    // Card elements in panel-community-share.uxml: the legacy GameObject names. A share card is
    // named after its GameObjectShowItem's GameObject (ShareCenter <- code "share-center").
    public const string elementActionTools = "ContainerActions";
    public const string elementActionAppRate = "ContainerActionAppRate";
    public const string elementBroadcastRecordLight = "ButtonGameCommunityBroadcastOpen/RecordObjectSprite";

    public override void BindElements(Engine.UI.UIRef root) {

        base.BindElements(root);

        if(!isToolkitPanel) {
            return;
        }

        // The action-tools broadcast button's record light: UIBroadcastRecordStatus fades it out
        // at Init and only pulses it while recording, and that component is suppressed with the
        // rest of the legacy subtree.
        toolkit.SetVisible(elementBroadcastRecordLight, BroadcastNetworks.IsRecording());
    }

    // ----------------------------------------------------------------------------------------

    public override void Awake() {

        if (Instance != null && this != Instance) {
            //There is already a copy of this script running
            //Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public static bool isInst {
        get {
            if (Instance != null) {
                return true;
            }
            return false;
        }
    }

    public override void Init() {
        base.Init();

        HideAllItems();

        Invoke("InitPlatform", 1);
    }

    public override void Start() {
        Init();
    }

    // EVENTS

    public override void OnEnable() {

        base.OnEnable();

        Messenger<string>.AddListener(
            BroadcastNetworksMessages.broadcastRecordingStatusChanged,
            OnBroadcastRecordStatusChanged);
    }

    public override void OnDisable() {

        base.OnDisable();

        Messenger<string>.RemoveListener(
            BroadcastNetworksMessages.broadcastRecordingStatusChanged,
            OnBroadcastRecordStatusChanged);
    }

    // Toolkit only: the legacy light is driven by its own (suppressed) UIBroadcastRecordStatus.
    public void OnBroadcastRecordStatusChanged(string broadcastStatus) {

        toolkit.SetVisible(elementBroadcastRecordLight,
            broadcastStatus == BroadcastNetworksMessages.broadcastRecordingStart);
    }

    public override void OnButtonClickEventHandler(string buttonName) {
        base.OnButtonClickEventHandler(buttonName);
    }

    //

    void InitPlatform() {
        ShowActionTools();
        ShowActionAppRate();
    }

    public void HideAllItems() {

        HideAllShares();
        HideActionTools();
        HideActionAppRate();
    }

    //

    public virtual void ShowShare(string code) {

        foreach (GameObjectShowItem item in
                containerShares.GetComponentsInChildren<GameObjectShowItem>(true)) {

            if (item.code == code) {
                HideAllShares();
                TweenUtil.ShowObjectBottom(item.gameObject);
                item.gameObject.ShowObjectDelayed(.7f);

                toolkit.SetCard(item.gameObject.name, true, UIPanelCommunityToolkit.CardEdge.Bottom);
            }
        }
    }

    public virtual void HideAllShares() {

        foreach (GameObjectShowItem item in
                containerShares.GetComponentsInChildren<GameObjectShowItem>(true)) {
            TweenUtil.HideObjectBottom(item.gameObject);
            item.gameObject.HideObjectDelayed(.5f);

            toolkit.SetCard(item.gameObject.name, false, UIPanelCommunityToolkit.CardEdge.Bottom);
        }
    }

    //

    public static void ShowSharesCenter() {
        if (isInst) {
            Instance.showSharesCenter();
        }
    }

    public virtual void showSharesCenter() {

        if (Context.Current.isWeb) {
            return;
        }

#if USE_GAME_LIB_GAMEVERSES
        ShowShare(GameCommunityUIShares.shareCenter);
#endif
    }

    public static void HideSharesCenter() {
        if (isInst) {
            Instance.hideSharesCenter();
        }
    }

    public virtual void hideSharesCenter() {
        HideAllShares();
    }

    //

    public static void ShowActionTools() {
        if (isInst) {
            Instance.showActionTools();
        }
    }

    public virtual void showActionTools() {

        if (Context.Current.isWeb) {
            return;
        }

        TweenUtil.ShowObjectBottom(containerActionTools);

        toolkit.SetCard(elementActionTools, true, UIPanelCommunityToolkit.CardEdge.Bottom);
    }

    public static void HideActionTools() {
        if (isInst) {
            Instance.hideActionTools();
        }
    }

    public virtual void hideActionTools() {

        TweenUtil.HideObjectBottom(containerActionTools);

        toolkit.SetCard(elementActionTools, false, UIPanelCommunityToolkit.CardEdge.Bottom);
    }

    //

    public static void ShowActionAppRate() {
        if (isInst) {
            Instance.showActionAppRate();
        }
    }

    public virtual void showActionAppRate() {

        if (Context.Current.isWeb) {
            return;
        }

        //Debug.Log("UIPaneCommnityShare::showActionAppRate:");

        TweenUtil.ShowObjectRight(containerActionAppRate);

        toolkit.SetCard(elementActionAppRate, true, UIPanelCommunityToolkit.CardEdge.Right);
    }

    public static void HideActionAppRate() {
        if (isInst) {
            Instance.hideActionAppRate();
        }
    }

    public virtual void hideActionAppRate() {

        //Debug.Log("UIPaneCommnityShare::hideActionAppRate:");

        TweenUtil.HideObjectRight(containerActionAppRate);

        toolkit.SetCard(elementActionAppRate, false, UIPanelCommunityToolkit.CardEdge.Right);
    }

    //

    public static void ShowDefault() {
        if (isInst) {
            Instance.AnimateIn();
        }
    }

    public static void HideAll() {
        if (isInst) {
            Instance.AnimateOut();
        }
    }

    public static void LoadData() {
        if (Instance != null) {
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