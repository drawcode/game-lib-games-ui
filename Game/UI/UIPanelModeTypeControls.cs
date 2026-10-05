#define DEV
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Engine.Events;
using Engine.Game.App;
using Engine.UI;
using Engine.Utility;

// THE CONTROLS / HOW-TO-PLAY OVERLAY (B5) -- GameSceneDynamic .../GamePanelModes/
// GamePanelModeTypeControls, a sibling of the overview and quiz overlays.
//
// That scene object has NO panel script: it is a Container (authored inactive) holding an
// overview-style card -- TUTORIAL / HOW TO PLAY / READY! buttons, the info strip -- and two
// UIPanelTips ("app-content-state-game-arcade", "app-content-state-game-training-choice-quiz").
// Nothing in the code base references it, so the legacy screen is never shown.
//
// HOSTING WITHOUT A SCENE EDIT. Giving the object a panel component in the scene would be a
// scene edit; hosting the view from UIPanelTips would put a view on every UIPanelTips in the
// game (the overview and prepare overlays hold them too). So this panel is attached AT RUNTIME,
// on first use: ShowDefault() finds the object by name and adds the component, which then wires
// what the inspector would have (panelContainer) and runs the overlay family's hosting
// (UIPanelModeTypeBase). Until something calls ShowDefault it does not exist, so it changes
// nothing for a game that never shows this screen.
//
// The buttons broadcast their names like every view button; no handler listens for
// ButtonGameModeOverview* (none ever did), so a click is the same no-op it is in legacy. The 3D
// bots inside the tip pages (playerDisplay) stay legacy and are hidden with the container: they
// are the B9 "3D content inside a converted panel" case.
public class UIPanelModeTypeControls : UIPanelModeTypeBase {

    public static UIPanelModeTypeControls Instance;

    // The scene object this panel attaches to.
    public static string legacyObjectName = "GamePanelModeTypeControls";

    public override string toolkitViewKey {
        get {
            return BaseUIPanel.panelModeTypeControls;
        }
    }

    protected override string[][] toolkitModeColorTargets {
        get {
            return UIPanelModeTypeViews.controlsModeColorTargets;
        }
    }

    // ATTACH

    public static UIPanelModeTypeControls Attach() {

        if(Instance != null) {
            return Instance;
        }

        GameObject host = GameObject.Find(legacyObjectName);

        if(host == null) {
            return null;
        }

        UIPanelModeTypeControls panel = host.GetComponent<UIPanelModeTypeControls>();

        if(panel == null) {
            panel = host.AddComponent<UIPanelModeTypeControls>();
        }

        return panel;
    }

    public static bool isInst {
        get {
            if(Instance != null) {
                return true;
            }
            return false;
        }
    }

    public override void Awake() {

        // Runs inside AddComponent, before OnEnable (which preloads the view): wire the one field
        // the scene would have serialized.
        if(panelContainer == null) {
            Transform container = transform.Find("Container");

            if(container != null) {
                panelContainer = container.gameObject;
            }
        }

        base.Awake();

        if(Instance != null && this != Instance) {
            return;
        }

        Instance = this;
    }

    // NOT UIPanelBase.Start, whose AnimateOut would undo a ShowDefault made in the same frame as
    // the attach (Start runs a frame after AddComponent). The panel already starts hidden: the
    // container is authored inactive and the view loads hidden.
    public override void Start() {
        Init();
    }

    public override void OnButtonClickEventHandler(string buttonName) {

    }

    // SHOW/HIDE

    public static void ShowDefault() {

        UIPanelModeTypeControls panel = Attach();

        if(panel != null) {
            panel.AnimateIn();
        }
    }

    public static void HideAll() {
        if(isInst) {
            Instance.AnimateOut();
        }
    }

