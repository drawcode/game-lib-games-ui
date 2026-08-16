using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
#else
using UnityEngine.UI;
#endif

using Engine.Events;
using Engine.UI;
using Engine.Utility;
using Engine.Game.Data;
using Engine.Game.App;

public class BaseGameHUD : GameUIPanelBase {

    // 3H: the in-game HUD chrome. HYBRID — the toolkit view draws only the FLAT chrome (M.A.N. box,
    // the three stat bars, score/star/coin counts, timer, pause button, level label). These stay
    // LEGACY and must keep rendering underneath it:
    //   * the two input pads (AxisInput-move / AxisInput-attack) — input devices, not decoration;
    //   * the centre indicators and the two radial overlays — world-tracking markers;
    //   * the 3D coin mesh (Coins/HUDCoin) — real geometry, and there is no coin sprite to port to.
    // Geometry/colours in common.uss are MEASURED off a live level, not derived from the prefab.
    // See contexts/context-hud-3h-spec.md.
    public override string toolkitViewKey {
        get {
            return BaseUIPanel.panelHUD;
        }
    }

    // Always-on in-game chrome: above gameplay, but BELOW the overlay band so the pause dialog and
    // the loader/prepare overlay still draw over it.
    public override int toolkitSortOrder {
        get {
            return UILayers.chrome;
        }
    }

    public float currentTimeBlock = 0.0f;
    public float actionInterval = 1.0f;
    public AsyncOperation asyncLevelLoad = null;
    public bool levelLoadInProgress = false;
    public bool lastLevelLoadInProgress = false;

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
    public UILabel labelScores;
    public UILabel labelScore;
    public UILabel labelSpecials;
    public UILabel labelCoins;
    public UILabel labelLevel;
    public UILabel labelTime;
    public UIImageButton buttonCamera;
    public UIImageButton buttonGameSafety;
    public UIImageButton buttonGameSmarts;
    public UIImageButton buttonGameTutorial;
    public UIImageButton buttonGameTips;
    public UIImageButton buttonGameOverview;
    public UISlider sliderHealth;
    public UISlider sliderEnergy;
#else
    // 2.11: agnostic UIRef handles, bound at runtime by name.
    public Engine.UI.UIRef labelScores;
    public Engine.UI.UIRef labelScore;
    public Engine.UI.UIRef labelCoins;
    public Engine.UI.UIRef labelSpecials;
    public Engine.UI.UIRef labelLevel;
    public Engine.UI.UIRef labelTime;
    public Engine.UI.UIRef buttonCamera;
    public Engine.UI.UIRef buttonGameSafety;
    public Engine.UI.UIRef buttonGameSmarts;
    public Engine.UI.UIRef buttonGameTutorial;
    public Engine.UI.UIRef buttonGameTips;
    public Engine.UI.UIRef buttonGameOverview;
    public Engine.UI.UIRef sliderHealth;
    public Engine.UI.UIRef sliderEnergy;
#endif

    public GameObject containerUseObject;
    public GameObject containerSmartsObject;
    public GameObject containerSafetyObject;
    public GameObject containerScoreObject;
    public GameObject containerScoresObject;
    public GameObject containerCoinsObject;
    public GameObject containerSpecialsObject;
    public GameObject containerCameraObject;
    public GameObject containerDevObject;
    public GameObject containerTimeObject;
    public GameObject containerOverviewObject;
    public bool initialized = false;
    public GameObject overlayFogObject;
    public GameObject overlayRedObject;
    public GameObject overlayMagicObject;
    public GameObject overlayFilterObject;
    public GameObject containerCharacters;
    public GameObject containerPause;
    public GameObject containerDisplay;
    public GameObject containerControlsLeft;
    public GameObject containerControlsRight;
    public GameObject containerInputLeft;
    public GameObject containerInputRight;
    public GameObject containerCamera;
    public GameObject containerHealth;
    public GameObject containerEnergy;
    public GameObject containerOffscreenIndicators;
    public static GameHUD Instance;

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

        // check platform
        HandlePlatform();

