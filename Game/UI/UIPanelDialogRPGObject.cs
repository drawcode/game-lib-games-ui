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

        // B9 S2: last, so the view's elements exist and are renamed (see STAGED 3D below).
        SetupToolkitLegacy3D();
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
    // STAGED 3D (B9 S2, 2026-10-03) — the bot and the price coin inside the toolkit view
    //
    // The default SuppressLegacyView hid the whole panelContainer, which took the two pieces of REAL
    // geometry with it: the bot (Character/RotatorContainer/Rotator/Container/playerDisplay, a
    // warbots-infernos rig under a RotateObject spinner) and the coin beside the price
    // (Buttons/ButtonRPG<Stat>BuyRecharge/Price/DialogCoin, DialogCoin.prefab). Both are now
    // rendered into RenderTextures shown in the view's CharacterStage / PriceCoin elements through
    // UIPanelBase.StageLegacy3D (S1's UIRenderStageBinding), which needs the content ACTIVE — so the
    // suppression here is selective: every flat NGUI widget goes, the two 3D subtrees stay.
    //
    // Scene paths (GameSceneDynamic.unity, GamePanelDialogRPGEnergy; Health is the same layout):
    //   panelContainer = <panel>/Container
    //   .../AnchorCenter/Center/PanelContents/Container/Content/PanelContent/Container/
    //       Backgrounds                                  hidden
    //       Container/Dialog                             hidden
    //       Container/Character/Progress                 hidden
    //       Container/Character/RotatorContainer         KEPT, staged -> CharacterStage
    //       Container/SmartScore (Health: ActionScore)   hidden
    //       ContentMain                                  hidden
    //       Buttons/ButtonRPG<Stat>BuyRecharge           kept as an ANCESTOR: BoxCollider disabled,
    //           Background, Label, Price/Label           hidden
    //           Price/DialogCoin                         KEPT, staged -> PriceCoin
    //       Buttons/ButtonRPG<Stat>Resume, -Missions/-Training   hidden
    // The walk below is generic (hide every subtree that holds neither kept root, disable colliders
    // on the kept roots' ancestors), so a layout difference between the dialogs cannot leak a widget.
    //
    // COLLIDERS: the bot carries none outside the inactive HelmetContainerEnemy, and DialogCoin none
    // at all; staging moves both subtrees to UIWidget3D (keepColliderLayers false), off every NGUI
    // event mask. The only live collider on the kept chain is the BUY RECHARGE button's BoxCollider,
    // the coin's ancestor — disabled while suppressed, because a live NGUI collider and toolkit
    // picking fire independently (c805b2d). The legacy bot is NOT draggable: Rotator is a
    // RotateObject auto-spinner (its drag hand-off is commented out) with no collider, so there is
    // no drag-rotate to preserve.
    //
    // LIGHT: both stages pass lightIntensity 0 and borrow the layer's light. These dialogs bind at
    // PRELOAD (level load) and a stage light is never toggled by visibility, so a light of their own
    // would be live the WHOLE round — four of them across Energy + Health — and, being directional
    // over the whole UIWidget3D layer, would stack onto the HUD coin (iter 13). The dialogs only open
    // from the in-round HUD, whose coin stage always carries a light (0.97), so borrowing is never
    // unlit; the header's coin light adds to it if the header view is still loaded.
    //
    // CPU: legacy deactivated the bot with panelContainer whenever the dialog was down. The kept
    // roots are toggled the same way here — active while the dialog is up or sliding out, inactive
    // after HidePanel — so the hidden dialog runs no spinner, no animation and no stage camera.
    // Only ONE dialog's bot is ever active, which also keeps the two dialogs' identically laid-out
    // bots out of each other's stage cameras (same layer, possibly the same parked position).
    //
    // Restored in FreeToolkitView (rule 116); re-asserted on every show and per frame while up.

    public const string elementCharacterStage = "CharacterStage";
    public const string elementPriceCoin = "PriceCoin";

    public const string legacyBotRootName = "RotatorContainer";
    public const string legacyCoinRootName = "DialogCoin";

    protected GameObject toolkitBotRoot;
    protected GameObject toolkitCoinRoot;

    protected UIRenderStageBinding toolkitBotStage;
    protected UIRenderStageBinding toolkitCoinStage;

    // Records of what the selective suppression changed, so FreeToolkitView puts back exactly that.
    private bool toolkitLegacySuppressed = false;
    private bool toolkitContainerWasActive = false;
    private bool toolkitBotWasActive = false;
    private bool toolkitCoinWasActive = false;
    private readonly List<GameObject> toolkitHiddenLegacy = new List<GameObject>();
    private readonly List<Collider> toolkitDisabledColliders = new List<Collider>();

    // Frames to wait after a show before re-fitting the bot camera: a just-activated Animation has
    // not sampled its pose yet, and the stage frames the BAKED pose.
    private int toolkitReframeFrames = 0;

    protected static Transform FindDeep(Transform parent, string childName) {

        if(parent == null) {
            return null;
        }

        for(int i = 0; i < parent.childCount; i++) {

            Transform child = parent.GetChild(i);

            if(child.name == childName) {
                return child;
            }

            Transform found = FindDeep(child, childName);

            if(found != null) {
                return found;
            }
        }

        return null;
    }

    protected virtual void ResolveToolkitLegacy3D() {

        if(panelContainer == null) {
            return;
        }

        if(toolkitBotRoot == null) {
            Transform t = FindDeep(panelContainer.transform, legacyBotRootName);
            toolkitBotRoot = t != null ? t.gameObject : null;
        }

        if(toolkitCoinRoot == null) {
            Transform t = FindDeep(panelContainer.transform, legacyCoinRootName);
            toolkitCoinRoot = t != null ? t.gameObject : null;
        }
    }

    private bool IsToolkitKeptRoot(Transform t) {
        return (toolkitBotRoot != null && t == toolkitBotRoot.transform)
            || (toolkitCoinRoot != null && t == toolkitCoinRoot.transform);
    }

    private bool HoldsToolkitKeptRoot(Transform t) {
        return (toolkitBotRoot != null && toolkitBotRoot.transform.IsChildOf(t))
            || (toolkitCoinRoot != null && toolkitCoinRoot.transform.IsChildOf(t));
    }

    // One-time walk (first suppression): hide every subtree that holds no kept root; on the kept
    // roots' ancestors, only disable colliders. Raw SetActive, NOT GameObject.Hide(): Hide also
    // disables every renderer below, and the restore must put back only what was changed.
    private void SuppressToolkitLegacyWalk(Transform node) {

        for(int i = 0; i < node.childCount; i++) {

            Transform child = node.GetChild(i);

            if(IsToolkitKeptRoot(child)) {
                continue;
            }

            if(HoldsToolkitKeptRoot(child)) {

                Collider[] colliders = child.GetComponents<Collider>();

                for(int c = 0; c < colliders.Length; c++) {

                    if(colliders[c].enabled) {
                        colliders[c].enabled = false;
                        toolkitDisabledColliders.Add(colliders[c]);
                    }
                }

                SuppressToolkitLegacyWalk(child);
                continue;
            }

            if(child.gameObject.activeSelf) {
                child.gameObject.SetActive(false);
                toolkitHiddenLegacy.Add(child.gameObject);
            }
        }
    }

    // Selective suppression, idempotent: the first call records, later calls re-assert the record
    // (legacy can re-show the container, e.g. a show that ran the NGUI path before the view landed).
    // Does NOT touch the kept roots' own active state — SetToolkitLegacy3DActive owns that.
    // Returns false when there is nothing 3D to keep (the caller falls back to hiding everything).
    protected virtual bool ApplyToolkitLegacySuppression() {

        if(panelContainer == null) {
            return false;
        }

        ResolveToolkitLegacy3D();

        if(toolkitBotRoot == null && toolkitCoinRoot == null) {
            return false;
        }

        if(!toolkitLegacySuppressed) {

            toolkitLegacySuppressed = true;
            toolkitContainerWasActive = panelContainer.activeSelf;
            toolkitBotWasActive = toolkitBotRoot != null && toolkitBotRoot.activeSelf;
            toolkitCoinWasActive = toolkitCoinRoot != null && toolkitCoinRoot.activeSelf;

            toolkitHiddenLegacy.Clear();
            toolkitDisabledColliders.Clear();

            SuppressToolkitLegacyWalk(panelContainer.transform);
        }
        else {

            for(int i = 0; i < toolkitHiddenLegacy.Count; i++) {

                GameObject go = toolkitHiddenLegacy[i];

                if(go != null && go.activeSelf) {
                    go.SetActive(false);
                }
            }

            for(int i = 0; i < toolkitDisabledColliders.Count; i++) {

                Collider c = toolkitDisabledColliders[i];

                if(c != null && c.enabled) {
                    c.enabled = false;
                }
            }
        }

        // Health's container is authored inactive, and the toolkit ShowPanel never shows it. Show()
        // (not SetActive) on purpose: a legacy panelContainer.Hide() disabled every renderer below,
        // and Show() is its inverse — the bot's meshes need them back.
        if(!panelContainer.activeSelf) {
            panelContainer.Show();
        }

        return true;
    }

    protected virtual void SetToolkitLegacy3DActive(bool active) {

        if(toolkitBotRoot != null && toolkitBotRoot.activeSelf != active) {
            toolkitBotRoot.SetActive(active);
        }

        if(toolkitCoinRoot != null && toolkitCoinRoot.activeSelf != active) {
            toolkitCoinRoot.SetActive(active);
        }
    }

    // Undo the record. SetActive is skipped while this panel is being deactivated or destroyed
    // (scene teardown; Unity refuses to (de)activate inside a hierarchy that is changing state) —
    // the record is kept, and OnEnable finishes the restore if the panel comes back legacy.
    protected virtual void RestoreToolkitLegacySuppression() {

        if(!toolkitLegacySuppressed) {
            return;
        }

        for(int i = 0; i < toolkitDisabledColliders.Count; i++) {

            if(toolkitDisabledColliders[i] != null) {
                toolkitDisabledColliders[i].enabled = true;
            }
        }

        toolkitDisabledColliders.Clear();

        if(this == null || !gameObject.activeInHierarchy) {
            return;
        }

        for(int i = 0; i < toolkitHiddenLegacy.Count; i++) {

            if(toolkitHiddenLegacy[i] != null) {
                toolkitHiddenLegacy[i].SetActive(true);
            }
        }

        toolkitHiddenLegacy.Clear();

        if(toolkitBotRoot != null) {
            toolkitBotRoot.SetActive(toolkitBotWasActive);
        }

        if(toolkitCoinRoot != null) {
            toolkitCoinRoot.SetActive(toolkitCoinWasActive);
        }

        if(panelContainer != null && panelContainer.activeSelf != toolkitContainerWasActive) {
            panelContainer.SetActive(toolkitContainerWasActive);
        }

        toolkitLegacySuppressed = false;

        // Re-resolve next time: a teardown may have rebuilt the tree.
        toolkitBotRoot = null;
        toolkitCoinRoot = null;
    }

    // From BindElements, once per built view (S1: never from a show/hide callback).
    protected virtual void SetupToolkitLegacy3D() {

        if(!ApplyToolkitLegacySuppression()) {
            return;
        }

        // Active for the Attach: the stage frames the content's posed mesh bounds at bind time.
        SetToolkitLegacy3DActive(true);

        if(toolkitBotRoot != null) {

            UIRenderStageBinding.Options o = new UIRenderStageBinding.Options();
            o.size = 512;               // fallback before layout; the element's pixel size decides
            o.framePadding = 1.15f;     // the header rig's tight crop: a bot has no particle spill
            o.followContent = true;     // Center may sit parked or be tweened; the camera follows
            o.keepColliderLayers = false;
            o.lightIntensity = 0f;      // borrow the layer light — see LIGHT above

            toolkitBotStage = StageLegacy3D(toolkitBotRoot, elementCharacterStage, o);
        }

        if(toolkitCoinRoot != null) {

            UIRenderStageBinding.Options o = new UIRenderStageBinding.Options();
            o.size = 128;
            o.maxSize = 128;            // a ~55-unit slot; 128 is the floor anyway
            o.framePadding = 1.3f;      // every staged coin's framing (header, HUD, products)
            o.followContent = true;
            o.keepColliderLayers = false;
            o.lightIntensity = 0f;

            toolkitCoinStage = StageLegacy3D(toolkitCoinRoot, elementPriceCoin, o);
        }

        SetToolkitLegacy3DActive(isVisible);

        if(isVisible) {
            toolkitReframeFrames = 2;
        }
    }

    protected override void SuppressLegacyView() {

        if(!ApplyToolkitLegacySuppression()) {
            base.SuppressLegacyView();
        }
    }

    protected override void FreeToolkitView() {

        // base first: UnstageAllLegacy3D detaches both stages (layers restored, RTs released)
        // before the widgets come back.
        base.FreeToolkitView();

        toolkitBotStage = null;
        toolkitCoinStage = null;
        toolkitReframeFrames = 0;

        RestoreToolkitLegacySuppression();
    }

    // The toolkit hide lands here when the slide ends (HideToolkitViewWhenSlideEnds), so the bot
    // stays in its RT for the whole slide and goes inactive with the view.
    public override void HidePanel() {

        base.HidePanel();

        if(isToolkitPanel && !isVisible && toolkitLegacySuppressed) {
            SetToolkitLegacy3DActive(false);
        }
    }

    // Per frame while up: allocation-free bool checks, re-assert only on a change.
    protected void UpdateToolkitLegacy3D() {

        if(!toolkitLegacySuppressed || !isToolkitPanel || !isVisible) {
            return;
        }

        if(panelContainer != null && !panelContainer.activeSelf) {
            ApplyToolkitLegacySuppression();
        }

        if((toolkitBotRoot != null && !toolkitBotRoot.activeSelf)
            || (toolkitCoinRoot != null && !toolkitCoinRoot.activeSelf)) {
            SetToolkitLegacy3DActive(true);
        }

        if(toolkitReframeFrames > 0 && --toolkitReframeFrames == 0) {

            if(toolkitBotStage != null && toolkitBotStage.isBound) {
                toolkitBotStage.stage.Reframe();
            }

            if(toolkitCoinStage != null && toolkitCoinStage.isBound) {
                toolkitCoinStage.stage.Reframe();
            }
        }
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

        // A restore FreeToolkitView could not finish (it ran while this panel was being disabled):
        // finish it now if the panel came back on the legacy path; the toolkit path re-suppresses.
        if(toolkitLegacySuppressed && !isToolkitPanel) {
            RestoreToolkitLegacySuppression();
        }

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

            // B9 S2: re-assert the selective suppression and bring the staged 3D up for this show.
            if(ApplyToolkitLegacySuppression()) {
                SetToolkitLegacy3DActive(true);
                toolkitReframeFrames = 2;
            }

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
        UpdateToolkitLegacy3D();
    }
}
