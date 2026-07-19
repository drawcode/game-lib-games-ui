using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Engine.Events;
using Engine.UI;
using Engine.Utility;

public class GameUIPanelFooterButtons {
    public static string gameNetworks = "game-networks";
    public static string character = "character";
    public static string characterLarge = "character-large";
    public static string characterTools = "character-tools";
    public static string characterHelp = "character-help";
    public static string characterCustomize = "character-customize";
    public static string statistics = "statistics";
    public static string achievements = "achievements";
    public static string progression = "progression";
    public static string customize = "customize";
    public static string products = "products";
}

public class GameUIPanelFooterButtonsSections {
    public static string productSections = "product-sections";
}

public class BaseGameUIPanelFooter : GameUIPanelBase {

    public static GameUIPanelFooter Instance;

    public GameObject containerButtons;

    public GameObject containerButtonsSettings;
    public GameObject containerButtonsCustomize;

    public GameObject containerButtonsCharacter;
    public GameObject containerButtonsCharacterLarge;
    public GameObject containerButtonsCharacterTools;

    public GameObject containerButtonsGameNetworks;
    public GameObject containerButtonsProgression;
    public GameObject containerButtonsCharacterHelp;
    public GameObject containerButtonsCharacterCustomize;

    public GameObject containerButtonsProducts;

    public GameObject containerButtonsProgressionAchievements;
    public GameObject containerButtonsProgressionStatistics;

    public bool optionsVisible = false;

    // Toolkit parallels (3B): bound by BindElements from the panel-footer manifest. The corner
    // containers slide in/out through the bottom edge, like their NGUI AnimateIn*Bottom moves.
    //
    // Center groups and subgroups are NOT fields: their ShowButtons / GameObjectInactive code
    // strings double as element names in panel-footer.uxml, so the wiring resolves them straight
    // from the code it is already handed — same dispatch the NGUI path does with
    // GameObjectShowItem.code, one lookup instead of a dozen binds.
    public UIRef settingsObjectRef;
    public UIRef customizeObjectRef;

    // Replayed onto the view when it arrives (the load is async — the first showMain/ShowButtons
    // of a session runs while the panel is still NGUI-only).
    protected bool toolkitCornersVisible = false;
    protected string currentNetworkSub = "";
    protected string currentProductSub = "";

    static readonly string[] toolkitGroupCodes = {
        GameUIPanelFooterButtons.customize,
        GameUIPanelFooterButtons.products,
        GameUIPanelFooterButtons.progression,
        GameUIPanelFooterButtons.gameNetworks,
        GameUIPanelFooterButtons.character,
        GameUIPanelFooterButtons.statistics,
        GameUIPanelFooterButtons.achievements
    };

    static readonly string[] toolkitProductSubs = {
        "product-earn",
        "product-store",
        GameUIPanelFooterButtonsSections.productSections
    };

    static readonly string[] toolkitNetworkSubs = {
        GameNetworkType.gameNetworkAppleGameCenter,
        GameNetworkType.gameNetworkGooglePlayServices
    };

    // Chrome motion (same variance as the header band).
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

    // Bottom chrome: the whole view slides through the BOTTOM edge, its own screen edge — not the
    // top like flow panels and the header.
    protected override void ShowToolkitViewSlide() {
        TweenUtil.ShowObjectBottom(viewRoot, toolkitShowPreset);
    }

    protected override void HideToolkitViewSlide() {
        TweenUtil.HideObjectBottom(viewRoot, toolkitHidePreset);
    }