        InitEvents();
    }

    public virtual void HandlePlatform() {

        HandleInput();
    }

    public virtual void HandleInput() {
        if(Context.Current.isWebGL || Application.isEditor) {
            // hide left virtual pad
            //HideInputLeftObject(.5f, 0f);
        }
        else {
            //ShowInputLeftObject(.5f, 0f);
        }
    }

    public override void OnEnable() {

        Messenger<string>.AddListener(ButtonEvents.EVENT_BUTTON_CLICK, OnButtonClickEventHandler);

        Messenger<string, string, object>.AddListener(GameMessages.gameActionItem, OnGameItem);

        Messenger<double>.AddListener(GameMessages.gameActionScore, OnGameShooterScore);
        Messenger<double>.AddListener(GameMessages.gameActionScores, OnGameShooterScores);
    }

    public override void OnDisable() {

        Messenger<string>.RemoveListener(ButtonEvents.EVENT_BUTTON_CLICK, OnButtonClickEventHandler);

        Messenger<string, string, object>.RemoveListener(GameMessages.gameActionItem, OnGameItem);

        Messenger<double>.RemoveListener(GameMessages.gameActionScore, OnGameShooterScore);
        Messenger<double>.RemoveListener(GameMessages.gameActionScores, OnGameShooterScores);

        // Chain to base so UIPanelBase.OnDisable -> FreeToolkitView runs when the HUD is put away,
        // NOTE (3H): also see SuppressLegacyView below — the chain is what restores the suppressed
        // legacy widgets and the ButtonGameSmartsShow collider.
        // else the toolkit view leaks once the HUD has one. 3H migration prerequisite (same fix the
        // settings/header/footer bases got in 3A/3B and the ten list bases got in 3D). The HUD is
        // put away often — HUDCamera deactivates at level end — so without this every level would
        // leak a PanelRenderer.
        //
        // OnEnable is deliberately NOT chained, matching those same panels: UIPanelBase.OnEnable
        // re-adds EVENT_BUTTON_CLICK -> OnButtonClickEventHandler, which this class already
        // subscribes above, so chaining it would fire every HUD button click TWICE. The listener
        // removals below/above are idempotent, so chaining only OnDisable is safe.
        base.OnDisable();
    }

    public override void OnButtonClickEventHandler(string buttonName) {
        if(UIUtil.IsButtonClicked(buttonCamera, buttonName)) {
            LogUtil.Log("Button camera Clicked: " + buttonName);
            if(!AppModes.Instance.isAppModeGameTraining) {
                ChangeCameraMode();
            }
        }
        else if(UIUtil.IsButtonClicked(buttonGameOverview, buttonName)) {
            LogUtil.Log("Button camera Clicked: " + buttonName);
            GameController.GameContentDisplay(GameContentDisplayTypes.gameModeContentOverview);
        }
        else if(UIUtil.IsButtonClicked(buttonGameSafety, buttonName)) {
            LogUtil.Log("Button camera Clicked: " + buttonName);
            GameController.GameContentDisplay(GameContentDisplayTypes.gameHealth);
        }
        else if(UIUtil.IsButtonClicked(buttonGameSmarts, buttonName)) {
            LogUtil.Log("Button camera Clicked: " + buttonName);
            GameController.GameContentDisplay(GameContentDisplayTypes.gameEnergy);
        }
        else if(UIUtil.IsButtonClicked(buttonGameTips, buttonName)) {
            LogUtil.Log("Button camera Clicked: " + buttonName);
            GameController.GameContentDisplay(GameContentDisplayTypes.gameTips);
        }
        else if(UIUtil.IsButtonClicked(buttonGameTutorial, buttonName)) {
            LogUtil.Log("Button camera Clicked: " + buttonName);
            GameController.GameContentDisplay(GameContentDisplayTypes.gameTutorial);
        }
    }

    public virtual void ChangeCameraMode() {
        GameController.CycleGameCameraMode();
    }

    public virtual void OnGameItem(string code, string type, object val) {
        //if(type == GamePlayerItemType.itemCoin) {            
        //    SetCoins(GameController.CurrentGamePlayerController.runtimeData.coins);
        //}
        //else if(type == GamePlayerItemType.itemHealth) {            
        //        //SetHealth(GameController.CurrentGamePlayerController.runtimeData.health);
        //}
    }

    public virtual void OnGameShooterScore(double score) {
        SetScore(GameController.CurrentGamePlayerController.runtimeData.score);
    }

    public virtual void OnGameShooterScores(double scores) {
        SetScores(GameController.CurrentGamePlayerController.runtimeData.scores);
    }

    public virtual void InitEvents() {

        LogUtil.Log("InitEvents:");
    }

    public virtual void LateUpdate() {

    }

    public virtual void ResetIndicators() {
        if(containerOffscreenIndicators != null) {
            containerOffscreenIndicators.DestroyChildren();
        }

        GameZoneGoalMarker marker = GameZoneGoalMarker.GetMarker();
        marker.UpdateIndicator();
    }

    public virtual void Show() {

    }

    public virtual void Hide() {

    }

    public virtual void Reset() {

    }

    // 3H VISIBILITY GATE
    //
    // showHUD() -> GameHUD.AnimateIn() fires EARLY — before the prepare/tips and mode-overview
    // screens are done. Under NGUI that was harmless: the legacy HUD painted UNDERNEATH those
    // overlays in the camera stack. A toolkit view composites ABOVE the entire NGUI stack, so the
    // same early show drew the HUD chrome on top of the READY screen (user, 2026-08-01).
    //
    // So the chrome is gated on the level actually RUNNING, not on AnimateIn. Update() reconciles
    // every frame, which also covers the reverse case (level ends / quits -> chrome goes away)
    // without needing a hook on every transition.
    protected override void ShowToolkitViewSlide() {

        // Not running yet (prepare, tips, mode overview): stay hidden. Update() reveals it the
        // moment gameplay starts.
        //
        // INSTANT hide, not the animated one. LoadToolkitView's continuation already called
        // backend.Show on the view before this runs, so animating OUT here made the chrome visibly
        // slide in and straight back out (user, 2026-08-01). HideObject sets display:none in the
        // same frame, so it never paints.
        if(!GameController.IsGameRunning) {
            UIUtil.HideObject(viewRoot);
            return;
        }

        // Same reason as in SyncToolkitChromeVisibility: the slide cannot undo a display:none.
        UIUtil.ShowObject(viewRoot);

        base.ShowToolkitViewSlide();
    }

    private bool toolkitChromeShown;

    // Gated PURELY on game state — do not reintroduce a "was Show requested" flag.
    //
    // The first attempt tracked intent in a toolkitChromeWanted flag set from ShowToolkitViewSlide.
    // That left the HUD permanently blank (user, 2026-08-01: "blank other than model/bot"), because
    // ShowToolkitViewSlide is never reached on the path that matters: the HUD's isVisible is
    // already true by the time the view finishes loading, so UIPanelBase.AnimateIn(time, delay)
    // early-returns at its `if(isVisible) return;` and the slide call never happens. The flag stayed
    // false forever, the view stayed display:none, and the only things left on screen were the
    // legacy 3D bits (bot + staged coin) that suppression deliberately keeps.
    //
    // IsGameRunning alone is the correct signal: while the HUD GameObject is active it is False
    // through prepare/tips/overview and True in gameplay. At the menu the HUD is inactive, so this
    // never runs there (and OnDisable has already freed the view).
    private void SyncToolkitChromeVisibility() {

        if(!isToolkitPanel) {
            return;
        }

        bool shouldShow = GameController.IsGameRunning;

        if(shouldShow == toolkitChromeShown) {
            return;
        }

        toolkitChromeShown = shouldShow;

        // The 3D bits travel WITH the chrome. Suppression deliberately keeps the bot rig and the
        // coin mesh rendering (they have no sprite equivalent), but they are part of the HUD — so
        // when the chrome hides for pause/prepare they must go too. Without this the bot stayed on
        // screen over the pause menu after the rest of the HUD had gone (user, 2026-08-02).
        SetLegacyHudVisualsVisible(shouldShow);

        if(shouldShow) {
            // RESTORE DISPLAY FIRST. TweenUtil's slides deliberately "do NOT touch display/active
            // state" (TweenUtil.cs: "gate learning #1: tweens never own visibility"), so after the
            // hide below set display:none, ShowToolkitViewSlide would animate an element that is
            // still display:none — invisible forever. That is what left the HUD showing nothing but
            // the legacy 3D bot and staged coin across two rounds.
            UIUtil.ShowObject(viewRoot);

            // Animate IN once, when gameplay actually begins.
            base.ShowToolkitViewSlide();
        }
        else {
            // Instant, for the same reason as above: this path also runs on the frames before the
            // level starts, and an animated hide there is exactly the in-then-out flicker.
            UIUtil.HideObject(viewRoot);
        }
    }

    // 3H SUPPRESSION
    //
    // The default UIPanelBase.SuppressLegacyView hides the whole panelContainer, which is WRONG
    // here: it would take the input pads, the indicators and the 3D coin down with it. So this
    // hides only the flat chrome the toolkit view replaces, cluster by cluster.

    // What we hid, so FreeToolkitView can put it all back for the kill switch.
    private readonly List<GameObject> suppressedLegacy = new List<GameObject>();

    // ButtonGameSmartsShow wraps a 3D player rig, so it can only be suppressed by hiding its FLAT
    // children — which leaves its own collider live. See below.
    private Collider smartsButtonCollider;
    private bool smartsButtonColliderWasEnabled;

    private const string hudTopLeft = "HUDContainer/AnchorTopLeft/TopLeft/Toolbar/DisplayObjectLeft/";
    private const string hudTopRight = "HUDContainer/AnchorTopRight/TopRight/Toolbar";

    // Every flat cluster the toolkit view replaces. Resolved once, then RE-ASSERTED every frame —
    // see ReassertLegacySuppression.
    private static readonly string[] legacyClusterPaths = {
        hudTopRight,                                            // pause/overview/level/camera/edit
        hudTopLeft + "MAN",                                     // M.A.N. face box (flat)
        hudTopLeft + "Character/Container",                     // the three stat bars
        hudTopLeft + "Score",
        hudTopLeft + "Scores",
        hudTopLeft + "Time",
        // COINS: flat children only — Coins/HUDCoin is a 3D mesh and must keep rendering.
        hudTopLeft + "Coins/BackgroundWhite",
        hudTopLeft + "Coins/LabelCoins",
        hudTopLeft + "Coins/Labelx",
        // ButtonGameSmartsShow wraps the 3D bot, so only its flat children can go.
        hudTopLeft + "Character/ButtonGameSmartsShow/Label",
        hudTopLeft + "Character/ButtonGameSmartsShow/Background"
    };

    private Transform[] legacyClusters;

    private void ResolveLegacyClusters() {

        if(legacyClusters != null) {
            return;
        }

        legacyClusters = new Transform[legacyClusterPaths.Length];

        for(int i = 0; i < legacyClusterPaths.Length; i++) {
            legacyClusters[i] = transform.Find(legacyClusterPaths[i]);
        }
    }

    // Suppression must be CONTINUOUS, not one-shot.
    //
    // The first version hid each cluster once, in SuppressLegacyView, and skipped anything that was
    // not active at that instant. On a level RESTART the game re-activates these clusters AFTER the
    // view has loaded (Reset/SetLevelInit/Show run again), so the skipped ones came back and stayed
    // — the legacy HUD rendering permanently on top of the toolkit HUD. That is the reported
    // "doubles on restart, persists" (user, 2026-08-02).
    //
    // Re-asserting each frame is cheap (11 cached transforms, no Find) and is robust to the game
    // re-showing a cluster at any point in the level lifecycle.
    // The two legacy 3D pieces the toolkit view cannot replace: the bot rig inside
    // ButtonGameSmartsShow, and the staged coin mesh. They render OUTSIDE the toolkit view, so
    // hiding the view does not hide them — they need to be toggled with it explicitly.
    //
    // The coin is drawn into a RenderTexture and shown through the view's IconCoin element, so the
    // element itself goes with the view; SetVisible stops the stage camera rendering too rather
    // than leaving it running for a texture nobody is showing.
    private void SetLegacyHudVisualsVisible(bool visible) {

        ResolveLegacyClusters();

        Transform smarts = transform.Find(hudTopLeft + "Character/ButtonGameSmartsShow");

        if(smarts != null && smarts.gameObject.activeSelf != visible) {

            if(visible) {
                smarts.gameObject.Show();
            }
            else {
                smarts.gameObject.Hide();
            }
        }

        if(hudCoinStage != null) {
            hudCoinStage.SetVisible(visible);
        }
    }

    private void ReassertLegacySuppression() {

        ResolveLegacyClusters();

        for(int i = 0; i < legacyClusters.Length; i++) {

            Transform t = legacyClusters[i];

            if(t == null || !t.gameObject.activeSelf) {
                continue;
            }

            t.gameObject.Hide();

            // Only track it once, so the restore list cannot grow across re-activations.
            if(!suppressedLegacy.Contains(t.gameObject)) {
                suppressedLegacy.Add(t.gameObject);
            }
        }

        // Same story for the smarts collider: if the game re-enables it mid-level it would start
        // stealing taps again. Re-disable without clobbering the ORIGINAL state kept for restore.
        if(smartsButtonCollider != null && smartsButtonCollider.enabled) {
            smartsButtonCollider.enabled = false;
        }
    }

    protected override void SuppressLegacyView() {

        // THE COLLIDER HAZARD, captured before the first sweep so the ORIGINAL enabled state is kept
        // for restore. ButtonGameSmartsShow contains a whole 3D player rig, so only its flat
        // children can be hidden — which leaves the BUTTON's own collider live and pickable while
        // nothing renders there. That is precisely the bug that made a suppressed header coin button
        // swallow every top-right tap (games-ui c805b2d). FreeToolkitView restores it.
        Transform smarts = transform.Find(hudTopLeft + "Character/ButtonGameSmartsShow");

        if(smarts != null) {

            smartsButtonCollider = smarts.GetComponent<Collider>();

            if(smartsButtonCollider != null) {
                smartsButtonColliderWasEnabled = smartsButtonCollider.enabled;
            }
        }

        // Everything else is the per-frame sweep — see ReassertLegacySuppression for why this is
        // not a one-shot.
        ReassertLegacySuppression();

        SetupHudCoinStage();
    }

    // The green FPS readout was a legacy UILabel owned by FPSDisplay, which the toolkit HUD now
    // draws over. Rather than give FPSDisplay (game-lib-games) a dependency on the HUD view, the
    // HUD PULLS the value here — FPSDisplay.GetCurrentFPS() is a public static and Update() already
    // runs every frame. Colour thresholds mirror the legacy lerp: green, yellow under 27, red
    // under 10.
    //
    // NOTE: this ties the readout to the HUD, so it only shows during gameplay. Legacy behaved the
    // same in the captures, but if it is wanted on menu screens too it needs its own always-on view.
    private string lastFpsText;
    private bool fpsLegacyHidden;

    // The legacy FPS label lives in the SCENE (GameSceneDynamic), not in HUDTemplate — verified:
    // the prefab has zero FPSDisplay components. So the cluster-by-cluster suppression above cannot
    // reach it and it would double-draw under the toolkit one. Hide it through the singleton
    // instead, which works wherever the scene puts it. Done lazily because FPSDisplay.Instance may
    // not exist yet when SuppressLegacyView runs.
    private void SuppressLegacyFpsLabel() {

        if(fpsLegacyHidden || !FPSDisplay.isInst) {
            return;
        }

        if(FPSDisplay.Instance.labelFPS == null) {
            return;
        }

        fpsLegacyHidden = true;

        GameObject go = FPSDisplay.Instance.labelFPS.gameObject;

        if(go.activeSelf) {
            go.Hide();
            suppressedLegacy.Add(go);
        }
    }

    private void UpdateToolkitFps() {

        if(!isToolkitPanel || !toolkitChromeShown) {
            return;
        }

        SuppressLegacyFpsLabel();

        float fps = FPSDisplay.GetCurrentFPS();
        string text = string.Format("{0:F2} FPS", fps);

        // Only touch the element when the string actually changes — this runs every frame.
        if(text == lastFpsText) {
            return;
        }

        lastFpsText = text;

        UIUtil.UpdateLabelObject(viewRoot, "LabelFPS", text);

        Color color = Color.green;

        if(fps < 10f) {
            color = Color.red;
        }
        else if(fps < 27f) {
            color = Color.yellow;
        }

        UIUtil.SetLabelColor(UIUtil.ResolveDeep(viewRoot, "LabelFPS"), color);
    }

    private Engine.UI.UIRenderStage hudCoinStage;

    // The HUD coin rendered DIM and hard to read, because in place it is a raw 3D mesh lit by
    // whatever the level's lighting happens to be (user, 2026-08-01: "needs to be unlit or better
    // visible, make it like the coin on the default header"). The header already solved this:
    // UIRenderStage moves the mesh to a dedicated widget layer with its own camera and renders it
    // to a RenderTexture, which the toolkit view then draws as a plain image — consistent and
    // unaffected by scene lighting. Same treatment here, same framing (128px RT, 1.3 headroom).
    private void SetupHudCoinStage() {

        if(hudCoinStage != null) {
            return;
        }

        Transform coin = transform.Find(hudTopLeft + "Coins/HUDCoin");

        if(coin == null) {
            return;
        }

        int layer = LayerMask.NameToLayer("UIWidget3D");

        if(layer < 0) {
            layer = LayerMask.NameToLayer("UI3D");
        }

        hudCoinStage = Engine.UI.UIRenderStage.Attach(coin.gameObject, layer, 128, 1.3f);

        if(hudCoinStage != null) {
            UIUtil.SetImageTexture(UIUtil.ResolveDeep(viewRoot, "IconCoin"), hudCoinStage.texture);
        }
    }

    // Symmetric restore, so flipping UIPlatform.toolkitViewsEnabled back off returns a working
    // legacy HUD rather than a half-hidden one.
    protected override void FreeToolkitView() {

        // Detach() puts the mesh back on its original layer — kill-switch safe.
        if(hudCoinStage != null) {
            hudCoinStage.Detach();
            hudCoinStage = null;
        }


        for(int i = 0; i < suppressedLegacy.Count; i++) {

            if(suppressedLegacy[i] != null) {
                suppressedLegacy[i].Show();
            }
        }

        suppressedLegacy.Clear();

        // Re-resolve on the next suppression: if a level teardown destroyed and rebuilt any of these
        // children, the cached Transforms would be stale.
        legacyClusters = null;

        // Let the legacy FPS label be re-hidden if the view is loaded again.
        fpsLegacyHidden = false;
        lastFpsText = null;

        // MUST reset: this tracks the visibility of a view that no longer exists. Leaving it true
        // across a teardown makes SyncToolkitChromeVisibility early-return on the NEXT level —
        // shouldShow(true) == toolkitChromeShown(true) — against a freshly loaded view that
        // LoadToolkitView's continuation left hidden, giving a blank HUD from the second level on.
        toolkitChromeShown = false;

        if(smartsButtonCollider != null) {
            smartsButtonCollider.enabled = smartsButtonColliderWasEnabled;
            smartsButtonCollider = null;
        }

        base.FreeToolkitView();
    }

    public virtual void SetLevelInit(GameLevel gameLevel) {

        if(gameLevel != null) {
            //GameController.Instance.runtimeData.ammo = gameLevel.ammo;            
            //GameController.Instance.runtimeData.score = 0;

            SetCoins(0);
            SetScore(0);
            SetScores(0);
            SetSpecials(0);
            SetLevel(gameLevel.code);
        }

    }

    // The label* fields are legacy UILabel handles, NOT UIRef, so BindElements cannot rebind them
    // to the toolkit view (same situation as the worlds panel's title/description). The toolkit
    // branch therefore writes BY ELEMENT NAME instead. The legacy write still runs underneath: the
    // widget is suppressed, so it costs nothing and keeps the kill-switch path correct.

    public virtual void SetScore(double score) {

        string value = score.ToString("N0");

        if(isToolkitPanel) {
            UIUtil.UpdateLabelObject(viewRoot, "LabelScore", value);
        }

        UIUtil.SetLabelValue(labelScore, value);
    }

    public virtual void SetScores(double scores) {

        string value = scores.ToString("N0");

        if(isToolkitPanel) {
            UIUtil.UpdateLabelObject(viewRoot, "LabelScoresValue", value);
        }

        UIUtil.SetLabelValue(labelScores, value);
    }

    public virtual void SetCoins(double coins) {

        string value = coins.ToString("N0");

        if(isToolkitPanel) {
            UIUtil.UpdateLabelObject(viewRoot, "LabelCoins", value);
        }

        UIUtil.SetLabelValue(labelCoins, value);
    }

    public virtual void SetSpecials(double specials) {
        // No toolkit element: the specials counter is not part of the 3H chrome (it does not render
        // in the legacy HUD capture either).
        UIUtil.SetLabelValue(labelSpecials, specials.ToString("N0"));
    }

    public virtual void SetLevel(string levelName) {

        if(isToolkitPanel) {
            UIUtil.UpdateLabelObject(viewRoot, "LabelLevel", levelName);
        }

        UIUtil.SetLabelValue(labelLevel, levelName);
    }

    public virtual void SetTime(double time) {

        string value = FormatUtil.GetFormattedTimeMinutesSecondsMsSmall(time);

        if(isToolkitPanel) {
            UIUtil.UpdateLabelObject(viewRoot, "LabelTime", value);
        }

        UIUtil.SetLabelValue(labelTime, value);
    }

    public virtual void ShowHitOne() {
        //LogUtil.Log("ShowHitOne");

        DeviceUtil.Vibrate();

        //HideOverlayRed(.1f, 0f, 0f);
        ShowOverlayRed(.2f, .1f, 0f, .4f);
        HideOverlayRed(1, .2f, .4f, 0f);
    }

    public virtual void ShowHitOne(float modifier) {
        //LogUtil.Log("ShowHitOne");

        DeviceUtil.Vibrate();

        //HideOverlayRed(.1f, 0f, 0f);
        ShowOverlayRed(.2f, .1f, 0f, .4f * modifier);
        HideOverlayRed(1, .2f, .4f * modifier, 0f);
    }

    public virtual void ShowOverlayRed() {
        ///ShowOverlayRed(.3f, .1f, 1f);
    }

    public virtual void ShowOverlayRed(float time, float delay, 
        float amountFrom, float amountTo) {

        TweenUtil.FadeToObject(overlayRedObject, amountTo, time, delay);
    }

    public virtual void HideOverlayRed() {
        HideOverlayRed(.1f, .2f, 0f, 0f);
    }

    public virtual void HideOverlayRed(float time, float delay, 
        float amountFrom, float amountTo) {

        TweenUtil.FadeToObject(overlayRedObject, amountTo, time, delay);
    }

    //

    public virtual void ShowCharacterObject(float time = .5f, float delay = .55f) {
        TweenUtil.ShowObject(containerCharacters, Vector3.zero.WithY(leftOpenX));
    }

    public virtual void HideCharacterObject(float time = .5f, float delay = 0f) {
       TweenUtil.ShowObject(containerCharacters, Vector3.zero.WithY(leftClosedX));
    }

    public virtual void ShowDisplayObject(float time = .5f, float delay = .55f) {

        TweenUtil.ShowObject(containerDisplay, 
            Vector3.zero.WithY(topOpenY),
            TweenCoord.local, true,
            time, delay);
    }

    public virtual void HideDisplayObject(float time = .5f, float delay = 0f) {

        TweenUtil.HideObject(containerDisplay, 
            Vector3.zero.WithY(topClosedY),
            TweenCoord.local, true,
            time = .5f, delay = .55f);
    }

    public virtual void ShowOverviewObject(float time = .5f, float delay = .55f) {

        TweenUtil.ShowObject(containerOverviewObject, 
            Vector3.zero.WithY(topOpenY),
            TweenCoord.local, true,
            time, delay);
    }

    public virtual void HideOverviewObject(float time = .5f, float delay = 0f) {

        TweenUtil.HideObject(containerOverviewObject, 
            Vector3.zero.WithY(topClosedY),
            TweenCoord.local, true,
            time, delay);
    }

    public virtual void ShowPauseObject(float time = .5f, float delay = .55f) {

        TweenUtil.ShowObject(containerPause, 
            Vector3.zero.WithX(rightOpenX), 
            TweenCoord.local, true,
            time, delay);
    }
    

    public virtual void HidePauseObject(float time = .5f, float delay = 0f) {

        TweenUtil.HideObject(containerPause,
            Vector3.zero.WithY(rightClosedX),
            TweenCoord.local, true,
            time, delay);
    }

    public virtual void ShowInputLeftObject(float time = .5f, float delay = .55f) {

        TweenUtil.ShowObject(containerInputLeft,
            Vector3.zero.WithX(leftOpenX),
            TweenCoord.local, true,
            time, delay);
    }

    public virtual void HideInputLeftObject(float time = .5f, float delay = 0f) {

        TweenUtil.HideObject(containerInputLeft,
            Vector3.zero.WithY(leftClosedX),
            TweenCoord.local, true,
            time, delay);
    }

    public virtual void ShowInputRightObject(float time = .5f, float delay = .55f) {

            TweenUtil.MoveToObject(containerInputRight, Vector3.zero.WithX(rightOpenX), time, delay);
    }

    public virtual void HideInputRightObject(float time = .5f, float delay = 0f) {

        TweenUtil.MoveToObject(containerInputRight, Vector3.zero.WithX(rightClosedX), time, delay);
    }

    public virtual void ShowControlsLeftObject(float time = .5f, float delay = .55f) {

        TweenUtil.MoveToObject(containerControlsLeft, Vector3.zero.WithX(leftOpenX), time, delay);
    }

    public virtual void HideControlsLeftObject(float time = .5f, float delay = 0f) {

        TweenUtil.MoveToObject(containerControlsLeft, Vector3.zero.WithX(leftClosedX), time, delay);
    }

    public virtual void ShowControlsRightObject(float time = .5f, float delay = .55f) {

        TweenUtil.MoveToObject(containerControlsRight, Vector3.zero.WithX(rightOpenX), time, delay);
    }

    public virtual void HideControlsRightObject(float time = .5f, float delay = 0f) {

        TweenUtil.MoveToObject(containerControlsRight, Vector3.zero.WithX(rightClosedX), time, delay);
    }

    public virtual void ShowEditState() {
        ShowCharacterObject();
        ShowPauseObject();
        HideDisplayObject();
        ShowControlsLeftObject();
        HideControlsRightObject();

        HandlePlatform();
    }

    public virtual void ShowGameState() {
        ShowCharacterObject();
        ShowPauseObject();
        ShowDisplayObject();
        ShowControlsLeftObject();
        ShowControlsRightObject();

        HandlePlatform();
    }

    public override void AnimateIn() {

        base.AnimateIn();

        HandleItems();

        if(GameDraggableEditor.isEditing) {
            ShowEditState();
        }
        else {
            ShowGameState();
        }

        if(AppModes.Instance.isAppModeGameTraining) {
            containerUseObject.Show();

            containerSmartsObject.Hide();
            containerSafetyObject.Hide();
            containerScoreObject.Hide();
            containerScoresObject.Hide();
            containerCoinsObject.Hide();
            containerSpecialsObject.Hide();
            containerCameraObject.Hide();
            containerTimeObject.Hide();
            containerOverviewObject.Hide();
        }
        else {
            containerUseObject.Hide();

            containerSmartsObject.Show();
            containerSafetyObject.Show();
            containerScoreObject.Show();
            containerScoresObject.Show();
            containerCoinsObject.Show();
            containerSpecialsObject.Show();
            containerCameraObject.Show();
            containerTimeObject.Show();
            containerOverviewObject.Show();
        }

        //HideOverlayRed();
    }

    public virtual void HandleItems() {

        // Handle by world

        string codeWorld = GameWorlds.Current.code;

        if(codeWorld.IsNullOrEmpty()) {
            return;
        }

        foreach(GameObjectInactive container in gameObject.GetList<GameObjectInactive>()) {

            if(container.type.IsEqualLowercase(BaseDataObjectKeys.display_items)) {

                foreach(GameObjectInactive item in container.gameObject.GetList<GameObjectInactive>()) {

                    if(item.type.IsEqualLowercase(BaseDataObjectKeys.display_item)) {
                        item.gameObject.HideChildren();
                    }
                }

                container.gameObject.Show();

                foreach(GameObjectData dataItem in container.gameObject.GetList<GameObjectData>()) {

                    Dictionary<string, object> data = dataItem.ToDictionary();

                    string val = data.Get<string>(BaseDataObjectKeys.world);

                    if(val.IsEqualLowercase(codeWorld)) {

                        dataItem.gameObject.Show();
                    }
                }
            }
        }
    }

    public virtual void AnimateInOverlayDamage() {

        base.AnimateIn();

        ShowOverlayRed();
    }

    public override void AnimateOut() {

        base.AnimateOut();

        //HideOverlayRed();
    }

    public virtual void Update() {

        SyncToolkitChromeVisibility();

        // Legacy suppression is re-asserted every frame a toolkit view OWNS the HUD — deliberately
        // NOT gated on the chrome being visible. The invariant is "if the toolkit view exists, the
        // legacy flat chrome stays hidden": during prepare/tips the toolkit chrome is intentionally
        // hidden, and if the game re-activated a cluster in that window the LEGACY HUD would flash
        // there — the exact window that is meant to stay clear.
        if(isToolkitPanel) {
            ReassertLegacySuppression();
        }

        UpdateToolkitFps();
        /*
        var ry = 0f;
        //var rx = 0f;
        if(Context.Current.isMobile) {
            ry =-Input.acceleration.y + Screen.height/2;
            //rx =-Input.acceleration.x + Screen.width/2;
        }
        else {
            ry =-Input.mousePosition.y + Screen.height/2;
            //rx =-Input.acceleration.x + Screen.width/2;
        }
        
        if(overlayRedObject != null) {
            //overlayRedObject.transform.Rotate(Vector3.forward * (ry * .005f) * Time.deltaTime);
        }
        */

        if(GameController.IsGameRunning) {
            if(GameController.CurrentGamePlayerController != null) {
                if(GameController.Instance != null) {
                    if(GameController.Instance.runtimeData != null) {
                        SetScore(GameController.CurrentGamePlayerController.runtimeData.score);
                        SetScores(GameController.CurrentGamePlayerController.runtimeData.scores);
                        SetCoins(GameController.CurrentGamePlayerController.runtimeData.coins);
                        SetSpecials(GameController.CurrentGamePlayerController.runtimeData.specials);
                        SetTime(GameController.Instance.runtimeData.timeRemaining);
                    }
                }
            }
        }

        if(Application.isEditor) {
            if(Input.GetKeyDown(KeyCode.P)) {
                ShowHitOne();
            }
        }
    }
}

