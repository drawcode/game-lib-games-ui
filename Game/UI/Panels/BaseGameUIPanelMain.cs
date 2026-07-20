using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Engine.UI;
using Engine.Utility;
using Engine.Events;

public class BaseGameUIPanelMain : GameUIPanelBase {

    public static GameUIPanelMain Instance;

    public GameObject listItemPrefab;
    public GameObject listItemSetPrefab;
    public GameObject containerObject;
    public GameObject containerLogoObject;
    public GameObject containerPlayerObject;
    public GameObject containerAppRate;
    public GameObject containerStartObject;

    public GameObject buttonPlayerDefaultObject;

    // Toolkit parallel (3B part 3): the pulsing tap-to-play CTA in panel-main.uxml, bound by
    // BindElements from the manifest. The logo/sponsor need no refs — no runtime ops touch them.
    public UIRef startObjectRef;

    // The view keeps the DEFAULT top slide: in the NGUI flow the logo/CTA (center) and the
    // sponsor (top-left) entered from the top/side — only the CHARACTER rose from the bottom,
    // and it still does, via the legacy edge slides below. (A bottom view slide was tried and
    // read wrong — user: "the logo and tap character to play came in from the top".)

    // HYBRID panel: the 3D character (panelBottomObject), particles (panelCenterObject), and
    // their plates stay live on the legacy side — the nine-edge slides must keep running so
    // they animate in/out with the toolkit view instead of parking on screen forever.
    protected override bool toolkitKeepsLegacyMotion {
        get {
            return true;
        }
    }

    // Only the FLAT widgets this view replaces hide: the logo sprite, the CTA label container,
    // and the sponsor watermark (not a serialized field — found by its anchor path). The 3D
    // character button, its backer plate, and the particle glows stay live on the legacy side;
    // they render beneath the toolkit overlay and take taps through it (every element in
    // panel-main.uxml is picking-Ignore).
    protected override void SuppressLegacyView() {

        if(containerLogoObject != null) {
            containerLogoObject.Hide();
        }

        if(containerStartObject != null) {
            containerStartObject.Hide();
        }

        Transform sponsor = transform.Find("Container/AnchorTopLeft/TopLeft");

        if(sponsor != null) {
            sponsor.gameObject.Hide();
        }

        // The view arrives ASYNC: the first AnimateIn's AnimateStartCharacter ran while the
        // panel was still NGUI-only, so replay the CTA pulse onto the freshly bound label.
        TweenUtil.FadeToObject(startObjectRef, .5f, "label-pulse");
    }

    // Kill switch / pooled-away: restore the suppressed NGUI widgets so the legacy path
    // renders whole again (same contract as the header's coin cluster restore).
    protected override void FreeToolkitView() {

        startObjectRef = UIRef.none;

        if(isToolkitPanel) {

            if(containerLogoObject != null) {
                containerLogoObject.Show();
            }

            if(containerStartObject != null) {
                containerStartObject.Show();
            }

            Transform sponsor = transform.Find("Container/AnchorTopLeft/TopLeft");

            if(sponsor != null) {
                sponsor.gameObject.Show();
            }
        }

        base.FreeToolkitView();
    }

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

        Messenger<string>.AddListener(
            ButtonEvents.EVENT_BUTTON_CLICK,
            OnButtonClickEventHandler);

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

        Messenger<string>.RemoveListener(
            ButtonEvents.EVENT_BUTTON_CLICK,
            OnButtonClickEventHandler);

        Messenger<string>.RemoveListener(
            UIControllerMessages.uiPanelAnimateIn,
            OnUIControllerPanelAnimateIn);

        Messenger<string>.RemoveListener(
            UIControllerMessages.uiPanelAnimateOut,
            OnUIControllerPanelAnimateOut);

        Messenger<string, string>.RemoveListener(
            UIControllerMessages.uiPanelAnimateType,
            OnUIControllerPanelAnimateType);

        // Chain to base so UIPanelBase.OnDisable -> FreeToolkitView runs when the panel is
        // pooled away — same 3B prerequisite fix as header/footer.
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

        //LoadData();
        //AnimateIn();

