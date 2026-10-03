#define DEV
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Engine.Game.App;
using Engine.UI;
using Engine.Utility;

// THE COMMUNITY PANELS as toolkit views (B6): UIPanelCommunityShare, -Broadcast, -Camera and
// -Background, the four panel scripts inside the UICommunity prefab (a scene instance at the
// GameSceneDynamic root, drawn by its own CommunityCamera, depth 53, layer 29).
//
// These are NOT flow panels. None of them is ever AnimateIn'd (each overrides Start with Init, so
// UIPanelBase's AnimateIn/AnimateOut pair never brackets a "screen"); instead each one keeps a few
// CARDS parked off-screen (+-3000, or x 231.8 for the app-rate badge) and slides them onto local 0
// one at a time -- the share card, the action tools bar, the app-rate badge, the broadcast and
// photo dialogs, the dark scrim. So the view is hosted the other way round from every other
// converted panel:
//   * the VIEW ROOT is a full-screen, non-picking layer that stays shown while the view is loaded;
//     panel-level AnimateIn/AnimateOut neither slide nor hide it (UIPanelCommunityBase and
//     UIPanelCommunityBackground no-op the view slide and the toolkit HidePanel);
//   * every legacy card show/hide is MIRRORED onto the element of the same name (SetCard), with
//     the same edge, and replayed after the async load -- the legacy calls keep running untouched;
//   * the legacy subtree (each panel's own children) is suppressed once the view is up, re-asserted
//     every frame, and given back on free (kill switch);
//   * the view only draws while the legacy CommunityCamera would. GameSceneDynamic disables that
//     camera (m_Enabled 0 on the prefab instance), so on the shipping path the NGUI community UI
//     draws nothing -- not the app-rate badge on the main menu, not the share card on Results, not
//     the action tools bar InitPlatform shows at boot. A view that ignored the camera would add all
//     three to the live game. toolkitFollowsLegacyCamera keeps parity; flipping it is an owner call.
//
// Both base classes own one of these (UIPanelCommunityBackground derives from UIPanelBase, not
// UIPanelCommunityBase), so the seams live here once.
public class UIPanelCommunityToolkit {

    public enum CardEdge {
        Bottom,
        Top,
        Right
    }

    readonly UIPanelBase owner;

    public UIPanelCommunityToolkit(UIPanelBase owner) {
        this.owner = owner;
    }

    UIRef viewRoot {
        get {
            return owner.viewRoot;
        }
    }

    bool isToolkitPanel {
        get {
            return owner.isToolkitPanel;
        }
    }

    // Element names in panel-community-*.uxml are the legacy GameObject names, and they repeat
    // (every button has a Background and a Label), so writes go by PATH: "A/B" resolves B inside A,
    // each segment a deep first-match lookup -- the UIPanelModeTypeBase convention.
    public UIRef ResolvePath(string path) {

        if(!isToolkitPanel || string.IsNullOrEmpty(path)) {
            return UIRef.none;
        }

        UIRef current = viewRoot;

        foreach(string segment in path.Split('/')) {

            current = UIUtil.ResolveDeep(current, segment);

            if(current == null || !current.alive) {
                return UIRef.none;
            }
        }

        return current;
    }

    // ----------------------------------------------------------------------------------------
    // DRAW GATE

    // The legacy camera: CommunityCamera, an ancestor of every community panel. Looked up once.
    Camera legacyCamera;
    bool legacyCameraLooked = false;

    public bool followsLegacyCamera = true;

    public bool legacyCameraDraws {
        get {

            if(!legacyCameraLooked) {
                legacyCameraLooked = true;
                legacyCamera = owner.GetComponentInParent<Camera>(true);
            }

            // No camera above the panel (another product's layout): nothing to keep parity with.
            return legacyCamera == null || legacyCamera.isActiveAndEnabled;
        }
    }

    public bool draws {
        get {
            return !followsLegacyCamera || legacyCameraDraws;
        }
    }

