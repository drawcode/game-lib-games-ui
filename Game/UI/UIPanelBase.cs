using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Engine.Events;
using Engine.UI;
using Engine.Utility;
using UnityEngine.UI;

public enum UIPanelBackgroundDisplayState {
    None,
    PanelBacker
}

// Which screen edge a centre-wired panel enters from / parks out to.
public enum UIPanelEnterDirection {
    Bottom,
    Top
}

public enum UIPanelCharacterDisplayState {
    None,
    Character,
    CharacterLarge
}

public enum UIPanelButtonsDisplayState {
    None,
    Character,
    CharacterLarge,
    CharacterTools,
    CharacterCustomize,
    Statistics,
    Achievements,
    GameNetworks,
    ProductsSections
}

public enum UIPanelAdDisplayState {
    None,
    BannerTop,
    BannerBottom,
    Video,
    VideoIncentivized,
    Interstitial
}

public class UIPanelBaseTypes {
    public static string typeDefault = "type-default";
    public static string typeDialogHUD = "type-dialog-hud";
    public static string typeModalHUD = "type-modal-hud";
    public static string typeDialogUI = "type-dialog-ui";
    public static string typeModalUI = "type-modal-ui";
    public static string typeDialogDialog = "type-dialog-dialog";
    public static string typeModalDialog = "type-modal-dialog";
    public static string typeDialogOverlay = "type-dialog-overlay";
    public static string typeModalOverlay = "type-modal-overlay";
}

public class UIPanelBase : UIAppPanel {

    public UIPanelCharacterDisplayState characterDisplayState = UIPanelCharacterDisplayState.None;
    public UIPanelButtonsDisplayState buttonDisplayState = UIPanelButtonsDisplayState.None;
    public UIPanelBackgroundDisplayState backgroundDisplayState = UIPanelBackgroundDisplayState.None;
    public UIPanelAdDisplayState adDisplayState = UIPanelAdDisplayState.None;
    public GameObject listGridRoot;
    
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3

    public UIGrid listGrid;
    public UIPanel panelClipped;
    public UIDraggablePanel draggablePanel;
    public UIScrollBar draggablePanelScrollbar;

#else
    // TODO Unity UI
    public GameObject listGrid;
    public GameObject panelClipped;
    public GameObject draggablePanel;
    public GameObject draggablePanelScrollbar;
#endif

    public GameObject panelLeftObject;
    public GameObject panelLeftBottomObject;
    public GameObject panelLeftTopObject;
    public GameObject panelRightObject;
    public GameObject panelRightBottomObject;
    public GameObject panelRightTopObject;
    public GameObject panelTopObject;
    public GameObject panelBottomObject;
    public GameObject panelCenterObject;
    public GameObject panelContainer;

    [NonSerialized]
    public float
        durationShow = .45f;
    [NonSerialized]
    public float
        durationHide = .45f;
    [NonSerialized]
    public float
        durationDelayShow = .5f;
    [NonSerialized]
    public float
        durationDelayHide = 0f;
    [NonSerialized]
    public float
        leftOpenX = 0f;
    [NonSerialized]
    public float
        leftClosedX = -4500f;
    [NonSerialized]
    public float
        rightOpenX = 0f;
    [NonSerialized]
    public float
        rightClosedX = 4500f;
    [NonSerialized]
    public float
        bottomOpenY = 0f;
    [NonSerialized]
    public float
        bottomClosedY = -4500f;
    [NonSerialized]
    public float
        topOpenY = 0f;
    [NonSerialized]
    public float
        topClosedY = 4500f;
    public int increment = 0;
    public List<string> panelTypes = new List<string>();//UIPanelBaseTypes.typeDefault;

    public override bool isVisible {
        get {

            if(panelContainer != null) {
                if(_isVisible) {
                    //if (!panelContainer.GetActive()) {
                    //_isVisible = false;
                    //}
                }
                else {
                    //if (panelContainer.GetActive()) {
                    //_isVisible = true;
                    //}
                }
            }
            return _isVisible;
        }
        set {
            _isVisible = value;
        }
    }

    public virtual void OnEnable() {

        panelTypes.Add(UIPanelBaseTypes.typeDefault);

        Messenger<string>.AddListener(ButtonEvents.EVENT_BUTTON_CLICK, OnButtonClickEventHandler);
        Messenger<string, Dictionary<string, object>>.AddListener(ButtonEvents.EVENT_BUTTON_CLICK_DATA, OnButtonClickEventDataHandler);

        Messenger<string>.AddListener(UIControllerMessages.uiPanelAnimateIn, OnUIControllerPanelAnimateIn);
        Messenger<string>.AddListener(UIControllerMessages.uiPanelAnimateOut, OnUIControllerPanelAnimateOut);

        Messenger<string>.AddListener(UIControllerMessages.uiPanelAnimateInType, OnUIControllerPanelAnimateInType);
        Messenger<string>.AddListener(UIControllerMessages.uiPanelAnimateOutType, OnUIControllerPanelAnimateOutType);

        Messenger<string, string>.AddListener(UIControllerMessages.uiPanelAnimateInClassType, OnUIControllerPanelAnimateInClassType);
        Messenger<string, string>.AddListener(UIControllerMessages.uiPanelAnimateOutClassType, OnUIControllerPanelAnimateOutClassType);

        Messenger<string, string>.AddListener(UIControllerMessages.uiPanelAnimateType, OnUIControllerPanelAnimateType);
    }

