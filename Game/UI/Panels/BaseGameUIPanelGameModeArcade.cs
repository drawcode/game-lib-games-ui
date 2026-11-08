using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Engine.Events;
using Engine.Game.App;
using Engine.Game.App.BaseApp;

#if ENABLE_FEATURE_GAME_MODE_ARCADE

// ARCADE (arcade P2, contexts games/shared/context-arcade-vs-missions.md): one row per arcade
// play type in the current world - the app-content-collection-data rows typed `arcade`, gated by
// GameContentGate like missions, with the next locked ones as "more to come" teasers. A row shows
// its stage's goal and the stage, and its button is renamed ButtonGamePlay$<arcade state>$<code>,
// which BaseUIController's play branch turns into a generated level (GameArcadeLevels.StartRound).
//
// The rows need the bitty JSON view with ArcadeList/ArcadeItemTemplate (Action Bots'
// panel-game-mode-arcade.json); a view without them shows no rows.
public class BaseGameUIPanelGameModeArcade : GameUIPanelBase {

    public static GameUIPanelGameModeArcade Instance;

    public static string listArcade = "ArcadeList";
    public static string listItemTemplate = "ArcadeItemTemplate";

    public GameObject listItemPrefab;

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
        // Chain to base so UIPanelBase.OnDisable -> FreeToolkitView runs when this panel is
        // pooled away, else the toolkit view leaks once the panel has one. Phase-3 migration
        // prerequisite (same fix the settings/header/footer bases got in 3A/3B).
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
        ///
    }

    public override void HandleShow() {
        base.HandleShow();

        buttonDisplayState = UIPanelButtonsDisplayState.None;
        // the description column sits where the large bot card would
        characterDisplayState = UIPanelCharacterDisplayState.None;
        backgroundDisplayState = UIPanelBackgroundDisplayState.PanelBacker;
    }

    public override void AnimateIn() {

        base.AnimateIn();

        loadData();
    }

    public static void LoadData() {
        if(GameUIPanelGameModeArcade.Instance != null) {
            GameUIPanelGameModeArcade.Instance.loadData();
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

        GameWorld world = GameWorlds.Current;
        int worldNum = world != null && world.data != null ? world.data.world_num : 1;

        UIUtil.UpdateLabelObject(viewRoot, "LabelWorld",
            L10n.TrOrDefault("game_ui_create_in_world", "IN WORLD {0}", worldNum));

        UIUtil.ClearListItems(viewRoot, listArcade);

        GameWorldsListRows.FillArcade(viewRoot, listArcade, listItemTemplate);

        UIUtil.ScrollToTop(UIUtil.ResolveDeep(viewRoot, listArcade), false);
    }
}
#endif
