using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Engine.UI;
using Engine.Utility;

// ==========================================================================================
// UI TOOLKIT (B11.1 -- the level editor seam)
//
// The three level-editor sheets that are UIAppPanels, NOT UIPanelBases (UIPanelEditAsset,
// UIPanelDialogEditMeta, UIPanelEditTools), share this one copy of the hand-rolled view
// lifecycle that UINotificationDisplayTip carries inline (EnsureToolkitView / LoadToolkitView /
// SuppressLegacyView / FreeToolkitView). It is COMPOSED, not inherited: core game-lib-* are
// additive-only and shared with other products, so the panels are not reparented -- each one
// owns an instance of this class and implements IUIPanelLevelEditorView.
//
// WHY THE EDITOR NEEDS ITS OWN SHAPE: in the shipping scene the whole editor is DARK
// (GameSceneDynamic overrides EditorContainer inactive and EditorCamera disabled, since 2019).
// The panel components therefore sit on INACTIVE GameObjects: no Awake, no OnEnable, no Start,
// and StartCoroutine throws. So nothing here depends on the panel being active -- the load is a
// backend callback, show/hide are backend + TweenUtil calls on the view, and the panels drive it
// from GameDraggableEditor (always active) through their public ShowEditorView/HideEditorView.
// The legacy container is NOT activated to get there: its NGUI colliders would come alive under
// the disabled EditorCamera and steal taps invisibly.
//
// ABSENT VIEW == LEGACY. A product with no Resources/ui/views/<key> gets view == null from the
// backend, the request is released for a later retry, and nothing else happens -- no suppress,
// no show. With the kill switch off (UIPlatform.toolkitViewsEnabled) no load is even requested.
// ==========================================================================================

public interface IUIPanelLevelEditorView {

    // The view key (Resources/ui/views/<key>). Empty means "stay on NGUI".
    string toolkitViewKey { get; }

    // Draw-order band (Engine.UI.UILayers). The editor is an overlay on a live round, above the
    // HUD; see each panel for its offset inside the band.
    int toolkitSortOrder { get; }

    // Which screen edge the view slides through: the toolbar/dialogs drop from the top like their
    // legacy AnchorTop parents, the asset sheet rises from the bottom like AnchorBottom.
    bool toolkitSlidesFromBottom { get; }

    string toolkitShowPreset { get; }

    string toolkitHidePreset { get; }

    // Runs once per loaded view, AFTER viewRoot is assigned and the legacy widgets are
    // suppressed, BEFORE the visibility match: resolve element refs, register the toggle/slider/
    // input change handlers, and REPLAY every write made while the view was still building.
    void OnToolkitViewReady(UIRef view);

    // Runs from Free BEFORE the view is destroyed (drop cached element refs).
    void OnToolkitViewFreed();
}

public class UIPanelLevelEditorView {

    readonly UIAppPanel panel;
    readonly IUIPanelLevelEditorView owner;

    // Set while a load is in flight, so a second Show does not kick a second LoadView while the
    // deferred PanelRenderer build is still pending; cleared by Free (the orphan check below).
    bool loadRequested = false;

    // What the game last ASKED for. The load lands whenever the PanelRenderer gets round to it,
    // so the continuation matches this rather than assuming "shown".
    bool wantVisible = false;

    // Bumped by every show and every hide, so a deferred display-none from an earlier hide can
    // not fire into a later show (the UIPanelBase.toolkitVisibilityToken idea).
    int visibilityToken = 0;

    readonly List<GameObject> suppressed = new List<GameObject>();

    public UIPanelLevelEditorView(UIAppPanel panel, IUIPanelLevelEditorView owner) {
        this.panel = panel;
        this.owner = owner;
    }

    public bool isLoaded {
        get {
            return panel != null && panel.viewRoot != null && panel.viewRoot.alive;
        }
    }

    public bool isShowing {
        get {
            return wantVisible;
        }
    }

    // UIRef.none until the view is up, so every UIUtil write on the result no-ops.
    public UIRef Resolve(string elementName) {

        if(!isLoaded || string.IsNullOrEmpty(elementName)) {
            return UIRef.none;
        }

        return UIUtil.ResolveDeep(panel.viewRoot, elementName);
    }

    public void Show() {

        wantVisible = true;
        visibilityToken++;

        if(isLoaded) {
            ShowLoaded();
            return;
        }

        Ensure();
    }

    public void Hide() {

        wantVisible = false;

        int token = ++visibilityToken;

        if(!isLoaded) {
            return;
        }

        if(owner.toolkitSlidesFromBottom) {
            TweenUtil.HideObjectBottom(panel.viewRoot, owner.toolkitHidePreset);
        }
        else {
            TweenUtil.HideObjectTop(panel.viewRoot, owner.toolkitHidePreset);
        }

        // Display none WITH the slide, not before it (UIPanelBase.HideToolkitViewWhenSlideEnds).
        // That needs a coroutine, and an inactive host cannot run one -- in the dark shipping
        // scene the view is simply left where the slide parks it: 720px off-screen and at alpha
        // 0, so it can neither be seen nor picked (IsPointerOverUI picks by on-screen position).
        if(panel.isActiveAndEnabled) {
            panel.StartCoroutine(HideWhenSlideEndsCo(token));
        }
    }