    public virtual void OnDisable() {

        // Pre-existing bug (flagged in the 2.9 design): these were AddListener on DISABLE, so
        // every hide re-subscribed the panel and listeners accumulated across enable cycles.
        // They must be RemoveListener, symmetric with OnEnable's AddListener.
        Messenger<string>.RemoveListener(ButtonEvents.EVENT_BUTTON_CLICK, OnButtonClickEventHandler);
        Messenger<string, Dictionary<string, object>>.RemoveListener(ButtonEvents.EVENT_BUTTON_CLICK_DATA, OnButtonClickEventDataHandler);

        Messenger<string>.RemoveListener(UIControllerMessages.uiPanelAnimateIn, OnUIControllerPanelAnimateIn);
        Messenger<string>.RemoveListener(UIControllerMessages.uiPanelAnimateOut, OnUIControllerPanelAnimateOut);

        Messenger<string>.RemoveListener(UIControllerMessages.uiPanelAnimateInType, OnUIControllerPanelAnimateInType);
        Messenger<string>.RemoveListener(UIControllerMessages.uiPanelAnimateOutType, OnUIControllerPanelAnimateOutType);

        Messenger<string, string>.RemoveListener(UIControllerMessages.uiPanelAnimateInClassType, OnUIControllerPanelAnimateInClassType);
        Messenger<string, string>.RemoveListener(UIControllerMessages.uiPanelAnimateOutClassType, OnUIControllerPanelAnimateOutClassType);

        Messenger<string, string>.RemoveListener(UIControllerMessages.uiPanelAnimateType, OnUIControllerPanelAnimateType);

        // Pooled-away: free the toolkit view so its UXML + PanelRenderer are reclaimed. Reloads
        // on the next show.
        FreeToolkitView();
    }

    // The real memory unload the per-panel PanelRenderer model exists for.
    //
    // This project POOLS panels — it does NOT destroy them on navigation. Verified in play mode:
    // a navigated-away panel lives on under _ObjectPoolKeyedManager, deactivated (SetActive false),
    // so OnDisable fires and OnDestroy does not. OnDisable is therefore the "panel put away"
    // signal, and freeing the view there is what makes the memory actually come back. When the
    // pooled panel is later re-shown (OnEnable -> AnimateIn -> EnsureToolkitView), it reloads.
    //
    // Also freed in OnDestroy for the genuine-teardown case (scene unload), so the separate
    // PanelRenderer GameObject can't outlive its panel. DestroyView on an already-freed
    // (UIRef.none) view is a no-op, so calling from both is safe.
    protected virtual void FreeToolkitView() {

        if(!isToolkitPanel) {
            return;
        }

        // Stop any in-flight show/hide slide first: destroying the view detaches the VisualElement,
        // and a still-ticking translate/fade tween would then write style on a panel-less element
        // (Unity NREs in ApplyStyleTranslate). Cancelling here is the clean stop; the detached-guard
        // in VisualElementTweenTarget is the backstop.
        TweenUtil.Cancel(viewRoot);

        IUIBackend backend = UIPlatform.For(viewRoot);

        if(backend != null) {
            backend.DestroyView(viewRoot);
        }

        viewRoot = UIRef.none;
        toolkitLoadRequested = false;
    }

    public virtual void OnDestroy() {
        FreeToolkitView();
    }

    public virtual void OnButtonClickEventHandler(
        string buttonName) {

    }

    public virtual void OnButtonClickEventDataHandler(
        string buttonName, Dictionary<string, object> data) {

    }

    public virtual void OnUIControllerPanelAnimateIn(string classNameTo) {

        if(className == classNameTo) {

            HandleUniquePanelTypes();

            AnimateIn();
        }
    }

    public virtual void OnUIControllerPanelAnimateOut(string classNameTo) {
        if(className == classNameTo) {
            AnimateOut();
        }
    }

    public virtual void OnUIControllerPanelAnimateInType(string panelTypeTo) {
        if(panelTypes.Contains(panelTypeTo)) {
            AnimateIn();
        }
    }

    public virtual void OnUIControllerPanelAnimateOutType(string panelTypeTo) {
        if(panelTypes.Contains(panelTypeTo)) {
            AnimateOut();
        }
    }

    public virtual void OnUIControllerPanelAnimateInClassType(string classNameTo, string panelTypeTo) {
        if(className != classNameTo && panelTypes.Contains(panelTypeTo)) {
            AnimateIn();
        }
    }

    public virtual void OnUIControllerPanelAnimateOutClassType(string classNameTo, string panelTypeTo) {
        if(className != classNameTo && panelTypes.Contains(panelTypeTo)) {
            AnimateOut();
        }
    }

    public virtual void OnUIControllerPanelAnimateType(string classNameTo, string code) {
        if(className == classNameTo) {
            //
        }
    }

