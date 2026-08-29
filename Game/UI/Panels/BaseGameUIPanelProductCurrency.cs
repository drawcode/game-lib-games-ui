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

    // ------------------------------------------------------------
    // 3D COIN STAGES
    //
    // Each green pack button owns a real spinning 3D UICoin in the prefab. World content can
    // never draw above a UI Toolkit panel, so — exactly as the header coin does — each one is
    // rendered by a UIRenderStage into its own RenderTexture and shown as the background of the
    // matching element in the view. The first pass stood in a flat gold disc instead; measured
    // against the baseline it was 38.8 ref units wide where legacy draws 69.8, i.e. flat AND
    // half-size, which is most of what read as "sizing off" on this screen.
    //
    // The default SuppressLegacyView hides the whole panelContainer, and the coins live inside it
    // (Content/Position/Buy/Row/<button>/UICoin), so Hide()'s SetActive(false) would take them
    // down with it.
    //
    // The first version lifted the coins OUT of the container with SetParent and put them back on
    // free. That is WRONG and Unity rejects it outright: this panel is pooled, so FreeToolkitView
    // runs from OnDisable inside SetActive(false), and "Cannot set the parent of the GameObject
    // 'UICoin' while activating or deactivating the parent GameObject". Reparenting in a
    // (de)activation callback is simply not allowed, and there is no safe later hook either —
    // OnEnable is under the same restriction, and Update does not run on a disabled panel.
    //
    // So no reparenting at all. Instead, hide every branch under panelContainer EXCEPT the paths
    // down to the coins — the header's pattern, generalised so it needs no hardcoded widget
    // names: mark each coin's ancestor chain, walk the tree, and hide any child that is not on a
    // chain. The hidden objects are recorded and re-shown on free, so the legacy path (kill
    // switch, or this panel re-shown on NGUI) gets its screen back whole.

    private static readonly string[] packCoinButtons = {
        "ButtonGameBuyCurrency",
        "ButtonActionItemBuyUse$action_coin_pack_1",
        "ButtonActionItemBuyUse$action_coin_pack_3",
    };

    // View elements that receive each stage's RenderTexture, in the same order.
    private static readonly string[] packCoinElements = {
        "CoinShowAllPacks",
        "CoinPack1",
        "CoinPack3",
    };

    private Engine.UI.UIRenderStage[] packCoinStages;
    private Transform[] packCoinRoots;
    private ParticleSystem[][] packCoinEffects;
    private float[][] packCoinEffectSizes;
    private List<GameObject> packCoinHidden;

    // NOT chaining to base: base hides panelContainer wholesale, which would take the coins with
    // it. HideAroundPackCoins does the same job at a finer grain.
    protected override void SuppressLegacyView() {

        SetupPackCoinStages();
    }

    protected virtual void SetupPackCoinStages() {

        if(packCoinStages != null || panelContainer == null) {
            return;
        }

        // Dedicated widget layer; UI3D as fallback (older project configs) — same choice as the
        // header coin and the character card.
        int layer = LayerMask.NameToLayer("UIWidget3D");

        if(layer < 0) {
            layer = LayerMask.NameToLayer("UI3D");
        }

        if(layer < 0) {
            return;
        }

        packCoinStages = new Engine.UI.UIRenderStage[packCoinButtons.Length];
        packCoinRoots = new Transform[packCoinButtons.Length];
        packCoinEffects = new ParticleSystem[packCoinButtons.Length][];
        packCoinEffectSizes = new float[packCoinButtons.Length][];

        for(int i = 0; i < packCoinButtons.Length; i++) {

            Transform button = FindDeepChild(panelContainer.transform, packCoinButtons[i]);

            if(button == null) {
                continue;
            }

            Transform uiCoin = button.Find("UICoin");

            if(uiCoin == null) {
                continue;
            }

            packCoinRoots[i] = uiCoin;

            // 1.3, the header coin's value. The engine's note suggests 1.6-1.8 for a widget whose
            // glow should spill, and 1.7 was tried — it rendered the coin AND the glow away
            // entirely (three empty RTs), so something in the wider framing breaks this rig.
            // Not chased further: 1.3 demonstrably renders both, and the glow is carried by the
            // start-size boost below instead. Revisit only with a capture to check against.
            packCoinStages[i] = Engine.UI.UIRenderStage.Attach(
                uiCoin.gameObject, layer, 128, 1.3f, false, false, 0.7f);

            if(packCoinStages[i] != null) {
                UIUtil.SetImageTexture(
                    UIUtil.ResolveDeep(viewRoot, packCoinElements[i]),
                    packCoinStages[i].texture);

                BoostPackCoinEffect(i, uiCoin, 1.8f);
            }
        }

        HideAroundPackCoins();
    }

    // Hide every branch under panelContainer except the ancestor chains down to the staged coins.
    // Recorded so FreePackCoinStages can put the legacy screen back.
    protected virtual void HideAroundPackCoins() {

        if(panelContainer == null) {
            return;
        }

        Transform root = panelContainer.transform;

        // The nodes that must stay active: each coin and every ancestor up to the container.
        HashSet<Transform> keep = new HashSet<Transform>();

        for(int i = 0; i < packCoinRoots.Length; i++) {

            Transform t = packCoinRoots[i];

            while(t != null) {
                keep.Add(t);

                if(t == root) {
                    break;
                }

                t = t.parent;
            }
        }

        packCoinHidden = new List<GameObject>();

        HideOffPath(root, keep);
    }

    private void HideOffPath(Transform node, HashSet<Transform> keep) {

        for(int i = 0; i < node.childCount; i++) {

            Transform child = node.GetChild(i);

            if(!keep.Contains(child)) {

                // An off-path branch: hide the whole thing, and remember it if it was showing.
                if(child.gameObject.activeSelf) {
                    packCoinHidden.Add(child.gameObject);
                    child.gameObject.Hide();
                }

                continue;
            }

            // On the path to a coin. The coin itself is a leaf for this walk — everything inside
            // it renders into the stage and must stay whole.
            bool isCoin = false;

            for(int j = 0; j < packCoinRoots.Length; j++) {
                if(packCoinRoots[j] == child) {
                    isCoin = true;
                    break;
                }
            }

            if(!isCoin) {
                HideOffPath(child, keep);
            }
        }
    }

    // Each UICoin carries a shine under UICoin/Effect (CFXM2_Blob). Tuned for the coin at its
    // full on-screen size in the legacy layout, it is close to invisible once the coin is framed
    // down into a 128px RT — the first staged pass lost the sparkle entirely, which is what
    // "what happened to the effect?" was. 1.8 — the header coin's value — is what visibly puts
    // the shine back at framePadding 1.3; 1.3 was tried and reads as no effect at all. Restored
    // on free: the multiplier lands on the SHARED prefab instance, so leaving it scaled would
    // leak a fattened glow into the legacy path.
    protected virtual void BoostPackCoinEffect(int index, Transform uiCoin, float factor) {

        Transform effect = uiCoin.Find("Effect");

        if(effect == null) {
            return;
        }

        ParticleSystem[] systems = effect.GetComponentsInChildren<ParticleSystem>(true);
        float[] sizes = new float[systems.Length];

        for(int i = 0; i < systems.Length; i++) {
            ParticleSystem.MainModule main = systems[i].main;
            sizes[i] = main.startSizeMultiplier;
            main.startSizeMultiplier = sizes[i] * factor;
        }

        packCoinEffects[index] = systems;
        packCoinEffectSizes[index] = sizes;
    }

    protected virtual void RestorePackCoinEffects() {

        if(packCoinEffects == null) {
            return;
        }

        for(int i = 0; i < packCoinEffects.Length; i++) {

            if(packCoinEffects[i] == null || packCoinEffectSizes[i] == null) {
                continue;
            }

            for(int j = 0; j < packCoinEffects[i].Length; j++) {

                if(packCoinEffects[i][j] == null) {
                    continue;
                }

                ParticleSystem.MainModule main = packCoinEffects[i][j].main;
                main.startSizeMultiplier = packCoinEffectSizes[i][j];
            }
        }

        packCoinEffects = null;
        packCoinEffectSizes = null;
    }

    protected override void FreeToolkitView() {

        FreePackCoinStages();

        base.FreeToolkitView();
    }

    protected virtual void FreePackCoinStages() {

        RestorePackCoinEffects();

        if(packCoinStages == null) {
            return;
        }

        for(int i = 0; i < packCoinStages.Length; i++) {

            if(packCoinStages[i] != null) {
                packCoinStages[i].Detach();       // restores the content's original layers
                packCoinStages[i] = null;
            }

            packCoinRoots[i] = null;
        }

        // Put the legacy screen back — Show() on a CHILD is fine inside the parent's OnDisable,
        // which is exactly what SetParent was not (see the note above SetupPackCoinStages).
        if(packCoinHidden != null) {

            for(int i = 0; i < packCoinHidden.Count; i++) {

                if(packCoinHidden[i] != null) {
                    packCoinHidden[i].Show();
                }
            }

            packCoinHidden = null;
        }

        packCoinStages = null;
        packCoinRoots = null;
    }

    // Name search over the whole subtree. The coins sit at
    // Center/Container/Content/Position/Buy/Row/<button>/UICoin, but hard-coding that path makes
    // the wiring break silently the first time the prefab's grouping changes, and a silent break
    // here is an invisible coin rather than an error.
    private static Transform FindDeepChild(Transform root, string name) {

        if(root == null) {
            return null;
        }

        foreach(Transform t in root.GetComponentsInChildren<Transform>(true)) {

            if(t.name == name) {
                return t;
            }
        }

        return null;
    }

    public override void AnimateIn() {

        base.AnimateIn();
    }

    public override void AnimateOut() {

        base.AnimateOut();
    }
}
#endif