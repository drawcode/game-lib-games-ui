#define DEV
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using Engine.Events;
using Engine.UI;

public class UIPanelCommunityBackground : UIPanelBase {

    public static UIPanelCommunityBackground Instance;

    public GameObject panelBackground;

    // ----------------------------------------------------------------------------------------
    // TOOLKIT (B6)
    //
    // The dark scrim under the community dialogs. Derives from UIPanelBase, not
    // UIPanelCommunityBase, so it forwards the same seams to its own UIPanelCommunityToolkit (see
    // that class for the hosting model). Unreachable in this title (only the community dialogs
    // show it); verified by a direct ShowBackground().

    UIPanelCommunityToolkit _toolkit;

    public UIPanelCommunityToolkit toolkit {
        get {
            if(_toolkit == null) {
                _toolkit = new UIPanelCommunityToolkit(this);
            }
            return _toolkit;
        }
    }

    public const string elementBackgroundCard = "PanelContentBackground";

    public override string toolkitViewKey {
        get {
            return BaseUIPanel.panelCommunityBackground;
        }
    }

    // One below the community dialogs (UILayers.overlay): legacy draws the scrim at depth 25 in
    // the same camera, under the dialog frames (30+).
    public override int toolkitSortOrder {
        get {
            return UILayers.overlay - 1;
        }
    }

    public override bool toolkitPreloadView {
        get {
            return true;
        }
    }

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

    }

    //

    public static void ShowBackground() {
        if(isInst) {
            Instance.showBackground();
        }
    }

    public void showBackground() {
        AnimateInBottom(panelBackground);

        toolkit.SetCard(elementBackgroundCard, true, UIPanelCommunityToolkit.CardEdge.Bottom);

        //Debug.Log("ShowBackground:");
    }

    public static void HideBackground() {
        if(isInst) {
            Instance.hideBackground();
        }
    }

    public void hideBackground() {
        AnimateOutBottom(panelBackground);

        toolkit.SetCard(elementBackgroundCard, false, UIPanelCommunityToolkit.CardEdge.Bottom);
    }

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

    public static void ShowDialog() {
        if(isInst) {
            Instance.showDialog();
        }
    }

    public void showDialog() {
        ShowBackground();
    }

    public static void ShowNone() {
        if(isInst) {
            Instance.showNone();
        }
    }

    public void showNone() {
        HideBackground();
    }

    public override void AnimateIn() {
        base.AnimateIn();
    }

    public override void AnimateOut() {
        base.AnimateOut();

        ShowNone();
    }

    //public void Update() {
    //base.Update();
    //}
}