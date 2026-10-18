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

	// TYPE TABS + PLACEABILITY (iter 40). The create picker ("all") lists the whole asset catalog,
	// which also carries sounds, UI screens and world terrains -- rows the editor cannot place
	// (EditModeCreateAsset loads level-asset prefabs, and characters through the actor queue).
	// Tabs group catalog keys into types; only placeable keys list unless "show unplaceable" is on.
	// Public static so an app can retune them without touching the panel.

	public static string levelAssetsHide = "level-assets-hide";
	public static string groupLevel = "level";

	// Characters are NOT placeable yet (measured iter 40): EditModeCreateAsset never sets the item
	// type, so a character code takes the level-asset path, finds no prefab, and leaves an empty,
	// invisible draggable (0 renderers, no actor) that a SAVE would keep. Level data spawns
	// characters only at round start (type "character" -> actor queue). Listed under "show
	// unplaceable" until the editor can create them.
	public static List<string> placeableKeys = new List<string> { levelAssets, levelAssetsHide };

	// Tab order; a key not listed gets its own tab after these.
	public static List<string> groupOrder = new List<string> {
		Engine.Game.Data.BaseDataObjectKeys.characters, groupLevel, "weapon", "item", "indicator",
		effects, "audio-mp3", "world", "ui" };

	public static Dictionary<string, string> groupLabelKeys = new Dictionary<string, string> {
		{ all, "game_ui_level_editor_type_all" },
		{ Engine.Game.Data.BaseDataObjectKeys.characters, "game_ui_level_editor_type_characters" },
		{ groupLevel, "game_ui_level_editor_type_level" },
		{ "weapon", "game_ui_level_editor_type_weapons" },
		{ "item", "game_ui_level_editor_type_items" },
		{ "indicator", "game_ui_level_editor_type_indicators" },
		{ effects, "game_ui_level_editor_type_effects" },
		{ "audio-mp3", "game_ui_level_editor_type_audio" },
		{ "world", "game_ui_level_editor_type_worlds" },
		{ "ui", "game_ui_level_editor_type_ui" } };

	// The thumbnail a row shows when it has no model to render (sprites.uss classes).
	public static Dictionary<string, string> groupIcons = new Dictionary<string, string> {
		{ Engine.Game.Data.BaseDataObjectKeys.characters, "spr-icon-profile-64" },
		{ groupLevel, "spr-icon-map-64" },
		{ "weapon", "spr-icon-aim-sm-64" },
		{ "item", "spr-icon-present-64" },
		{ "indicator", "spr-icon-locpoint-64" },
		{ effects, "spr-icon-fire-64" },
		{ "audio-mp3", "spr-icon-music-64" },
		{ "world", "spr-icon-compass-64" },
		{ "ui", "spr-icon-index-64" } };

	public static string defaultIcon = "spr-icon-help-64";

	// Extra catalog key -> group map, consulted by GroupOf before the built-in mapping. Empty by
	// default (Action Bots adds nothing); a game fills it through AddGroup.
	public static Dictionary<string, string> keyGroups = new Dictionary<string, string>();

	// One call per game-specific catalog key: its tab (group, label loc key, fallback icon), its
	// tab position (after `afterGroup`, or at the end when that group is not listed) and whether
	// the create picker may place it (EditModeCreateAsset loads the code from the level-asset
	// prefab folder, so placeable keys must name level-asset prefabs). Idempotent.
	public static void AddGroup(
		string assetKey, string group, string labelKey, string icon, bool placeable, string afterGroup) {

		if(string.IsNullOrEmpty(assetKey) || string.IsNullOrEmpty(group)) {
			return;
		}

		keyGroups[assetKey] = group;

		if(!groupOrder.Contains(group)) {

			int after = string.IsNullOrEmpty(afterGroup) ? -1 : groupOrder.IndexOf(afterGroup);

			if(after >= 0) {
				groupOrder.Insert(after + 1, group);
			}
			else {
				groupOrder.Add(group);
			}
		}

		if(!string.IsNullOrEmpty(labelKey)) {
			groupLabelKeys[group] = labelKey;
		}

		if(!string.IsNullOrEmpty(icon)) {
			groupIcons[group] = icon;
		}

		if(placeable && !placeableKeys.Contains(assetKey)) {
			placeableKeys.Add(assetKey);
		}
	}

	public static string GroupOf(string assetKey) {

		string mapped;

		if(assetKey != null && keyGroups.TryGetValue(assetKey, out mapped)) {
			return mapped;
		}

		if(assetKey == levelAssets || assetKey == levelAssetsHide) {
			return groupLevel;
		}

		return string.IsNullOrEmpty(assetKey) ? "" : assetKey;
	}

	public static bool IsPlaceable(string assetKey) {
		return placeableKeys.Contains(assetKey);
	}

	public static string IconOf(string group) {

		string icon;

		return group != null && groupIcons.TryGetValue(group, out icon) ? icon : defaultIcon;
	}

	public static string LabelOf(string group) {

		string key;

		if(group != null && groupLabelKeys.TryGetValue(group, out key)) {
			return Engine.Game.App.BaseApp.L10n.Tr(key);
		}

		return string.IsNullOrEmpty(group) ? "" : group.ToUpperInvariant();
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

        ReleaseThumbnails();
        UnregisterPickerClicks();

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

        RegisterPickerClicks();

        // Only for its token bump: it cancels a deferred hide still pending from the last
        // dismissal (and returns at once, isVisible being set).
        ShowPanel();

        // Loads on first call; the base continuation shows the view because isVisible is set.
        EnsureToolkitView();

        if(isToolkitPanel) {
            UIUtil.ShowObject(viewRoot);
            ShowToolkitViewSlide();

            // A reopen without a reload (the rows survived the hide, their textures did not):
            // re-arm the window; the layout pass that follows the show pages the rows back in.
            RefreshThumbnailWindow();
        }
    }

    public virtual void HideEditorView() {

        isVisible = false;

        UnregisterPickerClicks();

        if(isToolkitPanel) {
            HideToolkitViewSlide();
            // Display none with the slide (or at once on an inactive panel, which cannot run the
            // base's deferred hide).
            HideToolkitViewWhenSlideEnds();
        }

        // Closing the picker frees every thumbnail RT (the dark panel hides at once, so nothing
        // is still on screen to go blank). Reopening re-renders: ~one frame budget per few rows.
        ReleaseThumbnails();
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

        // A reload (filter change) supersedes the queued thumbnails of the rows it is about to
        // clear; the cached textures stay and answer the new rows at once.
        thumbnailGeneration++;

        if(thumbnailSnapshots != null) {
            thumbnailSnapshots.CancelPending();
        }

        UIUtil.ClearListItems(viewRoot, elementItemList);
        ClearThumbnailRows();

        if(AppContentAssets.Instance == null) {
            return;
        }

        List<AppContentAsset> assets = AppContentAssets.Instance.GetAll();

        LogUtil.Log("Load loadDataItemsToolkit: filterType:" + filterType
            + " typeTab:" + typeTab + " showUnplaceable:" + showUnplaceable
            + " assets.Count:" + assets.Count);

        // Tabs and the show-unplaceable row belong to the CREATE picker only; the asset sheet's
        // select-item / select-effect pickers are already narrowed to one key.
        bool createPicker = filterType == UIPanelDialogEditItemsFilter.all;

        LoadTypeTabs(assets, createPicker);

        int i = 0;

        foreach(AppContentAsset asset in assets) {

            if(!UIPanelDialogEditItemsFilter.Matches(filterType, asset.key)) {
                continue;
            }

            bool placeable = !createPicker || UIPanelDialogEditItemsFilter.IsPlaceable(asset.key);
            string group = UIPanelDialogEditItemsFilter.GroupOf(asset.key);

            if(createPicker) {

                if(!placeable && !showUnplaceable) {
                    continue;
                }

                if(typeTab != UIPanelDialogEditItemsFilter.all && group != typeTab) {
                    continue;
                }
            }

            Engine.UI.UIRef item = UIUtil.AddListItem(
                viewRoot, elementItemList, elementItemTemplate, "AssetItem" + i);

            // 60 catalog rows carry no display name; the code is better than a blank tile.
            UIUtil.UpdateLabelObject(item, elementItemLabelName,
                string.IsNullOrEmpty(asset.display_name) ? asset.code : asset.display_name);

            if(!placeable) {
                // Dimmed, USE hidden (common.uss .lei-row--unplaceable): listed to be seen, not placed.
                UIUtil.SetClass(item, rowUnplaceableClass, true);
            }

            // B11.4 THUMBNAILS. The legacy row instantiates the asset's 3D prefab
            // (GameDraggableEditor.LoadSpriteUI, "portal-" codes use their "-sm" variant) under
            // GameLevelItemObject on the UIEditor layer. A view cannot host live 3D, and a live
            // UIRenderStage per row would be 163 cameras: each code is rendered ONCE into a cached
            // texture (engine UIRenderSnapshot, the layer's shared stage light). Rows are only
            // REGISTERED here; RefreshThumbnailWindow pages the renders in as the rows scroll into
            // view (iter 40). Characters render their model too (legacy rows were text-only).
            // Level assets render their prefab and characters their model (placeable or not);
            // every other type has nothing to render and goes straight to its icon.
            if(placeable || asset.key == Engine.Game.Data.BaseDataObjectKeys.characters) {
                AddThumbnailRow(item, asset, group);
            }
            else {
                SetThumbnailIcon(UIUtil.ResolveDeep(item, elementItemThumbnail), group);
            }

            UIUtil.SetElementName(
                UIUtil.ResolveDeep(item, elementItemButton),
                GameDraggableEditorButtons.buttonGameLevelItemObject + "$" + asset.code);

            i++;
        }

        UIUtil.ScrollToTop(UIUtil.ResolveDeep(viewRoot, elementItemList), false);

        // The rows get bounds a frame later; the window callback fires then and pages them in.
        RefreshThumbnailWindow();
    }

    // ------------------------------------------------------------------------------------------
    // TYPE TABS + SHOW UNPLACEABLE (iter 40)
    //
    // Tabs are rows of "LevelItemTabs" (the bitty list pattern, template "LevelItemTabTemplate"),
    // renamed "ButtonGameEditItemsTab$<group>"; the check row is "ButtonGameEditItemsShowAll". The
    // panel's own click listener is registered by ShowEditorView, not OnEnable: in the shipping
    // scene this panel's GameObject never wakes (EditorContainer is inactive).

    public const string elementTabList = "LevelItemTabs";
    public const string elementTabTemplate = "LevelItemTabTemplate";
    public const string elementTabLabel = "LabelTab";
    public const string elementShowAll = "ButtonGameEditItemsShowAll";
    public const string buttonTabPrefix = "ButtonGameEditItemsTab$";

    public const string tabOnClass = "lei-tab--on";
    public const string checkOnClass = "lei-check-row--on";
    public const string rowUnplaceableClass = "lei-row--unplaceable";
    public const string thumbIconClass = "lei-thumb-icon";

    // The selected type tab (a group from UIPanelDialogEditItemsFilter.GroupOf, or "all").
    public string typeTab = UIPanelDialogEditItemsFilter.all;

    // Off by default: the create picker lists only what the editor can place.
    public bool showUnplaceable = false;

    readonly List<string> tabGroups = new List<string>();
    bool pickerClicksRegistered = false;

    void LoadTypeTabs(List<AppContentAsset> assets, bool createPicker) {

        Engine.UI.UIRef tabs = UIUtil.ResolveDeep(viewRoot, elementTabList);
        Engine.UI.UIRef showAll = UIUtil.ResolveDeep(viewRoot, elementShowAll);

        UIUtil.ClearListItems(viewRoot, elementTabList);
        tabGroups.Clear();

        if(!createPicker) {
            UIUtil.HideObject(tabs);
            UIUtil.HideObject(showAll);
            return;
        }

        UIUtil.ShowObject(tabs);
        UIUtil.ShowObject(showAll);
        UIUtil.SetClass(showAll, checkOnClass, showUnplaceable);

        // The groups that have at least one row under the current show-unplaceable setting.
        List<string> present = new List<string>();

        foreach(AppContentAsset asset in assets) {

            if(!showUnplaceable && !UIPanelDialogEditItemsFilter.IsPlaceable(asset.key)) {
                continue;
            }

            string group = UIPanelDialogEditItemsFilter.GroupOf(asset.key);

            if(!string.IsNullOrEmpty(group) && !present.Contains(group)) {
                present.Add(group);
            }
        }

        tabGroups.Add(UIPanelDialogEditItemsFilter.all);

        foreach(string group in UIPanelDialogEditItemsFilter.groupOrder) {
            if(present.Contains(group)) {
                tabGroups.Add(group);
            }
        }

        foreach(string group in present) {
            if(!tabGroups.Contains(group)) {
                tabGroups.Add(group);
            }
        }

        // A tab whose rows were just hidden (unplaceable switched off) falls back to ALL.
        if(!tabGroups.Contains(typeTab)) {
            typeTab = UIPanelDialogEditItemsFilter.all;
        }

        for(int i = 0; i < tabGroups.Count; i++) {

            string group = tabGroups[i];

            Engine.UI.UIRef tab = UIUtil.AddListItem(
                viewRoot, elementTabList, elementTabTemplate, buttonTabPrefix + group);

            UIUtil.UpdateLabelObject(tab, elementTabLabel, UIPanelDialogEditItemsFilter.LabelOf(group));
            UIUtil.SetClass(tab, tabOnClass, group == typeTab);
        }
    }

    public virtual void SelectTypeTab(string group) {

        if(string.IsNullOrEmpty(group) || group == typeTab) {
            return;
        }

        typeTab = group;
        loadDataItemsToolkit();
    }

    public virtual void SetShowUnplaceable(bool show) {

        if(show == showUnplaceable) {
            return;
        }

        showUnplaceable = show;
        loadDataItemsToolkit();
    }

    void RegisterPickerClicks() {

        if(pickerClicksRegistered) {
            return;
        }

        pickerClicksRegistered = true;
        Engine.Events.Messenger<string>.AddListener(ButtonEvents.EVENT_BUTTON_CLICK, OnPickerClick);
    }

    void UnregisterPickerClicks() {

        if(!pickerClicksRegistered) {
            return;
        }

        pickerClicksRegistered = false;
        Engine.Events.Messenger<string>.RemoveListener(ButtonEvents.EVENT_BUTTON_CLICK, OnPickerClick);
    }

    void OnPickerClick(string buttonName) {

        if(!isVisible || !isToolkitPanel || string.IsNullOrEmpty(buttonName)) {
            return;
        }

        if(buttonName.StartsWith(buttonTabPrefix)) {
            SelectTypeTab(buttonName.Substring(buttonTabPrefix.Length));
        }
        else if(buttonName == elementShowAll) {
            SetShowUnplaceable(!showUnplaceable);
        }
    }

    void SetThumbnailIcon(Engine.UI.UIRef thumb, string group) {

        if(!thumb.alive) {
            return;
        }

        UIUtil.SetClass(thumb, thumbIconClass, true);
        UIUtil.SetClass(thumb, UIPanelDialogEditItemsFilter.IconOf(group), true);
    }

    // ------------------------------------------------------------------------------------------
    // B11.4 THUMBNAILS, PAGED (iter 40)
    //
    // A row's thumbnail is a 3D model rendered once into a texture. Rendering all 163 at open
    // (the B11.4 shape) instantiated every prefab and held every RT whether or not the row was ever
    // scrolled to. Now only rows in or near the viewport load:
    //   - rows in view render first, then rows within thumbnailLoadMargin viewports above/below;
    //   - a scroll re-queues: renders still pending for rows that left the window are dropped;
    //   - rows beyond thumbnailKeepMargin viewports release their texture (and come back on the
    //     way back, from a fresh render);
    //   - a hidden picker has no layout, so nothing is in view and nothing loads.
    // The window is read from row bounds (UIUtil.ListItemsInView), so the 4-column grid pages too.

    // Off: rows are text-only (the B11.2 shell). For A/B and for a device that cannot spare it.
    public static bool thumbnailsEnabled = true;

    // 1.6x the 70 x 50 .lei-row-thumb box, same aspect: SetImageTexture stretches to the element.
    public static int thumbnailWidth = 112;
    public static int thumbnailHeight = 80;

    // Viewport heights beyond the visible area: load ahead of the scroll, keep a little behind it.
    public static float thumbnailLoadMargin = 1f;
    public static float thumbnailKeepMargin = 2f;

    // Characters face +Z; the stage camera looks along +Z, so turn them to face it, three-quarter.
    public static float thumbnailCharacterYaw = 155f;

    Engine.UI.UIRenderSnapshot thumbnailSnapshots;

    // Bumped on every reload / release: a callback for an older list must not write into rows
    // that were cleared (toolkit elements are recycled -- a stale row may be another view's now).
    int thumbnailGeneration = 0;

    enum ThumbState { None, Requested, Shown }

    class ThumbRow {
        public Engine.UI.UIRef thumb;
        public string code;
        public bool character;
        public string group;
        public ThumbState state;
    }

    readonly Dictionary<string, ThumbRow> thumbRows = new Dictionary<string, ThumbRow>();
    readonly List<string> thumbWindow = new List<string>();
    readonly List<string> thumbKeep = new List<string>();
    readonly HashSet<string> thumbKeepSet = new HashSet<string>();
    readonly HashSet<string> thumbKeptCodes = new HashSet<string>();
    readonly List<string> thumbDrop = new List<string>();
    System.Action thumbWindowUnregister;

    public Engine.UI.UIRenderSnapshot thumbnails {
        get {
            return thumbnailSnapshots;
        }
    }

    // PROBES: rows registered / with a render queued / showing a texture.
    public int thumbnailRowCount {
        get {
            return thumbRows.Count;
        }
    }

    public int thumbnailShownCount {
        get {
            return CountThumbState(ThumbState.Shown);
        }
    }

    public int thumbnailRequestedCount {
        get {
            return CountThumbState(ThumbState.Requested);
        }
    }

    int CountThumbState(ThumbState state) {

        int n = 0;

        foreach(ThumbRow r in thumbRows.Values) {
            if(r.state == state) {
                n++;
            }
        }

        return n;
    }

    void AddThumbnailRow(Engine.UI.UIRef item, AppContentAsset asset, string group) {

        if(!thumbnailsEnabled || asset == null || string.IsNullOrEmpty(asset.code)
            || !item.alive || string.IsNullOrEmpty(item.name)) {
            return;
        }

        Engine.UI.UIRef thumb = UIUtil.ResolveDeep(item, elementItemThumbnail);

        if(!thumb.alive) {
            return;
        }

        ThumbRow row = new ThumbRow();
        row.thumb = thumb;
        row.code = asset.code;
        row.character = asset.key == Engine.Game.Data.BaseDataObjectKeys.characters;
        row.group = group;
        row.state = ThumbState.None;

        thumbRows[item.name] = row;
    }

    void ClearThumbnailRows() {
        thumbRows.Clear();
    }

    // Re-solve which rows load. Cheap when nothing changed (a list walk); called on every scroll
    // step, layout pass and open.
    public void RefreshThumbnailWindow() {

        if(!thumbnailsEnabled || !isToolkitPanel || thumbRows.Count == 0
            || !GameDraggableEditor.isInst) {
            return;
        }

        if(thumbWindowUnregister == null) {
            thumbWindowUnregister = UIUtil.OnListWindowChanged(
                viewRoot, elementItemList, RefreshThumbnailWindow);
        }

        if(thumbnailSnapshots == null) {
            thumbnailSnapshots = new Engine.UI.UIRenderSnapshot();
            thumbnailSnapshots.width = thumbnailWidth;
            thumbnailSnapshots.height = thumbnailHeight;
        }

        // Visible rows first, then the load margin (duplicates skipped by state below).
        thumbWindow.Clear();
        UIUtil.ListItemsInView(viewRoot, elementItemList, 0f, thumbWindow);
        UIUtil.ListItemsInView(viewRoot, elementItemList, thumbnailLoadMargin, thumbWindow);

        thumbKeep.Clear();
        UIUtil.ListItemsInView(viewRoot, elementItemList, thumbnailKeepMargin, thumbKeep);

        thumbKeepSet.Clear();
        thumbKeptCodes.Clear();

        for(int i = 0; i < thumbKeep.Count; i++) {

            ThumbRow kept;

            thumbKeepSet.Add(thumbKeep[i]);

            if(thumbRows.TryGetValue(thumbKeep[i], out kept)) {
                thumbKeptCodes.Add(kept.code);
            }
        }

        // Pending renders are re-queued in window order below; anything that left the window
        // simply is not queued again.
        thumbnailSnapshots.CancelPending();

        thumbDrop.Clear();

        foreach(KeyValuePair<string, ThumbRow> kv in thumbRows) {

            ThumbRow r = kv.Value;

            if(r.state == ThumbState.Requested) {
                r.state = ThumbState.None;
            }
            else if(r.state == ThumbState.Shown && !thumbKeepSet.Contains(kv.Key)) {
                thumbDrop.Add(kv.Key);
            }
        }

        for(int i = 0; i < thumbDrop.Count; i++) {

            ThumbRow r = thumbRows[thumbDrop[i]];

            UIUtil.ClearImageTexture(r.thumb);
            r.state = ThumbState.None;

            // A code shared with a row still kept stays cached for that row.
            if(!thumbKeptCodes.Contains(r.code)) {
                thumbnailSnapshots.Release(r.code);
            }
        }

        int generation = thumbnailGeneration;

        for(int i = 0; i < thumbWindow.Count; i++) {

            ThumbRow r;

            if(!thumbRows.TryGetValue(thumbWindow[i], out r) || r.state != ThumbState.None) {
                continue;
            }

            r.state = ThumbState.Requested;

            ThumbRow target = r;

            thumbnailSnapshots.Request(
                r.code,
                r.character ? (Engine.UI.UIRenderSnapshot.Spawner)SpawnCharacterThumbnail : SpawnThumbnailContent,
                (c, texture) => {

                    if(generation != thumbnailGeneration || !isToolkitPanel
                        || target.state != ThumbState.Requested) {
                        return;
                    }

                    if(texture == null) {
                        // No model (a level asset whose prefab is missing, an effect): the type's
                        // icon, and never asked again this list.
                        SetThumbnailIcon(target.thumb, target.group);
                        target.state = ThumbState.Shown;
                        return;
                    }

                    UIUtil.SetImageTexture(target.thumb, texture);
                    target.state = ThumbState.Shown;
                });
        }
    }

    // Spawned exactly as the legacy row did (LoadSpriteUI, the "-sm" portal variant).
    static GameObject SpawnThumbnailContent(GameObject parent, string code) {

        string assetCode = code;

        if(assetCode.Contains("portal-")) {
            assetCode = assetCode + "-sm";
        }

        return GameDraggableEditor.LoadSpriteUI(parent, assetCode, Vector3.one);
    }

    // A character row's code is its model asset code (character-bot-1 has a model but no
    // GameCharacter row, so the model is loaded directly). Unpooled: the snapshot destroys it.
    static GameObject SpawnCharacterThumbnail(GameObject parent, string code) {

        GameObject go = AppContentAssets.LoadAsset(code, Vector3.zero, Quaternion.identity, false);

        if(go == null) {
            return null;
        }

        go.transform.parent = parent.transform;
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one;
        go.transform.localRotation = Quaternion.Euler(0f, thumbnailCharacterYaw, 0f);

        return go;
    }

    public void ReleaseThumbnails() {

        thumbnailGeneration++;

        if(thumbWindowUnregister != null) {
            thumbWindowUnregister();
            thumbWindowUnregister = null;
        }

        foreach(ThumbRow r in thumbRows.Values) {

            if(r.state == ThumbState.Shown) {
                UIUtil.ClearImageTexture(r.thumb);
            }

            r.state = ThumbState.None;
        }

        if(thumbnailSnapshots != null) {
            thumbnailSnapshots.Clear();
        }
    }

    // ==========================================================================================
}