#define DEV
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using Engine.Events;
using Engine.Game.App;
using Engine.UI;
using Engine.Utility;

public class UIPanelDialogRPGObject : UIPanelBase {
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3

    public UIImageButton buttonBuyRecharge;
    public UIImageButton buttonEarn;
    public UIImageButton buttonResume;

    public UILabel labelTip;
    public UILabel labelTitle;
    public UILabel labelAbout;
    public UILabel labelScore;

    public UISlider sliderValue;
#else
    // B10: agnostic UIRef handles, bound at runtime by name (binds/panel-dialog-rpg.json).
    public Engine.UI.UIRef buttonBuyRecharge;
    public Engine.UI.UIRef buttonEarn;
    public Engine.UI.UIRef buttonResume;

    public Engine.UI.UIRef labelTip;
    public Engine.UI.UIRef labelTitle;
    public Engine.UI.UIRef labelAbout;
    public Engine.UI.UIRef labelScore;

    public Engine.UI.UIRef sliderValue;
#endif

    public GameObject containerContent;

    // ----------------------------------------------------------------------------------------
    // TOOLKIT (B3 — the RPG stat dialogs)

    // SCENE-RESIDENT singletons, like UIPanelDialogDisplay: Energy and Health live in
    // GameSceneDynamic.unity under DialogsContainer/ContainerStatic/GamePanelDialogRPGs, and
    // BaseUIController never catalog-loads them. Their legacy trees are the SAME layout (a framed
    // white card, a title band, a tagline, a how-to-recharge list, one stat meter, the current
    // action score, BUY RECHARGE / RESUME and a hidden third button) with only the copy and the
    // button names differing — so every dialog on this base loads ONE view and the per-stat parts
    // are written at runtime (see the toolkitStat* hooks below).
    public override string toolkitViewKey {
        get {
            return BaseUIPanel.panelDialogRPG;
        }
    }

    // A modal over live gameplay, opened from the HUD: the view has to exist BEFORE the tap, or
    // the first show runs the NGUI path for a frame or two (the 3F pause-overlay lesson). The base
    // OnEnable already calls PreloadToolkitView, and this base chains base.OnEnable(), so the
    // flag is the whole cost.
    public override bool toolkitPreloadView {
        get {
            return true;
        }
    }

    // Above the in-game HUD (UILayers.chrome), like every other dialog in the family.
    public override int toolkitSortOrder {
        get {
            return UILayers.overlay;
        }
    }

    // Legacy enters from the BOTTOM: backgroundDisplayState is None, so centerEnterDirection
    // resolves to Bottom, and the scene parks Center at y -3000. The base default drops in from
    // the top, which is the flow-panel choreography.
    protected override void ShowToolkitViewSlide() {
        TweenUtil.ShowObjectBottom(viewRoot, toolkitShowPreset);
    }

    protected override void HideToolkitViewSlide() {
        TweenUtil.HideObjectBottom(viewRoot, toolkitHidePreset);
    }

    // Element names in panel-dialog-rpg.uxml. The three buttons are AUTHORED with the Energy
    // dialog's scene GameObject names (the reachable one), and renamed in BindElements to the
    // owning dialog's own legacy names — Health's are ButtonRPGHealthBuyRecharge / -Resume /
    // -Training. Clicks travel by element name (UIToolkitBackend.OnClick -> EVENT_BUTTON_CLICK)
    // and every handler compares against its legacy button's GameObject name, so the view must
    // carry exactly those names. Renaming also keeps two loaded RPG views from answering each
    // other's taps: both dialogs are scene-resident and both listen on the click bus.
    public const string elementButtonBuyRecharge = "ButtonRPGEnergyBuyRecharge";
    public const string elementButtonResume = "ButtonRPGEnergyResume";
    public const string elementButtonEarn = "ButtonRPGEnergyMissions";

