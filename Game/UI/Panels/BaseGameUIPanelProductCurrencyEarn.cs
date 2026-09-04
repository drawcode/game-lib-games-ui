using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Engine.Game.App.BaseApp;


#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
#else
using UnityEngine.UI;
#endif

using Engine.Events;

#if ENABLE_FEATURE_PRODUCT_CURRENCY

public class BaseGameUIPanelProductCurrencyEarn : GameUIPanelBase {

    public static GameUIPanelProductCurrencyEarn Instance;

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
    public UIImageButton buttonHelp;
    public UIImageButton buttonEarnLogin;
    public UIImageButton buttonEarnWebsite;
    public UIImageButton buttonEarnTwitter;
    public UIImageButton buttonEarnFacebook;
    public UIImageButton buttonEarnVideoAds;
    public UIImageButton buttonEarnOffers;
    public UIImageButton buttonEarnMoreGames;
    public UIImageButton buttonEarnViewFullscreenAds;
#else
    // 2.11: agnostic UIRef handles, bound at runtime by name.
    public Engine.UI.UIRef buttonHelp;
    public Engine.UI.UIRef buttonEarnLogin;
    public Engine.UI.UIRef buttonEarnWebsite;
    public Engine.UI.UIRef buttonEarnTwitter;
    public Engine.UI.UIRef buttonEarnFacebook;
    public Engine.UI.UIRef buttonEarnVideoAds;
    public Engine.UI.UIRef buttonEarnOffers;
    public Engine.UI.UIRef buttonEarnMoreGames;
    public Engine.UI.UIRef buttonEarnViewFullscreenAds;
#endif

    // The HELP tile's element name. Unguarded on purpose (both define branches need it): the
    // serialized buttonHelp ref is UNASSIGNED on this prefab, so the name IS the contract.
    // See OnButtonClickEventHandler.
    public static string buttonNameSettingsHelp = "ButtonSettingsHelp";


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

        // 

        Messenger<double>.AddListener(AdNetworksMessages.videoAd, OnVideoAdWatched);
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

        // 

        Messenger<double>.RemoveListener(AdNetworksMessages.videoAd, OnVideoAdWatched);

        // Chain to base so UIPanelBase.OnDisable -> FreeToolkitView runs when this panel is pooled
        // away, else the toolkit view leaks once this panel gets a toolkitViewKey. Standing
        // Phase-3 migration prerequisite; latent until then.
        //
        // OnDisable ONLY: UIPanelBase.OnEnable re-adds EVENT_BUTTON_CLICK ->
        // OnButtonClickEventHandler, which this panel already subscribes itself, so chaining
        // OnEnable would fire every button click twice. RemoveListener is idempotent, so the
        // one-sided chain is safe.
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

    public virtual void OnVideoAdWatched(double amountWatched) {

        LogUtil.Log("OnVideoWatched:" + " amountWatched:" + amountWatched);

        if(amountWatched > .9) {
            GameProfileRPGs.Current.AddCurrency(100);
        }

    }

    public virtual void OnFacebookLike(string account) {

        LogUtil.Log("OnFacebookLike:" + " account:" + account);

        //if(url) {
        GameProfileRPGs.Current.AddCurrency(100);
        //}

    }

    public virtual void OnTwitterFollow(string account) {

        LogUtil.Log("OnTwitterFollow:" + " account:" + account);

        //if(url) {
        GameProfileRPGs.Current.AddCurrency(100);
        //}

    }

    public virtual void OnWebsiteViewed(string url) {

        LogUtil.Log("OnWebsiteViewed:" + " url:" + url);

        //if(url) {
        GameProfileRPGs.Current.AddCurrency(100);
        //}

    }

    public override void OnButtonClickEventHandler(string buttonName) {
        //LogUtil.Log("OnButtonClickEventHandler: " + buttonName);

        if(UIUtil.IsButtonClicked(buttonEarnVideoAds, buttonName)) {

            LogUtil.Log("buttonEarnVideoAds: " + buttonName);

            AdNetworks.ShowVideoAdIncentivized();
        }
        else if(UIUtil.IsButtonClicked(buttonEarnOffers, buttonName)) {

            LogUtil.Log("buttonEarnOffers: " + buttonName);

            AdNetworks.ShowOfferWall();
        }
        else if(UIUtil.IsButtonClicked(buttonEarnMoreGames, buttonName)) {

            LogUtil.Log("buttonEarnMoreGames: " + buttonName);

            AdNetworks.ShowMoreApps();
        }
        else if(UIUtil.IsButtonClicked(buttonEarnFacebook, buttonName)) {

            LogUtil.Log("buttonEarnFacebook: " + buttonName);

            OnFacebookLike("default");

            GameCommunity.LikeUrl(
                SocialNetworkTypes.facebook,
                Locos.Get(LocoKeys.app_web_url));
        }
        else if(UIUtil.IsButtonClicked(buttonEarnTwitter, buttonName)) {

            LogUtil.Log("buttonEarnTwitter: " + buttonName);

            OnTwitterFollow("default");

            Platforms.ShowWebView(
                Locos.Get(LocoKeys.app_display_name),
                Locos.Get(LocoKeys.app_web_url_twitter));
        }
        else if(UIUtil.IsButtonClicked(buttonEarnWebsite, buttonName)) {

            LogUtil.Log("buttonEarnWebsite: " + buttonName);

            OnWebsiteViewed("default");

            Platforms.ShowWebView(
                Locos.Get(LocoKeys.app_display_name),
                Locos.Get(LocoKeys.app_web_url));
        }
        else if(UIUtil.IsButtonClicked(buttonEarnViewFullscreenAds, buttonName)) {

            LogUtil.Log("buttonEarnViewFullscreenAds: " + buttonName);

            AdNetworks.ShowFullscreenAd();
        }

        // HELP. The ONLY listener for this name in the codebase is
        // BaseGameUIPanelSettings.buttonSettingsHelp, and panel-settings is loaded LAZILY by
        // syncPanelLoaded -- until the player has opened Settings once, that panel does not
        // exist and this tile's broadcast reaches nobody. Same shape as the STORE tile
        // (iter 18): never leave a toolkit element depending on a listener that lives on a
        // panel which may not be up. The serialized buttonHelp ref was already wired to this
        // tile and simply never checked.
        //
        // Safe when panel-settings IS loaded: showUIPanel early-returns on the current panel
        // code, so the second dispatch is a no-op rather than a double navigation.
#if ENABLE_FEATURE_SETTINGS_HELP
        // By NAME, not through buttonHelp: that serialized ref is UNASSIGNED on this prefab
        // (verified in play 2026-09-04 — panel-product-currency's is wired, this one's is null),
        // so IsButtonClicked could never match. The element name is the contract.
        else if(buttonName == buttonNameSettingsHelp) {
            GameUIController.ShowSettingsHelp();
        }
#endif
    }

    public static void LoadData() {
        if(GameUIPanelProductCurrencyEarn.Instance != null) {
            GameUIPanelProductCurrencyEarn.Instance.loadData();
        }
    }

    public virtual void loadData() {

        StartCoroutine(loadDataCo());
    }

    IEnumerator loadDataCo() {

        LogUtil.Log("LoadDataCo");

        yield return new WaitForEndOfFrame();

    }

    public override void HandleShow() {
        base.HandleShow();

        backgroundDisplayState = UIPanelBackgroundDisplayState.PanelBacker;
        adDisplayState = UIPanelAdDisplayState.BannerBottom;
    }

    public override void AnimateIn() {

        base.AnimateIn();

    }

    public override void AnimateOut() {

        base.AnimateOut();
    }
}
#endif