        if(containerAppRate != null) {
            if(Context.Current.isWeb) {
                containerAppRate.Hide();
            }
        }
    }

    public void LoadData() {
        StartCoroutine(LoadDataCo());
    }

    IEnumerator LoadDataCo() {
        yield break;
    }

    public override void HandleShow() {
        base.HandleShow();

        buttonDisplayState = UIPanelButtonsDisplayState.None;
        characterDisplayState = UIPanelCharacterDisplayState.None;
        backgroundDisplayState = UIPanelBackgroundDisplayState.None;

#if USE_GAME_LIB_GAMEVERSES
        GameCommunity.HideBroadcastRecordPlayShare();
#endif
    }

    public override void HandleHide() {
        base.HandleHide();

#if USE_GAME_LIB_GAMEVERSES
        GameCommunity.HideActionAppRate();
        GameCommunity.HideBroadcastRecordPlayShare();
#endif
    }

    public override void AnimateIn() {

        backgroundDisplayState = UIPanelBackgroundDisplayState.None;

        base.AnimateIn();

        AnimateStartCharacter();
        Invoke("AnimateInDelayed", 1);
    }

    public override void AnimateOut() {
        base.AnimateOut();

        CancelInvoke("AnimateInDelayed");

        HandleHide();
    }

    public virtual void AnimateInDelayed() {

        GameUIPanelFooter.ShowMain();

#if USE_GAME_LIB_GAMEVERSES
        GameCommunity.HideBroadcastRecordPlayShare();

        GameCommunity.ShowActionAppRate();
#endif
    }

    public virtual void AnimateStartCharacter() {

        // Toolkit parallel: same pingPong alpha pulse via the label-pulse motion token
        // (tokens.json). No-ops until the view is bound; the NGUI fade below is harmless once
        // the legacy container is suppressed.
        TweenUtil.FadeToObject(startObjectRef, .5f, "label-pulse");

        if(containerStartObject != null) {

            TweenUtil.FadeToObject(
                containerStartObject, .5f, 2f, 0f, true,
                TweenCoord.local,
                TweenEaseType.quadEaseInOut,
                TweenLoopType.pingPong);

            //UITweenerUtil.FadeTo(containerStartObject,
            //    UITweener.Method.EaseInOut, UITweener.Style.PingPong, 2f, 0f, .5f);
        }

        /*
        if(buttonPlayerGlowObject != null) {
            UITweenerUtil.FadeTo(buttonPlayerGlowObject.gameObject,
                UITweener.Method.EaseInOut, UITweener.Style.PingPong, 2f, 0f, .1f);
        }
  */
    }

    public override void OnButtonClickEventHandler(string buttonName) {

        bool loadCharacter = false;

        /*
        if (UIUtil.IsButtonClicked(buttonPlayerUCFObject, buttonName)) {

            GameProfileCustomItem customItem = GameProfileCharacters.currentCustom;
            
            // SET CUSTOM VALUES FOR THIS PLAYER
                        
            customItem = GameCustomController.UpdateTexturePresetObject(
                customItem, GameController.CurrentGamePlayerController.gameObject,  
                AppContentAssetTexturePresets.Instance.GetByCode("fiestabowl"));

            customItem = GameCustomController.UpdateColorPresetObject(
                customItem, GameController.CurrentGamePlayerController.gameObject,   
                AppColorPresets.Instance.GetByCode("game-college-ucf-knights"));
                        
            GameCustomController.SaveCustomItem(customItem); 

            loadCharacter = true;
        }
        else if (UIUtil.IsButtonClicked(buttonPlayerBUObject, buttonName)) {
                
            GameProfileCustomItem customItem = GameProfileCharacters.currentCustom;

            // SET CUSTOM VALUES FOR THIS PLAYER
            
            customItem = GameCustomController.UpdateTexturePresetObject(
                customItem, GameController.CurrentGamePlayerController.gameObject,  
                AppContentAssetTexturePresets.Instance.GetByCode("fiestabowl"));
            
            customItem = GameCustomController.UpdateColorPresetObject(
                customItem, GameController.CurrentGamePlayerController.gameObject,  
                AppColorPresets.Instance.GetByCode("game-college-baylor-bears"));

            GameCustomController.SaveCustomItem(customItem);
            
            loadCharacter = true;
        }
        else 
        */

        if(UIUtil.IsButtonClicked(buttonPlayerDefaultObject, buttonName)) {
            loadCharacter = true;
        }

        if(loadCharacter) {
            LogUtil.Log("Player Clicked: " + buttonName);

            GameController.LoadCurrentProfileCharacter();
#if ENABLE_FEATURE_GAME_MODE
            GameUIController.ShowGameMode();
#endif
        }
    }
}