    public virtual void HandleUniquePanelTypes() {

        if(panelTypes.Count > 1) {
            // if this is a special panel, hide the others like it such as dialogs...modals
            foreach(string panelType in panelTypes) {

                if(panelType == UIPanelBaseTypes.typeDefault) {
                    continue;
                }

                Messenger<string, string>.Broadcast(UIControllerMessages.uiPanelAnimateOutClassType, className, panelType);
                LogUtil.Log("OnUIControllerPanelAnimateIn:", " className:" + className + " panelType:" + panelType);
            }
        }
    }

    public override void Start() {
        base.Start();

        AnimateOut();
    }

    public void HideAllPanels() {
        foreach(UIAppPanelBase baseItem in Resources.FindObjectsOfTypeAll(typeof(UIAppPanelBase))) {
            baseItem.AnimateOut();
        }
    }

    public void HideAllPanelsNow() {
        foreach(UIAppPanelBase baseItem in Resources.FindObjectsOfTypeAll(typeof(UIAppPanelBase))) {
            baseItem.AnimateOut(); // handle per panel actions
            baseItem.AnimateOutNow(); // but animate it out now it now
        }
    }

    // CENTER

    public virtual void AnimateInCenter(float time = .5f, float delay = .5f, bool fade = true) {
        AnimateInCenter(panelCenterObject, time, delay, fade);
    }

    // Which edge centre-wired content enters from. DEFAULT: panels showing the shared PanelBacker
    // enter WITH it (top — the backer leads, content follows); bare flows (worlds/levels, no
    // backer) keep the classic bottom rise. Safe to derive here: HandleShow() (where panels set
    // backgroundDisplayState) runs before AnimateInCenter in the AnimateIn flow. Override per
    // panel for anything that wants an explicit direction.
    public virtual UIPanelEnterDirection centerEnterDirection {
        get {
            return backgroundDisplayState == UIPanelBackgroundDisplayState.PanelBacker
                ? UIPanelEnterDirection.Top
                : UIPanelEnterDirection.Bottom;
        }
    }

    public virtual void AnimateInCenter(GameObject go, float time = .5f, float delay = .5f, bool fade = true) {

        if(centerEnterDirection == UIPanelEnterDirection.Top) {
            TweenUtil.ShowObjectTop(go, TweenCoord.local, fade, time, delay);
        }
        else {
            TweenUtil.ShowObjectBottom(go, TweenCoord.local, fade, time, delay);
        }
    }

    public virtual void AnimateOutCenter(float time = .3f, float delay = 0f, bool fade = true) {
        AnimateOutCenter(panelCenterObject, time, delay, fade);
    }

    public virtual void AnimateOutCenter(GameObject go, float time = .3f, float delay = 0f, bool fade = true) {

        if(centerEnterDirection == UIPanelEnterDirection.Top) {
            TweenUtil.HideObjectTop(go, TweenCoord.local, fade, time, delay);
        }
        else {
            TweenUtil.HideObjectBottom(go, TweenCoord.local, fade, time, delay);
        }
    }

    // LEFT

    public virtual void AnimateInLeft(float time = .5f, float delay = .5f, bool fade = true) {
        AnimateInLeft(panelLeftObject, time, delay, fade);
    }

    public virtual void AnimateInLeft(GameObject go, float time = .5f, float delay = .5f, bool fade = true) {
        TweenUtil.ShowObjectLeft(go, TweenCoord.local, fade, time, delay);
    }

    public virtual void AnimateOutLeft(float time = .3f, float delay = 0f, bool fade = true) {
        AnimateOutLeft(panelLeftObject, time, delay, fade);
    }

    public virtual void AnimateOutLeft(GameObject go, float time = .3f, float delay = 0f, bool fade = true) {
        TweenUtil.HideObjectLeft(go, TweenCoord.local, fade, time, delay);
    }

    // LEFT BOTTOM

    public virtual void AnimateInLeftBottom(float time = .5f, float delay = .5f, bool fade = true) {
        AnimateInLeftBottom(panelLeftBottomObject, time, delay, fade);
    }

    public virtual void AnimateInLeftBottom(GameObject go, float time = .5f, float delay = .5f, bool fade = true) {
        AnimateInLeft(go, time, delay, fade);
    }

    public virtual void AnimateOutLeftBottom(float time = .3f, float delay = 0f, bool fade = true) {
        AnimateOutLeftBottom(panelLeftBottomObject, time, delay, fade);
    }

    public virtual void AnimateOutLeftBottom(GameObject go, float time = .3f, float delay = 0f, bool fade = true) {
        AnimateOutLeft(go, time, delay, fade);
    }

    // LEFT TOP

    public virtual void AnimateInLeftTop(float time = .5f, float delay = .5f, bool fade = true) {
        AnimateInLeftTop(panelLeftTopObject, time, delay, fade);
    }

    public virtual void AnimateInLeftTop(GameObject go, float time = .5f, float delay = .5f, bool fade = true) {
        AnimateInLeft(go, time, delay, fade);
    }

