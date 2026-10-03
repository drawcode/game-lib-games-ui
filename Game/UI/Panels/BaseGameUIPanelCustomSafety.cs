using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Engine.Events;

#if ENABLE_FEATURE_CHARACTER_CUSTOMIZE_SAFETY

public class BaseGameUIPanelCustomSafety : GameUIPanelBase {

    public static GameUIPanelCustomSafety Instance;

    public GameObject listItemPrefab;

    public GameObject helmetObject;
    public GameObject helmetObjectRotator;

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
    public UIImageButton buttonClose;
#else
    public Engine.UI.UIRef buttonClose; // B7: agnostic handle (2.11 pattern), bound by name
#endif

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

        // B7 (2026-10-03): chain to the base. UIPanelBase.OnDisable is what calls FreeToolkitView;
        // without it the view leaks its PanelRenderer and the kill switch cannot restore legacy.
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

    public override void Start() {
        Init();
    }

    public override void Init() {
        base.Init();

        loadData();
    }

    public virtual void LoadDefault() {
        LoadData();
    }

    public static void ShowDefault() {
        if(GameUIPanelCustomSafety.Instance != null) {
            GameUIPanelCustomSafety.Instance.AnimateIn();
            GameUIPanelCustomSafety.Instance.LoadDefault();
        }
    }

    public override void OnButtonClickEventHandler(string buttonName) {
        //LogUtil.Log("OnButtonClickEventHandler: " + buttonName);	
    }

    public static void LoadData() {
        if(GameUIPanelCustomSafety.Instance != null) {
            GameUIPanelCustomSafety.Instance.loadData();
        }
    }

    public virtual void loadData() {
        StartCoroutine(loadDataCo());
    }

    IEnumerator loadDataCo() {

        InputSystem.Instance.currentDraggableGameObject = helmetObjectRotator;

        yield return new WaitForSeconds(1f);
    }

    public static void HideAll() {
        if(GameUIPanelCustomSafety.Instance != null) {
            GameUIPanelCustomSafety.Instance.AnimateOut();
        }
    }

    public override void AnimateIn() {

        base.AnimateIn();

        //ShowPanelDefault();
    }

    public override void AnimateOut() {

        base.AnimateOut();

        InputSystem.Instance.currentDraggableGameObject = null;

        //HidePanelDefault();
    }

    // ----------------------------------------------------------------------------------------
    // B7 (2026-10-03): UI Toolkit hosting -- Resources/ui/views/panel-custom-safety.uxml.
    //
    // HYBRID, like the customize-character screen: the flat widgets move to the view, the 3D
    // helmet stays legacy. HelmetDisplay holds the helmet meshes (Rotator, the drag target set in
    // loadDataCo, and GameCustomPlayer) plus their dark NGUI card. Toolkit views composite ABOVE
    // every camera, so a toolkit card would cover the helmet: keeping the legacy card keeps the
    // legacy stacking (card, helmet, then the flat widgets above both). So:
    //  * suppress ONLY the flat subtrees the view replaces, never the whole panelContainer;
    //  * keep the legacy edge slides running (HelmetDisplay lives under panelCenterObject, which is
    //    PARKED at y -720 in the prefab and only reaches 0 through AnimateInCenter);
    //  * restore on free, so the kill switch returns the full legacy screen (rule 116).
    public override string toolkitViewKey {
        get {
            return BaseUIPanel.panelCustomSafety;
        }
    }

    protected override bool toolkitKeepsLegacyMotion {
        get {
            return true;
        }
    }

    private const string legacyCenter = "Container/AnchorCenter/Center/Container/";

    // Paths from the panel root (the prefab root carries this script). Everything under Container
    // except Center/Container/HelmetDisplay. Tips, HelmetSection and ButtonGameSafetyContinue are
    // serialized inactive; suppressing their parents covers them if a game ever activates them.
    private static readonly string[] legacySuppressPaths = {
        "Container/AnchorBottom",
        legacyCenter + "Dialog",
        legacyCenter + "SafetyScore",
        legacyCenter + "Tools",
        legacyCenter + "Tips",
    };

    // Resolved once per view (Find is not a per-frame call); re-hidden from Update so a runtime
    // re-activation cannot draw the legacy copy under the view (iter-7 rule 1: continuous).
    private readonly List<GameObject> suppressedLegacy = new List<GameObject>();

    protected override void SuppressLegacyView() {

        suppressedLegacy.Clear();

        foreach(string path in legacySuppressPaths) {

            Transform t = transform.Find(path);

            if(t != null) {
                suppressedLegacy.Add(t.gameObject);
            }
        }

        ReassertLegacySuppression();
    }

    private void ReassertLegacySuppression() {

        if(!isToolkitPanel) {
            return;
        }

        for(int i = 0; i < suppressedLegacy.Count; i++) {

            GameObject go = suppressedLegacy[i];

            if(go != null && go.activeSelf) {
                go.Hide();
            }
        }
    }

    protected override void FreeToolkitView() {

        for(int i = 0; i < suppressedLegacy.Count; i++) {

            GameObject go = suppressedLegacy[i];

            if(go != null) {
                go.Show();
            }
        }

        suppressedLegacy.Clear();

        base.FreeToolkitView();
    }

    public virtual void Update() {

        // Before the gates below: suppression must hold whatever state the game is in.
        ReassertLegacySuppression();

        if(GameConfigs.isGameRunning) {
            return;
        }

        if(!isVisible) {
            return;
        }
    }
}
#endif
