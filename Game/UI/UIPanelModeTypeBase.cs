#define DEV
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Engine.Events;
using Engine.UI;
using Engine.Utility;
using Engine.Game.App;

// THE IN-GAME MODE OVERLAYS as toolkit views (B5): UIPanelModeTypeChoice, UIPanelModeTypeTutorial,
// UIPanelModeTypeCollection and UIPanelModeTypeControls, the siblings of the converted
// UIPanelOverviewMode under GameSceneDynamic .../ContainerStatic/GamePanelModes. They take the same
// seams as that panel, for the same reasons, so the seams live here once:
//   * overlay band, preloaded (scene-resident), and the whole view slides from the BOTTOM like
//     the legacy AnimateInBottom cards;
//   * labels live in the #if NGUI branch, so the view is written BY ELEMENT PATH and every write
//     (and every card show/hide) is replayed after the async load;
//   * suppression is continuous and restored on free (kill switch).
//
// What differs from UIPanelOverviewMode is that these screens are CARD STACKS: the quiz panels
// carry four cards (PanelOverview / PanelDisplayItem / PanelResultItem / PanelResults) that the
// legacy parks off-screen and tweens in one at a time. Card names repeat their children's names
// (every card has a LabelOverviewTip, a LabelTitle, a Background...), so a bare ResolveDeep would
// find the FIRST card's element. Writes therefore go by PATH: "PanelResults/LabelOverviewStatus"
// resolves each segment inside the previous one.
public class UIPanelModeTypeBase : UIPanelBase {

    // Above the chrome band, like the overview and prepare overlays these sit with.
    public override int toolkitSortOrder {
        get {
            return UILayers.overlay;
        }
    }

    // Scene singletons under GamePanelModes, enabled at scene load: the preload case.
    public override bool toolkitPreloadView {
        get {
            return true;
        }
    }

    // The legacy cards enter from the bottom (AnimateInBottom(container...)).
    protected override void ShowToolkitViewSlide() {
        TweenUtil.ShowObjectBottom(viewRoot, toolkitShowPreset);
    }

    protected override void HideToolkitViewSlide() {
        TweenUtil.HideObjectBottom(viewRoot, toolkitHidePreset);
    }

    // PATHS

    // "A/B/C": B is resolved inside A, C inside B. Each segment is a deep (first-match) lookup, so
    // a path only needs the segments that disambiguate.
    protected UIRef ResolveViewPath(UIRef root, string path) {

        if(root == null || !root.alive || string.IsNullOrEmpty(path)) {
            return UIRef.none;
        }

        UIRef current = root;

        foreach(string segment in path.Split('/')) {

            current = UIUtil.ResolveDeep(current, segment);

            if(current == null || !current.alive) {
                return UIRef.none;
            }
        }

        return current;
    }

    // TEXT

    protected Dictionary<string, string> viewTextPending = new Dictionary<string, string>();

    protected virtual void SetViewLabel(string path, string value) {

        viewTextPending[path] = value;

        if(isToolkitPanel) {
            UIUtil.SetLabelValue(ResolveViewPath(viewRoot, path), value);
        }
    }

    // CARDS
    //
    // The legacy shows a card with AnimateInBottom and hides it with AnimateOutBottom -- a slide
    // and a fade, never a deactivation, so every hidden card stays parked and live. In the view a
    // faded card would still be PICKED (opacity 0 does not stop picking) and all four cards put
    // their advance button in the same place, so a hidden card is display:none once its slide
    // ends; the token stops a stale hide landing on a card shown again mid-slide.
    protected Dictionary<string, bool> viewVisiblePending = new Dictionary<string, bool>();

    private readonly Dictionary<string, int> viewVisibleTokens = new Dictionary<string, int>();

    protected virtual void SetViewVisible(string path, bool visible) {
        SetViewVisible(path, visible, true);
    }