    public virtual void AnimateOutLeftTop(float time = .3f, float delay = 0f, bool fade = true) {
        AnimateOutLeftTop(panelLeftTopObject, time, delay, fade);
    }

    public virtual void AnimateOutLeftTop(GameObject go, float time = .3f, float delay = 0f, bool fade = true) {
        AnimateOutLeft(go, time, delay, fade);
    }

    // RIGHT

    public virtual void AnimateInRight(float time = .5f, float delay = .5f, bool fade = true) {
        AnimateInRight(panelRightObject, time, delay, fade);
    }

    public virtual void AnimateInRight(GameObject go, float time = .5f, float delay = .5f, bool fade = true) {
        TweenUtil.ShowObjectRight(go, TweenCoord.local, fade, time, delay);
    }

    public virtual void AnimateOutRight(float time = .3f, float delay = 0f, bool fade = true) {
        AnimateOutRight(panelRightObject, time, delay, fade);
    }

    public virtual void AnimateOutRight(GameObject go, float time = .3f, float delay = 0f, bool fade = true) {
        TweenUtil.HideObjectRight(go, TweenCoord.local, fade, time, delay);
    }

    // BOTTOM RIGHT

    public virtual void AnimateInRightBottom(float time = .5f, float delay = .5f, bool fade = true) {
        AnimateInRightBottom(panelRightBottomObject, time, delay, fade);
    }

    public virtual void AnimateInRightBottom(GameObject go, float time = .5f, float delay = .5f, bool fade = true) {
        AnimateInRight(go, time, delay, fade);
    }

    public virtual void AnimateOutRightBottom(float time = .3f, float delay = 0f, bool fade = true) {
        AnimateOutRightBottom(panelRightBottomObject, time, delay, fade);
    }

    public virtual void AnimateOutRightBottom(GameObject go, float time = .3f, float delay = 0f, bool fade = true) {
        AnimateOutRight(go, time, delay, fade);
    }

    // TOP RIGHT

    public virtual void AnimateInRightTop(float time = .5f, float delay = .5f, bool fade = true) {
        AnimateInRightTop(panelRightTopObject, time, delay, fade);
    }

    public virtual void AnimateInRightTop(GameObject go, float time = .5f, float delay = .5f, bool fade = true) {
        AnimateInRight(go, time, delay, fade);
    }

    public virtual void AnimateOutRightTop(float time = .3f, float delay = 0f, bool fade = true) {
        AnimateOutRightTop(panelRightTopObject, time, delay, fade);
    }

    public virtual void AnimateOutRightTop(GameObject go, float time = .3f, float delay = 0f, bool fade = true) {
        AnimateOutRight(go, time, delay, fade);
    }

    // TOP

    public virtual void AnimateInTop(float time = .5f, float delay = .5f, bool fade = true) {
        AnimateInTop(panelTopObject, time, delay, fade);
    }

    public virtual void AnimateInTop(GameObject go, float time = .5f, float delay = .5f, bool fade = true) {
        TweenUtil.ShowObjectTop(go, TweenCoord.local, fade, time, delay);
    }

    public virtual void AnimateOutTop(float time = .3f, float delay = 0f, bool fade = true) {
        AnimateOutTop(panelTopObject, time, delay, fade);
    }

    public virtual void AnimateOutTop(GameObject go, float time = .3f, float delay = 0f, bool fade = true) {
        TweenUtil.HideObjectTop(go, TweenCoord.local, fade, time, delay);
    }

    // BOTTOM

    public virtual void AnimateInBottom(float time = .5f, float delay = .5f, bool fade = true) {
        AnimateInBottom(panelBottomObject, time, delay, fade);
    }

    public virtual void AnimateInBottom(GameObject go, float time = .5f, float delay = .5f, bool fade = true) {
        TweenUtil.ShowObjectBottom(go, TweenCoord.local, fade, time, delay);
    }

    public virtual void AnimateOutBottom(float time = .3f, float delay = 0f, bool fade = true) {
        AnimateOutBottom(panelBottomObject, time, delay, fade);
    }

    public virtual void AnimateOutBottom(GameObject go, float time = .3f, float delay = 0f, bool fade = true) {
        TweenUtil.HideObjectBottom(go, TweenCoord.local, fade, time, delay);
    }

    // ANIMATE

    // ------------------------------------------------------------------------
    // UI TOOLKIT PATH
    //
    // A panel is a "toolkit panel" once it has a loaded view (a UXML tree bound to viewRoot).
    // Until then every panel is an NGUI panel and takes the original path untouched — that is
    // what lets a migrated screen and an NGUI screen coexist in the same frame through all of
    // Phase 3.
    //
    // The toolkit path deliberately does NOT reproduce the nine-edge slide choreography
    // (panelLeftObject, panelRightTopObject, ...). Those are ±4500-unit GameObject slides that
    // exist because NGUI had no layout engine; in UXML a panel is one view that fades/translates
    // as a unit, driven by a named preset from tokens.json. Panels that genuinely need per-edge
    // entrances get them back as bitty patterns in Phase 3.

    public bool isToolkitPanel {
        get {
            return viewRoot != null && viewRoot.alive;
        }
    }

