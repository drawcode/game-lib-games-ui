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

public class BaseGameUIPanelProductCurrency : GameUIPanelBase {

    public static GameUIPanelProductCurrency Instance;

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
    public UIImageButton buttonGameBuyProducts;

    public UIImageButton buttonGameBuyCurrency;
    public UIImageButton buttonGameBuyCurrencyFeature1;
    public UIImageButton buttonGameBuyCurrencyFeature2;

    public UIImageButton buttonGameBuyCurrencyTier1;
    public UIImageButton buttonGameBuyCurrencyTier2;
    public UIImageButton buttonGameBuyCurrencyTier3;
    public UIImageButton buttonGameBuyCurrencyTier5;
    public UIImageButton buttonGameBuyCurrencyTier10;
    public UIImageButton buttonGameBuyCurrencyTier20;
    public UIImageButton buttonGameBuyCurrencyTier50;

    public UIImageButton buttonGameEarnCurrency;
    public UIImageButton buttonGameBuyModifier;

    public UIImageButton buttonHelp;
    public UIImageButton buttonPlay;
#else
    // 2.11: agnostic UIRef handles, bound at runtime by name.
    public Engine.UI.UIRef buttonGameBuyProducts;

    public Engine.UI.UIRef buttonGameBuyCurrency;
    public Engine.UI.UIRef buttonGameBuyCurrencyFeature1;
    public Engine.UI.UIRef buttonGameBuyCurrencyFeature2;

    public Engine.UI.UIRef buttonGameBuyCurrencyTier1;
    public Engine.UI.UIRef buttonGameBuyCurrencyTier2;
    public Engine.UI.UIRef buttonGameBuyCurrencyTier3;
    public Engine.UI.UIRef buttonGameBuyCurrencyTier5;
    public Engine.UI.UIRef buttonGameBuyCurrencyTier10;
    public Engine.UI.UIRef buttonGameBuyCurrencyTier20;
    public Engine.UI.UIRef buttonGameBuyCurrencyTier50;

    public Engine.UI.UIRef buttonGameEarnCurrency;
    public Engine.UI.UIRef buttonGameBuyModifier;

    public Engine.UI.UIRef buttonHelp;
    public Engine.UI.UIRef buttonPlay;
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

        if(UIUtil.IsButtonClicked(buttonGameBuyProducts, buttonName)) {
            GameUIController.ShowProducts();
        }
        else if(UIUtil.IsButtonClicked(buttonGameBuyCurrency, buttonName)) {
            GameUIController.ShowProducts(GameProductType.currency);
        }
        else if(UIUtil.IsButtonClicked(buttonGameBuyCurrencyTier1, buttonName)) {
            // action_coin_pack_1
        }
        else if(UIUtil.IsButtonClicked(buttonGameBuyCurrencyTier2, buttonName)) {
            // action_coin_pack_2
        }
        else if(UIUtil.IsButtonClicked(buttonGameBuyCurrencyTier3, buttonName)) {
            // action_coin_pack_3
        }
        else if(UIUtil.IsButtonClicked(buttonGameBuyCurrencyTier5, buttonName)) {
            // action_coin_pack_5
        }
        else if(UIUtil.IsButtonClicked(buttonGameBuyCurrencyTier10, buttonName)) {
            // action_coin_pack_10
        }
        else if(UIUtil.IsButtonClicked(buttonGameBuyCurrencyTier20, buttonName)) {
            // action_coin_pack_20
        }
        else if(UIUtil.IsButtonClicked(buttonGameBuyCurrencyTier50, buttonName)) {
            // action_coin_pack_50
        }
        else if(UIUtil.IsButtonClicked(buttonGameBuyCurrencyFeature1, buttonName)) {

        }
        else if(UIUtil.IsButtonClicked(buttonGameBuyCurrencyFeature2, buttonName)) {

        }
        else if(UIUtil.IsButtonClicked(buttonGameEarnCurrency, buttonName)) {
            GameUIController.ShowProductCurrencyEarn();
        }
    }


    public static void LoadData() {
        if(GameUIPanelProductCurrency.Instance != null) {
            GameUIPanelProductCurrency.Instance.loadData();
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
    }

    public override void AnimateIn() {

        base.AnimateIn();
    }

    public override void AnimateOut() {

        base.AnimateOut();
    }
}
#endif