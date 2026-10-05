using System;
using System.Collections;
using System.Collections.Generic;
using Engine.Game.App;
using UnityEngine;
using UnityEngine.UI;

public class UIPanelDialogEditItemsFilter {
	public static string all = "all";
	public static string levelAssets = "level-assets";
	public static string levelEnvironment = "level-environments";
	public static string levelEffect = "level-effects";

	// B11.1 (defect 2): the asset catalog files effects under the key "effects"
	// (BaseDataObjectKeys.effects; action-bots app-content-asset-data: 11 rows) -- no row has ever
	// carried "level-effects", so that filter listed nothing. levelEffect is kept (public API,
	// shared libs) and Matches() lets it select either key.
	public static string effects = "effects";

	// The one place a filter is tested against a catalog key, shared by the legacy grid and the
	// toolkit list so the two can never disagree.
	public static bool Matches(string filterType, string assetKey) {

		if(filterType == all) {
			return true;
		}

		if(filterType == levelEffect || filterType == effects) {
			return assetKey == levelEffect || assetKey == effects;
		}

		return assetKey == filterType;
	}
}

public class UIPanelDialogEditItems : UIAppPanelBaseList {

    public GameObject listItemPrefab;

    public static UIPanelDialogEditItems Instance;

    public string filterType = UIPanelDialogEditItemsFilter.all;

    public override void Awake() {
        base.Awake();

        if(Instance != null && this != Instance) {
            //There is already a copy of this script running
            Destroy(this);
            return;
        }

        Instance = this;
    }

    public static bool isInst {
        get {
            if(Instance != null) {
                return true;
            }
            return false;
        }
    }

    public override void Start() {
        Init();
    }

    public override void Init() {
        base.Init();

        LoadData();
    }

    public void LoadData(string levelAssetKey) {
        filterType = levelAssetKey;
        LoadData();
    }

    public void LoadData() {

        // Toolkit list first, and as a REPLAY: GameDraggableEditor calls this before the async
        // view has landed on the first open, and on a panel that may never have woken up (the
        // dark editor), so it cannot wait for the view in a coroutine the way the products and
        // missions lists do. BindElements fills it when the view arrives.
        toolkitListPending = true;

        if(isToolkitPanel) {
            loadDataItemsToolkit();
        }

        // StartCoroutine on an inactive GameObject only logged an error and ran nothing.
        if(!gameObject.activeInHierarchy) {
            return;
        }

        StartCoroutine(LoadDataCo());
    }