    // slide false: display only, for content that fades rather than slides (tip pages).
    protected virtual void SetViewVisible(string path, bool visible, bool slide) {

        viewVisiblePending[path] = visible;

        int token = 0;
        viewVisibleTokens.TryGetValue(path, out token);
        viewVisibleTokens[path] = ++token;

        if(!isToolkitPanel) {
            return;
        }

        UIRef element = ResolveViewPath(viewRoot, path);

        if(element == null || !element.alive) {
            return;
        }

        if(visible) {
            UIUtil.ShowObject(element);

            if(slide) {
                TweenUtil.ShowObjectBottom(element, toolkitShowPreset);
            }

            return;
        }

        if(!slide || !gameObject.activeInHierarchy || toolkitHideSeconds <= 0f) {
            UIUtil.HideObject(element);
            return;
        }

        TweenUtil.HideObjectBottom(element, toolkitHidePreset);
        StartCoroutine(HideViewCardWhenSlideEndsCo(path, token));
    }

    IEnumerator HideViewCardWhenSlideEndsCo(string path, int token) {

        yield return new WaitForSecondsRealtime(toolkitHideSeconds);

        int current = 0;
        viewVisibleTokens.TryGetValue(path, out current);

        if(current != token || !isToolkitPanel) {
            yield break;
        }

        UIUtil.HideObject(ResolveViewPath(viewRoot, path));
    }

    // MODE COLOURS
    //
    // The cards' BackgroundColor and most of their titles carry UIColorModeTypeObject, which
    // REPAINTS them at runtime on every UIColors.UpdateColors() -- purple in training (where the
    // quiz runs), blue in challenge, orange in arcade, green otherwise -- so the authored colour
    // is not what renders (rule 113). The view keeps the authored colour as its default and the
    // same broadcast repaints the same elements here. Each entry is { scope path, element name };
    // the name is matched by UIUtil.SetTextColor, so it reaches every element under the scope
    // whose name contains it (the Controls backer is TWO BackgroundColor sprites).
    protected virtual string[][] toolkitModeColorTargets {
        get {
            return new string[0][];
        }
    }

    // UIColors.UpdateColor(container, colour) overrides the mode colour for one card (the result
    // card goes green or red) until the next UpdateColors -- the same lifetime here.
    protected Dictionary<string, Color> viewModeColorOverrides = new Dictionary<string, Color>();

    public static Color GetModeColor() {

        if(AppModes.Instance == null) {
            return UIColors.colorGreen;
        }

        if(AppModes.Instance.isAppModeGameTraining) {
            return UIColors.colorPurple;
        }
        else if(AppModes.Instance.isAppModeGameChallenge) {
            return UIColors.colorBlue;
        }
        else if(AppModes.Instance.isAppModeGameArcade) {
            return UIColors.colorOrange;
        }

        return UIColors.colorGreen;
    }

    protected virtual void ApplyViewModeColors() {

        if(!isToolkitPanel) {
            return;
        }

        Color modeColor = GetModeColor();

        foreach(string[] target in toolkitModeColorTargets) {

            Color colorTo = modeColor;

            foreach(KeyValuePair<string, Color> pair in viewModeColorOverrides) {
                if(target[0].StartsWith(pair.Key, StringComparison.Ordinal)) {
                    colorTo = pair.Value;
                }
            }

            UIUtil.SetTextColor(ResolveViewPath(viewRoot, target[0]), target[1], colorTo);
        }
    }

    protected virtual void SetViewModeColor(string scopePath, Color colorTo) {

        viewModeColorOverrides[scopePath] = colorTo;

        ApplyViewModeColors();
    }

    // UpdateColors is broadcast from ~20 call sites across every flow, and all of these panels keep
    // a preloaded view, so the repaint is skipped while nothing of this view is on screen. Every
    // legacy card show is followed by its own UpdateColors, so a card never shows stale.
    void OnColorsUpdateHandler() {

        viewModeColorOverrides.Clear();

        if(!isVisible && !viewVisiblePending.ContainsValue(true)) {
            return;
        }

        ApplyViewModeColors();
    }

    // BIND

    public override void BindElements(UIRef root) {

        base.BindElements(root);

        foreach(KeyValuePair<string, string> pair in viewTextPending) {
            UIUtil.SetLabelValue(ResolveViewPath(root, pair.Key), pair.Value);
        }

        // Replayed WITHOUT the slide: the view itself slides in as one unit.
        foreach(KeyValuePair<string, bool> pair in viewVisiblePending) {

            UIRef element = ResolveViewPath(root, pair.Key);

            if(pair.Value) {
                UIUtil.ShowObject(element);
            }
            else {
                UIUtil.HideObject(element);
            }
        }

        ApplyViewModeColors();
    }

