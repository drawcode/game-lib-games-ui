using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
#else
using UnityEngine.UI;
#endif

using Engine.Events;
using Engine.Utility;

public class BaseGameUIPanelHeader : GameUIPanelBase {

    public static GameUIPanelHeader Instance;

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
    public UIImageButton buttonCoins;
    public UIImageButton buttonBack;
    public UILabel labelSection;
#else
    // 2.11: agnostic UIRef handles, bound at runtime by name.
    public Engine.UI.UIRef buttonCoins;
    public Engine.UI.UIRef buttonBack;
    public Engine.UI.UIRef labelSection;
#endif


    /*
    easeInQuad
    easeOutQuad
    easeInOutQuad
    easeInCubic
    easeOutCubic
    easeInOutCubic
    easeInQuart
    easeOutQuart
    easeInOutQuart
    easeInQuint
    easeOutQuint
    easeInOutQuint
    easeInSine
    easeOutSine
    easeInOutSine
    easeInExpo
    easeOutExpo
    easeInOutExpo
    easeInCirc
    easeOutCirc
    easeInOutCirc
    linear
    spring
    easeInBounce
    easeOutBounce
    easeInOutBounce
    easeInBack
    easeOutBack
    easeInOutBack
    easeInElastic
    easeOutElastic
    easeInOutElastic
    
    */

    public GameObject coinObject;
    public GameObject backObject;
    public GameObject backerObject;
    public GameObject titleObject;

    // Chrome motion: slightly faster + different ease than the content body (chrome-show/hide vs
    // panel-show/hide, tokens.json) so the header's entrance reads as fluid variance.
    public override string toolkitShowPreset {
        get {
            return "chrome-show";
        }
    }

    public override string toolkitHidePreset {
        get {
            return "chrome-hide";
        }
    }

    // Toolkit parallels (3B): bound by BindElements from the panel-header manifest. The GameObject
    // fields above stay wired to the NGUI prefab; the show/hide helpers drive BOTH, so the same
    // showFull/showMain choreography works on whichever backend is rendering. Unguarded on purpose
    // — UIRef is an engine type and compiles in both define branches; on NGUI they are simply
    // never bound and every op no-ops.
    public Engine.UI.UIRef labelCoin;
    public Engine.UI.UIRef coinObjectRef;
    public Engine.UI.UIRef backObjectRef;
    public Engine.UI.UIRef backerObjectRef;
    public Engine.UI.UIRef titleObjectRef;

    // The coin element in the toolkit view; a UIRenderStage feeds it the LIVE 3D coin (mesh +
    // particle effects) from the NGUI prefab as a RenderTexture — the real coin, key to drawing
    // players into the store/coin flows, composited inside the toolkit chrome.
    public Engine.UI.UIRef coinIconRef;

    private Engine.UI.UIRenderStage coinStage;
    private GameObject coinFlatLabel;
    private GameObject coinFlatButtonLabel;
    private GameObject coinFlatButtonBackground;
    private Collider coinFlatButtonCollider;
    private bool coinFlatButtonColliderWasEnabled;

    // The coin's glow particles get boosted while staged (the eye-draw spills past the coin);
    // originals restored when the toolkit view frees.
    private ParticleSystem[] coinEffectSystems;
    private float[] coinEffectOriginalSizes;

    // 3I — THE CHARACTERLARGE CLUSTER (backdrop card + posed 3D bot + CUSTOMIZE button).
    //
    // The header OWNS the shared character rig: main, game-mode, results, customize-character and
    // the customize/game-mode leaves all display the SAME one. So the conversion belongs here and
    // nowhere else — staging flips the rig's layers, and if an individual panel staged it every
    // other screen showing the character would fight over those layers (iter-8 finding: almost
    // nothing large on the customize screen is actually the panel's).
    //
    // It gets its OWN view rather than an element inside panel-header.uxml, because of draw order:
    // the card must render BEHIND flow panels (their nav arrows and name plates sit on top of it)
    // while the header band renders ABOVE them. One view in UILayers.backdrop, one owner, and
    // every character screen is fixed at once.
    public const string characterLargeViewKey = "panel-character-large";

    // ...and a SECOND view for the pieces that belong IN FRONT of the panel. The cluster
    // straddles the flow panel in legacy NGUI: the dark backer draws behind a screen's content
    // while the bot and the CUSTOMIZE button draw over it (the coop baseline is the clear case —
    // both overlap the green mode buttons, the backer does not). One view per side of the
    // `panel` band is the only way to reproduce that, since a view is composited as a unit.
    public const string characterLargeFrontViewKey = "panel-character-large-front";

