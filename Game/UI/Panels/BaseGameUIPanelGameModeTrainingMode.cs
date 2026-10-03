using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Engine.Events;

#if ENABLE_FEATURE_TRAINING

public class BaseGameUIPanelGameModeTrainingMode : GameUIPanelBase {

    public static GameUIPanelGameModeTrainingMode Instance;

    public GameObject listItemPrefab;

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
    public UIImageButton buttonGamePlayChoiceQuiz; // quiz
    public UIImageButton buttonGamePlayCollectionSmarts; // concussions,
    public UIImageButton buttonGamePlayCollectionSafety; // equipment
#else
    public Engine.UI.UIRef buttonGamePlayChoiceQuiz; // quiz
    public Engine.UI.UIRef buttonGamePlayCollectionSmarts; // concussions,
    public Engine.UI.UIRef buttonGamePlayCollectionSafety; // equipment
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

        // Chain to the base: UIPanelBase.OnDisable is what calls FreeToolkitView. Without this
        // the view leaks its PanelRenderer and the kill switch cannot restore the legacy view.
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

    // B0b (2026-10-03): kept INERT, on purpose. This block has been commented out since the
    // file was created (lib-games 39e44ba, 2014-06-22) and none of its routes can work today:
    //  * ShowGameModeTrainingModeChoiceQuiz / CollectionSmarts / CollectionSafety do not exist --
    //    commented out in GameUIController and BaseUIController -- and their panel codes are
    //    not in app-content-asset-data, so syncPanelLoaded would never load the screens anyway.
    //  * Switching to the per-mode app content state here races the global handler: the three
    //    play buttons are named ButtonGameModePlayTraining*, so BaseUIController's
    //    "ButtonGameModePlay" contains-match already calls GameController.PlayGame in the generic
    //    appContentStateGameTraining (set by ButtonGameModeTraining on the way in), and the
    //    choice-quiz state dereferences UIPanelModeTypeChoice.Instance in onGamePrepare.
    //  * ButtonGameEquipmentRoom is already routed globally (ShowEquipment); handling it here
    //    too would show the equipment screen twice.
    // So every button on this screen is served by the global handler, and this one adds nothing.
    public override void OnButtonClickEventHandler(string buttonName) {
        /*
        if(UIUtil.IsButtonClicked(buttonGamePlayChoiceQuiz, buttonName)) {

            GameController.ChangeGameStates(AppContentStateMeta.appContentStateGameTrainingChoiceQuiz);
            GameUIController.ShowGameModeTrainingModeChoiceQuiz();
        }
        else if(UIUtil.IsButtonClicked(buttonGamePlayCollectionSmarts, buttonName)) {
            GameController.ChangeGameStates(AppContentStateMeta.appContentStateGameTrainingCollectionSmarts);
            GameUIController.ShowGameModeTrainingModeCollectionSmarts();
        }
        else if(UIUtil.IsButtonClicked(buttonGamePlayCollectionSafety, buttonName)) {
            GameController.ChangeGameStates(AppContentStateMeta.appContentStateGameTrainingCollectionSafety);
            GameUIController.ShowGameModeTrainingModeCollectionSafety();

        }
        else if(UIUtil.IsButtonClicked(buttonGameEquipmentRoom, buttonName)) {
            GameUIController.ShowEquipment();
        }
        */
    }

    public override void HandleShow() {
        base.HandleShow();

        // Same dark content card as the other game-mode sub-screens. No bot card: the prefab
        // puts EQUIPMENT ROOM where the CharacterLarge pill would sit.
        backgroundDisplayState = UIPanelBackgroundDisplayState.PanelBacker;
    }

    // B0b (2026-10-03): calls through again. Since lib-games 39e44ba (2014) this override never
    // called base.AnimateIn: it redirected straight to the quiz screen instead
    // (ChangeGameStates(...TrainingChoiceQuiz) + ShowGameModeTrainingModeChoiceQuiz). lib-games
    // 3868abc (2018-07-10, "Update ui controller loading from type to codes. TODO switch to data
    // driven for back and lookup/load.") commented that redirect out with the quiz Show method
    // and left the body empty, so the footer's Help/Tutorial tile opened a screen that drew
    // nothing. The redirect stays off: its target no longer exists (see the handler above).
    public override void AnimateIn() {

        base.AnimateIn();

        //GameController.ChangeGameStates(AppContentStateMeta.appContentStateGameTrainingChoiceQuiz);
        //GameUIController.ShowGameModeTrainingModeChoiceQuiz();
    }

    public static void LoadData() {
        if(GameUIPanelGameModeTrainingMode.Instance != null) {
            GameUIPanelGameModeTrainingMode.Instance.loadData();
        }
    }

    public virtual void loadData() {
        StartCoroutine(loadDataCo());
    }

    IEnumerator loadDataCo() {

        yield return new WaitForSeconds(1f);
    }
}
#endif