    // SUPPRESSION
    //
    // panelContainer ("Container" under the panel root) holds every legacy widget and is AUTHORED
    // INACTIVE on all four overlays; the legacy ShowPanel activates it. The toolkit ShowPanel
    // never does, so once the view is up the container only comes back on if something else
    // activates it -- the first show racing the async load, or a kill-switch flip. Hence the
    // overview rule: hide it only when it is ACTIVE, track it, re-assert every frame, and give
    // back on free exactly what was taken. A blind Show() on free would light up a legacy screen
    // that was never visible.
    private readonly List<GameObject> suppressedLegacy = new List<GameObject>();

    protected override void SuppressLegacyView() {
        // NOT base: that hides panelContainer unconditionally and never gives it back.
        ReassertLegacySuppression();
    }

    protected void ReassertLegacySuppression() {

        if(!isToolkitPanel || panelContainer == null || !panelContainer.activeSelf) {
            return;
        }

        panelContainer.Hide();

        if(!suppressedLegacy.Contains(panelContainer)) {
            suppressedLegacy.Add(panelContainer);
        }
    }

    protected override void FreeToolkitView() {

        foreach(GameObject go in suppressedLegacy) {
            if(go != null) {
                go.Show();
            }
        }

        suppressedLegacy.Clear();

        base.FreeToolkitView();
    }

    // EVENTS

    public override void OnEnable() {

        base.OnEnable();

        Messenger.AddListener(UIColorsMessages.uiColorsUpdate, OnColorsUpdateHandler);
    }

    public override void OnDisable() {

        base.OnDisable();

        Messenger.RemoveListener(UIColorsMessages.uiColorsUpdate, OnColorsUpdateHandler);
    }

    // LateUpdate, not Update: Choice and Tutorial already declare a public Update, which would
    // hide one declared here.
    protected virtual void UpdateToolkitView() {
    }

    public virtual void LateUpdate() {

        if(isToolkitPanel) {
            ReassertLegacySuppression();
            UpdateToolkitView();
        }
    }
}

// Element paths into the B5 views (Resources/ui/views/panel-mode-type-*.uxml). Element names are
// the legacy GameObject names; a path names only the segments that disambiguate (see
// UIPanelModeTypeBase.ResolveViewPath). The quiz CARDS are shared by UIPanelModeTypeChoice,
// UIPanelModeTypeTutorial and UIPanelModeTypeCollection -- the three scene subtrees are the same
// cards (names, text, colours and geometry relative to their anchor match exactly).
public class UIPanelModeTypeViews {

    public static string cardOverview = "PanelOverview";
    public static string cardDisplayItem = "PanelDisplayItem";
    public static string cardResultItem = "PanelResultItem";
    public static string cardResults = "PanelResults";

    // Where each serialized label field of UIPanelModeTypeChoice/Tutorial points in the scene.
    // NOTE the scene wires labelOverviewTip/Type/Status to the RESULT ITEM card's labels, not the
    // overview card's (both panels, measured from the YAML) -- mirrored as wired.
    public static string labelOverviewTip = "PanelResultItem/LabelResultItemTip";
    public static string labelOverviewType = "PanelResultItem/LabelResultItemType";
    public static string labelOverviewStatus = "PanelResultItem/LabelResultItemStatus";
    public static string labelOverviewTitle = "PanelOverview/LabelTitle";
    public static string labelOverviewBlurb = "PanelOverview/LabelBlurb";
    public static string labelOverviewBlurb2 = "PanelOverview/LabelBlurb2";
    public static string labelOverviewNextSteps = "PanelOverview/LabelNextSteps";
    public static string buttonOverviewAdvance = "PanelOverview/ButtonChoiceAdvanceOverview";

    public static string labelDisplayItemTip = "PanelDisplayItem/LabelOverviewTip";
    public static string labelDisplayItemType = "PanelDisplayItem/LabelOverviewType";
    public static string labelDisplayItemStatus = "PanelDisplayItem/LabelOverviewStatus";
    public static string labelDisplayItemTitle = "PanelDisplayItem/LabelTitle";
    public static string labelDisplayItemAnswers = "PanelDisplayItem/LabelAnswers";
    public static string labelDisplayItemNote = "PanelDisplayItem/LabelNote";
    public static string labelDisplayItemQuestion = "PanelDisplayItem/LabelQuestion";
    public static string buttonDisplayItemAdvance = "PanelDisplayItem/ButtonChoiceAdvanceDisplayItem";