    public override void AnimateIn() {

        // A show straight after the attach finds the view still building. Decide on
        // isToolkitMigrated (sync), not isToolkitPanel (async): taking the legacy branch here
        // would light the NGUI container for a frame before the view arrives and suppresses it.
        // LoadToolkitView's continuation shows and slides the view because isVisible is set.
        if(isToolkitMigrated && !isToolkitPanel) {

            EnsureToolkitView();

            isVisible = true;

            return;
        }

        base.AnimateIn();

        // The legacy UIColorModeTypeObject components repaint on their first enable.
        ApplyViewModeColors();
    }

    // TIPS MIRROR
    //
    // The same job as UIPanelOverviewMode's "Tip n of m" mirror, for what this screen shows: which
    // UIPanelTips is up and which of its pages (GameObjectInactive "tip-1-start", "tip-2-mode") is
    // active. Read from the legacy objects' activeSelf -- the suppressed container makes
    // activeInHierarchy false for all of them -- and pushed only on change. Resolved once: these
    // are fixed scene objects, and a per-frame GetComponentsInChildren would allocate.
    private readonly List<KeyValuePair<string, GameObject>> tipsMirror = new List<KeyValuePair<string, GameObject>>();
    private readonly Dictionary<string, bool> tipsMirrorShown = new Dictionary<string, bool>();
    private bool tipsMirrorResolved = false;

    private void ResolveTipsMirror() {

        tipsMirrorResolved = true;

        foreach(UIPanelTips tips in GetComponentsInChildren<UIPanelTips>(true)) {

            tipsMirror.Add(new KeyValuePair<string, GameObject>(tips.name, tips.gameObject));

            foreach(GameObjectInactive page in tips.GetComponentsInChildren<GameObjectInactive>(true)) {
                tipsMirror.Add(new KeyValuePair<string, GameObject>(tips.name + "/" + page.name, page.gameObject));
            }
        }
    }

    protected override void UpdateToolkitView() {

        if(!tipsMirrorResolved) {
            ResolveTipsMirror();
        }

        for(int i = 0; i < tipsMirror.Count; i++) {

            KeyValuePair<string, GameObject> item = tipsMirror[i];

            if(item.Value == null) {
                continue;
            }

            bool shown = item.Value.activeSelf && IsTipsSetForCurrentState(item.Key);
            bool last;

            if(tipsMirrorShown.TryGetValue(item.Key, out last) && last == shown) {
                continue;
            }

            tipsMirrorShown[item.Key] = shown;
            SetViewVisible(item.Key, shown, false);
        }
    }

    // Both tips sets ("app-content-state-game-arcade", "...-training-choice-quiz") are active in the scene
    // with their tip-1-start page up, so mirroring activeSelf alone drew the two sets on top of each
    // other. UIPanelOverviewMode picks the set whose name matches AppContentStates.Current.code
    // (ShowTipsObjectMode); do the same here, falling back to the first set when none matches.
    // Legacy has no script on this screen to choose, and nothing ever showed it.
    private string tipsSetChosen = null;
    private string tipsSetChosenFor = null;

    private bool IsTipsSetForCurrentState(string mirrorKey) {

        string current = AppContentStates.Current != null ? AppContentStates.Current.code : "";

        if(tipsSetChosen == null || tipsSetChosenFor != current) {

            tipsSetChosenFor = current;
            tipsSetChosen = null;

            for(int i = 0; i < tipsMirror.Count; i++) {

                string key = tipsMirror[i].Key;

                if(key.IndexOf('/') >= 0) {
                    continue;
                }

                if(tipsSetChosen == null) {
                    tipsSetChosen = key;  // fallback: the first set
                }

                if(!string.IsNullOrEmpty(current) && key.Contains(current)) {
                    tipsSetChosen = key;
                    break;
                }
            }
        }

        int slash = mirrorKey.IndexOf('/');
        string set = slash >= 0 ? mirrorKey.Substring(0, slash) : mirrorKey;

        return set == tipsSetChosen;
    }

    protected override void FreeToolkitView() {

        tipsMirrorShown.Clear();
        tipsSetChosen = null;

        base.FreeToolkitView();
    }
}