/*
 * 
 * 
 * using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public enum GameHUDState {
    GAME_ENTER,
    GAME_PREPARE,
    GAME_COUNTDOWN_1,
    GAME_COUNTDOWN_2,
    GAME_COUNTDOWN_3,
    GAME_START,
    GAME_FINISH,
    GAME_EXIT
}

public enum GameHudCameraState {
    CAMERA_FIXED_DEFAULT,
    CAMERA_FIXED_TIGHT,
    CAMERA_LOOK_AHEAD,
    CAMERA_FOLLOW_DEFAULT,
    CAMERA_FOLLOW_CLOSE
}

public class GameCameraMode {
    public string displayName = "Follow Cam";
    public CameraMode cameraMode = CameraMode.Follow;
}

public class GameHUD : GameObjectBehavior {
    
    public static GameHUD Instance;
    
    public bool destroyed = false;
    
    public GameHUDState hudState = GameHUDState.GAME_ENTER;
    public int countdownStepCompleted = 0;
    
    public GameObject hudContainer;
    
    public AssetBundle bundle; // level asset bundle if loaded
    
    public Vector3 panelPauseObjectInitialPosition;
    public Vector3 seriesInfoObjectInitialPosition;
    public Vector3 goObjectInitialPosition;
    public Vector3 goObjectInitialScale;
    public Vector3 countdown1ObjectInitialPosition;
    public Vector3 countdown1ObjectInitialScale;
    public Vector3 countdown2ObjectInitialPosition;
    public Vector3 countdown2ObjectInitialScale;
    public Vector3 countdown3ObjectInitialPosition;
    public Vector3 countdown3ObjectInitialScale;
    public Vector3 finishObjectInitialPosition;
    
    public Vector3 quitButtonInitialPosition;
    
    public GameObject panelPauseObject;
    public GameObject panelPauseObjectBackground;
    public GameObject overlayFadeObject;
    public GameObject overlayFadeInfoObject;
    public GameObject hudOutputObject;
    public GameObject inputsObject;
    public GameObject tipsObject;
    public GameObject seriesInfoObject;
    public GameObject seriesInfoObjectBackground;
    
    
    public GameObject panelTransitionObject;
    public GameObject countdown1Object;
    public GameObject countdown2Object;
    public GameObject countdown3Object;
    public GameObject goObject;
    public GameObject finishObject;
    
    public UIButton inputLeft;
    public UIButton inputRight;
        
    public UIButton buttonHitLeft;
    public UIButton buttonHitRight;
    
    public UIButton buttonPause;
    public UIButton buttonPauseHitArea;
        
    public UIButton buttonQuit;
    public UIButton buttonResume;
    public UIButton buttonRestart;
    
    public UIButton buttonCameraSwitcher;
    public UILabel labelCameraSwitcher;
    
    public UILabel labelTime;
    public UILabel labelLapCurrent;
    public UILabel labelLapTotal;
    public UILabel labelLapPlace;   
    
    public UILabel labelName;
    public UILabel labelSponsor;
    public UILabel labelLaps;
    public UILabel labelPlace;
    public UILabel labelTouchToSkip;
    public GameObject labelTouchToSkipObject;
    
    public UILabel labelRaceMode;
    
    public bool pauseExpanded = false;  
    
    public float currentTotalTime = 0f;
    public int currentPosition = 1;
    public int lastCurrentPosition = -1;
    
    float currentTimeBlockLocal = 0.0f;
    float actionIntervalLocal = 1.0f;   
    
    float currentTimeBlockMicro = 0.0f;
    float actionIntervalMicro = 0.3f;   
    
    GameObject legacyLeftButton;
    GameObject legacyRightButton;
    int limitOnLegacyCheck = 500;
    
    bool touchInputEnabled = true;
    bool lastTouchInputEnabled = false;
    
    bool initialized = false;
        
    float lastEventStartTime = 0f;
    float lastEventFinishTime = 0f;
    float lastEventLapTime = 0f;
    float lastEventResetTime = 0f;
    float lastEventBoostTime = 0f;
    float lastEventPassTime = 0f;
    float lastEventPassedTime = 0f;
    float lastEventCollisionTime = 0f;
    float lastEventCauseCollisionTime = 0f;
    
    float lastSoundBoost = 0f;
    float lastSoundCheer = 0f;
    float lastSoundBoo = 0f;
    float lastSoundBikeJump = 0f;
    float lastSoundBikeRev = 0f;
    float lastSoundBikeRace = 0f;
    float lastSoundCheerConstant = 0f;
    
    VehicleBehaviourScript humanVehicle;
    VehicleSounds humanVehicleSounds;
    VehicleStats humanVehicleStats;
    
    VehicleStatsData humanVehicleStatsData;
    
    
    CameraBehavior humanVehicleCameraBehavior;
    public List<GameCameraMode> cameraModes;
    public int currentSelectedCameraMode = 0;
    public GameCameraMode currentCameraMode;
    
    public bool raceActive = false;
    public bool raceQuit = false;
    
    UIButtonMeta buttonMeta;
    float currentTimeBlock = 0.0f;
    float actionInterval = 1.0f;    
    
    public AsyncOperation asyncLevelLoad = null;
    public bool levelLoadInProgress = false;
    public bool lastLevelLoadInProgress = false;
    
    float soundDelayModifier = 1f;
    
    bool hasCustomAudioCrowdCheer = false;
    bool hasCustomAudioCrowdJump = false;
    bool hasCustomAudioCrowdBoo = false;
    bool hasCustomAudioBikeJumping = false;
    bool hasCustomAudioBikeRacing = false;
    bool hasCustomAudioBikeRevving = false;
    
    void Awake() {
        if (Instance != null && this != Instance) {
            //There is already a copy of this script running
            //Destroy(this);
            return;
        }
    
        Instance = this;
        
        DontDestroyOnLoad(gameObject);
        
        Init();
    }
    
    public void Init() {
        
        
        humanVehicleStatsData = new VehicleStatsData();
        
        soundDelayModifier = 1f;
#if UNITY_IPHONE        
        if(iPhone.generation == iPhoneGeneration.iPad1Gen
            || iPhone.generation == iPhoneGeneration.iPhone3GS) {
            soundDelayModifier = soundDelayModifier * 1.2f;
        }
                
#endif
        panelPauseObjectInitialPosition = panelPauseObject.transform.position;  
        seriesInfoObjectInitialPosition = seriesInfoObject.transform.position;
        goObjectInitialPosition = goObject.transform.position;
        goObjectInitialScale = goObject.transform.localScale;
        countdown1ObjectInitialPosition = countdown1Object.transform.position;
        countdown2ObjectInitialPosition = countdown2Object.transform.position;
        countdown3ObjectInitialPosition = countdown3Object.transform.position;
        
        countdown1ObjectInitialScale = countdown1Object.transform.localScale;
        countdown2ObjectInitialScale = countdown2Object.transform.localScale;
        countdown3ObjectInitialScale = countdown3Object.transform.localScale;
        
        finishObjectInitialPosition = finishObject.transform.position;
        
        quitButtonInitialPosition = buttonQuit.transform.localPosition;
        labelTouchToSkipObject = labelTouchToSkip.gameObject;
        
        InitObjects();
        
        InitEvents();       
        
        Hide();
    } 
    
    void InitObjects() {
        
        Tweens.Instance.FadeToObject(inputsObject, 0f, 0f, 0f);     
        Tweens.Instance.FadeToObject(countdown1Object, 0f, 0f, 0f);
        Tweens.Instance.FadeToObject(countdown2Object, 0f, 0f, 0f);
        Tweens.Instance.FadeToObject(countdown3Object, 0f, 0f, 0f);
        Tweens.Instance.FadeToObject(goObject, 0f, 0f, 0.0f);
        Tweens.Instance.FadeToObject(finishObject, 0f, 0f, 0.0f);
        Tweens.Instance.FadeToObject(labelTouchToSkipObject, 1f, 0f, 0f);
        Tweens.Instance.FadeToObject(seriesInfoObject, 1f, 0f, 0f);
        Tweens.Instance.FadeToObject(seriesInfoObjectBackground, .5f, 0f, .05f);
        Tweens.Instance.FadeToObject(panelPauseObject, 1f, 0f, 0f);
        Tweens.Instance.FadeToObject(panelPauseObjectBackground, .5f, 0f, 0f);
        //FadeToObject(overlayFadeObject, 0f, .5f, .1f);
        Tweens.Instance.FadeToObject(hudOutputObject, 0f, 0f, 0f);
    }
        
    void InitEvents() {
        
        buttonMeta.SetButton("buttonPause", ref buttonPause, delegate () {  
            TogglePausePanel();
        });
        
        buttonMeta.SetButton("buttonPauseHitArea", ref buttonPauseHitArea, delegate () {    
            TogglePausePanel();
        });
        
        buttonMeta.SetButton("buttonResume", ref buttonResume, delegate () {    
            TogglePausePanel();
        });
        
        buttonMeta.SetButton("buttonRestart", ref buttonRestart, delegate () {  
            TogglePausePanel();
            RestartRace();
        });
        
        buttonMeta.SetButton("buttonQuit", ref buttonQuit, delegate () {
            TogglePausePanel();     
            GameDatas.Current.lastRaceQuit = true;  
            Invoke("QuitRace", 1.1f);
        });
        
        buttonMeta.SetButton("buttonCameraSwitcher", ref buttonCameraSwitcher, delegate () {    
            NextCameraMode();
        });
        
        ShowOrHideInputs();
    }
    
    public void ResetCameraMode() {
        humanVehicleCameraBehavior = null;  
        FindCameraBehavior();
        int currentCameraModeSaved = GameProfiles.Current.GetCurrentCameraMode();
        if(currentCameraModeSaved > cameraModes.Count - 1) {
            currentCameraModeSaved = 0;
        }
        SelectCamera(currentCameraModeSaved);
    }
    
    public void Reset() {
        
        seriesInfoObject.SetActiveRecursively(true);
        panelPauseObject.SetActiveRecursively(true);
        
        ResetCameraMode();
        
        InitObjects();
                
        panelPauseObject.transform.position = panelPauseObjectInitialPosition;  
        seriesInfoObject.transform.position = seriesInfoObjectInitialPosition;  
        goObject.transform.position = goObjectInitialPosition;  
        goObject.transform.localScale = goObjectInitialScale;   
        countdown1Object.transform.position = countdown1ObjectInitialPosition;  
        countdown1Object.transform.localScale = countdown1ObjectInitialScale;
        countdown2Object.transform.position = countdown2ObjectInitialPosition;
        countdown2Object.transform.localScale = countdown2ObjectInitialScale;   
        countdown3Object.transform.position = countdown3ObjectInitialPosition;      
        countdown3Object.transform.localScale = countdown3ObjectInitialScale;
        finishObject.transform.position = finishObjectInitialPosition;  
        buttonQuit.transform.localPosition = quitButtonInitialPosition;         
    }
    
    public void Show() {    
        hudContainer.SetActiveRecursively(true);
        initialized = true;
        limitOnLegacyCheck = 500;
        ShowOrHideInputs();
        Reset();
        LoadSeriesInfo();   
        Tweens.Instance.MoveFromObject(seriesInfoObject,
                                       new Vector3(seriesInfoObject.transform.position.x,
                                              seriesInfoObject.transform.position.y - 9,
                                             seriesInfoObject.transform.position.z), .5f, 0f);
    }
    
    public void Hide() {
        initialized = false;        
        Tweens.Instance.FadeToObject(inputsObject, 0f, 0f, 0f); 
        
        if(GameLoadingObject.Instance != null) {
            GameLoadingObject.Instance.ShowAndHideLoadingHelp();
        }
        hudContainer.SetActiveRecursively(false);
    }
    
    public void PrepareCurrentLevel() {
                
        PrepareAndLoadLevel();  
                
        CheckTouchInputState();
    }
    
    public void StartLoadedLevel() {
        if(RaceManagerScript.Instance != null) {
            RaceManagerScript.Instance.raceEnabled = true;
        }
    }
        
    public void LoadSeriesInfo() {
        
        string raceModeName = "Series Event Mode";
        string place = "";
        string laps = "";
        string sponsorName = "";
        string levelName = "";
                    
        if(GameDatas.Current.IsRaceModeArcade()) {
            raceModeName = "Arcade Mode";
            place = "";
            laps = "Difficulty: " + GameDatas.Current.currentDifficultyValue.ToString("P");
            sponsorName = GamePacks.Instance.GetById(GameLevels.Current.pack[0]).display_name;
            levelName = GameLevels.Current.display_name;                
        }
        else if (GameDatas.Current.IsRaceModeEndless()) {
            raceModeName = "Endless Mode";
            place = "";
            laps = "Difficulty: " + GameDatas.Current.currentDifficultyValue.ToString("P");
            sponsorName = GamePacks.Instance.GetById(GameLevels.Current.pack[0]).display_name;
            levelName = GameLevels.Current.display_name;        
        }
        else { //(GameDatas.Current.IsRaceModeSeries()) {
            raceModeName = "Series Event Mode";
            place = "";
            laps = "";
            sponsorName = "";
            levelName = ""; 
            
            GameSeriesEvent seriesEvent = GameSeriesEvents.Instance.GetCurrentEvent();
            
            if(seriesEvent != null) {
                string stageName = "Qualifier";
                if(seriesEvent.stage == GameSeriesEventStage.SEMI_FINAL) {
                    stageName = "Semi-final";
                }
                else if(seriesEvent.stage == GameSeriesEventStage.FINAL) {
                    stageName = "Final";
                }
                raceModeName = raceModeName + " " + stageName;
                
                LogUtil.LogAlways("Environment:" + seriesEvent.environmentName);
                
                levelName = GameLevels.Instance.GetById(seriesEvent.environmentName).display_name;
                laps = GameDatas.Current.currentTotalLaps.ToString() + " Laps";
                sponsorName = seriesEvent.sponsor;
            
                int placeInt = GameSeriesEvents.Instance.GetPlaceByPoints(seriesEvent.minimumScore);
                place = GameSeriesEvents.Instance.GetPrettyPlace(placeInt) + " Required";
            }
        }
            
        if(labelName != null) {
            labelName.text = levelName;
        }

        if(labelLaps != null) {
            labelLaps.text = laps;
        }
        
        if(labelSponsor != null) {
            labelSponsor.text = sponsorName;
        }
        
        if(labelPlace != null) {
            labelPlace.text = place;
        }
            
        if(labelRaceMode) {
            labelRaceMode.text = raceModeName;
        }
    }
    
    public void ChangeState(GameHUDState stateTo) {
        hudState = stateTo;
    }   
    
    void OnEnable() {
        //LogUtil.Log("GameRaceHUD::OnEnable");
        
        Messenger.AddListener(GamePlayerMessages.EventRacePrepare, OnEventRacePrepare);
        Messenger.AddListener(GamePlayerMessages.EventRaceStart, OnEventRaceStart);
        Messenger.AddListener(GamePlayerMessages.EventRaceFinish, OnEventRaceFinish);
        Messenger.AddListener(GamePlayerMessages.EventRaceVehicleBoost, OnEventRaceVehicleBoost);
        Messenger.AddListener(GamePlayerMessages.EventRaceVehicleReset, OnEventRaceVehicleReset);
        
        Messenger.AddListener(GamePlayerMessages.EventRaceVehicleCollision, OnEventRaceVehicleCollision);
        Messenger.AddListener(GamePlayerMessages.EventRaceVehicleCauseCollision, OnEventRaceVehicleCauseCollision);
        Messenger.AddListener(GamePlayerMessages.EventRaceVehiclePass, OnEventRaceVehiclePass);
        Messenger.AddListener(GamePlayerMessages.EventRaceVehiclePassed, OnEventRaceVehiclePassed);
        
        Messenger<int>.AddListener(GamePlayerMessages.EventRaceCountdown, OnEventRaceCountdown);
        Messenger<int>.AddListener(GamePlayerMessages.EventRaceLapEvent, OnEventRaceLapEvent);
        
        // TODO add wreck/bump/mud
        
    }
    
    void OnDisable() {
        //LogUtil.Log("GameRaceHUD::onDisable");
        
        Messenger.RemoveListener(GamePlayerMessages.EventRacePrepare, OnEventRacePrepare);
        Messenger.RemoveListener(GamePlayerMessages.EventRaceStart, OnEventRaceStart);
        Messenger.RemoveListener(GamePlayerMessages.EventRaceFinish, OnEventRaceFinish);
        Messenger.RemoveListener(GamePlayerMessages.EventRaceVehicleBoost, OnEventRaceVehicleBoost);
        Messenger.RemoveListener(GamePlayerMessages.EventRaceVehicleReset, OnEventRaceVehicleReset);
        
        Messenger.RemoveListener(GamePlayerMessages.EventRaceVehicleCollision, OnEventRaceVehicleCollision);
        Messenger.RemoveListener(GamePlayerMessages.EventRaceVehicleCauseCollision, OnEventRaceVehicleCauseCollision);
        Messenger.RemoveListener(GamePlayerMessages.EventRaceVehiclePass, OnEventRaceVehiclePass);
        Messenger.RemoveListener(GamePlayerMessages.EventRaceVehiclePassed, OnEventRaceVehiclePassed);
        
        Messenger<int>.RemoveListener(GamePlayerMessages.EventRaceLapEvent, OnEventRaceLapEvent);
        Messenger<int>.RemoveListener(GamePlayerMessages.EventRaceCountdown, OnEventRaceCountdown);
    }
    
    public void ChangeCamera(GameCameraMode gameCameraMode) {
        FindCameraBehavior();
        
        currentCameraMode = gameCameraMode;
        
        if(gameCameraMode != null) {
            
            if(humanVehicleCameraBehavior != null) {
                humanVehicleCameraBehavior.SetCameraMode(gameCameraMode.cameraMode);
            }
            
            SetCameraDisplay();
        }
    }
    
    public void SetCameraDisplay() {
        if(labelCameraSwitcher != null 
            && currentCameraMode != null) {
            labelCameraSwitcher.text = currentCameraMode.displayName + " Cam";
        }
    }
    
    public void NextCameraMode() {
        SelectCamera(currentSelectedCameraMode + 1);        
    }
    
    void SelectCamera(int index) {
        FindCameraBehavior();
        
        if(index > cameraModes.Count - 1) {
            index = 0;
        }
        else if (index < 0) {
            index = cameraModes.Count - 1;
        }       
        
        currentSelectedCameraMode = index;      
        currentCameraMode = cameraModes[currentSelectedCameraMode];
        
        GameProfiles.Current.SetCurrentCameraMode(currentSelectedCameraMode);
        
        ChangeCamera(currentCameraMode);    
    }
    
    void FindCameraBehavior() {
        //
        
        if(cameraModes == null) {
            // Fill camera modes
            
            cameraModes = new List<GameCameraMode>();
            
            GameCameraMode cameraFollow = new GameCameraMode();
            cameraFollow.cameraMode = CameraMode.Follow;
            cameraFollow.displayName = "Fixed";
            cameraModes.Add(cameraFollow);
            
            currentCameraMode = cameraFollow;
            currentSelectedCameraMode = 0;
            
            GameCameraMode cameraFollowClose = new GameCameraMode();
            cameraFollowClose.cameraMode = CameraMode.FollowClose;
            cameraFollowClose.displayName = "Fixed Near";
            cameraModes.Add(cameraFollowClose);
            
            GameCameraMode cameraLookAhead = new GameCameraMode();
            cameraLookAhead.cameraMode = CameraMode.LookAhead;
            cameraLookAhead.displayName = "Stadium";
            cameraModes.Add(cameraLookAhead);
            
            GameCameraMode cameraLookAheadClose = new GameCameraMode();
            cameraLookAheadClose.cameraMode = CameraMode.LookAheadClose;
            cameraLookAheadClose.displayName = "Stadium Zoom";
            cameraModes.Add(cameraLookAheadClose);
            
            GameCameraMode cameraFollowBehind = new GameCameraMode();
            cameraFollowBehind.cameraMode = CameraMode.FollowBehind;
            cameraFollowBehind.displayName = "Follow Behind";
            cameraModes.Add(cameraFollowBehind);
            
            GameCameraMode cameraFollowBehindClose = new GameCameraMode();
            cameraFollowBehindClose.cameraMode = CameraMode.FollowBehindClose;
            cameraFollowBehindClose.displayName = "Follow Behind Near";
            cameraModes.Add(cameraFollowBehindClose);
            
            GameCameraMode cameraFollowBirdsEye = new GameCameraMode();
            cameraFollowBirdsEye.cameraMode = CameraMode.FollowBirdsEye;
            cameraFollowBirdsEye.displayName = "Bird's Eye";
            cameraModes.Add(cameraFollowBirdsEye);
            
            GameCameraMode cameraFollowBirdsEyeClose = new GameCameraMode();
            cameraFollowBirdsEyeClose.cameraMode = CameraMode.FollowBirdsEyeClose;
            cameraFollowBirdsEyeClose.displayName = "Bird's Eye Low";
            cameraModes.Add(cameraFollowBirdsEyeClose);

            ChangeCamera(cameraFollow);
        }
        
        FindHumanPlayer();
                
        if(humanVehicle != null 
            && humanVehicleCameraBehavior == null) {
            
            LogUtil.Log("FindCameraBehavior() Trying..." );
            
            UnityEngine.Object cameraBehavior = GameObject.FindObjectOfType(typeof(CameraBehavior));
            if(cameraBehavior != null) {
                humanVehicleCameraBehavior = cameraBehavior as CameraBehavior;
                humanVehicleCameraBehavior.initialXFollow = 11f;
                humanVehicleCameraBehavior.initialZFollow = 11f;
                humanVehicleCameraBehavior.XFollow = 11f;
                humanVehicleCameraBehavior.ZFollow = 11f;
                LogUtil.Log("FindCameraBehavior() FOUND..." + humanVehicleCameraBehavior);
            }
        }
    }
        
    IEnumerator CustomizePlayer() {
        
        hasCustomAudioCrowdCheer = false;
        hasCustomAudioCrowdJump = false;
        hasCustomAudioCrowdBoo = false;
        hasCustomAudioBikeJumping = false;
        hasCustomAudioBikeRacing = false;
        hasCustomAudioBikeRevving = false;
        
        yield return null;
        
        FindHumanPlayer();
        
        yield return new WaitForSeconds(1f);
        
        if(humanVehicle != null) {
        
            // colors
            CustomPlayerColors playerColors = GameProfiles.Current.GetCustomColors();
            
            Transform mxBike = humanVehicle.transform.FindChild("Body Lean/SSC_MX_Bike/MX_LowPoly");
            Transform mxRider = humanVehicle.transform.FindChild("Body Lean/SSC_MX_Rider_IdleTurnsImpact/MX_RiderMesh");            
                    
            if(mxBike) {
                if(mxBike.gameObject) {
                    humanVehicle.isCustomized = true;
                    mxBike.gameObject.renderer.materials[1].color = playerColors.bikeColor.GetColor();
                    LogUtil.Log("CustomizePlayer colorBike:" + playerColors.bikeColor.GetColor() );
                }
            }
            if(mxRider) {
                if(mxRider.gameObject) {
                    humanVehicle.isCustomized = true;
                    mxRider.gameObject.renderer.materials[0].color = playerColors.bootsSleevesPants.GetColor();//colorSleevesPants;
                    mxRider.gameObject.renderer.materials[1].color = playerColors.shirtColor.GetColor();//colorShirt;
                    mxRider.gameObject.renderer.materials[2].color = playerColors.skinColor.GetColor();//colorSkin;
                    mxRider.gameObject.renderer.materials[3].color = playerColors.riderColor.GetColor();//colorRider;
                    mxRider.gameObject.renderer.materials[4].color = playerColors.bootsGlovesColor.GetColor();//colorBootsGloves;
                    LogUtil.Log("CustomizePlayer colorRider:" + playerColors.riderColor.GetColor() );
                }
            }
            
            // audio
                
            CustomPlayerAudio customPlayerAudio = GameProfiles.Current.GetCustomAudio();            
        
            // Also set the bike sounds on ai if custom
            VehicleBehaviourScript[] bikes = GameObject.FindObjectsOfType(typeof(VehicleBehaviourScript)) as VehicleBehaviourScript[];//("MXBike");
            
            if(bikes != null) {
                LogUtil.Log("CustomSounds: bikes:" + bikes.Length);
                GameAudioRecorder.Instance.ClearLoadedClips();
            }
            
            foreach(VehicleBehaviourScript bike in bikes) {
                                
                if(bike != null) {
                
                    if(bike.gameObject != null) {
                        LogUtil.Log("CustomSounds: bike.gameObject.name:" + bike.gameObject.name);
                    }
                    LogUtil.Log("CustomSounds: bike.AIControlled:" + bike.AIControlled);
                    
                    VehicleSounds vehicleSounds = null;
                    vehicleSounds = bike.GetComponent<VehicleSounds>();
                    
                    LogUtil.Log("CustomSounds: vehicleSounds:" + vehicleSounds);
                    
                    if(!bike.AIControlled) {
                        humanVehicleSounds = vehicleSounds;
                    }
                    
                    if(vehicleSounds != null) { 
            
                        LogUtil.Log("CustomSounds: vehicleSounds:" + vehicleSounds);
                        
    
                        // default
                        //vehicleSounds.soundIdle.Add(GameAudio.LoadLoop(GameAudioEffects.audio_effect_bike_revs_idle));
                        vehicleSounds.soundRacing.Add(GameAudio.LoadLoop(GameAudioEffects.audio_effect_bike_medium_gear));
                        
                        vehicleSounds.soundJumping.Add(GameAudio.LoadLoop(GameAudioEffects.audio_effect_bike_jump_250_2));
                        vehicleSounds.soundJumping.Add(GameAudio.LoadLoop(GameAudioEffects.audio_effect_bike_jump_250_1));
                        vehicleSounds.soundJumping.Add(GameAudio.LoadLoop(GameAudioEffects.audio_effect_bike_jump2));
                        
                        //vehicleSounds.soundCrashing.Add(GameAudio.LoadLoop(GameAudioEffects.audio_effect_bike_jump_250_2));
                        vehicleSounds.soundRevving.Add(GameAudio.LoadLoop(GameAudioEffects.audio_effect_bike_revs_idle));
                        
                
                            
                        if(customPlayerAudio.audioItems != null) {
                            LogUtil.Log("CustomSounds: customPlayerAudio.audioItems.Count:" + customPlayerAudio.audioItems.Count);
                        }
                        
                        foreach(KeyValuePair<string, CustomPlayerAudioItem> item in customPlayerAudio.audioItems) {
                        
                            LogUtil.Log("CustomSounds: item:" + item.Key);
                            LogUtil.Log("CustomSounds: item.Value.useCustom:" + item.Value.useCustom);
                            
                            if(item.Value.useCustom) {
                                                            
                                if(item.Key.ToLower() == CustomPlayerAudioKeys.audioBikeBoosting) {

                                    var onSuccess = new Action<AudioClip, VehicleSounds>( (clip, vehicleSoundsItem)  => {
                                        LogUtil.Log("CustomSounds: soundAddedForBoosting:" + clip.name);
                                        LogUtil.Log("CustomSounds: soundAddedForBoosting:" + clip);
                                        if(clip != null && vehicleSounds != null) {                                         
                                            vehicleSoundsItem.isRunning = false;
                                            if(string.IsNullOrEmpty(clip.name)) {
                                                clip.name = CustomPlayerAudioKeys.audioBikeBoosting;
                                            }
                                            vehicleSoundsItem.soundJumping.Add(clip);
                                            //vehicleSoundsItem.soundCrashing.Add(clip);
                                            vehicleSoundsItem.soundJumpingCustomized = true;
                                            vehicleSoundsItem.isRunning = true;
                                            LogUtil.Log("CustomSounds: soundAddedForBoosting complete:" + clip.name);
                                            LogUtil.Log("CustomSounds: soundAddedForBoosting complete:" + clip);
                                        }
                                    });                             
                                    
                                    LogUtil.Log("CustomSounds: soundAddingForBoosting:" + item.Key);
                                
                                    GameAudioRecorder.Instance.LoadVehicleSounds(CustomPlayerAudioKeys.audioBikeBoosting, vehicleSounds, onSuccess);
                                    
                                    hasCustomAudioBikeJumping = true;
                                    yield return null;
                                }
                                else if(item.Key.ToLower() == CustomPlayerAudioKeys.audioBikeRacing) {
                                    var onSuccess = new Action<AudioClip, VehicleSounds>( (clip, vehicleSoundsItem) => {
                                        LogUtil.Log("CustomSounds: soundAddedForRacing:" + clip.name);
                                        LogUtil.Log("CustomSounds: soundAddedForRacing clip:" + clip);
                                        LogUtil.Log("CustomSounds: soundAddedForRacing vehicleSoundsItem:" + vehicleSoundsItem);
                                        if(clip != null && vehicleSounds != null) {
                                            vehicleSoundsItem.raceSoundRunning = false;
                                            vehicleSoundsItem.isRunning = false;
                                            if(string.IsNullOrEmpty(clip.name)) {
                                                clip.name = CustomPlayerAudioKeys.audioBikeRacing;
                                            }
                                            vehicleSoundsItem.soundRacing.Add(clip);
                                            LogUtil.Log("CustomSounds: vehicleSoundsItem.isRunning:" + vehicleSoundsItem.isRunning);
                                            LogUtil.Log("CustomSounds: vehicleSoundsItem.raceSoundRunning:" + vehicleSoundsItem.raceSoundRunning);
                                            vehicleSoundsItem.SetRacingRound(clip);
                                            vehicleSoundsItem.soundRacingCustomized = true;
                                            vehicleSoundsItem.isRunning = true;
                                            LogUtil.Log("CustomSounds: soundAddedForRacing complete:" + clip.name);
                                            LogUtil.Log("CustomSounds: soundAddedForRacing complete clip:" + clip);
                                            LogUtil.Log("CustomSounds: soundAddedForRacing complete vehicleSoundsItem:" + vehicleSoundsItem);
                                        }
                                    });     
                                        
                                    LogUtil.Log("CustomSounds: soundAddingForRacing:" + item.Key);              
                                
                                    GameAudioRecorder.Instance.LoadVehicleSounds(CustomPlayerAudioKeys.audioBikeRacing, vehicleSounds, onSuccess);
                                    
                                    hasCustomAudioBikeRacing = true;
                                    yield return new WaitForSeconds(.5f);
                                }
                                else if(item.Key.ToLower() == CustomPlayerAudioKeys.audioBikeRevving) { 

                                    var onSuccess = new Action<AudioClip, VehicleSounds>( (clip, vehicleSoundsItem) => {
                                        LogUtil.Log("CustomSounds: soundAddedForRevving:" + clip.name);
                                        
                                        if(clip != null && vehicleSounds != null) {
                                            //vehicleSoundsItem.soundIdle.Add(clip);
                                            if(string.IsNullOrEmpty(clip.name)) {
                                                clip.name = CustomPlayerAudioKeys.audioBikeRevving;
                                            }
                                            vehicleSoundsItem.soundRevving.Add(clip);
                                            vehicleSoundsItem.soundRevvingCustomized = true;
                                            vehicleSoundsItem.isRunning = true;
                                            LogUtil.Log("CustomSounds: soundAddedForRevving complete:" + clip.name);
                                        }
                                    });                             
                                
                                    LogUtil.Log("CustomSounds: soundAddingForRevving:" + item.Key);
                                    
                                    GameAudioRecorder.Instance.LoadVehicleSounds(CustomPlayerAudioKeys.audioBikeRevving, vehicleSounds, onSuccess);
                                    hasCustomAudioBikeRevving = true;
                                    yield return null;
                                }
                                else if(item.Key.ToLower() == CustomPlayerAudioKeys.audioCrowdBoo) {    
                                    hasCustomAudioCrowdBoo = true;
                                    yield return null;
                                }
                                else if(item.Key.ToLower() == CustomPlayerAudioKeys.audioCrowdCheer) {  
                                    hasCustomAudioCrowdCheer = true;
                                    yield return null;
                                }
                                else if(item.Key.ToLower() == CustomPlayerAudioKeys.audioCrowdJump) {
                                    hasCustomAudioCrowdJump = true;
                                    yield return null;
                                }
                                
                                yield return null;
                            }
                        }
                        
                        vehicleSounds.isRunning = true;
                        vehicleSounds.Revving();
                    }
                }
            }
        }
        
        yield return null;
        
        //Resources.UnloadUnusedAssets();
        GC.Collect();
    }
    
    void FadeAndStopVehicleSounds() {
        VehicleBehaviourScript[] bikes = GameObject.FindObjectsOfType(typeof(VehicleBehaviourScript)) as VehicleBehaviourScript[];//("MXBike");

        foreach(VehicleBehaviourScript bike in bikes) {
                                
            if(bike != null) {
                VehicleSounds vehicleSounds = bike.GetComponent<VehicleSounds>();
                if(vehicleSounds != null) {
                    vehicleSounds.FadeVehicleSounds(2f);
                    vehicleSounds.isRunning = false;
                }
            }
        }
    }
        
    void AudioPlayCrowdCheer() {
        if(Time.time > lastSoundCheer + 9f * soundDelayModifier ) {
            lastSoundCheer = Time.time;
            GameAudio.PlayCustomOrDefaultEffect(CustomPlayerAudioKeys.audioCrowdCheer, hasCustomAudioCrowdCheer);
        }
    }
    
    void AudioPlayCrowdCheerLow() {
        if(Time.time > lastSoundCheer + 15f * soundDelayModifier ) {
            lastSoundCheer = Time.time;
            GameAudio.PlayCustomOrDefaultEffect(CustomPlayerAudioKeys.audioCrowdCheer, hasCustomAudioCrowdCheer);
        }
    }
    
    void AudioPlayCrowdBoo() {  
        if(Time.time > lastSoundBoo + 11f * soundDelayModifier ) {
            lastSoundBoo = Time.time;   
            GameAudio.PlayCustomOrDefaultEffect(CustomPlayerAudioKeys.audioCrowdBoo, hasCustomAudioCrowdBoo);
        }
    }
    
    void AudioPlayCrowdBooLow() {
        if(Time.time > lastSoundBoo + 15f * soundDelayModifier ) {
            lastSoundBoo = Time.time;
            GameAudio.PlayCustomOrDefaultEffect(CustomPlayerAudioKeys.audioCrowdBoo, hasCustomAudioCrowdBoo);
        }
    }
    
    void AudioPlayCrowdBoost() {
        if(Time.time > lastSoundBoost + 11f * soundDelayModifier ) {
            lastSoundBoost = Time.time;
            GameAudio.PlayCustomOrDefaultEffect(CustomPlayerAudioKeys.audioCrowdJump, hasCustomAudioCrowdJump);
        }
    }
    
    void AudioPlayCrowdCheerConstant() {
        if(Time.time > lastSoundCheerConstant + 20f * soundDelayModifier ) {
            lastSoundCheerConstant = Time.time;
            GameAudio.PlayEffect(GameAudioEffects.audio_effect_crowd_cheer_1, (float)GameProfiles.Current.GetAudioEffectsVolume() * .2f);
        }
    }
    
    void AudioPlayBikeRevving() {
        if(Time.time > lastSoundBikeRev + 6f * soundDelayModifier ) {
            lastSoundBikeRev = Time.time;
            GameAudio.PlayCustomOrDefaultEffect(CustomPlayerAudioKeys.audioBikeRevving, hasCustomAudioBikeRevving); 
        }
    }
    
    void AudioPlayBikeBoost() {
        if(Time.time > lastSoundBikeJump + 8f * soundDelayModifier ) {
            lastSoundBikeJump = Time.time;
            GameAudio.PlayCustomOrDefaultEffect(CustomPlayerAudioKeys.audioBikeBoosting, hasCustomAudioBikeJumping);
        }
    }
    
    void AudioPlayBikeRacing() {
        if(Time.time > lastSoundBikeRace + 10f * soundDelayModifier ) {
            lastSoundBikeRace = Time.time;
            GameAudio.PlayCustomOrDefaultEffect(CustomPlayerAudioKeys.audioBikeRacing, hasCustomAudioBikeRacing);   
        }
    }
    
    void OnEventRacePrepare() {
        //LogUtil.Log("GameRaceHUD::OnEventRacePrepare");
        
        // Find Human Player and handle customizations  
                
        GameAudio.SetVolumeForRace(true);
        GameAudio.PlayGameMainLoop(GameAudioEffects.audio_effect_crowd_cheer_constant_1,
                           (float)(GameProfiles.Current.GetAudioEffectsVolume()* .1));
        
        //GameAudio.PlayCustomOrDefaultEffect(CustomPlayerAudioKeys.audioBikeRevving);
        
        Tweens.Instance.FadeToObject(overlayFadeObject, 0f, .5f, .1f);
        Tweens.Instance.FadeToObject(overlayFadeInfoObject, .4f, .5f, .1f);     
        
        if(UIGameAchievement.Instance) {
            UIGameAchievement.Instance.Reset();
        }
        
        ChangeState(GameHUDState.GAME_ENTER);
                
        FindCameraBehavior();
        
        StartCoroutine(CustomizePlayer());
    }
    
    void OnEventRaceStart() {
        //LogUtil.Log("GameRaceHUD::OnEventRaceStart");
        
        if(Time.time > lastEventStartTime + 2f ) {
            lastEventStartTime = Time.time;
            
            AudioPlayCrowdCheer();
            AudioPlayCrowdCheerConstant();
            
            GameAudio.StartGameLapLoops();
            GameAudio.StartGameLoop(1);
                
            countdownStepCompleted = 0;
            Tweens.Instance.FadeToObject(countdown1Object, 0f, .5f, 0f);
            Tweens.Instance.ScaleToObject(countdown1Object, new Vector3(1f, 1f, 1f), .5f, 0f);
        
            Tweens.Instance.FadeToObject(goObject, 1f, .6f, 0f);
            Tweens.Instance.ScaleToObject(goObject, new Vector3(1.5f, 1.5f, 1.5f), .5f, .6f);
            Tweens.Instance.FadeToObject(goObject, 0f, .5f, 1.1f);
            
            
            Tweens.Instance.MoveToObject(seriesInfoObject,  
                                         new Vector3(seriesInfoObject.transform.position.x,
                                         seriesInfoObject.transform.position.y - 9,
                                         seriesInfoObject.transform.position.z),
                                         .5f, 0f);
            Tweens.Instance.FadeToObject(seriesInfoObject, .05f, .5f, 1f);
                        
            Tweens.Instance.FadeToObject(inputsObject, 1f, .5f, 0f);
            
            //Tweens.Instance.FadeToObject(tipsObject, 0f, .5f, .2f);
            //Tweens.Instance.MoveToObject(tipsObject, new Vector3(0, -20, 0), .5f, .2f);
                        
            Tweens.Instance.FadeToObject(hudOutputObject, 1f, .5f, 0f);
            //FadeToObject(overlayFadeObject, 0f, .5f, 0);
            
            SetLapCurrentLabel(1);
            SetLapTotalLabel(3);
            
            Tweens.Instance.FadeToObject(overlayFadeInfoObject, 0f, .5f, .1f);
            
            raceActive = true;
            raceQuit = false;
            
            // Delay for tap to go starts.
            GrowHitAreaForInputs();
            
            ChangeState(GameHUDState.GAME_START);
        }
    }   
    
    void OnEventRaceFinish() {
        LogUtil.Log("GameRaceHUD::OnEventRaceFinish");  
        
        GamePlayerProgress.Instance.SetStatAccumulate(GameStatistics.STAT_LAPS, 1);
        
        GameAudio.PlayCustomOrDefaultEffect(CustomPlayerAudioKeys.audioCrowdJump);
        GameAudio.PlayCustomOrDefaultEffect(CustomPlayerAudioKeys.audioCrowdCheer);
        GameAudio.PlayCustomOrDefaultEffect(CustomPlayerAudioKeys.audioBikeRevving);
        GameAudio.PlayCustomOrDefaultEffect(CustomPlayerAudioKeys.audioBikeBoosting);
                            
        if(Time.time > lastEventFinishTime + 1f ) {
                    
            lastEventFinishTime = Time.time;
            
            GameDatas.Current.lastRaceQuit = false;
            FinishRace(false);
        }
    }
    
    void FinishRace(bool backToTrackSelect) {
        StartCoroutine(FinishRaceCo(backToTrackSelect));
    }
    
    IEnumerator FinishRaceCo(bool backToTrackSelect) {
        
        Tweens.Instance.FadeToObject(finishObject, 1f, .5f, 0f);
        Tweens.Instance.FadeToObject(finishObject, 0f, .5f, 3f);
        Tweens.Instance.FadeToObject(overlayFadeObject, 1f, .5f, 1.3f);
        
        yield return null;
        
        if(UIGameAchievement.Instance != null && !backToTrackSelect) {
            float raceTime = RaceManagerScript.Instance.TotalRaceTime;
            UIGameAchievement.Instance.CheckFastestRace(raceTime);
            
            yield return null;
        
            GameRPG.IncrementXP(GameRPGPoints.XP_RACE);
            
            yield return null;
        }
        
        raceActive = false;
        
        GameAudio.StartGameLoop(-1);
        
        yield return null;
                        
        ChangeState(GameHUDState.GAME_FINISH);
        
        yield return null;
                
        if(RaceManagerScript.Instance != null) {                
            GameDatas.SetRaceResultsData(RaceManagerScript.Instance.VehiclesSortedByRaceOrder,
                                         RaceManagerScript.Instance.VehicleScripts);
        }
        
        yield return null;
        
        if(!backToTrackSelect) {
            StartCoroutine(ProcessProgress(backToTrackSelect));
            yield return new WaitForSeconds(6f);
        }   
                    
        StartCoroutine(AdvanceToResults(backToTrackSelect));
    }
    
    
    IEnumerator ProcessProgress(bool backToTrackSelect) {
        yield return null;      
        GamePlayerProgress.Instance.ScoreMode(currentPosition); 
        yield return null;
        SyncHumanVehicleStats();
        GamePlayerProgress.Instance.ProcessRaceResult(humanVehicleStatsData);   
        yield return null;
    }
    
    IEnumerator AdvanceToResults(bool backToTrackSelect) {
        if(!destroyed) {
            //destroyed = true;
            Tweens.Instance.FadeToObject(overlayFadeInfoObject, .4f, .8f, .3f);
                            
            FadeAndStopVehicleSounds();
            
            yield return new WaitForSeconds(1.6f);          
            
            if(GameLoadingObject.Instance != null) {
                GameLoadingObject.Instance.ShowBlack();
                GameLoadingObject.Instance.ShowLoadingHelp();
                Hide();
            }
            
            yield return new WaitForSeconds(1.1f);
                            
            GameAudio.SetVolumeForRace(false);
            GameAudio.StartAmbience();          
        
            yield return null;      
        
            TestFlight.LogSceneExit();
            
            yield return null;      
                        
            if(gameObject != null) {
                //Destroy(gameObject);
            }
            if(backToTrackSelect) {             
                SceneLoader.Instance.LoadSceneRaceSelect();
            }
            else {
                SceneLoader.Instance.LoadSceneRaceResults();
            }                       
                        
            Invoke ("UnloadLevelBundle", 3);
            
            //StopAllCoroutines();
        }
    }

    void UnloadLevelBundle() {  
        Contents.UnloadLevelBundle();
    }
    
    void OnEventRaceLapEvent(int lapNumber) {
        LogUtil.Log("GameRaceHUD::OnEventRaceLapEvent:" + lapNumber);
        
        if(Time.time > lastEventLapTime + 10f ) {       
            int lapNumberItem = lapNumber + 1;
            SetLapCurrentLabel(lapNumberItem);      
            if(lapNumberItem <= 3) {
                GameAudio.StartGameLoop(lapNumberItem);             
                GameRPG.IncrementXP(GameRPGPoints.XP_LAP);
            }
            
            GamePlayerProgress.Instance.SetStatAccumulate(GameStatistics.STAT_LAPS, 1);
            
            if(UIGameAchievement.Instance != null) {
                int lapIndex = lapNumber - 1;
                if(humanVehicle.LapTimes.Length > lapIndex) {
                    float lapTime = humanVehicle.LapTimes[lapIndex];
                    UIGameAchievement.Instance.CheckFastestLap(lapTime);
                }
            }
            
            SyncHumanVehicleStats();
            GamePlayerProgress.Instance.HandleInGameLapAchievements(humanVehicleStatsData, lapNumberItem, false);           
        }           
    }
    
    void SetLapCurrentLabel(int lapNumber) {
        if(labelLapCurrent != null) {
            labelLapCurrent.text = lapNumber.ToString();
        }
    }
    
    void SetLapTotalLabel(int laps) {
        if(labelLapTotal != null) {
            labelLapTotal.text = laps.ToString();
        }
    }
    
    void FindHumanPlayer() {
        foreach(VehicleBehaviourScript vehicle in RaceManagerScript.Instance.VehiclesSortedByRaceOrder) {
            if(!vehicle.AIControlled) {
                humanVehicle = vehicle;
                break;
            }
        }
    }
        
    void UpdatePlayerPosition() {
        if(RaceManagerScript.Instance != null) {
            
            // get human player and their position
            
            if(humanVehicle != null) {
                currentPosition = humanVehicle.GetRacePosition();
            }
            
            if(currentPosition != lastCurrentPosition) {
                // update hud place
                lastCurrentPosition = currentPosition;              
                labelLapPlace.text = GameSeriesEvents.Instance.GetPrettyPlace(currentPosition);
            }           
        }
    }
    
    void OnEventRaceVehiclePass() {
        //LogUtil.Log("GameRaceHUD::OnEventRaceVehicleBoost");      
        
        if(Time.time > lastEventPassTime + 1f) {            
            GameRPG.IncrementXP(GameRPGPoints.XP_PASS);         
            AudioPlayCrowdCheerLow();           
            lastEventPassTime = Time.time;
        }
    }
    
    void OnEventRaceVehiclePassed() {
        //LogUtil.Log("GameRaceHUD::OnEventRaceVehicleBoost");      
        if(Time.time > lastEventPassedTime + 1f) {  
            AudioPlayCrowdBoo();
            GameRPG.IncrementXP(GameRPGPoints.XP_PASSED);
            lastEventPassedTime = Time.time;        
        }
    }
    
    void OnEventRaceVehicleCollision() {
        //LogUtil.Log("GameRaceHUD::OnEventRaceVehicleBoost");
        
        if(Time.time > lastEventCollisionTime + 2f) {           
            AudioPlayCrowdBooLow();
            GameRPG.IncrementXP(GameRPGPoints.XP_COLLIDE);
            lastEventCollisionTime = Time.time;         
        }
    }
    
    void OnEventRaceVehicleCauseCollision() {
        //LogUtil.Log("GameRaceHUD::OnEventRaceVehicleBoost");
        
        if(Time.time > lastEventCauseCollisionTime + 2f ) {
            AudioPlayCrowdCheer();
            GameRPG.IncrementXP(GameRPGPoints.XP_BUMP);
            lastEventCauseCollisionTime = Time.time;
        }
    }
    
    bool playVehicleBoost = true;
    
    void OnEventRaceVehicleBoost() {
        //LogUtil.Log("GameRaceHUD::OnEventRaceVehicleBoost");
        
        if(Time.time > lastEventBoostTime + 3f ) {
            
            if(playVehicleBoost || humanVehicle.DistanceOffGround > 1.5f) {
                AudioPlayCrowdBoost();
            }
            
            playVehicleBoost = !playVehicleBoost;
            
            AudioPlayCrowdCheer();
            
            GameRPG.IncrementXP(GameRPGPoints.XP_BOOST);
            lastEventBoostTime = Time.time;
        }
    }
    
    void OnEventRaceVehicleReset() {
        //LogUtil.Log("GameRaceHUD::OnEventRaceVehicleReset");
        
        if(Time.time > lastEventResetTime + 3f ) {
            lastEventResetTime = Time.time;         
            GameLoadingObject.Instance.ShowAndHideBlack();
        }
    }
    
    void OnEventRaceCountdown(int countdown) {
        //LogUtil.Log("GameRaceHUD::OnEventRaceCountdown:" + countdown);        
        // Show countdown
        if (countdown == 3 
                 && countdownStepCompleted == 0) {
            countdownStepCompleted = countdown;         
            Tweens.Instance.FadeToObject(countdown3Object, 1f, .3f, 0f);
            Tweens.Instance.ScaleToObject(countdown3Object, new Vector3(1f, 1f, 1f), .8f, 0f);
            Tweens.Instance.FadeToObject(labelTouchToSkipObject, 0f, .3f, 0f);
        }
        else if (countdown == 2 
                 && countdownStepCompleted == (countdown + 1)) {
            countdownStepCompleted = countdown;         
            Tweens.Instance.FadeToObject(countdown3Object, 0f, .3f, 0f);            
            Tweens.Instance.FadeToObject(countdown2Object, 1f, .3f, 0f);
            Tweens.Instance.ScaleToObject(countdown2Object, new Vector3(1f, 1f, 1f), .8f, 0f);
        }
        else if(countdown == 1 
           && countdownStepCompleted == (countdown + 1)) {
            countdownStepCompleted = countdown;         
            Tweens.Instance.FadeToObject(countdown2Object, 0f, .3f, 0f);            
            Tweens.Instance.FadeToObject(countdown1Object, 1f, .3f, 0f);
            Tweens.Instance.ScaleToObject(countdown1Object, new Vector3(1f, 1f, 1f), .8f, 0f);
        }
    }   
    
    void GrowHitAreaForInputs() {
        // Make hit area big temp fix   
        if(legacyLeftButton) {
            legacyLeftButton.ScaleTo(new Vector3(0.025f, 0.025f, 0.025f), 4f, 5f);
        }
                    
        if(legacyRightButton) {
            legacyRightButton.ScaleTo(new Vector3(0.025f, 0.025f, 0.025f), 4f, 5f);
        }
    }
    
    void PrepareAndLoadLevel() {    
        
        Tweens.Instance.FadeToObject(overlayFadeInfoObject, .4f, .5f, 0f);
        Tweens.Instance.FadeToObject(overlayFadeObject, 1f, .5f, .5f);
        
        if(GameGlobal.Instance) {
            LogUtil.Log("LoadingLevel:" + GameLevels.Current.name);
            Invoke("LoadLevelDelay", 1);
            //LoadLevelDelay();
        }
    }
    
    void LoadLevelDelay() {
        foreach(AudioListener listener in gameObject.GetComponentsInChildren<AudioListener>()) {
            listener.enabled = false;
        }
        LogUtil.Log("GameLevels.Current.name:" + GameLevels.Current.name);
        
        GameLevel gameLevel = GameLevels.Current;       
        
        TestFlight.LogScenePlayingLevel(gameLevel.name);
        
        if(Contents.IsDownloadableContent(GameLevels.Current.pack[0])) {
            Contents.LoadSceneOrDownloadScenePackAndLoad(GameLevels.Current.pack[0]);
        }
        else {
            LoadLevelHandler();
        }       
    }
    
    public void LoadLevelHandler() {
        levelLoadInProgress = true;
        Messenger<string>.Broadcast(ContentMessages.ContentItemLoadStarted, "Content loading..." );     
        StartCoroutine(LoadLevelHandlerCo());
    }
    
    IEnumerator LoadLevelHandlerCo() {
        
        yield return new WaitForSeconds(.6f);
        
        asyncLevelLoad = Application.LoadLevelAsync(GameLevels.Current.name);
        yield return asyncLevelLoad;
        
        yield return new WaitForSeconds(.4f);
        levelLoadInProgress = false;
        
        Messenger<string>.Broadcast(ContentMessages.ContentItemLoadSuccess, "Content loaded!" );
        
        if(GameLoadingObject.Instance != null) {
            GameLoadingObject.Instance.HideLoadingHelp();
            GameLoadingObject.Instance.ShowReadyDelayed();
        }
        
        LogUtil.Log("LoadLevelItem: Complete");
        Messenger<string>.Broadcast(UIMessages.EventLevelLoaded, GameLevels.Current.name);
        StopCoroutine("LoadLevelHandlerCo");
    }
        
    public void StartLoadedLevelDelayed(int seconds) {
        Invoke("StartLoadedLevel", seconds);
    }
        
    void RestartRace() {
        GameAudio.StartGameLoop(-1);
        
        Hide();
        
        if(GameLoadingObject.Instance != null) {
            GameLoadingObject.Instance.ShowBackground();
            GameLoadingObject.Instance.ShowHelpTips();
            GameLoadingObject.Instance.ShowLoadingHelp();
            GameLoadingObject.Instance.LoadNextTip();
        }
        
        GamePlayerProgress.Instance.SetAchievement(GameAchievements.ACHIEVE_UI_RACE_RESTART, true);
        
        humanVehicleSounds.isRunning = false;
        humanVehicleSounds.raceSoundRunning = false;
                
        SceneLoader.Instance.LoadScene("UISceneRaceSetup");
                
        //Contents.UnloadLevelBundle();
    }
    
    void QuitRace() {
        if(!raceQuit) {
            raceQuit = true;
            raceActive = false;
            FinishRace(true);
            GamePlayerProgress.Instance.SetAchievement(GameAchievements.ACHIEVE_UI_RACE_QUIT, true);
        }
    }   
    
    void ShowOrHideInputs() {
                
        if(legacyLeftButton == null && limitOnLegacyCheck > 0) {
            legacyLeftButton = GameObject.Find("Left Button");
            limitOnLegacyCheck--;
            if(legacyLeftButton != null) {
                legacyLeftButton.renderer.enabled = false;
            }
        }
        if(legacyRightButton == null && limitOnLegacyCheck > 0) {
            legacyRightButton = GameObject.Find("Right Button");
            limitOnLegacyCheck--;
            if(legacyRightButton != null) {
                legacyRightButton.renderer.enabled = false;
            }
        }
                
        if(GameProfiles.Current.GetControlTouch()) {
            // show the inputs
            if(inputLeft) {
                inputLeft.gameObject.SetActiveRecursively(true);
            }
            
            if(inputRight) {
                inputRight.gameObject.SetActiveRecursively(true);
            }           
            
            if(legacyLeftButton) {
                legacyLeftButton.gameObject.SetActiveRecursively(true);
            }
                        
            if(legacyRightButton) {
                legacyRightButton.gameObject.SetActiveRecursively(true);
            }
                        
            if(legacyLeftButton) {
                legacyLeftButton.renderer.enabled = false;
            }           
            
            if(legacyRightButton) {
                legacyRightButton.renderer.enabled = false;
            }
        }
        else {
            if(inputLeft) {
                inputLeft.gameObject.SetActiveRecursively(false);
            }
            
            if(inputRight) {
                inputRight.gameObject.SetActiveRecursively(false);
            }           
            
            if(legacyLeftButton) {
                legacyLeftButton.gameObject.SetActiveRecursively(false);
            }
                        
            if(legacyRightButton) {
                legacyRightButton.gameObject.SetActiveRecursively(false);
            }
        }
    }
    
    void TogglePausePanel() {
        SetTimeScale(1f);
        if(pauseExpanded) { 
            //Tweens.Instance.FadeToObject(panelPauseObject, 0f, 0f, .0f);
            iTween.MoveTo(panelPauseObject, iTween.Hash("x", 20, "time", .3f, "delay", .11f, "easeType", "easeInOutQuad", 
                                                        "oncomplete", "StartTime", "oncompletetarget", gameObject));
            pauseExpanded = false;
        }
        else {
            iTween.MoveTo(panelPauseObject, iTween.Hash("x", -2.5, "time", .5f, "delay", .02f, "easeType", "easeInOutQuad", "oncomplete", "StopTime", "oncompletetarget", gameObject));
            //Tweens.Instance.FadeToObject(panelPauseObject, 0f, 0f, 1f);
            pauseExpanded = true;
        }
    }
    
    public void StopTime() {
        SetTimeScale(0f);
        AudioListener.pause = true;
    }
    
    public void StartTime() {
        //ResetButtonPositions();
        SetTimeScale(1f);
        AudioListener.pause = false;
    }
    
    public void SetTimeScale(float timeScale) {
        Time.timeScale = timeScale;
    }
    
    public void CheckTouchInputState() {
        if(GameSettings.Instance != null) {
            touchInputEnabled = !GameSettings.Instance.TiltControlEnabled;
            lastTouchInputEnabled = touchInputEnabled;
        }
    }
    
    public virtual void LateUpdate() {
        
        if(buttonMeta != null) {
            buttonMeta.ResetButtons();
            
            currentTimeBlock += Time.deltaTime;
            
            if(currentTimeBlock > actionInterval) {
                currentTimeBlock = 0.0f;
                
                buttonMeta.SetButtonsAlertState();
            }
        }
    }
        
    public void SyncHumanVehicleStats() {
        if(humanVehicleStats == null) {
            if(humanVehicle != null) {
                humanVehicleStats = humanVehicle.GetComponent<VehicleStats>();
            }
        }
        
        if(humanVehicleStats != null) {
            if(humanVehicleStatsData == null) {
                humanVehicleStatsData = new VehicleStatsData();
            }
            
            humanVehicleStatsData.BoostsHit = humanVehicleStats.BoostsHit;
            humanVehicleStatsData.CleanLaps = humanVehicleStats.CleanLaps;
            humanVehicleStatsData.CollisionsFromOtherBikes = humanVehicleStats.CollisionsFromOtherBikes;
            humanVehicleStatsData.CollisionsOnOtherBikes = humanVehicleStats.CollisionsOnOtherBikes;
            humanVehicleStatsData.GotHoleShot = humanVehicleStats.GotHoleShot;
            humanVehicleStatsData.LapsInFirstPlace = humanVehicleStats.LapsInFirstPlace;
            humanVehicleStatsData.MilesDriven = humanVehicleStats.MilesDriven;
            humanVehicleStatsData.MudPuddlesHit = humanVehicleStats.MudPuddlesHit;
            humanVehicleStatsData.PrematureStart = humanVehicleStats.PrematureStart;
            humanVehicleStatsData.TimeInFirstPlace = humanVehicleStats.TimeInFirstPlace;
            humanVehicleStatsData.TimesHitOnThisLap = humanVehicleStats.TimesHitOnThisLap;
            humanVehicleStatsData.TimesIPassedSomeone = humanVehicleStats.TimesIPassedSomeone;
            humanVehicleStatsData.TimesPassed = humanVehicleStats.TimesPassed;
        }
    }
    
    public void Update() {
                
        if(initialized) {
            
            FindCameraBehavior();
                
            currentTimeBlockLocal += Time.deltaTime;
            
            if(currentTimeBlockLocal > actionIntervalLocal) {
                currentTimeBlockLocal = 0.0f;
                
                if(humanVehicle) {
                    SyncHumanVehicleStats();
                    GamePlayerProgress.Instance.HandleInGameAchievements(humanVehicleStatsData, false);
                }
            }           
                        
            if(touchInputEnabled != lastTouchInputEnabled
               && raceActive) {
                
                CheckTouchInputState();
                
                lastTouchInputEnabled = touchInputEnabled;
                if(inputLeft != null)  {
                    inputLeft.gameObject.renderer.enabled = touchInputEnabled;
                }
                if(buttonHitLeft != null)  {
                    buttonHitLeft.gameObject.renderer.enabled = touchInputEnabled;
                }
                if(inputRight != null)  {
                    inputRight.gameObject.renderer.enabled = touchInputEnabled;
                }
                if(buttonHitRight != null)  {
                    buttonHitRight.gameObject.renderer.enabled = touchInputEnabled;
                }               
            }
            
            if(raceActive) {
                ShowOrHideInputs();
            }
            
            if(labelTime != null) {
                if(RaceManagerScript.Instance != null) {
                    string time = FormatUtil.GetFormattedTimeMinutesSecondsMs(RaceManagerScript.Instance.TotalRaceTime);
                    // 2.11: labelTime is UIRef in the non-NGUI branch; SetLabelValue is
                    // backend-blind (UILabel/Text/UIRef overloads) so this works either way.
                    UIUtil.SetLabelValue(labelTime, time);
                }
            }
            
        }
        
        currentTimeBlockMicro += Time.deltaTime;
        
        if(currentTimeBlockMicro > actionIntervalMicro) {
            currentTimeBlockMicro = 0.0f;
            
            UpdatePlayerPosition();     
        }
        
    }
    
}

 * 
 * */