    public const string elementFrame = "BackgroundColor";
    public const string elementTitleBand = "LabelOverviewType";
    public const string elementTitle = "SubtitleMeter";
    public const string elementTagline = "SubtitleTagline";
    public const string elementAbout = "SubtitleAbout";
    public const string elementMeter = "ProgressSmarts";
    public const string elementMeterFill = "ProgressForeground";
    public const string elementMeterTitle = "ProgressLabelTitle";
    public const string elementMeterPercent = "LabelProgress";
    public const string elementScoreValue = "LabelValue";
    public const string elementEarnLabel = "EarnLabel";

    // Which stat this dialog explains: "energy", "health". Drives the per-stat copy
    // (game_ui_dialog_rpg_<stat>_title / _tagline / _about) and the meter caption
    // (game_ui_customize_character_<stat>). Empty = no per-stat copy; the view's static text
    // still shows.
    public virtual string toolkitStatCode {
        get {
            return "";
        }
    }

    // The meter value, 0..1. Below zero hides the meter. On legacy this is a UIGameRPG* component
    // on the ProgressSmarts widget, which stops ticking once the container is suppressed — so the
    // view reads the profile itself.
    public virtual double GetToolkitStatValue() {
        return -1;
    }

    // Energy prints "100%" under its meter caption; the Health scene tree has no LabelProgress.
    public virtual bool toolkitShowStatPercent {
        get {
            return true;
        }
    }

    // The SmartScore / ActionScore readout (legacy: a UIGameRPGXP on that widget).
    public virtual double GetToolkitScoreValue() {
        return (int)Math.Round(GameProfileCharacters.currentProgress.GetGamePlayerProgressXP(10));
    }

    // The hidden third button's caption: MISSIONS on Energy, TRAINING on Health.
    public virtual string toolkitEarnLabelKey {
        get {
            return "game_ui_game_mode_missions";
        }
    }

    // Fallback button names for a dialog whose legacy refs are not assigned (the UGUI build, or a
    // dialog with no scene object): "ButtonRPGEnergy" + "BuyRecharge" and so on — the scene's own
    // naming scheme.
    protected virtual string toolkitButtonPrefix {
        get {
            return "Button" + GetType().Name.Replace("UIPanelDialog", "");
        }
    }

    protected virtual string toolkitButtonEarnSuffix {
        get {
            return "Missions";
        }
    }

    protected string ToolkitButtonName(string suffix) {
        return toolkitButtonPrefix + suffix;
    }

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
    // The legacy GameObject name wins whenever the scene assigned the ref (rule: names byte for
    // byte); the fallback only covers an unassigned one.
    protected static string LegacyButtonName(UIImageButton button, string fallback) {
        return button != null ? button.name : fallback;
    }
#endif

    protected void RenameToolkitButton(string authoredName, string targetName) {

        if(authoredName == targetName) {
            return;
        }

        UIUtil.SetElementName(UIUtil.ResolveDeep(viewRoot, authoredName), targetName);
    }

    // Runs from LoadToolkitView's continuation, every time a view is (re)built: a freed view comes
    // back from the UXML with the authored names, so the rename has to be redone each time.
    public override void BindElements(UIRef root) {

        base.BindElements(root);

        if(!isToolkitPanel) {
            return;
        }

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
        string nameBuyRecharge = LegacyButtonName(buttonBuyRecharge, ToolkitButtonName("BuyRecharge"));
        string nameResume = LegacyButtonName(buttonResume, ToolkitButtonName("Resume"));
        string nameEarn = LegacyButtonName(buttonEarn, ToolkitButtonName(toolkitButtonEarnSuffix));
#else
        string nameBuyRecharge = ToolkitButtonName("BuyRecharge");
        string nameResume = ToolkitButtonName("Resume");
        string nameEarn = ToolkitButtonName(toolkitButtonEarnSuffix);
#endif

        RenameToolkitButton(elementButtonBuyRecharge, nameBuyRecharge);
        RenameToolkitButton(elementButtonResume, nameResume);
        RenameToolkitButton(elementButtonEarn, nameEarn);

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
#else
        // base.BindElements bound these through the manifest under the AUTHORED names, and a
        // UIRef caches its name at construction — IsButtonClicked(UIRef) would keep comparing the
        // old one. Re-resolve under the names the clicks now carry.
        buttonBuyRecharge = UIUtil.ResolveDeep(viewRoot, nameBuyRecharge);
        buttonResume = UIUtil.ResolveDeep(viewRoot, nameResume);
        buttonEarn = UIUtil.ResolveDeep(viewRoot, nameEarn);
#endif

        ApplyToolkitText();
        SyncToolkitColors();

        // The preload lands at level load, long before any show; the profile is only read once
        // the dialog is actually up (AnimateIn), or here if the view arrived mid-show.
        if(isVisible) {
            RefreshToolkitStats(true);
        }
    }

