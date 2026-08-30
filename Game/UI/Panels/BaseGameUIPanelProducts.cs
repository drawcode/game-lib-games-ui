using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Engine.Events;
using UnityEngine.UI;
using Engine.Game.App.BaseApp;

public class BaseGameUIPanelProducts : GameUIPanelBase {

    public static GameUIPanelProducts Instance;

    public GameObject listItemItemPrefab;

    // ONE staged 3D coin, shared by every row. Legacy instantiates an independent spinning UICoin
    // per row, but they are the same prefab at the same phase, so N stages would buy N cameras and
    // N RenderTextures for an identical image. The donor below is a lone UICoin instantiated out
    // of listItemItemPrefab — the legacy rows themselves are never built on the toolkit path.
    protected Engine.UI.UIRenderStage productCoinStage;
    protected GameObject productCoinDonor;
    protected ParticleSystem[] productCoinEffects;
    protected float[] productCoinEffectSizes;

    public string currentProductType;
    public string productCodeUse = "";
    public string productTypeUse = "";
    public string productCharacterUse = "";

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

    }

    /*
 public static void ChangeList(BaseGameUIPanelStoreListType listType) {
     if(isInst) {
         Instance.changeList(listType);
     }
 }
 
 public void changeList(BaseGameUIPanelStoreListType listType) {
     panelListType = listType;
     loadData();
     AnimateInList();
 }
 */

    public static void LoadData() {
        if(GameUIPanelProducts.Instance != null) {
            GameUIPanelProducts.Instance.loadData();
        }
    }

    public virtual void loadData() {
        loadData(currentProductType);
    }

    public static void LoadData(string productType) {
        if(GameUIPanelProducts.Instance != null) {
            GameUIPanelProducts.Instance.loadData(productType);
        }
    }

    public virtual void loadData(string productType) {

        //if(currentProductType == productType
        //   && !string.IsNullOrEmpty(currentProductType)) {
        //    return;
        //}

        StartCoroutine(loadDataCo(productType));
    }

    //bool loading = false;
    string lastProductType = "";

    IEnumerator loadDataCo(string productType) {

        //if (loading) {
        //    yield break;
        //}

        //loading = true;

        LogUtil.Log("LoadDataCo");

        currentProductType = productType;

        // Toolkit: rows are rebuilt from the view's own template, so the legacy grid below is
        // skipped entirely. AnimateIn calls LoadData, and the view arrives ASYNC a frame or two
        // after the first show — wait for it rather than populating a list that does not exist
        // yet. Same wait the missions list uses.
        if(!string.IsNullOrEmpty(toolkitViewKey)) {

            for(int waitFrames = 0; waitFrames < 60 && !isToolkitPanel; waitFrames++) {
                yield return null;
            }

            if(isToolkitPanel) {

                loadDataProductsToolkit(productType);

                // A filtered list that keeps its old offset reads as EMPTY — go from ALL (30 rows)
                // scrolled down to UPGRADES (3) and all three sit above the viewport. Same reason
                // the legacy path calls RepositionListScroll(0) below, and only on a type CHANGE
                // so re-entering the same filter keeps the player's place.
                if(lastProductType != currentProductType) {

                    lastProductType = currentProductType;

                    UIUtil.ScrollToTop(UIUtil.ResolveDeep(viewRoot, "ProductList"));
                }

                yield break;
            }
        }

        if(listGridRoot != null) {
            //listGridRoot.DestroyChildren();
            ClearList();

            yield return new WaitForSeconds(1f);

            yield return new WaitForEndOfFrame();

            loadDataProducts(productType);

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
            yield return new WaitForEndOfFrame();
            listGridRoot.GetComponent<UIGrid>().Reposition();
#endif
            yield return new WaitForEndOfFrame();
        }

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
        listGridRoot.GetComponent<UIGrid>().Reposition();
#endif

        // Reposition scroll for items with less products
        // but keep in place in case user is buying many of one thing 
        // i.e. upgrades etc.

        if(lastProductType != currentProductType) {
            lastProductType = currentProductType;

            RepositionListScroll(0);
        }

        //loading = false;
    }

    public virtual void loadDataRPGUpgrades() {
        loadData(GameProductType.rpgUpgrade);
    }

    public virtual void loadDataPowerups() {
        loadData(GameProductType.powerup);
    }

    public virtual void loadDataProducts(string type) {

        loadDataProductsItems(type);
    }

    public virtual void loadDataProductsItems(string type) {

        LogUtil.Log("Load loadDataProducts:type:" + type);

        List<GameProduct> products = null;

        if(!string.IsNullOrEmpty(type)) {
            products = GameProducts.Instance.GetListByType(type);
        }
        else {
            products = GameProducts.Instance.GetAll();
        }

        LogUtil.Log("Load products: products.Count: " + products.Count);

        int i = 0;

        foreach(GameProduct product in products) {

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
            GameObject item = NGUITools.AddChild(listGridRoot, listItemItemPrefab);
#else
            GameObject item = GameObjectHelper.CreateGameObject(
                listItemItemPrefab, Vector3.zero, Quaternion.identity, false);
            // NGUITools.AddChild(listGridRoot, listItemPrefab);
            item.transform.parent = listGridRoot.transform;
            item.ResetLocalPosition();
#endif

            item.name = "WeaponItem" + i;

            GameProductInfo info = product.GetDefaultProductInfoByLocale();

            UIUtil.UpdateLabelObject(item.transform, "LabelName", info.display_name);
            UIUtil.UpdateLabelObject(item.transform, "LabelDescription", info.description);
            UIUtil.UpdateLabelObject(item.transform, "LabelCost", info.cost);

            Transform inventoryItem = item.transform.Find("Container/Inventory");

            if(inventoryItem != null) {

                double currentValue = 0;

                if(product.type == GameProductType.rpgUpgrade) {

                    currentValue = GameProfileRPGs.Current.GetUpgrades();

                    UIUtil.UpdateLabelObject(
                        inventoryItem, "LabelCurrentValue", currentValue.ToString("N0"));
                }
                else {
                    inventoryItem.gameObject.Hide();
                }
            }

            Transform iconTransform = item.transform.Find("Container/Icon");

            if(iconTransform != null) {

                GameObject iconObject = iconTransform.gameObject;

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
                UISprite iconSprite = iconObject.GetComponent<UISprite>();
#else
                GameObject iconSprite = null;

                if(iconObject.Has<SpriteRenderer>()) {
                    iconSprite = iconObject.Get<SpriteRenderer>().gameObject;
                }
#endif

                if(iconSprite != null) {

                    SpriteUtil.SetColorAlpha(iconSprite.gameObject, 1f);

                    // TODO change out image...
                }
            }

            // Update button action

            Transform buttonObject = item.transform.Find("Container/Button/ButtonAction");

            if(buttonObject != null) {

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
                UIImageButton button = buttonObject.gameObject.GetComponent<UIImageButton>();
#else
                Button button = buttonObject.gameObject.Get<Button>();
#endif

                if(button != null) {

                    // TODO change to get from character skin
                    string productType = product.type;
                    string productCode = product.code;
                    string productCharacter =
                        GameProfileCharacters.Current.GetCurrentCharacterProfileCode();

                    //productCode = productCode.Replace(productType + "-", "");

                    button.name = BaseUIButtonNames.buttonGameActionItemBuyUse +
                        "$" + productType + "$" + productCode + "$" + productCharacter;
                }
            }

            i++;
        }
    }

    // The toolkit twin of loadDataProductsItems: one row per product, rebuilt from
    // ProductItemTemplate. Deliberately mirrors the legacy method's field-for-field writes —
    // name / description / cost, the rpgUpgrade-only inventory line, and the $-encoded button
    // rename — so the two paths cannot drift.
    //
    // Rows carry no per-row component and no GameObject, so the button's NAME is the only place
    // the buy payload can live. That is already how legacy does it, which is the whole reason this
    // panel was cheap to convert while panel-customize-character-rpg was not.
    public virtual void loadDataProductsToolkit(string type) {

        UIUtil.ClearListItems(viewRoot, "ProductList");

        List<GameProduct> products = null;

        if(!string.IsNullOrEmpty(type)) {
            products = GameProducts.Instance.GetListByType(type);
        }
        else {
            products = GameProducts.Instance.GetAll();
        }

        LogUtil.Log("Load loadDataProductsToolkit:type:" + type
            + " products.Count:" + products.Count);

        string characterCode =
            GameProfileCharacters.Current.GetCurrentCharacterProfileCode();

        int i = 0;

        foreach(GameProduct product in products) {

            Engine.UI.UIRef item = UIUtil.AddListItem(
                viewRoot, "ProductList", "ProductItemTemplate", "ProductItem" + i);

            GameProductInfo info = product.GetDefaultProductInfoByLocale();

            UIUtil.UpdateLabelObject(item, "LabelName", info.display_name);
            UIUtil.UpdateLabelObject(item, "LabelDescription", info.description);
            UIUtil.UpdateLabelObject(item, "LabelCost", info.cost);

            // Inventory line is rpgUpgrade-only in legacy; every other product hides it.
            Engine.UI.UIRef inventory = UIUtil.ResolveDeep(item, "Inventory");

            if(product.type == GameProductType.rpgUpgrade) {

                UIUtil.ShowObject(inventory);

                UIUtil.UpdateLabelObject(
                    item,
                    "LabelCurrentValue",
                    GameProfileRPGs.Current.GetUpgrades().ToString("N0"));
            }
            else {
                UIUtil.HideObject(inventory);
            }

            // One staged coin shared by every row — see SetupProductCoinStage.
            if(productCoinStage != null) {

                UIUtil.SetImageTexture(
                    UIUtil.ResolveDeep(item, "Coin"), productCoinStage.texture);
            }

            UIUtil.SetElementName(
                UIUtil.ResolveDeep(item, "ButtonAction"),
                BaseUIButtonNames.buttonGameActionItemBuyUse
                    + "$" + product.type
                    + "$" + product.code
                    + "$" + characterCode);

            i++;
        }
    }

    public virtual void ClearList() {

        if(isToolkitPanel) {
            UIUtil.ClearListItems(viewRoot, "ProductList");
        }

        if(listGridRoot != null) {
            listGridRoot.DestroyChildren();
        }
    }

    // Chain to base — hiding panelContainer wholesale is right here, unlike on the COINS screen:
    // the donor coin is parented to THIS panel, not to the container, so it is not swept up.
    protected override void SuppressLegacyView() {

        base.SuppressLegacyView();

        SetupProductCoinStage();
    }

    // Settings carried over from the COINS pack coins, which were tuned against a capture:
    // framePadding 1.3 (1.7 renders the coin AND its glow away entirely), exposure 0.7 (the 1.1
    // default clips 73% of the coin's pixels at green=255), particle start size x1.8 (1.3 reads
    // as no effect at all in a small RT).
    //
    // followContent TRUE: UIRenderStage frames its camera ONCE at Attach time, and an RT that
    // came back fully transparent because the camera was left behind is the single most expensive
    // bug this migration has hit. Only pinned chrome is safe with false.
    protected virtual void SetupProductCoinStage() {

        if(productCoinStage != null || listItemItemPrefab == null) {
            return;
        }

        int layer = LayerMask.NameToLayer("UIWidget3D");

        if(layer < 0) {
            layer = LayerMask.NameToLayer("UI3D");
        }

        if(layer < 0) {
            return;
        }

        Transform donor = FindDeepChild(listItemItemPrefab.transform, "UICoin");

        if(donor == null) {
            return;
        }

        // Instantiate the coin's PARENT ("Coin", the node that carries the x40), not the UICoin
        // on its own, and then stage the UICoin inside it. That reproduces the prefab's scale
        // chain exactly — which is the arrangement the COINS pack coins are staged in, and the
        // one those settings were tuned against.
        //
        // Instantiating the UICoin alone was tried first and is wrong: it drops the parent's x40
        // while the coin's particle effects keep their world-space size, so the stage camera
        // framed a cloud of full-size particles around a 40x-too-small coin. Measured 3.1% of the
        // RT non-transparent against 43% for a correctly framed one. Collapsing the chain into a
        // single localScale instead is NOT the fix — same numbers, but it also puts a x800 on z,
        // and the bounds that produces are not what the stage was tuned for.
        Transform donorRoot = donor.parent != null ? donor.parent : donor;

        productCoinDonor = GameObjectHelper.CreateGameObject(
            donorRoot.gameObject, Vector3.zero, Quaternion.identity, false);

        if(productCoinDonor == null) {
            return;
        }

        productCoinDonor.name = "ProductCoinDonor";
        productCoinDonor.transform.SetParent(transform, false);
        productCoinDonor.transform.localScale = donorRoot.localScale;

        Transform staged = FindDeepChild(productCoinDonor.transform, "UICoin");

        if(staged == null) {
            staged = productCoinDonor.transform;
        }

        // lightIntensity 0 = borrow the layer's existing stage light, do NOT add another.
        // Stage lights are directional with cullingMask = the whole layer, so they do not isolate
        // the way the stage CAMERA does — a second light here lands on the always-on header coin
        // as well and over-exposes it (measured: 1.1 + 0.7 = 1.8, and a shaded gold coin renders
        // flat yellow). The header is chrome and is always present on this screen, so its 1.1 is
        // exactly the exposure a lone coin wants.
        productCoinStage = Engine.UI.UIRenderStage.Attach(
            staged.gameObject, layer, 128, 1.3f, false, true, 0f);

        if(productCoinStage != null) {
            BoostProductCoinEffect(1.8f);
        }
    }

    // A small RT shrinks the glow to nothing unless its particles are scaled up with it. The
    // multiplier lands on the instantiated donor, but restore anyway — the sizes are read back on
    // free so a changed prefab default cannot bake in a compounding boost.
    protected virtual void BoostProductCoinEffect(float multiplier) {

        if(productCoinDonor == null) {
            return;
        }

        productCoinEffects = productCoinDonor.GetComponentsInChildren<ParticleSystem>(true);
        productCoinEffectSizes = new float[productCoinEffects.Length];

        for(int i = 0; i < productCoinEffects.Length; i++) {

            ParticleSystem.MainModule main = productCoinEffects[i].main;

            productCoinEffectSizes[i] = main.startSizeMultiplier;
            main.startSizeMultiplier = productCoinEffectSizes[i] * multiplier;
        }
    }

    protected override void FreeToolkitView() {

        FreeProductCoinStage();

        base.FreeToolkitView();
    }

    protected virtual void FreeProductCoinStage() {

        if(productCoinEffects != null) {

            for(int i = 0; i < productCoinEffects.Length; i++) {

                if(productCoinEffects[i] != null) {
                    ParticleSystem.MainModule main = productCoinEffects[i].main;
                    main.startSizeMultiplier = productCoinEffectSizes[i];
                }
            }

            productCoinEffects = null;
            productCoinEffectSizes = null;
        }

        if(productCoinStage != null) {
            productCoinStage.Detach();
            productCoinStage = null;
        }

        // The donor is ours — nothing else references it, so it goes with the view.
        if(productCoinDonor != null) {
            GameObject.Destroy(productCoinDonor);
            productCoinDonor = null;
        }
    }

    // Name search over the whole subtree rather than a hard-coded
    // Container/Button/Coin/UICoin path: a silent break here is an invisible coin, not an error.
    protected static Transform FindDeepChild(Transform root, string name) {

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

    public override void HandleShow() {
        base.HandleShow();

        buttonDisplayState = UIPanelButtonsDisplayState.ProductsSections;

        // RESTORED (user, iter 13). This screen shipped with the SMALL character —
        // TweenUtil.ShowObjectTop, so it slides in from the top above the filter tiles — and the
        // migration turned it off in 8ebb01e ("drop 3 that collide"). That pass was reasoning
        // about the character CARD (CharacterLarge, a 279x476 backer that does own this whole
        // left column and genuinely does collide with the ProductsSections tiles); the small rig
        // is a different container and was never the thing in the way.
        characterDisplayState = UIPanelCharacterDisplayState.Character;
        backgroundDisplayState = UIPanelBackgroundDisplayState.PanelBacker;
        adDisplayState = UIPanelAdDisplayState.BannerBottom;
    }

    public override void AnimateIn() {

        base.AnimateIn();

        LoadData(currentProductType);
    }

    public override void AnimateOut() {

        base.AnimateOut();

        ClearList();
    }
}