    // A panel migrates to UI Toolkit by overriding this with its view key — the SAME string the
    // NGUI path already uses (e.g. "panel-settings-credits"). Empty means "stay on NGUI".
    //
    // The view is loaded lazily on first AnimateIn, NOT in Init/Start. Panels are instantiated
    // inactive (BaseUIController.syncPanelLoaded parents a pooled prefab), so Start never runs
    // until the panel is first shown — hooking Init meant LoadToolkitView was simply never
    // called, and the panel silently stayed on NGUI with no error. First-show is the only moment
    // the panel is guaranteed to be alive.
    public virtual string toolkitViewKey {
        get {
            return "";
        }
    }

    // Which draw-order band this panel's view sits in (see Engine.UI.UILayers). Default `auto`
    // keeps the original behavior (draw order == load order, within the panel band). Always-on
    // chrome overrides this with UILayers.chrome so it renders ABOVE screens that load after it.
    //
    // NOTE: this is draw order only — it is NOT a lifetime hint. A view is still released by
    // FreeToolkitView on OnDisable, so a flow-scoped panel is loaded and cleaned up exactly as
    // before; chrome merely stays resident for as long as its GameObject stays enabled.
    public virtual int toolkitSortOrder {
        get {
            return UILayers.auto;
        }
    }

    // Named motion presets driving this panel's toolkit show/hide slide (tokens.json -> TweenPresets).
    // Chrome overrides these with chrome-show/chrome-hide so the header's entrance has slight
    // timing/ease variance from the content body — fluid, not mechanical lock-step.
    public virtual string toolkitShowPreset {
        get {
            return "panel-show";
        }
    }

    public virtual string toolkitHidePreset {
        get {
            return "panel-hide";
        }
    }

    protected virtual void EnsureToolkitView() {

        // Global kill switch: NGUI stays the shipping path, and one flag turns the whole toolkit
        // path off without touching any panel's code.
        if(!UIPlatform.toolkitViewsEnabled) {
            return;
        }

        if(isToolkitPanel || string.IsNullOrEmpty(toolkitViewKey)) {
            return;
        }

        LoadToolkitView(toolkitViewKey);
    }

    // set true once a load is in flight, so EnsureToolkitView doesn't kick a second one while the
    // deferred PanelRenderer build is still pending.
    private bool toolkitLoadRequested = false;

    // Requests this panel's view from the backend and binds it when ready.
    //
    // ASYNCHRONOUS: PanelRenderer builds its UXML on a later panel update (verified in-editor), so
    // the bind/hide work runs in the onReady continuation, not inline. On the very first show the
    // panel therefore briefly runs its NGUI path until the view arrives (a frame or two later),
    // then the continuation hides the NGUI container and the toolkit view takes over. Subsequent
    // shows are instant (viewRoot already alive). Pre-loading at scene load to remove that first
    // frame is a Phase 3 refinement.
    public virtual void LoadToolkitView(string viewKey) {

        IUIBackend backend = UIPlatform.viewBackend;

        if (backend == null || string.IsNullOrEmpty(viewKey) || toolkitLoadRequested) {
            return;
        }

        toolkitLoadRequested = true;

        backend.LoadView(viewKey, toolkitSortOrder, (UIRef view) => {

            if (view == null || !view.alive) {
                // No UXML for this key: stay on NGUI. Allow a later retry.
                toolkitLoadRequested = false;
                return;
            }

            // The load is deferred (a frame or two). If the panel was pooled away in the meantime,
            // FreeToolkitView cleared toolkitLoadRequested — the view we just built is orphaned, so
            // destroy it now instead of leaking its PanelRenderer.
            if (!toolkitLoadRequested) {
                backend.DestroyView(view);
                return;
            }

            viewRoot = view;

            LoadBindManifest(viewKey);
            BindElements(view);

            // The NGUI prefab for this panel is still instantiated (BaseUIController
            // .syncPanelLoaded loads it by code, unchanged). Its widgets would render UNDERNEATH
            // the toolkit view — two copies of the same screen. Suppressing the NGUI container is
            // what makes a migrated panel replace its predecessor rather than double it.
            SuppressLegacyView();

            // Match whatever visibility the panel should currently have: if it was shown while the
            // load was still pending, show now; otherwise start hidden (Start() -> AnimateOut()).
            //
            // Slide, don't pop: on a fresh show the view arrives ASYNC — AnimateIn already ran
            // (isToolkitPanel was still false there, so its ShowObjectTop never fired) and a bare
            // Show() made first shows appear in place while re-shows slid. ShowObjectTop parks the
            // view off-screen-top in the same frame as Show, so there's no flash.
            if(isVisible) {
                backend.Show(view);
                TweenUtil.ShowObjectTop(view, toolkitShowPreset);
            }
            else {
                backend.Hide(view);
            }
        });
    }

    // What LoadToolkitView hides so the NGUI prefab doesn't render underneath the toolkit view.
    // Default: the whole panelContainer. Overridable because some panels carry NON-flat content
    // inside their container that must survive the swap — the header's 3D character preview
    // (Characters lives inside its Container) is the first case; it hides only the flat widgets
    // its view replaces.
    protected virtual void SuppressLegacyView() {

        if(panelContainer != null) {
            panelContainer.Hide();
        }
    }

