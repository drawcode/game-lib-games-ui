using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Engine.Events;
using Engine.Game.App.BaseApp;

// CREATE (iter 43, owner): the level editor's home. Coop's mode-select tile is hidden and this
// one sits in its place; the worlds screen no longer has EDIT.
//
//   NEW LEVEL  - ButtonGameModePlayEditWorld: the next free create-<world>-<n> in the current
//                world, entered to edit (BaseUIController's play branch, GameLevelCodes)
//   MY LEVELS  - one row per SAVED created level (GameLevelCodes.ListCreated, newest first); the
//                row's button is renamed ButtonGameModePlayEditLevel$<code>, which reopens it
//
// UI Toolkit only (no NGUI counterpart: new work, not a migration). The view is the bitty JSON
// panel-game-mode-create.json; rows come from its CreateLevelItemTemplate.
public class BaseGameUIPanelGameModeCreate : GameUIPanelBase {

    public static GameUIPanelGameModeCreate Instance;

    public static string listCreateLevels = "CreateLevelList";
    public static string listItemTemplate = "CreateLevelItemTemplate";

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
    }

    public override void OnEnable() {

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
            UIControllerMessages.uiPanelAnimateIn,
            OnUIControllerPanelAnimateIn);

        Messenger<string>.RemoveListener(
            UIControllerMessages.uiPanelAnimateOut,
            OnUIControllerPanelAnimateOut);

        Messenger<string, string>.RemoveListener(
            UIControllerMessages.uiPanelAnimateType,
            OnUIControllerPanelAnimateType);

        // Chain to base so UIPanelBase.OnDisable -> FreeToolkitView runs when this panel is
        // pooled away.
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

    public override void HandleShow() {
        base.HandleShow();

        buttonDisplayState = UIPanelButtonsDisplayState.None;
        characterDisplayState = UIPanelCharacterDisplayState.None;
        backgroundDisplayState = UIPanelBackgroundDisplayState.PanelBacker;
    }

    public override void AnimateIn() {

        base.AnimateIn();

        loadData();
    }

    public static void LoadData() {
        if(GameUIPanelGameModeCreate.Instance != null) {
            GameUIPanelGameModeCreate.Instance.loadData();
        }
    }

    public virtual void loadData() {
        StartCoroutine(loadDataCo());
    }

    IEnumerator loadDataCo() {

        // the view loads async on first AnimateIn
        for(int waitFrames = 0; waitFrames < 60 && !isToolkitPanel; waitFrames++) {
            yield return null;
        }

        if(isToolkitPanel) {
            loadDataToolkit();
        }
    }

    public virtual void loadDataToolkit() {

        int worldNum = GameWorlds.Current != null && GameWorlds.Current.data != null
            ? GameWorlds.Current.data.world_num : 1;

        UIUtil.UpdateLabelObject(viewRoot, "LabelNewWorld",
            L10n.TrOrDefault("game_ui_create_in_world", "IN WORLD {0}", worldNum));

        UIUtil.ClearListItems(viewRoot, listCreateLevels);

        List<string> codes = GameLevelCodes.ListCreated();

        for(int i = 0; i < codes.Count; i++) {

            int world;
            int level;

            if(!GameLevelCodes.TryParseCreated(codes[i], out world, out level)) {
                continue;
            }

            Engine.UI.UIRef item = UIUtil.AddListItem(
                viewRoot, listCreateLevels, listItemTemplate, "CreateLevelItem" + i);

            UIUtil.UpdateLabelObject(item, "LabelName",
                L10n.TrOrDefault("game_ui_create_level_row", "WORLD {0} - LEVEL {1}", world, level));

            // the click bus carries only the element name, so the row's button carries the code
            UIUtil.SetElementName(
                UIUtil.ResolveDeep(item, "ButtonAction"),
                BaseUIButtonNames.buttonGameModePlayEditLevel + "$" + codes[i]);
        }

        Engine.UI.UIRef empty = UIUtil.ResolveDeep(viewRoot, "LabelEmpty");

        if(codes.Count == 0) {
            UIUtil.ShowObject(empty);
        }
        else {
            UIUtil.HideObject(empty);
        }

        UIUtil.ScrollToTop(UIUtil.ResolveDeep(viewRoot, listCreateLevels), false);
    }
}