    private Engine.UI.UIRef characterLargeView = Engine.UI.UIRef.none;
    private bool characterLargeLoadRequested;
    private Engine.UI.UIRef characterLargeFrontView = Engine.UI.UIRef.none;
    private bool characterLargeFrontLoadRequested;
    private Engine.UI.UIRenderStage characterLargeStage;

    // The flat NGUI pieces the view replaces — hidden while staged, restored on unstage so the
    // legacy path (and the kill switch) renders whole again.
    private GameObject characterLargeBacker;
    private GameObject characterLargeButton;

    // Staged == the toolkit card is what the player is seeing. Screens that have NOT been migrated
    // still show the rig through NGUI, so this flips per panel rather than latching on.
    private bool characterLargeStaged;

    public GameObject containerCharacters;
    public GameObject containerCharacter;
    public GameObject containerCharacterLarge;
    public GameCustomPlayerContainer containerCustomCharacterSmall;
    public GameCustomPlayerContainer containerCustomCharacterLarge;

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

        InitCharacters();

        //base.AnimateIn();
        AnimateIn();
    }

    public virtual void InitCharacters() {

        if(containerCustomCharacterSmall == null) {
            containerCustomCharacterSmall = containerCharacter.Get<GameCustomPlayerContainer>();
        }

        if(containerCustomCharacterLarge == null) {
            containerCustomCharacterLarge = containerCharacterLarge.Get<GameCustomPlayerContainer>();
        }

        characterLargeShowFront();
        characterLargeZoomOut();
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

        // Chain to base so UIPanelBase.OnDisable -> FreeToolkitView runs if the header is ever
        // disabled (leaving the menu flow). Draw order keeps it above panels (UILayers.chrome);
        // LIFETIME stays the standard enable/disable contract like every other panel. 3B prereq.
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

        HideCharacters();

        if(className == classNameTo) {
            //

            if(code.Contains("-internal")) {
                AnimateInInternal();
            }
        }
    }

    public override void OnButtonClickEventHandler(string buttonName) {
        //LogUtil.Log("OnButtonClickEventHandler: " + buttonName);

#if ENABLE_FEATURE_PRODUCT_CURRENCY
        if(buttonCoins != null) {

            if(buttonName == buttonCoins.name) {
                GameCommunity.HideGameCommunity();
                GameUIController.ShowProductCurrency();
            }
        }
#endif
    }

    public override void AnimateIn() {

        backgroundDisplayState = UIPanelBackgroundDisplayState.None;

        base.AnimateIn();
    }

    public virtual void AnimateInMain() {

        AnimateIn();

        showMain();
    }

    public virtual void AnimateInInternal() {

        AnimateIn();

        showFull();
    }

    public override void AnimateOut() {
        base.AnimateOut();

        HideBackButtonObject();
        HideBackerObject();
        HideCoinsObject();
        HideTitleObject();

        HideCharacter();
    }

    //

    public static void CharacterLargeShowPose() {
        if(Instance != null) {
            Instance.characterLargeShowPose();
        }
    }

    public void characterLargeShowPose() {
        characterLargeRotation(.89);
    }

    public static void CharacterLargeShowFront() {
        if(Instance != null) {
            Instance.characterLargeShowFront();
        }
    }

    public void characterLargeShowFront() {
        characterLargeRotation(0);
    }

    public static void CharacterLargeShowBack() {
        if(Instance != null) {
            Instance.characterLargeShowBack();
        }
    }

    public void characterLargeShowBack() {
        characterLargeRotation(.5);
    }

    //

    public static void CharacterLargeZoomOut() {
        if(Instance != null) {
            Instance.characterLargeZoomOut();
        }
    }

    public void characterLargeZoomOut() {
        characterLargeZoom(1.0);
    }

    public static void CharacterLargeZoomIn() {
        if(Instance != null) {
            Instance.characterLargeZoomIn();
        }
    }

    public void characterLargeZoomIn() {
        characterLargeZoom(2.0);
    }

    public static void CharacterLargeZoom(double scaleTo) {
        if(Instance != null) {
            Instance.characterLargeZoom(scaleTo);
        }
    }

    public void characterLargeZoom(double scaleTo) {
        characterLargeScale(scaleTo);
    }

    //

    public static void CharacterLargeRotation(double valEnd) {
        if(Instance != null) {
            Instance.characterLargeRotation(valEnd);
        }
    }

    public void characterLargeRotation(double rotationTo) {

        if(containerCustomCharacterLarge == null) {
            return;
        }

        containerCustomCharacterLarge.HandleContainerRotation(rotationTo);
    }

    //

    public static void CharacterLargeScale(double valEnd) {
        if(Instance != null) {
            Instance.characterLargeScale(valEnd);
        }
    }

    public void characterLargeScale(double scaleTo) {

        if(containerCustomCharacterLarge == null) {
            return;
        }

        containerCustomCharacterLarge.HandleContainerScale(scaleTo);
    }

    //

    public static void CharacterSmallScale(double scaleTo) {
        if(Instance != null) {
            Instance.characterSmallScale(scaleTo);
        }
    }

    public void characterSmallScale(double scaleTo) {

        if(containerCustomCharacterSmall == null) {
            return;
        }

        containerCustomCharacterSmall.HandleContainerScale(scaleTo);
    }

    //

    public static void HideTitle() {
        if(GameUIPanelHeader.Instance != null) {
            GameUIPanelHeader.Instance.hideTitle();
        }
    }

    public virtual void hideTitle() {

        if(isToolkitPanel) {
            UIUtil.HideLabel(UIUtil.ResolveDeep(viewRoot, "LabelSection"));
        }

        UIUtil.HideLabel(labelSection);
    }

    public static void ShowTitle(string title) {
        if(GameUIPanelHeader.Instance != null) {
            GameUIPanelHeader.Instance.showTitle(title);
        }
    }

    // WHY THIS WRITES BY ELEMENT NAME instead of through a bound ref: `labelSection` is declared
    // inside the `#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3` branch at the top of this file, and that
    // branch IS the one compiled here — so the field is a legacy `UILabel`, not a `UIRef`, and
    // BindElements can never rebind it however correct binds/panel-header.json looks. The title
    // was therefore only ever written to the NGUI label, which SuppressLegacyView had already
    // hidden: the header band has rendered title-less on every migrated screen all along. Same
    // class of bug (and same fix) as the worlds labelWorldTitle/labelWorldDescription pair.
    //
    // Cached because the write can land before the async view exists: the header titles a screen
    // from AnimateIn, which on a cold header runs a frame or two before LoadToolkitView's
    // continuation. SuppressLegacyView (which runs IN that continuation) replays it.
    protected string toolkitTitle = "";

    public virtual void showTitle(string title) {

        toolkitTitle = title;

        if(isToolkitPanel) {
            Engine.UI.UIRef label = UIUtil.ResolveDeep(viewRoot, "LabelSection");
            UIUtil.ShowLabel(label);
            UIUtil.SetLabelValue(label, title);
        }

        UIUtil.ShowLabel(labelSection);
        UIUtil.SetLabelValue(labelSection, title);
    }

    public static void ShowFull() {
        if(GameUIPanelHeader.Instance != null) {
            GameUIPanelHeader.Instance.showFull();
        }
    }

    public virtual void showFull() {
        ShowCoinsObject();
        ShowBackerObject();
        ShowBackButtonObject();
        ShowTitleObject();
        RefreshCoins();
    }

    public static void ShowMain() {
        if(GameUIPanelHeader.Instance != null) {
            GameUIPanelHeader.Instance.showMain();
        }
    }

    public virtual void showMain() {
        ShowCoinsObject();
        HideBackerObject();
        HideBackButtonObject();
        HideTitleObject();
        RefreshCoins();
    }

    // Coin count refreshes FROM DATA on show (user decision 2026-07-15): while hidden it may go
    // stale, but every show re-reads the profile. Replaces the NGUI prefab's UIGameRPGCurrency
    // 1s poller for the toolkit path — no per-frame work while the header just sits there.
    public virtual void RefreshCoins() {

        if(isToolkitPanel) {
            UIUtil.SetLabelValue(labelCoin,
                GameProfileRPGs.Current.GetCurrency().ToString("N0"));
        }
    }

    // The 3D character preview containers (Characters) live INSIDE this panel's Container, so the
    // default whole-container suppression would kill the customize screens' character display.
    // Hide only the flat NGUI widgets the toolkit view replaces; everything else stays live.
    //
    // The coin cluster is suppressed at a FINER grain: its flat NGUI bits (count label, "+"
    // label, sprite backer) hide, but the 3D coin subtree (mesh + effect particles) stays alive
    // and is handed to a UIRenderStage — an isolated layer + tiny camera renders it to a
    // RenderTexture shown by the toolkit view's CoinIcon element. World content can't draw above
    // a toolkit panel, so the RT is how the real animated coin survives the chrome migration.
    protected override void SuppressLegacyView() {

        SuppressCoinCluster();

        if(backObject != null) {
            backObject.Hide();
        }

        if(backerObject != null) {
            backerObject.Hide();
        }

        if(titleObject != null) {
            titleObject.Hide();
        }

        // Replay the title requested before the view existed (see showTitle).
        if(!string.IsNullOrEmpty(toolkitTitle)) {
            UIUtil.SetLabelValue(UIUtil.ResolveDeep(viewRoot, "LabelSection"), toolkitTitle);
        }
    }

    protected virtual void SuppressCoinCluster() {

        if(coinObject == null) {
            return;
        }

        Transform t = coinObject.transform;

        coinFlatLabel = t.Find("LabelCoin") != null
            ? t.Find("LabelCoin").gameObject : null;
        coinFlatButtonLabel = t.Find("ButtonGameProductCurrency/Label") != null
            ? t.Find("ButtonGameProductCurrency/Label").gameObject : null;
        coinFlatButtonBackground = t.Find("ButtonGameProductCurrency/Background") != null
            ? t.Find("ButtonGameProductCurrency/Background").gameObject : null;

        if(coinFlatLabel != null) {
            coinFlatLabel.Hide();
        }

        if(coinFlatButtonLabel != null) {
            coinFlatButtonLabel.Hide();
        }

        if(coinFlatButtonBackground != null) {
            coinFlatButtonBackground.Hide();
        }

        SuppressCoinButtonCollider(t);

        SetupCoinStage(t);
    }

    // ButtonGameProductCurrency itself is NEVER hidden here — only its Label/Background children —
    // because the staged 3D coin (UICoin) lives INSIDE it and Hide() does SetActive(false), which
    // would take the coin down too. That left the button INVISIBLE BUT STILL PICKABLE: its collider
    // stayed live on the menu UI layer, whose UICamera sits at depth 10 while the in-game HUDCamera
    // is at depth 6. NGUI sorts UICameras by DESCENDING depth, so the invisible coin button won every
    // tap in the top-right corner — the same corner as the HUD pause button. Symptom: tapping pause
    // during gameplay opened the STORE and pause never fired at all (2026-07-23 user report; probe
    // confirmed UICamera.Raycast -> ButtonGameProductCurrency while physics found ButtonGamePause).
    // Disabling just the collider keeps UICoin rendering while making the suppressed button inert.
    protected virtual void SuppressCoinButtonCollider(Transform coinRoot) {

        Transform button = coinRoot.Find("ButtonGameProductCurrency");

        if(button == null) {
            return;
        }

        coinFlatButtonCollider = button.GetComponent<Collider>();

        if(coinFlatButtonCollider == null) {
            return;
        }

        coinFlatButtonColliderWasEnabled = coinFlatButtonCollider.enabled;
        coinFlatButtonCollider.enabled = false;
    }

    protected virtual void SetupCoinStage(Transform coinRoot) {

        if(coinStage != null) {
            return;
        }

        Transform uiCoin = coinRoot.Find("ButtonGameProductCurrency/UICoin");

        if(uiCoin == null) {
            return;
        }

        // Dedicated widget layer; UI3D as fallback (older project configs).
        int layer = LayerMask.NameToLayer("UIWidget3D");

        if(layer < 0) {
            layer = LayerMask.NameToLayer("UI3D");
        }

        // Modest framing headroom: the coin fills most of the element, with RT room for the
        // boosted glow to reach just past its edge.
        coinStage = Engine.UI.UIRenderStage.Attach(uiCoin.gameObject, layer, 128, 1.3f);

        if(coinStage != null) {
            UIUtil.SetImageTexture(coinIconRef, coinStage.texture);
            BoostCoinEffect(uiCoin, 1.8f);
        }
    }

    // Scale the glow's particle START SIZE (not the element, not the transform — size multiplier
    // works regardless of the systems' scaling mode) so the effect draws the eye by spilling
    // just outside the coin. Restored in FreeToolkitView.
    protected virtual void BoostCoinEffect(Transform uiCoin, float factor) {

        Transform effect = uiCoin.Find("Effect");

        if(effect == null) {
            return;
        }

        coinEffectSystems = effect.GetComponentsInChildren<ParticleSystem>(true);
        coinEffectOriginalSizes = new float[coinEffectSystems.Length];

        for(int i = 0; i < coinEffectSystems.Length; i++) {

            ParticleSystem.MainModule main = coinEffectSystems[i].main;
            coinEffectOriginalSizes[i] = main.startSizeMultiplier;
            main.startSizeMultiplier = coinEffectOriginalSizes[i] * factor;
        }
    }

    protected virtual void RestoreCoinEffect() {

        if(coinEffectSystems == null) {
            return;
        }

        for(int i = 0; i < coinEffectSystems.Length; i++) {

            if(coinEffectSystems[i] != null) {
                ParticleSystem.MainModule main = coinEffectSystems[i].main;
                main.startSizeMultiplier = coinEffectOriginalSizes[i];
            }
        }

        coinEffectSystems = null;
        coinEffectOriginalSizes = null;
    }

    // ---- 3I: the CharacterLarge cluster ------------------------------------
    //
    // Entry point, called by the panel coming up (UIPanelBase.HandleCharacterDisplay). `toolkit`
    // is that panel's isToolkitPanel: a migrated screen gets the converted card, an unmigrated one
    // keeps the legacy NGUI rig untouched. Toolkit views composite above the ENTIRE camera stack,
    // so showing the card on an unmigrated screen would bury its NGUI content — this flag is the
    // migration seam, and once every character screen is converted it is simply always true.
    public static void SetCharacterLargeToolkit(bool toolkit) {

        if(GameUIPanelHeader.Instance != null) {
            GameUIPanelHeader.Instance.setCharacterLargeToolkit(toolkit);
        }
    }

    public virtual void setCharacterLargeToolkit(bool toolkit) {

        if(toolkit && Engine.UI.UIPlatform.toolkitViewsEnabled) {
            StageCharacterLarge();
        }
        else {
            UnstageCharacterLarge();
        }
    }

    // Hide the flat NGUI chrome the view replaces, stage the 3D rig into a RenderTexture, and
    // bring the card view up. The 3D subtree is NOT hidden — hiding it would kill the animating
    // bot; it is staged instead, so it keeps running and renders into the RT.
    protected virtual void StageCharacterLarge() {

        if(characterLargeStaged || containerCharacterLarge == null) {
            return;
        }

        Transform container = containerCharacterLarge.transform.Find("ContainerCharacterLarge");

        if(container == null) {
            return;
        }

        characterLargeStaged = true;

        characterLargeBacker = container.Find("Background-a-40") != null
            ? container.Find("Background-a-40").gameObject : null;
        characterLargeButton = container.Find("ButtonGameCustomize") != null
            ? container.Find("ButtonGameCustomize").gameObject : null;

        // Unlike the coin button (which has the 3D coin INSIDE it, so only its children could be
        // hidden), ButtonGameCustomize holds nothing but flat widgets and its collider — so the
        // whole GameObject goes down, and with it the invisible-but-pickable trap that cost a
        // session on the coin. The view draws a name-bridged ButtonGameCustomize in its place.
        if(characterLargeBacker != null) {
            characterLargeBacker.Hide();
        }

        if(characterLargeButton != null) {
            characterLargeButton.Hide();
        }

        SetupCharacterLargeStage(container);
        LoadCharacterLargeView();
    }

    protected virtual void SetupCharacterLargeStage(Transform container) {

        if(characterLargeStage != null) {
            return;
        }

        Transform rig = container.Find("Container");

        if(rig == null) {
            return;
        }

        // Dedicated widget layer; UI3D as fallback (older project configs) — same as the coin.
        int layer = LayerMask.NameToLayer("UIWidget3D");

        if(layer < 0) {
            layer = LayerMask.NameToLayer("UI3D");
        }

        // 512, not the coin's 128: the bot is the largest 3D widget in the game and fills a
        // ~816x514 card. framePadding 1.15 crops tight — this rig has no particle spill to leave
        // room for, unlike the coin's glow.
        //
        // keepColliderLayers KEEPS THE BOT DRAGGABLE. showCharacterLargeCo hands Rotator's
        // collider to InputSystem as the current draggable; Rotator carries a collider and no
        // renderer, so it stays on the UI event layer while the meshes move to the stage layer.
        // Without it the bot would render correctly and silently stop spinning under the finger.
        // followContent: this rig is TWEENED into place, unlike the coin. Attach happens while the
        // container is still parked off-screen, so a fixed camera frames empty space ~14 units
        // above the bot and the RT comes back fully transparent (measured, iter 9). The camera
        // has to travel with the container.
        characterLargeStage = Engine.UI.UIRenderStage.Attach(
            rig.gameObject, layer, 512, 1.15f, true, true);

        SyncCharacterLargeTexture();
    }

    // ASYNC like every other view load (PanelRenderer builds a frame or two later), so the texture
    // bind runs in the continuation as well as at attach time — whichever lands second wins.
    protected virtual void LoadCharacterLargeView() {

        LoadCharacterLargePart(
            characterLargeViewKey,
            Engine.UI.UILayers.backdrop,
            delegate { return characterLargeView; },
            delegate(Engine.UI.UIRef v) { characterLargeView = v; },
            delegate { return characterLargeLoadRequested; },
            delegate(bool b) { characterLargeLoadRequested = b; });

        LoadCharacterLargePart(
            characterLargeFrontViewKey,
            Engine.UI.UILayers.foreground,
            delegate { return characterLargeFrontView; },
            delegate(Engine.UI.UIRef v) { characterLargeFrontView = v; },
            delegate { return characterLargeFrontLoadRequested; },
            delegate(bool b) { characterLargeFrontLoadRequested = b; });
    }

    // One half of the cluster. Parameterised rather than duplicated because the two halves differ
    // only in key and band, and the in-flight-orphan handling below is the part that must not
    // drift between them.
    protected virtual void LoadCharacterLargePart(
        string viewKey,
        int band,
        System.Func<Engine.UI.UIRef> getView,
        System.Action<Engine.UI.UIRef> setView,
        System.Func<bool> getRequested,
        System.Action<bool> setRequested) {

        if(getView().alive) {
            SyncCharacterLargeTexture();
            return;
        }

        if(getRequested()) {
            return;
        }

        Engine.UI.IUIBackend backend = Engine.UI.UIPlatform.viewBackend;

        if(backend == null) {
            return;
        }

        setRequested(true);

        backend.LoadView(viewKey, band, (Engine.UI.UIRef view) => {

            if(view == null || !view.alive) {
                setRequested(false);
                return;
            }

            // Unstaged (or already re-loaded) while the build was in flight — destroy the orphan
            // rather than leak its PanelRenderer. Same contract as UIPanelBase.LoadToolkitView.
            if(!getRequested() || getView().alive) {
                backend.DestroyView(view);
                return;
            }

            setView(view);

            SyncCharacterLargeTexture();

            if(characterLargeStaged) {
                backend.Show(view);
            }
            else {
                backend.Hide(view);
            }
        });
    }

    // The stage element moved to the FRONT view when the cluster was split across the panel band.
    protected virtual void SyncCharacterLargeTexture() {

        if(characterLargeStage == null || !characterLargeFrontView.alive) {
            return;
        }

        UIUtil.SetImageTexture(
            UIUtil.ResolveDeep(characterLargeFrontView, "CharacterLargeStage"),
            characterLargeStage.texture);
    }

    // Give the rig back to NGUI: original layers restored, flat widgets shown, card hidden. The
    // view itself is kept (hidden) so returning to a migrated screen does not pay another async
    // build; FreeToolkitView is what actually destroys it.
    protected virtual void UnstageCharacterLarge() {

        characterLargeLoadRequested = false;
        characterLargeFrontLoadRequested = false;

        if(!characterLargeStaged) {
            return;
        }

        characterLargeStaged = false;

        if(characterLargeStage != null) {
            characterLargeStage.Detach();
            characterLargeStage = null;
        }

        if(characterLargeBacker != null) {
            characterLargeBacker.Show();
            characterLargeBacker = null;
        }

        if(characterLargeButton != null) {
            characterLargeButton.Show();
            characterLargeButton = null;
        }

        UIUtil.HideObject(characterLargeView);
        UIUtil.HideObject(characterLargeFrontView);
    }

    protected virtual void FreeCharacterLargeView() {

        UnstageCharacterLarge();

        if(characterLargeView.alive) {

            Engine.UI.IUIBackend backend = Engine.UI.UIPlatform.For(characterLargeView);

            if(backend != null) {
                backend.DestroyView(characterLargeView);
            }
        }

        characterLargeView = Engine.UI.UIRef.none;

        if(characterLargeFrontView.alive) {

            Engine.UI.IUIBackend frontBackend = Engine.UI.UIPlatform.For(characterLargeFrontView);

            if(frontBackend != null) {
                frontBackend.DestroyView(characterLargeFrontView);
            }
        }

        characterLargeFrontView = Engine.UI.UIRef.none;
    }

    // The stage + suppressed NGUI pieces belong to the toolkit view's lifetime: when the view is
    // freed (header disabled, or kill switch), restore the NGUI coin/flat widgets so the legacy
    // path renders whole again.
    protected override void FreeToolkitView() {

        RestoreCoinEffect();

        if(coinStage != null) {
            coinStage.Detach();
            coinStage = null;
        }

        FreeCharacterLargeView();

        if(coinFlatLabel != null) {
            coinFlatLabel.Show();
        }

        if(coinFlatButtonLabel != null) {
            coinFlatButtonLabel.Show();
        }

        if(coinFlatButtonBackground != null) {
            coinFlatButtonBackground.Show();
        }

        // Symmetric with SuppressCoinButtonCollider: the legacy path needs a clickable coin button.
        if(coinFlatButtonCollider != null) {
            coinFlatButtonCollider.enabled = coinFlatButtonColliderWasEnabled;
            coinFlatButtonCollider = null;
        }

        base.FreeToolkitView();
    }

    public static void ShowNone() {
        if(GameUIPanelHeader.Instance != null) {
            GameUIPanelHeader.Instance.showNone();
        }
    }

    public virtual void showNone() {
        AnimateOut();
    }

    // characters

    public static void HideCharacters() {
        HideCharacter();
        HideCharacterLarge();
    }

    // characters 

    public static void ShowCharacter() {
        if(GameUIPanelHeader.Instance != null) {
            GameUIPanelHeader.Instance.showCharacter();
        }
    }

    public virtual void showCharacter() {
        StartCoroutine(showCharacterCo());
    }

    public IEnumerator showCharacterCo() {
        yield return new WaitForSeconds(.55f);
        TweenUtil.ShowObjectTop(containerCharacter);

        if(containerCharacter != null) {
            containerCharacter.ResetRigidBodiesVelocity();
        }

        if(containerCustomCharacterSmall != null) {
            containerCustomCharacterSmall.HandleContainerScale(1);
            containerCustomCharacterSmall.HandleContainerRotation(.91);

            InputSystem.Instance.currentDraggableUIGameObject =
                containerCustomCharacterSmall.containerRotator;
        }
    }

    public static void HideCharacter() {
        if(GameUIPanelHeader.Instance != null) {
            GameUIPanelHeader.Instance.hideCharacter();
        }
    }

    public virtual void hideCharacter() {
        TweenUtil.HideObjectTop(containerCharacter);

        InputSystem.Instance.currentDraggableUIGameObject =
            null;
    }

    // large

    public static void ShowCharacterLarge() {
        if(GameUIPanelHeader.Instance != null) {
            GameUIPanelHeader.Instance.showCharacterLarge();
        }
    }

    public virtual void showCharacterLarge() {
        StartCoroutine(showCharacterLargeCo());
    }

    public IEnumerator showCharacterLargeCo() {
        yield return new WaitForSeconds(.55f);

        // STAGED: snap the rig into its parked position instead of sliding it (time/delay 0).
        // The stage camera frames WORLD space at attach time and does not follow, so tweening the
        // container would slide the bot out through the edge of its own RenderTexture. All the
        // motion belongs to the toolkit card, which slides as one view just below the panels.
        if(characterLargeStaged) {
            TweenUtil.ShowObjectTop(containerCharacterLarge, TweenCoord.local, true, 0f, 0f);
        }
        else {
            TweenUtil.ShowObjectTop(containerCharacterLarge);
        }

        if(containerCharacterLarge != null) {
            containerCharacterLarge.ResetRigidBodiesVelocity();

            InputSystem.Instance.currentDraggableUIGameObject =
                containerCustomCharacterLarge.containerRotator;
        }

        if(characterLargeStaged) {

            if(characterLargeStage != null) {
                characterLargeStage.SetVisible(true);
            }

            TweenUtil.ShowObjectTop(characterLargeView);
            TweenUtil.ShowObjectTop(characterLargeFrontView);
        }

        characterLargeShowPose();
        characterLargeZoomOut();

        // The pose and the zoom both change the rig's SIZE, and Attach framed it at whatever size
        // it happened to be beforehand — leaving the bot correct but small in its own RT. Re-fit
        // once it has settled. characterLargeZoomOut is an EASED tween (QuadEaseInOut), not a set,
        // so a next-frame re-fit would measure the rig mid-zoom and then let it grow out of frame;
        // wait out the ease first. The SkinnedMeshRenderer's bounds update lazily too.
        if(characterLargeStaged && characterLargeStage != null) {

            yield return new WaitForSeconds(.75f);

            if(characterLargeStage != null) {
                characterLargeStage.Reframe();
            }
        }
    }

    public static void HideCharacterLarge() {
        if(GameUIPanelHeader.Instance != null) {
            GameUIPanelHeader.Instance.hideCharacterLarge();
        }
    }

    public virtual void hideCharacterLarge() {

        // Staged: same reasoning as the show — snap the (invisible) NGUI container to its hidden
        // state so legacy state stays consistent for a later unstage, and let the card slide out
        // on its own. The stage camera goes off first, so the RT simply freezes rather than
        // showing the rig leave frame.
        if(characterLargeStaged) {

            if(characterLargeStage != null) {
                characterLargeStage.SetVisible(false);
            }

            TweenUtil.HideObjectTop(characterLargeView);
            TweenUtil.HideObjectTop(characterLargeFrontView);
            TweenUtil.HideObjectTop(containerCharacterLarge, TweenCoord.local, true, 0f, 0f);
        }
        else {
            TweenUtil.HideObjectTop(containerCharacterLarge);
        }

        InputSystem.Instance.currentDraggableUIGameObject = null;
    }

    public virtual void ShowBackButtonObject() {

        // Toolkit parallel: same show, on the bound view element (no-op when unbound/NGUI).
        TweenUtil.FadeToObject(backObjectRef, 1f, "fade-in");

        // Once the toolkit view owns the header, the NGUI widgets must STAY suppressed —
        // without this gate every showFull() re-showed them under the toolkit band (double
        // header). Same gate on every helper below.
        if(isToolkitPanel) {
            return;
        }

        if(backObject != null) {

            backerObject.Show();

            TweenUtil.ShowObjectLeft(backObject);

            //UITweenerUtil.MoveTo(backObject,
            //    UITweener.Method.EaseInOut, UITweener.Style.Once, .3f, .3f, Vector3.zero);

            //UITweenerUtil.FadeTo(backObject,
            //    UITweener.Method.EaseInOut, UITweener.Style.Once, 1f, .3f, 1f);

            foreach(Transform t in backObject.transform) {

                TweenUtil.FadeToObject(t.gameObject, 1f, 1f);

                //UITweenerUtil.FadeTo(t.gameObject,
                //    UITweener.Method.EaseInOut, UITweener.Style.Once, 1f, .3f, 1f);
            }
        }
    }

    public virtual void HideBackButtonObject() {

        TweenUtil.FadeToObject(backObjectRef, 0f, "fade-out");

        if(isToolkitPanel) {
            return;
        }

        if(backObject != null) {

            TweenUtil.HideObjectLeft(backObject);

            //UITweenerUtil.MoveTo(backObject,
            //    UITweener.Method.EaseInOut, UITweener.Style.Once, .3f, .3f, Vector3.zero.WithX(-3000));

            //UITweenerUtil.FadeTo(backObject,
            //    UITweener.Method.EaseInOut, UITweener.Style.Once, .3f, .3f, 0f);

            foreach(Transform t in backObject.transform) {

                TweenUtil.FadeToObject(t.gameObject, 0f, .3f);

                //UITweenerUtil.FadeTo(t.gameObject,
                //UITweener.Method.EaseInOut, UITweener.Style.Once, .3f, .3f, 0f);
            }
        }
    }

    public virtual void ShowBackerObject() {
        TweenUtil.FadeToObject(backerObjectRef, 1f, "fade-in");

        if(!isToolkitPanel) {
            TweenUtil.FadeToObject(backerObject, 1f);
        }
    }

    public virtual void HideBackerObject() {
        TweenUtil.FadeToObject(backerObjectRef, 0f, "fade-out");

        if(!isToolkitPanel) {
            TweenUtil.FadeToObject(backerObject, 0f);
        }
    }

    public virtual void ShowTitleObject() {
        TweenUtil.FadeToObject(titleObjectRef, 1f, "fade-in");

        if(!isToolkitPanel) {
            TweenUtil.FadeToObject(titleObject, 1f);
        }
    }

    public virtual void HideTitleObject() {
        TweenUtil.FadeToObject(titleObjectRef, 0f, "fade-out");

        if(!isToolkitPanel) {
            TweenUtil.FadeToObject(titleObject, 0f);
        }
    }

    public virtual void ShowCoinsObject() {
        TweenUtil.FadeToObject(coinObjectRef, 1f, "fade-in");

        // The RT stage only renders while the coin is on screen.
        if(coinStage != null) {
            coinStage.SetVisible(true);
        }

        if(!isToolkitPanel) {
            TweenUtil.FadeToObject(coinObject, 1f);
        }
    }

    public virtual void HideCoinsObject() {
        TweenUtil.FadeToObject(coinObjectRef, 0f, "fade-out");

        if(coinStage != null) {
            coinStage.SetVisible(false);
        }

        if(!isToolkitPanel) {
            TweenUtil.FadeToObject(coinObject, 0f);
        }
    }

    public static void LoadData() {
        if(GameUIPanelHeader.Instance != null) {
            GameUIPanelHeader.Instance.loadData();
        }
    }

    public virtual void loadData() {
        StartCoroutine(loadDataCo());
    }

    IEnumerator loadDataCo() {

        yield return new WaitForSeconds(1f);
    }

    public virtual void Update() {

        if(Input.GetKey(KeyCode.LeftControl)) {
            if(Input.GetKey(KeyCode.LeftAlt)) {

                if(Input.GetKey(KeyCode.N)) {

                    CharacterLargeScale(2.0f);
                }

                if(Input.GetKey(KeyCode.M)) {

                    CharacterLargeScale(1.0f);
                }
            }
        }
    }
}