    IEnumerator HideWhenSlideEndsCo(int token) {

        Engine.Animation.TweenPreset preset = Engine.Animation.TweenPresets.Get(owner.toolkitHidePreset);

        yield return new WaitForSecondsRealtime(preset.time + preset.delay);

        if(token != visibilityToken || wantVisible || !isLoaded) {
            yield break;
        }

        UIUtil.HideObject(panel.viewRoot);
    }

    void ShowLoaded() {

        IUIBackend backend = UIPlatform.For(panel.viewRoot);

        if(backend != null) {
            backend.Show(panel.viewRoot);
        }

        if(owner.toolkitSlidesFromBottom) {
            TweenUtil.ShowObjectBottom(panel.viewRoot, owner.toolkitShowPreset);
        }
        else {
            TweenUtil.ShowObjectTop(panel.viewRoot, owner.toolkitShowPreset);
        }
    }

    void Ensure() {

        // Global kill switch: no view, no suppression, the legacy path untouched.
        if(!UIPlatform.toolkitViewsEnabled) {
            return;
        }

        string viewKey = owner.toolkitViewKey;

        if(isLoaded || string.IsNullOrEmpty(viewKey)) {
            return;
        }

        IUIBackend backend = UIPlatform.viewBackend;

        if(backend == null || loadRequested) {
            return;
        }

        loadRequested = true;

        backend.LoadView(viewKey, owner.toolkitSortOrder, (UIRef view) => {

            if(view == null || !view.alive) {
                // No view for this key in this product: stay on NGUI, and allow a later retry.
                loadRequested = false;
                return;
            }

            // Destroyed while the deferred build was pending (scene unload). Unity's overloaded
            // == is true for a destroyed object; the view is orphaned either way.
            if(panel == null) {
                backend.DestroyView(view);
                return;
            }

            // Freed while the deferred build was pending.
            if(!loadRequested) {
                backend.DestroyView(view);
                return;
            }

            panel.viewRoot = view;

            SuppressLegacyView();

            owner.OnToolkitViewReady(view);

            if(wantVisible) {
                ShowLoaded();
            }
            else {
                backend.Hide(view);
            }
        });
    }

    // Hides the panel's legacy widgets so they cannot draw (or take taps) under the view. Every
    // ACTIVE CHILD of the panel's own GameObject: the component sits on the sheet's root
    // (GameEditAsset, DialogMeta, GameEditTools), its widgets one level down (GameEdit,
    // DialogContainer, the backer sprite). The root itself stays as it is, so GameDraggableEditor's
    // legacy position tweens on it keep the open/closed state true for a later kill-switch flip.
    //
    // In the dark shipping scene everything here is already inactive in hierarchy, but activeSelf
    // is what is recorded and restored, so a product that shows the legacy editor gets the same
    // swap every other converted panel does.
    void SuppressLegacyView() {

        if(panel == null || suppressed.Count > 0) {
            return;
        }

        Transform root = panel.transform;

        for(int i = 0; i < root.childCount; i++) {

            GameObject child = root.GetChild(i).gameObject;

            if(!child.activeSelf) {
                continue;
            }

            suppressed.Add(child);
            child.SetActive(false);
        }
    }

    // Called from the panel's OnDisable/OnDestroy AND from GameDraggableEditor.OnDisable: a panel
    // that never became active never gets OnDisable/OnDestroy (Unity only sends them to objects
    // that were awake), so the always-active editor is the one place that sees the teardown of a
    // view built for a dark panel. Idempotent -- a second call finds nothing loaded.
    public void Free() {

        // Symmetric restore, so flipping UIPlatform.toolkitViewsEnabled back off returns a working
        // legacy sheet rather than an empty one.
        for(int i = 0; i < suppressed.Count; i++) {

            if(suppressed[i] != null) {
                suppressed[i].SetActive(true);
            }
        }

        suppressed.Clear();

        visibilityToken++;

        // ReferenceEquals, not Unity's ==: Free also runs on scene teardown, when the panel may
        // already read as destroyed while its managed viewRoot still holds a live view that must
        // be released rather than leaked.
        UIRef view = ReferenceEquals(panel, null) ? null : panel.viewRoot;

        if(view == null || !view.alive) {
            loadRequested = false;
            return;
        }

        owner.OnToolkitViewFreed();

        // Stop any in-flight slide before the VisualElement is detached, or the tween writes style
        // on a panel-less element.
        TweenUtil.Cancel(view);

        IUIBackend backend = UIPlatform.For(view);

        if(backend != null) {
            backend.DestroyView(view);
        }

        panel.viewRoot = UIRef.none;
        loadRequested = false;
    }
}