    IEnumerator LoadDataCo() {

        yield return new WaitForSeconds(.1f);

        // The view owns the list once it is up; building the legacy grid as well would spawn a
        // 3D thumbnail per asset under a container nobody can see.
        if(isToolkitPanel) {
            yield break;
        }

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
        if(listGridRoot != null) {
            foreach(Transform item in listGridRoot.transform) {
                Destroy(item.gameObject);
            }

            List<AppContentAsset> assets = AppContentAssets.Instance.GetAll();

            LogUtil.Log("Load AppContentAsset: assets.Count: " + assets.Count);

            int i = 0;

            //int totalPoints = 0;

            foreach(AppContentAsset asset in assets) {

                if(filterType != UIPanelDialogEditItemsFilter.all) {
                    if(!UIPanelDialogEditItemsFilter.Matches(filterType, asset.key)) {
                        continue;
                    }
                }

                GameObject item = NGUITools.AddChild(listGridRoot, listItemPrefab);

                //GameObject item = NGUITools.AddChild(listGridRoot, listItemPrefab);
                item.name = "AssetItem" + i;

                Transform labelItemName = item.transform.Find("LabelName");

                if(labelItemName != null) {
                    UIUtil.SetLabelValue(labelItemName.gameObject, asset.display_name);
                }

                GameObject gameLevelItemObject = 
                    item.transform.Find("GameLevelItemObject").gameObject;

                // clear current items

                foreach(Transform t in gameLevelItemObject.transform) {
                    Destroy(t.gameObject);
                }

                //LogUtil.Log("Load AppContentAsset: gameLevelItemObject.transform: " + gameLevelItemObject.transform.childCount);

                if(GameDraggableEditor.Instance == null) {
                    yield break;
                }
                //
                //LogUtil.Log("Load AppContentAsset: GameDraggableEditor: " + true);

                string assetCode = asset.code;
                if(assetCode.Contains("portal-")) {
                    assetCode = assetCode + "-sm";
                }

                GameObject go = GameDraggableEditor.LoadSpriteUI(
                    gameLevelItemObject, assetCode, Vector3.one);

                gameLevelItemObject.ChangeLayersRecursively("UIEditor");

                //LogUtil.Log("Load AppContentAsset: go: " + go);

                float maxSize = .8f;

                if(go != null) {
                    /*PackedSprite sprite = go.GetComponent<PackedSprite>();
					if(sprite != null) {
						
						float adjust = 1;
						
						if(sprite.height > sprite.width) {
							if(sprite.height > maxSize) {
								adjust = maxSize/sprite.height;
							}
						}
						else {
							if(sprite.width > maxSize) {
								adjust = maxSize/sprite.width;
							}
						}
						
						go.transform.localScale = go.transform.localScale.WithX(adjust).WithY(adjust).WithZ(adjust);
					}
					else {
						*/
                    float adjust = 1;

                    Collider col = go.GetComponent<Collider>();
                    if(col != null) {
                        Bounds bounds = col.bounds;

                        if(bounds.size.x > bounds.size.y) {
                            if(bounds.size.y > maxSize) {
                                adjust = maxSize / bounds.size.x;
                            }
                        }
                        else {
                            if(bounds.size.x > maxSize) {
                                adjust = maxSize / bounds.size.y;
                            }
                        }
                    }
                    adjust = adjust / 2;

                    go.transform.localScale = 
                        go.transform.localScale.WithX(adjust).WithY(adjust).WithZ(adjust);
                    //}

                }

                Transform buttonGameLevelItemObject = item.transform.Find("ButtonGameLevelItemObject");

                if(buttonGameLevelItemObject != null) {

                    buttonGameLevelItemObject.GetComponent<UIButton>().name
                            = "ButtonGameLevelItemObject$" + asset.code; ///levels[y].name;
                }

                if(filterType == UIPanelDialogEditItemsFilter.all) {

                }

                i++;
            }

            yield return new WaitForEndOfFrame();
            listGridRoot.GetComponent<UIGrid>().Reposition();
            yield return new WaitForEndOfFrame();

        }
#else
        // B11.1: the no-NGUI build has no legacy row to clone -- the former UGUI half instantiated
        // listItemPrefab under listGridRoot and renamed a UnityEngine.UI.Button, and neither is
        // wired in a UI Toolkit product. The list is the view's (loadDataItemsToolkit, from
        // LoadData and the BindElements replay); nothing is left to do here.
        yield break;
#endif
    }

    // ==========================================================================================
    // UI TOOLKIT (B11.1 -- the "Select Item to Create" picker)
    //
    // A UIPanelBase, so the view lifecycle is the base's (EnsureToolkitView / LoadToolkitView ->
    // BindElements -> SuppressLegacyView -> show-if-isVisible, FreeToolkitView on OnDisable).
    // What the base cannot do here is be DRIVEN: it shows through AnimateIn on the UIController
    // broadcasts, and this panel is shown by GameDraggableEditor moving a GameObject -- on a panel
    // that, in the shipping scene, never even woke up (EditorContainer is inactive). So
    // ShowEditorView/HideEditorView drive the base seam directly, with no coroutine and no
    // AnimateIn (whose HandleShow/character/ad/background side effects belong to flow screens).
    //
    // NOT preloaded (toolkitPreloadView stays false): the picker opens on a tap inside an editor
    // session, a frame or two of build is invisible there, and a preload would hold a view for
    // every player who never opens the editor.
    //
    // Rows follow the bitty list pattern (BaseGameUIPanelProducts.loadDataProductsToolkit): the
    // view carries a "LevelItemList" with one "LevelItemTemplate" row (class list-item-template)
    // holding a "LabelName" and a "ButtonGameLevelItemObject" button. Each row is named
    // "AssetItem<i>" (the legacy row name) and its button is RENAMED to
    // "ButtonGameLevelItemObject$<asset code>" -- the $ payload GameDraggableEditor's click
    // handler already splits, so a toolkit pick and an NGUI pick are the same broadcast.

    public const string elementItemList = "LevelItemList";
    public const string elementItemTemplate = "LevelItemTemplate";
    public const string elementItemLabelName = "LabelName";
    public const string elementItemButton = "ButtonGameLevelItemObject";
    // B11.4 SEAM: the row's thumbnail element (the legacy GameLevelItemObject 3D sprite).
    public const string elementItemThumbnail = "GameLevelItemObject";