    // ----------------------------------------------------------------------------------------
    // CARDS
    //
    // The legacy slides a card in (TweenUtil.ShowObject<Edge>) and out (HideObject<Edge>) without
    // ever deactivating it, so a hidden card stays parked and live. In the view a faded card would
    // still be PICKED, so a hidden card goes display:none once its slide ends; the token stops a
    // stale hide landing on a card shown again mid-slide.

    readonly Dictionary<string, bool> cardPending = new Dictionary<string, bool>();
    readonly Dictionary<string, CardEdge> cardEdges = new Dictionary<string, CardEdge>();
    readonly Dictionary<string, int> cardTokens = new Dictionary<string, int>();

    public bool IsCardShown(string path) {
        bool shown = false;
        cardPending.TryGetValue(path, out shown);
        return shown;
    }

    public void SetCard(string path, bool visible, CardEdge edge) {

        cardPending[path] = visible;
        cardEdges[path] = edge;

        int token = 0;
        cardTokens.TryGetValue(path, out token);
        cardTokens[path] = ++token;

        UIRef element = ResolvePath(path);

        if(element == null || !element.alive) {
            return;
        }

        if(visible) {
            UIUtil.ShowObject(element);
            SlideIn(element, edge);
            return;
        }

        float seconds = hideSeconds;

        if(seconds <= 0f || !owner.gameObject.activeInHierarchy) {
            UIUtil.HideObject(element);
            return;
        }

        SlideOut(element, edge);
        owner.StartCoroutine(HideCardWhenSlideEndsCo(path, token, seconds));
    }

    static void SlideIn(UIRef element, CardEdge edge) {

        if(edge == CardEdge.Top) {
            TweenUtil.ShowObjectTop(element, "panel-show");
        }
        else if(edge == CardEdge.Right) {
            TweenUtil.ShowObjectRight(element, "panel-show");
        }
        else {
            TweenUtil.ShowObjectBottom(element, "panel-show");
        }
    }

    static void SlideOut(UIRef element, CardEdge edge) {

        if(edge == CardEdge.Top) {
            TweenUtil.HideObjectTop(element, "panel-hide");
        }
        else if(edge == CardEdge.Right) {
            TweenUtil.HideObjectRight(element, "panel-hide");
        }
        else {
            TweenUtil.HideObjectBottom(element, "panel-hide");
        }
    }

    static float hideSeconds {
        get {
            Engine.Animation.TweenPreset preset = Engine.Animation.TweenPresets.Get("panel-hide");

            return preset.time + preset.delay;
        }
    }

    IEnumerator HideCardWhenSlideEndsCo(string path, int token, float seconds) {

        yield return new WaitForSecondsRealtime(seconds);

        int current = 0;
        cardTokens.TryGetValue(path, out current);

        if(current != token || !isToolkitPanel) {
            yield break;
        }

        UIUtil.HideObject(ResolvePath(path));
    }

    // Display only (a legacy GameObject.Show/Hide, no tween): buttons and sub-containers.
    readonly Dictionary<string, bool> visiblePending = new Dictionary<string, bool>();

    public void SetVisible(string path, bool visible) {

        visiblePending[path] = visible;

        UIRef element = ResolvePath(path);

        if(element == null || !element.alive) {
            return;
        }

        if(visible) {
            UIUtil.ShowObject(element);
        }
        else {
            UIUtil.HideObject(element);
        }
    }

    // Static-key text written at runtime (the broadcast status lines). Through SetLabelLocalized,
    // so a later language change re-applies it like the view's own @loc labels.
    readonly Dictionary<string, string> textKeyPending = new Dictionary<string, string>();

    public void SetLabelKey(string path, string key) {

        textKeyPending[path] = key;

        UIRef element = ResolvePath(path);

        if(element == null || !element.alive) {
            return;
        }

        UIUtil.SetLabelLocalized(element, key);
    }

    // ----------------------------------------------------------------------------------------
    // BIND / SHOW

    bool rootShown = false;
    bool rootDirty = true;

