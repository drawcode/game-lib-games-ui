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
//   row name   - (iter 45) a delayed TextField: the player's name for the level, kept in its
//                saved file (GameLevelCodes.RenameCreated); cleared, it reads WORLD x - LEVEL y
//   DELETE     - (iter 45) ButtonGameModeCreateDelete$<code>, tap again within
//                deleteConfirmSeconds to remove the level for good (the profile strip's pattern
//                and strings)
//
// UI Toolkit only (no NGUI counterpart: new work, not a migration). The view is the bitty JSON
// panel-game-mode-create.json; rows come from its CreateLevelItemTemplate.
public class BaseGameUIPanelGameModeCreate : GameUIPanelBase {

    public static GameUIPanelGameModeCreate Instance;

    public static string listCreateLevels = "CreateLevelList";
    public static string listItemTemplate = "CreateLevelItemTemplate";

    public static string buttonDelete = "ButtonGameModeCreateDelete";
    // not plain "InputName": a TextField change is broadcast by element name, and the level
    // editor's META dialog has one
    public static string elementInputName = "InputCreateLevelName";
    public static string elementLabelDelete = "LabelDelete";

    public static string locDelete = "game_ui_settings_profiles_delete";
    public static string locDeleteConfirm = "game_ui_settings_profiles_tap_again_to_delete";

    public float deleteConfirmSeconds = 3f;

    // the row armed by a first DELETE tap, or null
    string deleteArmed;

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

    // Chains to UIPanelBase, which subscribes the click bus (DELETE) and the animate messages. The
    // panel used to add the animate listeners itself without chaining, so it never heard a click.
    public override void OnEnable() {
        base.OnEnable();
    }

    public override void OnDisable() {

        DisarmDelete();

        // Chain to base so UIPanelBase.OnDisable -> FreeToolkitView runs when this panel is
        // pooled away.
        base.OnDisable();
    }

    public override void OnButtonClickEventHandler(string buttonName) {

        base.OnButtonClickEventHandler(buttonName);

        if(UIUtil.IsButtonClickedLike(buttonDelete + "$", buttonName)) {
            Delete(buttonName.Substring(buttonName.IndexOf('$') + 1));
        }
    }

    // First tap arms the row (its label asks for a second); a second tap on the same row within
    // deleteConfirmSeconds deletes the level and rebuilds the list.
    public virtual void Delete(string code) {

        if(deleteArmed != code) {

            DisarmDelete();

            deleteArmed = code;
            SetDeleteLabel(code, locDeleteConfirm);

            CancelInvoke("DisarmDelete");
            Invoke("DisarmDelete", deleteConfirmSeconds);
            return;
        }

        CancelInvoke("DisarmDelete");
        deleteArmed = null;

        GameLevelCodes.DeleteCreated(code);

        loadData();
    }

    void DisarmDelete() {

        CancelInvoke("DisarmDelete");

        if(deleteArmed != null) {
            SetDeleteLabel(deleteArmed, locDelete);
            deleteArmed = null;
        }
    }

    void SetDeleteLabel(string code, string key) {

        if(!isToolkitPanel) {
            return;
        }

        Engine.UI.UIRef button = UIUtil.ResolveDeep(viewRoot, buttonDelete + "$" + code);

        if(button.alive) {
            UIUtil.SetLabelValue(UIUtil.ResolveDeep(button, elementLabelDelete), Locos.Get(key));
        }
    }

    // The name a row shows: the player's, else WORLD x - LEVEL y.
    public static string RowName(string code, int world, int level) {

        string name = GameLevelCodes.CreatedName(code);

        if(!string.IsNullOrEmpty(name)) {
            return name;
        }

        return L10n.TrOrDefault("game_ui_create_level_row", "WORLD {0} - LEVEL {1}", world, level);
    }

    public virtual void Rename(string code, string name, Engine.UI.UIRef input, int world, int level) {
        RenameRow(code, name, input, world, level);
    }

    public static void RenameRow(string code, string name, Engine.UI.UIRef input, int world, int level) {

        string fallback = L10n.TrOrDefault("game_ui_create_level_row", "WORLD {0} - LEVEL {1}", world, level);

        string trimmed = string.IsNullOrEmpty(name) ? "" : name.Trim();

        // committing the default text unchanged is not a name
        if(trimmed == fallback) {
            trimmed = "";
        }

        GameLevelCodes.RenameCreated(code, trimmed);

        if(trimmed.Length == 0) {
            UIUtil.SetInputValue(input, fallback);
        }
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

        DisarmDelete();

        UIUtil.ClearListItems(viewRoot, listCreateLevels);

        int added = GameWorldsListRows.FillCreated(viewRoot, listCreateLevels, listItemTemplate, buttonDelete, 0);

        Engine.UI.UIRef empty = UIUtil.ResolveDeep(viewRoot, "LabelEmpty");

        if(added == 0) {
            UIUtil.ShowObject(empty);
        }
        else {
            UIUtil.HideObject(empty);
        }

        UIUtil.ScrollToTop(UIUtil.ResolveDeep(viewRoot, listCreateLevels), false);
    }
}