    public virtual void AnimateIn() {

        //AnimateOut(0f, 0f);

        EnsureToolkitView();

        HandleUniquePanelTypes();

        ShowPanel();

        float time = durationShow;
        float delay = durationDelayShow;

        //AnimateCancelEasing(delay);

        AnimateIn(time, delay);
    }

    public virtual void AnimateCancelEasing(float delay) {

        StartCoroutine(AnimateCancelEasingCo(delay));
    }

    IEnumerator AnimateCancelEasingCo(float delay) {

        yield return new WaitForSeconds(delay);

        TweenUtil.CancelAll();
    }

    public virtual void AnimateIn(float time = .5f, float delay = .5f) {

        if(isVisible) {
            return;
        }

        //ShowCamera();

        HandleShow();

        HandleCharacterDisplay();

        HandleAdDisplay();

        HandleButtonDisplay();

        HandleBackgroundDisplay();

        // Toolkit panels slide the whole view DOWN from off-screen top + fade, synced with the
        // shared PanelBacker (which also enters from the top). Fade-only left the content sitting
        // still while the backer moved. The nine-edge slide below is the NGUI choreography and does
        // not apply. Everything above (HandleShow, character/ad/button/background) still runs.
        if(isToolkitPanel) {

            TweenUtil.ShowObjectTop(viewRoot, toolkitShowPreset);

            isVisible = true;

            return;
        }

        AnimateInCenter(time, delay);
        AnimateInLeft(time, delay);
        AnimateInLeftBottom(time, delay);
        AnimateInLeftTop(time, delay);
        AnimateInRight(time, delay);
        AnimateInRightBottom(time, delay);
        AnimateInRightTop(time, delay);
        AnimateInTop(time, delay);
        AnimateInBottom(time, delay);

        isVisible = true;
    }

    public virtual void AnimateOut() {

        float time = durationHide;
        float delay = durationDelayHide;

        AnimateOut(time, delay);
    }

    public virtual void AnimateOutNow() {

        float time = 0f;
        float delay = 0f;

        AnimateOut(time, delay);
    }

    public virtual void AnimateOut(float time, float delay) {

        if(!isVisible) {
            //return;
        }

        //HideCamera();

        HandleHide();

        AdNetworks.HideAd();

        if(isToolkitPanel) {

            TweenUtil.HideObjectTop(viewRoot, toolkitHidePreset);

            isVisible = false;

            HidePanel();

            return;
        }

        AnimateOutCenter(time, delay);
        AnimateOutLeft(time, delay);
        AnimateOutLeftBottom(time, delay);
        AnimateOutLeftTop(time, delay);
        AnimateOutRight(time, delay);
        AnimateOutRightBottom(time, delay);
        AnimateOutRightTop(time, delay);
        AnimateOutTop(time, delay);
        AnimateOutBottom(time, delay);

        ListClear();

        if(panelContainer != null) {
            if(!panelContainer.activeSelf || !panelContainer.activeInHierarchy) {
                panelContainer.Hide();
            }
            else {
                StartCoroutine(HidePanelCo(delay + .5f));
            }
        }

        isVisible = false;
    }

    public IEnumerator HidePanelCo(float delay) {
        yield return new WaitForSeconds(delay);

        HidePanel();
    }

    public virtual void HidePanel() {

        // Display state, never a tween side effect (gate learning #1). The tween fades opacity;
        // this is what actually removes the view from layout.
        if(isToolkitPanel) {

            if(!isVisible) {
                UIUtil.HideObject(viewRoot);
            }

            return;
        }

        if(!isVisible) {
            if(panelContainer != null) {
                //isVisible = false;
                panelContainer.Hide();
            }
        }
    }

    public virtual void ShowPanel() {

        if(isVisible) {
            return;
        }

        if(isToolkitPanel) {
            UIUtil.ShowObject(viewRoot);
            return;
        }

        if(panelContainer != null) {
            //isVisible = true;
            panelContainer.Show();
        }
    }

    void Update() {

    }

    public void ListContainerScale(GameObject listObject, float scaleTo) {

        if(listObject == null) {
            return;
        }

        Vector3 currentScale = listObject.transform.localScale;

        float screenWidth = 640;
        float screenHeight = 960;

        scaleTo = Mathf.Clamp(scaleTo / (screenWidth / screenHeight), .5f, 2f);

        currentScale = currentScale.WithX(scaleTo).WithY(scaleTo).WithZ(scaleTo);

        listObject.transform.localScale = currentScale;
    }