    // Something outside the gate touched the root's display (UIPanelBase.ShowPanel, a rebuilt
    // view): re-apply the gate on the next UpdateRoot whatever it last decided.
    public void InvalidateRoot() {
        rootDirty = true;
    }

    // From the owner's BindElements, every time a view is (re)built: a freed view comes back from
    // the UXML with the authored state, so every mirrored write is replayed -- cards WITHOUT the
    // slide (the legacy one already ran, or is running, off-screen).
    public void Bind() {

        rootDirty = true;

        foreach(KeyValuePair<string, bool> pair in visiblePending) {
            if(pair.Value) {
                UIUtil.ShowObject(ResolvePath(pair.Key));
            }
            else {
                UIUtil.HideObject(ResolvePath(pair.Key));
            }
        }

        foreach(KeyValuePair<string, bool> pair in cardPending) {
            if(pair.Value) {
                UIUtil.ShowObject(ResolvePath(pair.Key));
            }
            else {
                UIUtil.HideObject(ResolvePath(pair.Key));
            }
        }

        foreach(KeyValuePair<string, string> pair in textKeyPending) {
            UIUtil.SetLabelLocalized(ResolvePath(pair.Key), pair.Value);
        }
    }

    // LoadToolkitView's continuation hides the fresh view (the panel is never "visible" in the
    // UIPanelBase sense); the root is (re)shown here, on the next LateUpdate, and follows the draw
    // gate from then on.
    public void UpdateRoot() {

        if(!isToolkitPanel) {
            return;
        }

        bool draw = draws;

        if(draw == rootShown && !rootDirty) {
            return;
        }

        rootShown = draw;
        rootDirty = false;

        if(draw) {
            UIUtil.ShowObject(viewRoot);
        }
        else {
            UIUtil.HideObject(viewRoot);
        }
    }

    // ----------------------------------------------------------------------------------------
    // SUPPRESSION
    //
    // The panels have no panelContainer, so the default SuppressLegacyView hides nothing. What the
    // view replaces is the panel's own children (ContainerStatic / Container). Hide only what is
    // ACTIVE, track it, re-assert every frame, give back on free exactly what was taken (the
    // UIPanelModeTypeBase rule). The legacy card tweens and delayed show/hides keep running on the
    // inactive subtree without error, so the legacy state is current when it comes back.
    readonly List<GameObject> suppressed = new List<GameObject>();

    public void ReassertSuppression() {

        if(!isToolkitPanel) {
            return;
        }

        foreach(Transform t in owner.transform) {

            GameObject go = t.gameObject;

            if(!go.activeSelf) {
                continue;
            }

            go.Hide();

            if(!suppressed.Contains(go)) {
                suppressed.Add(go);
            }
        }
    }

    public void RestoreSuppressed() {

        foreach(GameObject go in suppressed) {
            if(go != null) {
                go.Show();
            }
        }

        suppressed.Clear();

        rootDirty = true;
    }

    // ----------------------------------------------------------------------------------------
    // MODE COLOURS
    //
    // The dialog frames carry UIColorModeTypeObject and the close buttons UIColorRPGEnergyObject:
    // both repaint at runtime per app mode (rule 113), and both stop once the legacy subtree is
    // suppressed. Same table as the RPG dialogs and the mode overlays.
    public void ApplyModeColors(string framePath, string closeBackgroundPath) {

        if(!isToolkitPanel) {
            return;
        }

        Color modeColor = UIPanelModeTypeBase.GetModeColor();

        if(!string.IsNullOrEmpty(framePath)) {
            UIUtil.SetSpriteColor(ResolvePath(framePath), modeColor);
        }

        AppModes modes = AppModes.Instance;

        if(modes == null || string.IsNullOrEmpty(closeBackgroundPath)) {
            return;
        }

        if(modes.isAppModeGameTraining || modes.isAppModeGameChallenge || modes.isAppModeGameArcade) {
            UIUtil.SetSpriteColor(ResolvePath(closeBackgroundPath), modeColor);
        }
    }
}
