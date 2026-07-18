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

    // The coin's glow particles get boosted while staged (the eye-draw spills past the coin);
    // originals restored when the toolkit view frees.
    private ParticleSystem[] coinEffectSystems;
    private float[] coinEffectOriginalSizes;
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
        UIUtil.HideLabel(labelSection);
    }

    public static void ShowTitle(string title) {
        if(GameUIPanelHeader.Instance != null) {
            GameUIPanelHeader.Instance.showTitle(title);
        }
    }

    public virtual void showTitle(string title) {
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

        SetupCoinStage(t);
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

    // The stage + suppressed NGUI pieces belong to the toolkit view's lifetime: when the view is
    // freed (header disabled, or kill switch), restore the NGUI coin/flat widgets so the legacy
    // path renders whole again.
    protected override void FreeToolkitView() {

        RestoreCoinEffect();

        if(coinStage != null) {
            coinStage.Detach();
            coinStage = null;
        }

        if(coinFlatLabel != null) {
            coinFlatLabel.Show();
        }

        if(coinFlatButtonLabel != null) {
            coinFlatButtonLabel.Show();
        }

        if(coinFlatButtonBackground != null) {
            coinFlatButtonBackground.Show();
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
        TweenUtil.ShowObjectTop(containerCharacterLarge);

        if(containerCharacterLarge != null) {
            containerCharacterLarge.ResetRigidBodiesVelocity();

            InputSystem.Instance.currentDraggableUIGameObject =
                containerCustomCharacterLarge.containerRotator;
        }

        characterLargeShowPose();
        characterLargeZoomOut();
    }

    public static void HideCharacterLarge() {
        if(GameUIPanelHeader.Instance != null) {
            GameUIPanelHeader.Instance.hideCharacterLarge();
        }
    }

    public virtual void hideCharacterLarge() {
        TweenUtil.HideObjectTop(containerCharacterLarge);

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