    public void ListScale(GameObject listObject, float scaleTo) {

        if(listObject == null) {
            return;
        }

        Vector3 currentScale = listObject.transform.localScale;

        float screenWidth = 640;
        float screenHeight = 960;

        scaleTo = Mathf.Clamp(scaleTo / (screenWidth / screenHeight), .5f, 2f);

        currentScale = currentScale.WithX(scaleTo).WithY(scaleTo).WithZ(scaleTo);

        listObject.transform.localScale = currentScale;
    }

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
    public void PanelScale(UIPanel panel) {

        if(panelClipped == null) {
            return;
        }

        Vector4 range = panelClipped.clipRange;
        range.x = 0f;
        //range.y = 0f;
        range.z = 2500f;
        //range.y = 2500f;
        //range.w = 380f;
        panelClipped.clipRange = range;
    }
#else
    public void PanelScale(GameObject panel) {
        if(panelClipped != null) {
            //Vector4 range = panelClipped.clipRange;
            //range.x = 0f;
            ////range.y = 0f;
            //range.z = 2500f;
            ////range.y = 2500f;
            ////range.w = 380f;
            //panelClipped.clipRange = range;
        }
    }

#endif

    public void ListScale(float scaleTo) {

        if(listGridRoot != null) {
            ListScale(listGridRoot, scaleTo);
        }

        PanelScale(panelClipped);

        ListReposition();
    }

    public void ListClear() {

        if(listGridRoot == null && !isVisible) {
            return;
        }

        ListClear(listGridRoot);
    }

    public void ListClear(GameObject listObject) {

        if(listObject == null && !isVisible) {
            return;
        }

        listObject.DestroyChildren();
    }

    public void ListReposition() {
        increment = 0;
        if(listGrid != null) {
            RepositionList(listGrid, listGridRoot);
        }
    }

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
    public void ListReposition(UIGrid grid, GameObject gridObject) {

        increment = 0;

        if(grid == null) {
            return;
        }
        
        RepositionList(grid, gridObject);
    }
#endif

    public void ListReposition(GameObject grid, GameObject gridObject) {
        increment = 0;

        if(grid == null) {
            return;
        }
        
        RepositionList(grid, gridObject);
    }

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
    public void RepositionList(UIGrid grid, GameObject gridObject) {

        if(grid == null) {
            return;
        }

        UIUtil.GridReposition(grid);

        if(gridObject.transform.parent == null) {
            return;
        }

        UIDraggablePanel[] dragPanels =
            gridObject.transform.parent.gameObject.GetComponentsInChildren<UIDraggablePanel>();

        if(dragPanels == null) {
            return;
        }

        foreach(UIDraggablePanel panel
         in dragPanels) {
            panel.ResetPosition();
            break;
        }

    }
#endif

    public void RepositionList(GameObject grid, GameObject gridObject) {

        if(grid == null) {
            return;
        }

        UIUtil.GridReposition(grid);

        if(gridObject.transform.parent == null) {
            return;
        }

        //ScrollRect[] dragPanels =
        //    gridObject.transform.parent.gameObject.GetComponentsInChildren<ScrollRect>();

        //if(dragPanels == null) {
        //    return;
        //}

        ////foreach(ScrollRect panel
        //// in dragPanels) {
        ////    //panel.ResetPosition();
        ////    break;
        ////}

    }

    public void RepositionListScroll(float scrollValue) {
        if(draggablePanelScrollbar != null) {
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
            draggablePanelScrollbar.scrollValue = 0;
#else
            // TODO Unity UI
            //UIUtil.SetSc
#endif
        }
        else if(draggablePanel != null) {
            draggablePanel.ResetPosition();
        }
    }


    // LOADING

    public virtual GameObject LoadObject(GameObject prefabObject, string itemName) {
        return LoadObject(listGridRoot, prefabObject, itemName);
    }

    public virtual GameObject LoadObject(GameObject prefabObject, string itemName,
                                         string title, string description, string note, string type) {
        return LoadObject(listGridRoot, prefabObject, itemName, title, description, note, type);
    }

    public virtual GameObject LoadObject(GameObject listObject, GameObject prefabObject, string itemName) {
        if(listObject == null) {
            return null;
        }
        if(prefabObject == null) {
            return null;
        }
        
        //GameObject item = NGUITools.AddChild(listObject, prefabObject);

        GameObject item = GameObjectHelper.CreateGameObject(prefabObject, Vector3.zero, Quaternion.identity, false);
        item.transform.parent = listObject.transform;
        item.ResetLocalPosition();
        // NGUITools.AddChild(listObject, prefabObject);
        item.name = "_" + increment++ + "_" + itemName;
        return item;
    }

    public virtual GameObject LoadObject(GameObject listObject, GameObject prefabObject, string itemName,
                                         string title, string description, string note, string type) {

        if(listObject == null) {
            return null;
        }

        if(prefabObject == null) {
            return null;
        }

        GameObject item = LoadObject(listObject, prefabObject, itemName);
        SetItemLabel(item, "LabelName", title);
        SetItemLabel(item, "LabelDescription", description);
        SetItemLabel(item, "LabelNote", note);

        // show type icon

        Transform typeObjects = item.transform.Find("types");

        if(typeObjects != null) {
            foreach(Transform t in typeObjects.gameObject.transform) {
                t.gameObject.Hide(); // hide all 
            }

            Transform typeObject = typeObjects.Find(type);
            if(typeObject != null) {
                // show current
                typeObject.gameObject.Show();
            }
        }

        return item;
    }