    bool toolkitListPending = false;

    public override string toolkitViewKey {
        get {
            return BaseUIPanel.panelLevelEditorItems;
        }
    }

    // A dialog: above the toolbar (+1) and the asset sheet (+2) it is opened from.
    public override int toolkitSortOrder {
        get {
            return Engine.UI.UILayers.overlay + 3;
        }
    }

    // The legacy dialog is a DialogContainer under the component's own GameObject, and this
    // panel's panelContainer is unwired in UIEditor.prefab, so the base default (hide
    // panelContainer) would hide nothing. Same restore-on-free contract as the toast's.
    readonly List<GameObject> toolkitSuppressed = new List<GameObject>();

    protected override void SuppressLegacyView() {

        base.SuppressLegacyView();

        if(toolkitSuppressed.Count > 0) {
            return;
        }

        for(int i = 0; i < transform.childCount; i++) {

            GameObject child = transform.GetChild(i).gameObject;

            if(!child.activeSelf) {
                continue;
            }

            toolkitSuppressed.Add(child);
            child.SetActive(false);
        }
    }

    protected override void FreeToolkitView() {

        for(int i = 0; i < toolkitSuppressed.Count; i++) {

            if(toolkitSuppressed[i] != null) {
                toolkitSuppressed[i].SetActive(true);
            }
        }

        toolkitSuppressed.Clear();

        base.FreeToolkitView();
    }

    public override void BindElements(Engine.UI.UIRef root) {

        base.BindElements(root);

        if(toolkitListPending) {
            loadDataItemsToolkit();
        }
    }

    // GameDraggableEditor's entry points, next to its legacy position tweens.
    public virtual void ShowEditorView() {

        isVisible = true;

        // Only for its token bump: it cancels a deferred hide still pending from the last
        // dismissal (and returns at once, isVisible being set).
        ShowPanel();

        // Loads on first call; the base continuation shows the view because isVisible is set.
        EnsureToolkitView();

        if(isToolkitPanel) {
            UIUtil.ShowObject(viewRoot);
            ShowToolkitViewSlide();
        }
    }

    public virtual void HideEditorView() {

        isVisible = false;

        if(isToolkitPanel) {
            HideToolkitViewSlide();
            // Display none with the slide (or at once on an inactive panel, which cannot run the
            // base's deferred hide).
            HideToolkitViewWhenSlideEnds();
        }
    }

    // A dark panel never gets OnDisable/OnDestroy, so GameDraggableEditor frees its view.
    public virtual void FreeEditorView() {
        FreeToolkitView();
    }

    public virtual void loadDataItemsToolkit() {

        if(!isToolkitPanel) {
            return;
        }

        toolkitListPending = false;

        UIUtil.ClearListItems(viewRoot, elementItemList);

        if(AppContentAssets.Instance == null) {
            return;
        }

        List<AppContentAsset> assets = AppContentAssets.Instance.GetAll();

        LogUtil.Log("Load loadDataItemsToolkit: filterType:" + filterType
            + " assets.Count:" + assets.Count);

        int i = 0;

        foreach(AppContentAsset asset in assets) {

            if(!UIPanelDialogEditItemsFilter.Matches(filterType, asset.key)) {
                continue;
            }

            Engine.UI.UIRef item = UIUtil.AddListItem(
                viewRoot, elementItemList, elementItemTemplate, "AssetItem" + i);

            UIUtil.UpdateLabelObject(item, elementItemLabelName, asset.display_name);

            // B11.4 SEAM -- THUMBNAILS. The legacy row instantiates the asset's 3D prefab
            // (GameDraggableEditor.LoadSpriteUI, "portal-" codes use their "-sm" variant) under
            // GameLevelItemObject on the UIEditor layer. A view cannot host live 3D; B11.4 renders
            // each code ONCE into a cached texture (new engine UIRenderSnapshot, borrowed light,
            // rule 189) and sets it here:
            //     UIUtil.SetImageTexture(UIUtil.ResolveDeep(item, elementItemThumbnail), texture);
            // Until then the row shows its name only.

            UIUtil.SetElementName(
                UIUtil.ResolveDeep(item, elementItemButton),
                GameDraggableEditorButtons.buttonGameLevelItemObject + "$" + asset.code);

            i++;
        }

        UIUtil.ScrollToTop(UIUtil.ResolveDeep(viewRoot, elementItemList), false);
    }

    // ==========================================================================================
}