    // The per-stat copy. @loc keys go through SetLabelLocalized so a later language change
    // re-applies them live, exactly as the view's static @loc labels do.
    protected virtual void ApplyToolkitText() {

        if(!isToolkitPanel) {
            return;
        }

        string stat = toolkitStatCode;

        if(!string.IsNullOrEmpty(stat)) {

            string titleKey = "game_ui_dialog_rpg_" + stat + "_title";

            UIUtil.SetLabelLocalized(UIUtil.ResolveDeep(viewRoot, elementTitleBand), titleKey);
            UIUtil.SetLabelLocalized(UIUtil.ResolveDeep(viewRoot, elementTitle), titleKey);
            UIUtil.SetLabelLocalized(UIUtil.ResolveDeep(viewRoot, elementTagline), "game_ui_dialog_rpg_" + stat + "_tagline");
            UIUtil.SetLabelLocalized(UIUtil.ResolveDeep(viewRoot, elementAbout), "game_ui_dialog_rpg_" + stat + "_about");
            UIUtil.SetLabelLocalized(UIUtil.ResolveDeep(viewRoot, elementMeterTitle), "game_ui_customize_character_" + stat);
        }

        UIUtil.SetLabelLocalized(UIUtil.ResolveDeep(viewRoot, elementEarnLabel), toolkitEarnLabelKey);

        if(!toolkitShowStatPercent) {
            UIUtil.HideObject(UIUtil.ResolveDeep(viewRoot, elementMeterPercent));
        }
    }

    // The legacy tints are RUNTIME-driven, not the scene's mColor: UIColorModeTypeObject on the
    // frame (green by default) and UIColorRPGEnergyObject on each button backer (left at the
    // authored lime outside the three modes). Same mode table, same UIColors palette, through
    // UIUtil.SetSpriteColor — which gamma-encodes on the toolkit path, so the view paints the
    // pixels NGUI paints.
    protected virtual void SyncToolkitColors() {

        if(!isToolkitPanel) {
            return;
        }

        AppModes modes = AppModes.Instance;

        bool training = modes.isAppModeGameTraining;
        bool challenge = !training && modes.isAppModeGameChallenge;
        bool arcade = !training && !challenge && modes.isAppModeGameArcade;

        Color modeColor = training ? UIColors.colorPurple
            : challenge ? UIColors.colorBlue
            : arcade ? UIColors.colorOrange
            : UIColors.colorGreen;

        UIUtil.SetSpriteColor(UIUtil.ResolveDeep(viewRoot, elementFrame), modeColor);

        if(training || challenge || arcade) {
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
            UIUtil.SetSpriteColor(UIUtil.ResolveDeep(viewRoot, LegacyButtonName(buttonBuyRecharge, ToolkitButtonName("BuyRecharge"))), modeColor);
            UIUtil.SetSpriteColor(UIUtil.ResolveDeep(viewRoot, LegacyButtonName(buttonResume, ToolkitButtonName("Resume"))), modeColor);
            UIUtil.SetSpriteColor(UIUtil.ResolveDeep(viewRoot, LegacyButtonName(buttonEarn, ToolkitButtonName(toolkitButtonEarnSuffix))), modeColor);
#else
            UIUtil.SetSpriteColor(buttonBuyRecharge, modeColor);
            UIUtil.SetSpriteColor(buttonResume, modeColor);
            UIUtil.SetSpriteColor(buttonEarn, modeColor);
#endif
        }
    }