    public void SetItemLabel(GameObject item, string labelName, string val) {
        if(item == null) {
            return;
        }

        GameObject label = GetItemLabel(item, labelName);

        UIUtil.SetLabelValue(label, val);
    }

    public GameObject GetItemLabel(GameObject item, string labelName) {
        if(item == null) {
            return null;
        }

        Transform t = item.transform.Find(labelName);

        if(t != null) {

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
            UILabel label = t.GetComponent<UILabel>();
#else

            Text label = t.GetComponent<Text>();
#endif
            if(label != null) {
                return label.gameObject;
            }
        }
        return null;
    }

    // PANEL SECTIONS STATES

    public void HandleCharacterDisplay() {

        // handle character display

        if(characterDisplayState ==
            UIPanelCharacterDisplayState.Character) {

            GameUIPanelHeader.HideCharacterLarge();
            GameUIPanelHeader.ShowCharacter();
        }
        else if(characterDisplayState ==
            UIPanelCharacterDisplayState.CharacterLarge) {

            GameUIPanelHeader.HideCharacter();
            GameUIPanelHeader.ShowCharacterLarge();
        }
        else {
            // Panels that declare no character must hide it, mirroring
            // HandleBackgroundDisplay's show-on-state / hide-on-None symmetry.
            // Without this the character card leaks onto every later panel and
            // collides with their content (GameCenter buttons on statistics/
            // achievements, filter tiles on products). The leak never rendered
            // pre-flip because the show fade was a silent no-op.
            GameUIPanelHeader.HideCharacters();
        }
    }

    public void HandleAdDisplay() {

        // handle character display

        if(adDisplayState ==
            UIPanelAdDisplayState.BannerBottom) {

            AdNetworks.ShowAd(
                AdDisplayType.Banner, AdPosition.BottomCenter);
        }
        else if(adDisplayState ==
            UIPanelAdDisplayState.BannerTop) {

            AdNetworks.ShowAd(
                AdDisplayType.Banner, AdPosition.TopCenter);
        }
        else if(adDisplayState ==
            UIPanelAdDisplayState.Video) {

            AdNetworks.ShowAd(
                AdDisplayType.Video, AdPosition.Full);
        }
        else if(adDisplayState ==
            UIPanelAdDisplayState.Video) {

            AdNetworks.ShowAd(
                AdDisplayType.VideoIncentivized, AdPosition.Full);
        }
        else {

            AdNetworks.HideAd();
        }
    }

    public void HandleButtonDisplay() {

        // handle buttons

        if(buttonDisplayState ==
            UIPanelButtonsDisplayState.CharacterCustomize) {

            GameUIPanelFooter.ShowButtonsCharacterCustomize();
        }
        else if(buttonDisplayState ==
            UIPanelButtonsDisplayState.Character) {

            GameUIPanelFooter.ShowButtonsCharacter();
        }
        else if(buttonDisplayState ==
            UIPanelButtonsDisplayState.CharacterLarge) {

            GameUIPanelFooter.ShowButtonsCharacterLarge();
        }
        else if(buttonDisplayState ==
            UIPanelButtonsDisplayState.CharacterTools) {

            GameUIPanelFooter.ShowButtonsCharacterTools();
        }
        else if(buttonDisplayState == UIPanelButtonsDisplayState.Statistics) {

            GameUIPanelFooter.ShowButtonsStatistics();
        }
        else if(buttonDisplayState == UIPanelButtonsDisplayState.Achievements) {

            GameUIPanelFooter.ShowButtonsAchievements();
        }
        else if(buttonDisplayState == UIPanelButtonsDisplayState.GameNetworks) {

            GameUIPanelFooter.ShowButtonGameNetworks();
        }
        else if(buttonDisplayState == UIPanelButtonsDisplayState.ProductsSections) {

            GameUIPanelFooter.ShowButtonsProductsSections();
        }
    }

    public void HandleBackgroundDisplay() {

        // handle character display

        if(backgroundDisplayState ==
            UIPanelBackgroundDisplayState.PanelBacker) {
            GameUIPanelBackgrounds.ShowUI();
        }
        else if(backgroundDisplayState ==
            UIPanelBackgroundDisplayState.None) {
            GameUIPanelBackgrounds.HideUI();
        }
    }

    //

    public virtual void HandleShow() {
        buttonDisplayState = UIPanelButtonsDisplayState.None;
        characterDisplayState = UIPanelCharacterDisplayState.None;
        backgroundDisplayState = UIPanelBackgroundDisplayState.None;
        adDisplayState = UIPanelAdDisplayState.None;

        bool showAd = false;

        if(!GameUIController.IsUIPanel(GameUIPanel.panelMain)) {
            // show around every third screen

            if(UnityEngine.Random.Range(0, 3) == 0) {
                showAd = true;
            }
        }

        if(showAd) {
            adDisplayState = UIPanelAdDisplayState.BannerBottom;
        }
    }

    public virtual void HandleHide() {

#if USE_GAME_LIB_GAMEVERSES
        // TODO event
        GameCommunity.HideActionAppRate();
        GameCommunity.HideSharesCenter();
#endif
        AdNetworks.HideAd();
    }
}