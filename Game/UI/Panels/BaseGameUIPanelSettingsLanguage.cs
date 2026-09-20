using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Engine.Events;
using Engine.Game.App.BaseApp;

#if ENABLE_FEATURE_SETTINGS_LANGUAGE

public class BaseGameUIPanelSettingsLanguage : GameUIPanelBase {

    public static GameUIPanelSettingsLanguage Instance;

    // UI Toolkit only -- this sub-panel has no NGUI counterpart (the language picker is new
    // work, not a migration), so there is no #if USE_UI_NGUI_2_7 branch of legacy widget fields.
    public Engine.UI.UIRef dropdownLanguage = Engine.UI.UIRef.none;

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

        PopulateChoices();
        SyncSelection();

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

        // Row 0 ("SYSTEM DEFAULT") is a localized string, so a language change while this panel
        // is open must re-render the choice list, not just the rest of the screen.
        GameLocalizationService.LanguageChanged += OnLanguageChanged;
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

        GameLocalizationService.LanguageChanged -= OnLanguageChanged;

        // Chain to base so UIPanelBase.OnDisable -> FreeToolkitView runs when this panel is
        // pooled away (destroy-on-hide). Prerequisite for the 3A migration, same as every other
        // settings sub-panel.
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

    // Register the toolkit side once the view exists -- the first moment the dropdown element is
    // real. Populate/sync again here (Init already tried, before the async LoadView finished, and
    // no-op'd on the still-unbound UIRef) same as BaseGameUIPanelSettingsControls does for its
    // toggles/slider.
    public override void BindElements(Engine.UI.UIRef root) {

        base.BindElements(root);

        UIUtil.SetDropdownHandlerChange(dropdownLanguage, OnDropdownChanged);

        PopulateChoices();
        SyncSelection();
    }

    // ------------------------------------------------------------------
    // CHOICES
    //
    // Row 0 is always "follow the system"; rows 1..N are GameLocales.All in registry (picker)
    // order. Index <-> code mapping used by SyncSelection/OnDropdownChanged must stay the
    // inverse of each other.

    public virtual void PopulateChoices() {

        List<string> choices = new List<string>();
        choices.Add(L10n.TrOrDefault("lang_system_default", "SYSTEM DEFAULT"));

        List<GameLocaleInfo> all = GameLocales.All;

        for(int i = 0; i < all.Count; i++) {
            choices.Add(all[i].native);
        }

        UIUtil.SetDropdownChoices(dropdownLanguage, choices);
    }

    public virtual void SyncSelection() {

        string saved = GameLocalizationService.SavedLanguage;

        int index = 0;

        if(!string.IsNullOrEmpty(saved)) {

            List<GameLocaleInfo> all = GameLocales.All;

            for(int i = 0; i < all.Count; i++) {
                if(string.Equals(all[i].code, saved, StringComparison.OrdinalIgnoreCase)) {
                    index = i + 1;
                    break;
                }
            }
        }

        UIUtil.SetDropdownIndex(dropdownLanguage, index, false);
    }

    public virtual void OnDropdownChanged(int index) {

        if(index <= 0) {
            GameLocalizationService.SetLanguage("");
            return;
        }

        List<GameLocaleInfo> all = GameLocales.All;

        int localeIndex = index - 1;

        if(localeIndex < 0 || localeIndex >= all.Count) {
            return;
        }

        GameLocalizationService.SetLanguage(all[localeIndex].code);
    }

    // Fires on every SetLanguage, including the one this panel's own dropdown just caused --
    // re-set choices (row 0's native text may have just changed) and the index WITHOUT notify,
    // or this would recurse straight back into OnDropdownChanged.
    public virtual void OnLanguageChanged(string code) {

        PopulateChoices();
        SyncSelection();
    }

    public override void HandleShow() {
        base.HandleShow();

        backgroundDisplayState = UIPanelBackgroundDisplayState.PanelBacker;
    }

    public virtual void loadData() {
        StartCoroutine(loadDataCo());
    }

    IEnumerator loadDataCo() {

        yield return new WaitForSeconds(1f);
    }
}
#endif