    public static string labelResultItemTip = "PanelResultItem/LabelResultItemTip";
    public static string labelResultItemType = "PanelResultItem/LabelResultItemType";
    public static string labelResultItemStatus = "PanelResultItem/LabelResultItemStatus";
    public static string labelResultItemChoiceDescription = "PanelResultItem/LabelChoiceResultDescription";
    public static string labelResultItemChoiceResultValue = "PanelResultItem/LabelChoiceResultValue";
    public static string labelResultItemChoiceResultType = "PanelResultItem/LabelChoiceResultType";
    public static string labelResultItemChoiceDisplayName = "PanelResultItem/LabelChoiceResultDisplayName";
    public static string labelResultItemNextSteps = "PanelResultItem/LabelNextSteps";
    public static string buttonResultItemAdvance = "PanelResultItem/ButtonChoiceAdvanceResultItem";

    public static string labelResultsTip = "PanelResults/LabelOverviewTip";
    public static string labelResultsType = "PanelResults/LabelOverviewType";
    public static string labelResultsStatus = "PanelResults/LabelOverviewStatus";
    public static string labelResultsTitle = "PanelResults/LabelRewardTitle";
    public static string labelResultsCoinsValue = "PanelResults/LabelRewardCoinsValue";
    public static string labelResultsScorePercentageValue = "PanelResults/LabelScorePercentage";
    public static string labelResultsScoreFractionValue = "PanelResults/LabelScoreFraction";
    public static string sliderScore = "PanelResults/SliderScore";
    public static string buttonResultsAdvance = "PanelResults/ButtonChoiceAdvanceResults";
    public static string buttonResultsReplay = "PanelResults/ButtonChoiceReplay";
    public static string buttonResultsModes = "PanelResults/ButtonChoiceModes";

    // The quiz elements that carry UIColorModeTypeObject in the scene: { scope, name }.
    // "LabelChoiceResult" deliberately matches the six result labels that carry it (and not
    // LabelNextSteps, which does not).
    public static string[][] quizModeColorTargets = new string[][] {
        new string[] { "PanelOverview/Backgrounds", "BackgroundColor" },
        new string[] { "PanelOverview/Actions", "LabelTitle" },
        new string[] { "PanelOverview/Actions", "LabelBlurb" },
        new string[] { "PanelDisplayItem/Backgrounds", "BackgroundColor" },
        new string[] { "PanelDisplayItem/Actions", "LabelTitle" },
        new string[] { "PanelDisplayItem/Actions", "LabelAnswers" },
        new string[] { "PanelDisplayItem/Actions", "LabelNote" },
        new string[] { "PanelDisplayItem/Actions", "LabelQuestion" },
        new string[] { "PanelResultItem/Backgrounds", "BackgroundColor" },
        new string[] { "PanelResultItem/Actions", "LabelChoiceResult" },
        new string[] { "PanelResults/Backgrounds", "BackgroundColor" },
        new string[] { "PanelResults/Coins", "LabelRewardCoinsValue" },
        new string[] { "PanelResults/ContainerReward", "LabelRewardTitle" },
        new string[] { "PanelResults/Actions", "LabelActionType" },
        new string[] { "PanelResults/Actions", "LabelBlurb" },
    };

    // GamePanelModeTypeControls: the two UIPanelTips it holds, and its mode-coloured elements.
    public static string[] controlsTips = new string[] {
        "app-content-state-game-arcade",
        "app-content-state-game-training-choice-quiz",
    };

    public static string[][] controlsModeColorTargets = new string[][] {
        new string[] { "PanelOverview/Backgrounds", "BackgroundColor" },
        new string[] { "PanelOverview/Actions", "LabelTitle" },
        new string[] { "PanelOverview/Actions", "LabelBlurb" },
        new string[] { "PanelOverview/Tips", "LabelBlurb" },
        new string[] { "PanelOverview/app-content-state-game-arcade/tip-1-start", "LabelTitle" },
        new string[] { "PanelOverview/app-content-state-game-training-choice-quiz/tip-1-start", "LabelTitle" },
    };
}