    // Written only on change: the readouts are strings, and the legacy meters re-read the
    // profile once a second, so a one-second poll while the dialog is up loses nothing. The
    // legacy count-up (lastValue stepping toward the profile value) is not reproduced — the view
    // shows the value it reads.
    public const float toolkitStatsInterval = 1f;

    protected float toolkitStatsElapsed = 0f;
    protected double toolkitLastStat = double.NaN;
    protected double toolkitLastScore = double.NaN;

    protected virtual void RefreshToolkitStats(bool force) {

        if(!isToolkitPanel) {
            return;
        }

        double stat = GetToolkitStatValue();

        if(force || stat != toolkitLastStat) {

            toolkitLastStat = stat;

            UIRef meter = UIUtil.ResolveDeep(viewRoot, elementMeter);

            if(stat < 0) {
                UIUtil.HideObject(meter);
            }
            else {
                UIUtil.ShowObject(meter);

                // A plain VisualElement fill: SetSliderValue falls back to width-percent, the same
                // model as NGUI 2.7's UISlider scaling its foreground sprite.
                UIUtil.SetSliderValue(UIUtil.ResolveDeep(viewRoot, elementMeterFill), (float)stat);

                if(toolkitShowStatPercent) {
                    UIUtil.SetLabelValue(UIUtil.ResolveDeep(viewRoot, elementMeterPercent), stat.ToString("P0"));
                }
            }
        }

        double score = GetToolkitScoreValue();

        if(force || score != toolkitLastScore) {

            toolkitLastScore = score;

            UIUtil.SetLabelValue(UIUtil.ResolveDeep(viewRoot, elementScoreValue),
                score.ToString("N0", Engine.Game.App.BaseApp.L10n.NumberFormat));
        }
    }

    protected void UpdateToolkitStats() {

        if(!isVisible || !isToolkitPanel) {
            return;
        }

        toolkitStatsElapsed += Time.unscaledDeltaTime;

        if(toolkitStatsElapsed < toolkitStatsInterval) {
            return;
        }

        toolkitStatsElapsed = 0f;

        RefreshToolkitStats(false);
    }

    // ----------------------------------------------------------------------------------------

    public override void Awake() {
        base.Awake();
    }

    public override void Init() {
        base.Init();

        //loadData();
    }

    public override void Start() {
        Init();
    }

    // EVENTS

    public override void OnEnable() {

        base.OnEnable();
    }

    public override void OnDisable() {

        base.OnDisable();
    }

    public override void OnButtonClickEventHandler(string buttonName) {

    }

    // OVERLAY DIALOG

    public void ContentPause() {
        GameController.GameRunningStateContent();
    }

    public void ContentRun() {
        GameController.GameRunningStateRun();
        //HideStates();
    }

    public void ShowContent() {

        UIPanelDialogBackground.ShowDefault();

        AnimateInBottom(containerContent);

        ContentPause();

        UIColors.UpdateColors();
    }

    public void HideContent() {

        UIPanelDialogBackground.HideAll();

        ContentPause();

        AnimateOutBottom(containerContent, 0f, 0f);
    }

    // base.AnimateIn is UIPanelBase's, so the show already takes the toolkit branch once the
    // (preloaded) view exists: EnsureToolkitView, ShowPanel -> the view, ShowToolkitViewSlide.
    // What the legacy widgets did for themselves on show — the mode tints (UIColors.UpdateColors
    // reaches only ENABLED legacy listeners) and the meters — is redone on the view here.
    public override void AnimateIn() {
        base.AnimateIn();

        ShowContent();

        if(isToolkitPanel) {

            SyncToolkitColors();

            toolkitStatsElapsed = 0f;

            RefreshToolkitStats(true);
        }
    }

    public override void AnimateOut() {
        base.AnimateOut();

        HideContent();
    }

    public virtual void Update() {
        UpdateToolkitStats();
    }
}