    protected virtual UIRef ResolveToolkitElement(string elementName) {

        if(!isToolkitPanel) {
            return UIRef.none;
        }

        IUIBackend backend = UIPlatform.For(viewRoot);

        if(backend == null) {
            return UIRef.none;
        }

        return backend.Resolve(viewRoot, elementName);
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

    public override void Start() {
        Init();
    }

    public override void Init() {
        base.Init();
        loadData();

        AnimateIn();
    }

    public override void AnimateIn() {

        base.AnimateIn();

        showNone();
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

        showNone();
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

        // Chain to base so UIPanelBase.OnDisable -> FreeToolkitView runs when the footer is
        // pooled away / disabled — same 3B prerequisite fix the header got.
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

    public override void OnButtonClickEventHandler(string buttonName) {
        //LogUtil.Log("OnButtonClickEventHandler: " + buttonName);

    }

    public virtual void HideContainerDelayed() {
        StartCoroutine(HideContainerDelayedCo());
    }

    IEnumerator HideContainerDelayedCo() {
        yield return new WaitForSeconds(2f);
        //HideOptionsContainer();
    }

    public static void ShowFull() {
        if(isInst) {
            Instance.showFull();
        }
    }

    public virtual void showFull() {
        showMain();
    }

    public static void ShowMain() {
        if(isInst) {
            Instance.showMain();
        }
    }

    public virtual void showMain() {
        showButtonSettings();
        showButtonCustomize();
    }

    public virtual void showNone() {

        hideButtonSettings();
        hideButtonCustomize();

        HideAllButtons();
    }

    //

    protected string currentButtonCode = "";

    public virtual void ShowButtons(string code, bool hideCurrent = true) {

        AnimateIn();

        currentButtonCode = code;

        ShowButtonsToolkit(code, hideCurrent);

        if(containerButtons == null) {
            return;
        }

        foreach(GameObjectShowItem item in
                containerButtons.GetComponentsInChildren<GameObjectShowItem>(true)) {

            if(item.code == code) {
                if(hideCurrent) {
                    HideAllButtonsLegacy();
                }
                TweenUtil.ShowObjectBottom(item.gameObject);
                item.gameObject.ShowObjectDelayed(.7f);
            }
        }
    }

    public virtual void ShowButtonsCurrent() {
        ShowButtons(currentButtonCode, false);
    }

    public virtual void HideAllButtons() {

        HideAllButtonsToolkit();

        HideAllButtonsLegacy();
    }

    // The original NGUI hide-all, unchanged. Kept separate so ShowButtons' hideCurrent pass can't
    // hide the toolkit group it is about to show.
    protected virtual void HideAllButtonsLegacy() {

        if(containerButtons == null) {
            return;
        }

        foreach(GameObjectShowItem item in
                containerButtons.GetComponentsInChildren<GameObjectShowItem>(true)) {
            TweenUtil.HideObjectBottom(item.gameObject);
            item.gameObject.HideObjectDelayed(.5f);
        }
    }

    // ------------------------------------------------------
    // TOOLKIT GROUPS (3B)

    protected virtual void ShowButtonsToolkit(string code, bool hideCurrent) {

        if(!isToolkitPanel) {
            return;
        }

        if(hideCurrent) {
            HideAllButtonsToolkit();
        }

        UIRef group = ResolveToolkitElement(code);

        if(group == null || !group.alive) {
            return;
        }

        TweenUtil.ShowObjectBottom(group);
    }

    protected virtual void HideAllButtonsToolkit() {

        if(!isToolkitPanel) {
            return;
        }

        for(int i = 0; i < toolkitGroupCodes.Length; i++) {

            UIRef group = ResolveToolkitElement(toolkitGroupCodes[i]);

            if(group != null && group.alive) {
                TweenUtil.HideObjectBottom(group);
            }
        }
    }

    // Subgroup display toggle — the toolkit twin of the GameObjectInactive walks below. An empty
    // selection hides every subgroup.
    protected virtual void ApplyToolkitSubgroup(string[] subNames, string selected) {

        if(!isToolkitPanel) {
            return;
        }

        IUIBackend backend = UIPlatform.For(viewRoot);

        if(backend == null) {
            return;
        }

        for(int i = 0; i < subNames.Length; i++) {

            UIRef sub = backend.Resolve(viewRoot, subNames[i]);

            if(sub == null || !sub.alive) {
                continue;
            }

            if(subNames[i] == selected) {
                backend.Show(sub);
            }
            else {
                backend.Hide(sub);
            }
        }
    }

    // Footer is entirely flat UGUI, so the default whole-container suppression is right; the
    // override exists to REPLAY the footer's current state onto the freshly arrived view (the
    // load is async — whatever showMain/ShowButtons ran first happened while still NGUI-only).
    protected override void SuppressLegacyView() {

        base.SuppressLegacyView();

        ApplyToolkitState();
    }

    protected virtual void ApplyToolkitState() {

        if(toolkitCornersVisible) {
            TweenUtil.ShowObjectBottom(settingsObjectRef, toolkitShowPreset);
            TweenUtil.ShowObjectBottom(customizeObjectRef, toolkitShowPreset);
        }

        if(string.IsNullOrEmpty(currentButtonCode)) {
            return;
        }

        ShowButtonsToolkit(currentButtonCode, false);

        if(currentButtonCode == GameUIPanelFooterButtons.gameNetworks
            && !string.IsNullOrEmpty(currentNetworkSub)) {
            ApplyToolkitSubgroup(toolkitNetworkSubs, currentNetworkSub);
        }

        if(currentButtonCode == GameUIPanelFooterButtons.products
            && !string.IsNullOrEmpty(currentProductSub)) {
            ApplyToolkitSubgroup(toolkitProductSubs, currentProductSub);
        }
    }

    protected override void FreeToolkitView() {

        settingsObjectRef = UIRef.none;
        customizeObjectRef = UIRef.none;

        base.FreeToolkitView();
    }

    public virtual void HideAndShowButtonsWithDelay(float delay) {

        StartCoroutine(HideAndShowButtonsWithDelayCo(delay));
    }

    IEnumerator HideAndShowButtonsWithDelayCo(float delay) {

        HideAllButtons();

        yield return new WaitForSeconds(delay);

        ShowButtonsCurrent();
    }

    // HIDE/SHOW

    // ------------------------------------------------------
    // BUTTONS SETTINGS

    public virtual void showButtonSettings() {

        toolkitCornersVisible = true;

        TweenUtil.ShowObjectBottom(settingsObjectRef, toolkitShowPreset);

        if(isToolkitPanel) {
            return;
        }

        AnimateInLeftBottom(containerButtonsSettings);
    }

    public virtual void hideButtonSettings() {

        toolkitCornersVisible = false;

        TweenUtil.HideObjectBottom(settingsObjectRef, toolkitHidePreset);

        if(isToolkitPanel) {
            return;
        }

        AnimateOutLeftBottom(containerButtonsSettings);
    }

    // ------------------------------------------------------
    // BUTTONS CUSTOMIZE

    public virtual void showButtonCustomize() {

        toolkitCornersVisible = true;

        TweenUtil.ShowObjectBottom(customizeObjectRef, toolkitShowPreset);

        if(isToolkitPanel) {
            return;
        }

        AnimateInRightBottom(containerButtonsCustomize);
    }

    public virtual void hideButtonCustomize() {

        toolkitCornersVisible = false;

        TweenUtil.HideObjectBottom(customizeObjectRef, toolkitHidePreset);

        if(isToolkitPanel) {
            return;
        }

        AnimateOutRightBottom(containerButtonsCustomize);
    }

    // ------------------------------------------------------
    // BUTTONS GAME NETWORKS

    public static void ShowButtonGameNetworks() {
        if(isInst) {
            Instance.showButtonGameNetworks();
        }
    }

    public virtual void showButtonGameNetworks() {

        AnimateIn();

        if(GameNetworks.gameNetworkiOSAppleGameCenterEnabled) {

            AnimateInRightBottom(containerButtonsGameNetworks);
            showButtonGameNetworkGameCenter();
        }
        else if(GameNetworks.gameNetworkAndroidGooglePlayEnabled) {

            AnimateInRightBottom(containerButtonsGameNetworks);
            showButtonGameNetworkPlayServices();
        }
        else {
            ShowButtonsProgression();
        }
    }

    public virtual void hideButtonGameNetworks() {

        AnimateOutRightBottom(containerButtonsGameNetworks);
    }

    // BUTTON GAME NETWORKS

    public virtual void showButtonGameNetworkGameCenter() {
        ShowButtonsGameNetwork(GameNetworkType.gameNetworkAppleGameCenter);
    }

    public virtual void showButtonGameNetworkPlayServices() {
        ShowButtonsGameNetwork(GameNetworkType.gameNetworkGooglePlayServices);
    }


    // BUTTONS

    public virtual void ShowButtonsGameNetwork() {
        ShowButtons(GameUIPanelFooterButtons.gameNetworks);
    }

    public virtual void ShowButtonsGameNetwork(string networkTypeTo) {

        currentNetworkSub = networkTypeTo;

        ShowButtonsGameNetwork();

        ApplyToolkitSubgroup(toolkitNetworkSubs, networkTypeTo);

        if(containerButtonsGameNetworks != null) {

            foreach(GameObjectInactive item in
                    containerButtonsGameNetworks.GetComponentsInChildren<GameObjectInactive>(true)) {
                if(item.name == networkTypeTo || item.code == networkTypeTo) {
                    item.gameObject.Show();
                }
                else {
                    item.gameObject.Hide();
                }
            }
        }
    }

    public virtual void HideButtonsGameNetworks() {

        currentNetworkSub = "";

        ApplyToolkitSubgroup(toolkitNetworkSubs, "");

        if(containerButtonsGameNetworks != null) {
            foreach(GameObjectInactive item in
                    containerButtonsGameNetworks.GetComponentsInChildren<GameObjectInactive>(true)) {
                item.gameObject.Hide();
            }
        }
    }

    // ------------------------------------------------------
    // BUTTONS CHARACTERS

    public static void ShowButtonsCharacterCustomize() {
        if(isInst) {
            Instance.showButtonsCharacterCustomize();
        }
    }

    public virtual void showButtonsCharacterCustomize() {
        ShowButtons(GameUIPanelFooterButtons.characterCustomize);
    }

    //

    public static void ShowButtonsCharacter() {
        if(isInst) {
            Instance.showButtonsCharacter();
        }
    }

    public virtual void showButtonsCharacter() {
        ShowButtons(GameUIPanelFooterButtons.character);
    }

    //

    public static void ShowButtonsCharacterLarge() {
        if(isInst) {
            Instance.showButtonsCharacterLarge();
        }
    }

    public virtual void showButtonsCharacterLarge() {
        ShowButtons(GameUIPanelFooterButtons.characterLarge);
    }

    //

    public static void ShowButtonsCharacterTools() {
        if(isInst) {
            Instance.showButtonsCharacterTools();
        }
    }

    public virtual void showButtonsCharacterTools() {
        ShowButtons(GameUIPanelFooterButtons.characterTools);
    }

    //

    public static void ShowButtonsCharacterHelp() {
        if(isInst) {
            Instance.showButtonsCharacterHelp();
        }
    }

    public virtual void showButtonsCharacterHelp() {
        ShowButtons(GameUIPanelFooterButtons.characterHelp);
    }

    // ------------------------------------------------------
    // BUTTONS PROGRESSION

    public static void ShowButtonsProgression() {
        if(isInst) {
            Instance.showButtonsProgression();
        }
    }

    public virtual void showButtonsProgression() {
        ShowButtons(GameUIPanelFooterButtons.progression);
    }

    public static void ShowButtonsStatistics() {
        if(isInst) {
            Instance.showButtonsStatistics();
        }
    }

    public virtual void showButtonsStatistics() {
        ShowButtons(GameUIPanelFooterButtons.statistics);
    }

    public static void ShowButtonsAchievements() {
        if(isInst) {
            Instance.showButtonsAchievements();
        }
    }

    public virtual void showButtonsAchievements() {
        ShowButtons(GameUIPanelFooterButtons.achievements);
    }

    // ------------------------------------------------------
    // BUTTONS PRODUCTS

    public static void ShowButtonProducts() {
        if(isInst) {
            Instance.showButtonProducts();
        }
    }

    public virtual void showButtonProducts() {

        AnimateIn();

        // Just show products sections for now
        ShowButtonsProducts();
    }

    public virtual void hideButtonProducts() {

        AnimateOutRightBottom(containerButtonsProducts);
    }

    public static void ShowButtonsProductsSections() {
        if(isInst) {
            Instance.showButtonsProductsSections();
        }
    }

    public virtual void showButtonsProductsSections() {
        ShowButtonsProducts(GameUIPanelFooterButtonsSections.productSections);
    }

    // BUTTONS

    public virtual void ShowButtonsProducts() {
        ShowButtons(GameUIPanelFooterButtons.products);
    }

    public virtual void ShowButtonsProducts(string productTypeTo) {

        currentProductSub = productTypeTo;

        ShowButtonsProducts();

        ApplyToolkitSubgroup(toolkitProductSubs, productTypeTo);

        if(containerButtonsProducts != null) {

            foreach(GameObjectInactive item in
                    containerButtonsProducts.GetComponentsInChildren<GameObjectInactive>(true)) {
                if(item.name == productTypeTo || item.code == productTypeTo) {
                    item.gameObject.Show();
                }
                else {
                    item.gameObject.Hide();
                }
            }
        }
    }

    public virtual void HideButtonsProducts() {

        currentProductSub = "";

        ApplyToolkitSubgroup(toolkitProductSubs, "");

        if(containerButtonsProducts != null) {
            foreach(GameObjectInactive item in
                    containerButtonsProducts.GetComponentsInChildren<GameObjectInactive>(true)) {
                item.gameObject.Hide();
            }
        }
    }

    //

    public virtual void loadData() {
        StartCoroutine(loadDataCo());
    }

    IEnumerator loadDataCo() {

        yield return new WaitForSeconds(1f);
    }

    /*
    public static void HideOptionsContainer() {
        if(Instance != null) {
            //Instance.hideOptionsContainer();
        }
    }
    
    public void hideOptionsContainer() {
        
        optionsVisible = false;
        
        if(containerOptionsExtra != null) {
            UITweenerUtil.FadeTo(containerOptionsExtra, 
                UITweener.Method.EaseInOut, UITweener.Style.Once, .5f, .2f, 0f);
        }
        
        if(buttonOptionsSocial != null) {
            UITweenerUtil.FadeTo(buttonOptionsSocial.gameObject, 
                UITweener.Method.EaseInOut, UITweener.Style.Once, .5f, .2f, 0f);
        }       
        
        if(buttonOptionsRate != null) {
            UITweenerUtil.FadeTo(buttonOptionsRate.gameObject, 
                UITweener.Method.EaseInOut, UITweener.Style.Once, .5f, .2f, 0f);
        }
        
        if(buttonOptionsCredits != null) {
            UITweenerUtil.FadeTo(buttonOptionsCredits.gameObject, 
                UITweener.Method.EaseInOut, UITweener.Style.Once, .5f, .3f, 0f);
        }
        
        if(buttonOptionsAudio != null) {
            UITweenerUtil.FadeTo(buttonOptionsAudio.gameObject, 
                UITweener.Method.EaseInOut, UITweener.Style.Once, .5f, .3f, 0f);
        }
        
        if(containerOptionsExtraBackground != null) {
            UITweenerUtil.FadeTo(containerOptionsExtraBackground, 
                UITweener.Method.EaseInOut, UITweener.Style.Once, .5f, .5f, 0f);
        }
    }
    
    
    public static void ShowOptionsContainer() {
        if(Instance != null) {
            Instance.showOptionsContainer();
        }
    }
    
    public void showOptionsContainer() {
        
        //if(!optionsVisible) {
        optionsVisible = true;
        
        if(containerOptionsExtra != null) {
            UITweenerUtil.FadeTo(containerOptionsExtra, 
                UITweener.Method.EaseInOut, UITweener.Style.Once, .5f, .3f, 1f);
        }
        
        if(buttonOptionsSocial != null) {
            UITweenerUtil.FadeTo(buttonOptionsSocial.gameObject, 
                UITweener.Method.EaseInOut, UITweener.Style.Once, .5f, .7f, 1f);
        }       
        
        if(buttonOptionsRate != null) {
            UITweenerUtil.FadeTo(buttonOptionsRate.gameObject, 
                UITweener.Method.EaseInOut, UITweener.Style.Once, .5f, .6f, 1f);
        }
        
        if(buttonOptionsCredits != null) {
            UITweenerUtil.FadeTo(buttonOptionsCredits.gameObject, 
                UITweener.Method.EaseInOut, UITweener.Style.Once, .5f, .5f, 1f);
        }
        
        if(buttonOptionsAudio != null) {
            UITweenerUtil.FadeTo(buttonOptionsAudio.gameObject, 
                UITweener.Method.EaseInOut, UITweener.Style.Once, .5f, .4f, 1f);
        }
        
        if(containerOptionsExtraBackground != null) {
            UITweenerUtil.FadeTo(containerOptionsExtraBackground, 
                UITweener.Method.EaseInOut, UITweener.Style.Once, .5f, .3f, .5f);
        }       
        //}
    }
 */
}