using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Engine.Utility;
using Engine.Game.App;

using Engine.Events;
using Engine.UI;

public enum UINotificationTipState {
    Showing,
    Hidden
}

public enum UINotificationTipType {
    Info,
    Achievement,
    Tip,
    Error,
    Point
}

public class UINotificationTipItem {
    public string code = "";
    public string title = "";
    public string description = "";
    public string score = "";
    public string icon = "";
    public bool immediate = false;
    public UINotificationTipType notificationType = UINotificationTipType.Info;
    
    public UINotificationTipItem() {
        
    }
}

public class UINotificationDisplayTip
    : UIAppPanel {

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3

    // Achievement
    public UILabel achievementTitle;
    public UILabel achievementDescription;
    public UILabel achievementScore;
    public UIImageButton achievementIcon;

    // Point
    public UILabel pointTitle;
    public UILabel pointDescription;
    public UILabel pointScore;
    public UIImageButton pointContinue;

    // Error
    public UILabel errorTitle;
    public UILabel errorDescription;
    public UILabel errorScore;
    public UIImageButton errorContinue;

    // Info
    public UILabel infoTitle;
    public UILabel infoDescription;
    public UILabel infoScore;
    public UIImageButton infoContinue;

    // Tip
    public UILabel tipTitle;
    public UILabel tipDescription;
    public UILabel tipScore;
    public UIImageButton tipContinue;
#else
    // B10: agnostic UIRef handles (was UGUI Text/Button), the BaseGameHUD pattern. Nothing binds
    // them today, so they stay null and every UIUtil call below no-ops on them; the toolkit view
    // is written by ELEMENT NAME instead (ApplyToolkitItem), which works in both builds.

    // Achievement
    public Engine.UI.UIRef achievementTitle;
    public Engine.UI.UIRef achievementDescription;
    public Engine.UI.UIRef achievementScore;
    public Engine.UI.UIRef achievementIcon;

    // Point
    public Engine.UI.UIRef pointTitle;
    public Engine.UI.UIRef pointDescription;
    public Engine.UI.UIRef pointScore;
    public Engine.UI.UIRef pointContinue;

    // Error
    public Engine.UI.UIRef errorTitle;
    public Engine.UI.UIRef errorDescription;
    public Engine.UI.UIRef errorScore;
    public Engine.UI.UIRef errorContinue;

    // Info
    public Engine.UI.UIRef infoTitle;
    public Engine.UI.UIRef infoDescription;
    public Engine.UI.UIRef infoScore;
    public Engine.UI.UIRef infoContinue;

    // Tip
    public Engine.UI.UIRef tipTitle;
    public Engine.UI.UIRef tipDescription;
    public Engine.UI.UIRef tipScore;
    public Engine.UI.UIRef tipContinue;
#endif

    public static UINotificationDisplayTip Instance;
    public GameObject notificationPanel;
    public GameObject notificationContainerAchievement;
    public GameObject notificationContainerPoint;
    public GameObject notificationContainerInfo;
    public GameObject notificationContainerTip;
    public GameObject notificationContainerError;

    float positionYOpenInGame = 0;
    float positionYClosedInGame = -900;
    UINotificationTipItem currentItem;
    UINotificationTipState notificationState = UINotificationTipState.Hidden;
    public bool paused = false;
    Queue<UINotificationTipItem> notificationQueue = new Queue<UINotificationTipItem>();

    public bool IsHidden {
        get {
            if(notificationState == UINotificationTipState.Hidden)
                return true;

            return false;
        }
    }

    public override void Awake() {

        base.Awake();

        if(Instance != null && this != Instance) {
            //There is already a copy of this script running
            Destroy(this);
            return;
        }

        Instance = this;

        //DontDestroyOnLoad(gameObject);
    }

    public override void Start() {

        base.Start();

        notificationState = UINotificationTipState.Hidden;
        HideDialog();
    }

    void OnEnable() {
        Messenger<string>.AddListener(ButtonEvents.EVENT_BUTTON_CLICK, OnButtonClickEventHandler);

        Messenger<string>.AddListener(GameNotificationMessages.gameQueueTipAchievement, OnQueueAchievement);
        Messenger<string, string>.AddListener(GameNotificationMessages.gameQueueTipError, OnQueueError);
        Messenger<string, string>.AddListener(GameNotificationMessages.gameQueueTipInfo, OnQueueInfo);
        Messenger<string, string>.AddListener(GameNotificationMessages.gameQueueTipTip, OnQueueTip);
        Messenger<string, string, double>.AddListener(GameNotificationMessages.gameQueueTipPoint, OnQueuePoint);

        // Warm the view now rather than at the first tip — see PreloadToolkitView.
        PreloadToolkitView();
    }

    void OnDisable() {
        Messenger<string>.RemoveListener(ButtonEvents.EVENT_BUTTON_CLICK, OnButtonClickEventHandler);

        Messenger<string>.RemoveListener(GameNotificationMessages.gameQueueTipAchievement, OnQueueAchievement);
        Messenger<string, string>.RemoveListener(GameNotificationMessages.gameQueueTipError, OnQueueError);
        Messenger<string, string>.RemoveListener(GameNotificationMessages.gameQueueTipInfo, OnQueueInfo);
        Messenger<string, string>.RemoveListener(GameNotificationMessages.gameQueueTipTip, OnQueueTip);
        Messenger<string, string, double>.RemoveListener(GameNotificationMessages.gameQueueTipPoint, OnQueuePoint);

        // Symmetric with the preload above: release the view and put the legacy widgets back.
        FreeToolkitView();
    }

    void OnButtonClickEventHandler(string buttonName) {

        // The toolkit ButtonIcon broadcasts the same name the legacy one does, so tipContinue's
        // name compare catches it in the NGUI build. The second test is for the no-NGUI build,
        // where tipContinue is an unbound UIRef and could never match.
        if(UIUtil.IsButtonClicked(tipContinue, buttonName)
            || (isToolkitPanel && UIUtil.IsButtonClicked(elementButtonIcon, buttonName))) {
            HideDialog();
        }
    }

    void OnQueueAchievement(string key) {
        QueueAchievement(key);
    }
    
    void OnQueueError(string title, string description) {
        QueueError(title, description);
    }

    void OnQueueInfo(string title, string description) {
        QueueInfo(title, description);
    }

    void OnQueueTip(string title, string description) {
        QueueTip(title, description, true);
    }

    void OnQueuePoint(string title, string description, double points) {
        QueuePoint(title, description, points);
    }

    //

    public void QueueNotification(
        string title,
        string description,
        double score,
        UINotificationTipType notificationType) {

        QueueNotification(title, description, score, notificationType, false);
    }

    public void QueueNotification(
        string title,
        string description,
        double score,
        UINotificationTipType notificationType,
        bool immediate) {

        UINotificationTipItem notification = new UINotificationTipItem();
        notification.title = title;
        notification.description = description;
        notification.notificationType = notificationType;
        notification.score = score.ToString("N0", Engine.Game.App.BaseApp.L10n.NumberFormat);
        notification.immediate = immediate;
        QueueNotification(notification);
    }

    public void QueueAchievement(string title, string description, double points) {
        QueueNotification(title, description, points, UINotificationTipType.Achievement);
    }

    public void QueuePoint(string title, string description, double points) {
        QueueNotification(title, description, points, UINotificationTipType.Point);
    }

    public void QueueInfo(string title, string description) {
        if(notificationQueue.Count < 10) {
            QueueNotification(title, description, 0, UINotificationTipType.Info);
        }
    }

    public void QueueError(string title, string description) {
        if(notificationQueue.Count < 10) {
            QueueNotification(title, description, 0, UINotificationTipType.Error);
        }
    }

    public void QueueTip(string title, string description) {
        if(notificationQueue.Count < 10) {
            QueueNotification(title, description, 0, UINotificationTipType.Tip);
        }
    }

    public void QueueTip(string title, string description, bool immediate) {
        if(notificationQueue.Count < 10) {
            QueueNotification(title, description, 0, UINotificationTipType.Tip, immediate);
        }
    }

    public void QueueNotification(UINotificationTipItem notificationItem) {
        notificationQueue.Enqueue(notificationItem);

        LogUtil.Log("Notification Queue("
            + notificationQueue.Count + ") "
            + "Notification Added:title:"
            + notificationItem.title
            + " notificationType:"
            + notificationItem.notificationType

        );

        if(notificationItem.immediate) {
            HideDialog();
        }

        ProcessNotifications();
    }

    public void QueueAchievement(string achievementCode) {

        string packCode = GamePacks.Current.code;
        string app_state = AppStates.Current.code;
        string app_content_state = AppContentStates.Current.code;

        LogUtil.Log("QueueAchievement:",
                    " achievementCode:" + achievementCode
            + " packCode:" + packCode
            + " app_state:" + app_state
            + " app_content_state:" + app_content_state
        );

        string achievementBaseCode = achievementCode;
        achievementBaseCode = achievementBaseCode.Replace("-" + app_state, "");
        achievementBaseCode = achievementBaseCode.Replace("_" + GameAchievementCodes.formatAchievementCode(app_state), "");
        achievementBaseCode = achievementBaseCode.Replace("-" + app_content_state, "");
        achievementBaseCode = achievementBaseCode.Replace("_" + GameAchievementCodes.formatAchievementCode(app_content_state), "");
        achievementBaseCode = achievementBaseCode.Replace("-" + packCode, "");
        achievementBaseCode = achievementBaseCode.Replace("_" + GameAchievementCodes.formatAchievementCode(packCode), "");

        LogUtil.Log("QueueAchievement2:",
                    " achievementCode:" + achievementCode
            + " achievementBaseCode:" + achievementBaseCode
        );

        GameAchievement achievement
            = GameAchievements.Instance.GetByCodeAndPack(
                achievementBaseCode,
                packCode,
                app_content_state
        );


        if(achievement != null) {
            //achievement.description = GameAchievements.Instance.FormatAchievementTags(
            //  app_state,
            //  app_content_state, 
            //  achievement.description);
            //LogUtil.Log("Queueing Achievement display:" + achievement.display_name);
        }
        else {
            LogUtil.Log("Achievement not found:" + achievementCode);
        }

        if(achievement != null) {
            UINotificationTipItem item = new UINotificationTipItem();
            item.code = achievement.code;
            item.description = achievement.description;
            item.icon = "";
            item.notificationType = UINotificationTipType.Achievement;
            item.score = achievement.data.points.ToString();
            item.title = achievement.display_name;
            QueueNotification(item);
        }

        if(achievementCode == "achieve_test1") {

            UINotificationTipItem item = new UINotificationTipItem();
            item.code = achievementCode;
            item.description = "This is an achievement test, you did awesome!";
            item.icon = "";
            item.notificationType = UINotificationTipType.Achievement;
            item.score = 3.ToString();
            item.title = "First Achievement Tested";
            QueueNotification(item);
        }
    }

    public void ClearQueue() {
        if(notificationQueue != null) {
            if(notificationQueue.Count > 0) {
                notificationQueue.Clear();
            }
        }
    }

    public void HideAndClearQueue() {
        ClearQueue();
        HideDialog();
    }

    public void ToggleDialog() {
        if(notificationState == UINotificationTipState.Hidden) {
            // Show
            ShowDialog();
        }
        else {
            // Hide
            HideDialog();
        }
    }

    public void ShowDialog() {

        ShowCamera();

        // The legacy panel is driven EITHER WAY. With the view up its widgets are suppressed, so
        // the move is invisible — but it keeps the panel's open/closed position true, so freeing
        // the view (kill switch off) hands back a legacy tip that is parked where it should be.
        TweenUtil.MoveToObject(notificationPanel, Vector3.zero.WithY(positionYOpenInGame), .6f, 0f);

        if(isToolkitPanel) {
            // Bottom-anchored like the legacy panel (AnchorBottom, closed at y -900), so it rises
            // from below the screen instead of dropping from the top like the sibling toast.
            UIUtil.ShowObject(viewRoot);
            TweenUtil.ShowObjectBottom(viewRoot, toolkitShowPreset);
        }

        Invoke("HideDialog", 3.0f);

        SetStateShowing();

        bool audioPlaySuccess = false;

        if(currentItem != null) {
            if(currentItem.notificationType == UINotificationTipType.Achievement) {
                audioPlaySuccess = true;
            }
            else if(currentItem.notificationType == UINotificationTipType.Error) {
                audioPlaySuccess = false;
                GameAudio.PlayEffect(GameAudioEffects.audio_effect_ui_button_1);
            }
            else if(currentItem.notificationType == UINotificationTipType.Info) {
                audioPlaySuccess = false;
                GameAudio.PlayEffect(GameAudioEffects.audio_effect_ui_button_1);
                GameAudio.PlayEffect(GameAudioEffects.audio_effect_ui_button_2);
            }
            else if(currentItem.notificationType == UINotificationTipType.Point) {
                audioPlaySuccess = true;
            }
            else if(currentItem.notificationType == UINotificationTipType.Tip) {
            }
        }

        if(audioPlaySuccess) {
            GameAudio.PlayEffect(GameAudioEffects.audio_effect_pickup_1);
        }
    }

    public void HideDialog() {

        TweenUtil.MoveToObject(notificationPanel, Vector3.zero.WithY(positionYClosedInGame), .2f, 0f);

        if(isToolkitPanel) {
            TweenUtil.HideObjectBottom(viewRoot, toolkitHidePreset);
        }

        Invoke("DisplayNextNotification", 1);
    }

    public void DisplayNextNotification() {

        HideCamera();

        SetStateHidden();
        ProcessNotifications();
    }

    public void Update() {

        if(Input.GetKeyDown(KeyCode.Alpha6)) {
            //achievementNumber++;
            //QueueAchievement("achieve_test1");
            //QueueAchievement("achieve_find_first");
            //QueueAchievement("Achievement here", "This is an achievement", 10);
        }

        if(Input.GetKeyDown(KeyCode.Alpha7)) {
            //achievementNumber++;
            //QueueError("Error Here", "This is an error, oh snap!");
        }

        if(Input.GetKeyDown(KeyCode.Alpha8)) {
            //achievementNumber++;
            QueueInfo("Info Here", "This is an info, just an FYI!");
        }

        if(Input.GetKeyDown(KeyCode.Alpha9)) {
            //achievementNumber++;
            QueueTip("CONTROL TIPS", "SWIPE TO ROTATE | PINCH TO ZOOM | TAP TO ADVANCE");
            QueueTip("SPECIAL TIPS", "TAP the crank on the box to start the box.");
        }

        if(Input.GetKeyDown(KeyCode.Alpha0)) {
            //achievementNumber++;
            //QueuePoint("Point Here", "This is an point, do better!", 1);
        }
    }

    public bool Paused {
        get {
            return false;
        }
        set {
            paused = value;
        }
    }

    public void ProcessNotifications() {
        if(!Paused) {
            if(notificationQueue.Count > 0)
                if(notificationState == UINotificationTipState.Hidden)
                    ProcessNextNotification();
        }
    }

    public void ShowNotificationContainerType(UINotificationTipType type) {

        ShowToolkitContainerType(type);

        if(type == UINotificationTipType.Achievement) {
            GameObjectHelper.ShowObject(notificationContainerAchievement);
        }
        else {
            GameObjectHelper.HideObject(notificationContainerAchievement);
        }

        if(type == UINotificationTipType.Point) {
            GameObjectHelper.ShowObject(notificationContainerPoint);
        }
        else {
            GameObjectHelper.HideObject(notificationContainerPoint);
        }

        if(type == UINotificationTipType.Error) {
            GameObjectHelper.ShowObject(notificationContainerError);
        }
        else {
            GameObjectHelper.HideObject(notificationContainerError);
        }

        if(type == UINotificationTipType.Tip) {
            GameObjectHelper.ShowObject(notificationContainerTip);
        }
        else {
            GameObjectHelper.HideObject(notificationContainerTip);
        }

        if(type == UINotificationTipType.Info) {
            GameObjectHelper.ShowObject(notificationContainerInfo);
        }
        else {
            GameObjectHelper.HideObject(notificationContainerInfo);
        }
    }

    public void ProcessNextNotification() {
        if(!Paused) {
            if(notificationQueue.Count > 0) {
                UINotificationTipItem notificationItem = notificationQueue.Dequeue();
                bool found = false;


                if(notificationItem.notificationType == UINotificationTipType.Achievement) {

                    ShowNotificationContainerType(notificationItem.notificationType);
                    UIUtil.SetLabelValue(achievementTitle, notificationItem.title);
                    UIUtil.SetLabelValue(achievementDescription, notificationItem.description);
                    UIUtil.SetLabelValue(achievementScore, notificationItem.score);

                    found = true;
                }
                else if(notificationItem.notificationType == UINotificationTipType.Point) {

                    ShowNotificationContainerType(notificationItem.notificationType);
                    UIUtil.SetLabelValue(pointTitle, notificationItem.title);
                    UIUtil.SetLabelValue(pointDescription, notificationItem.description);
                    UIUtil.SetLabelValue(pointScore, notificationItem.score);

                    found = true;
                }
                else if(notificationItem.notificationType == UINotificationTipType.Info) {

                    ShowNotificationContainerType(notificationItem.notificationType);
                    UIUtil.SetLabelValue(infoTitle, notificationItem.title);
                    UIUtil.SetLabelValue(infoDescription, notificationItem.description);
                    UIUtil.SetLabelValue(infoScore, notificationItem.score);


                    found = true;
                }
                else if(notificationItem.notificationType == UINotificationTipType.Tip) {

                    ShowNotificationContainerType(notificationItem.notificationType);
                    UIUtil.SetLabelValue(tipTitle, notificationItem.title);
                    UIUtil.SetLabelValue(tipDescription, notificationItem.description);
                    UIUtil.SetLabelValue(tipScore, notificationItem.score);

                    found = true;
                }
                else if(notificationItem.notificationType == UINotificationTipType.Error) {

                    ShowNotificationContainerType(notificationItem.notificationType);
                    UIUtil.SetLabelValue(errorTitle, notificationItem.title);
                    UIUtil.SetLabelValue(errorDescription, notificationItem.description);
                    UIUtil.SetLabelValue(errorScore, notificationItem.score);

                    found = true;
                }

                if(found) {

                    // Recorded BEFORE ShowDialog, and as a replay: currentItem is only assigned
                    // after ShowDialog returns, and the view can still be building on the first tip.
                    ApplyToolkitItem(notificationItem);

                    LogUtil.Log("Notification Queue("
                        + notificationQueue.Count + ") "
                        + "Notification Removed:title:"
                        + notificationItem.title
                        + " notificationType:"
                        + notificationItem.notificationType

                    );

                    ShowDialog();
                }

                currentItem = notificationItem;
            }
        }
    }

    public void SetStateShowing() {
        notificationState = UINotificationTipState.Showing;
    }

    public void SetStateHidden() {
        notificationState = UINotificationTipState.Hidden;
    }

    // ==========================================================================================
    // UI TOOLKIT (B2 — the bottom TIP toast)
    //
    // The sibling of UINotificationDisplay's wave-3G seam, and built the same way for the same
    // reason: this class is a UIAppPanel, NOT a UIPanelBase, and core game-lib-* are additive-only
    // and shared with other products, so it is NOT reparented. It carries its own small copy of
    // the seam (preload / EnsureToolkitView / LoadToolkitView / SuppressLegacyView /
    // FreeToolkitView), drives UIPlatform.viewBackend and TweenUtil directly, and shares the
    // sibling's element-name constants. Every branch is gated on a LOADED view, so a product with
    // no panel-notification-tip.uxml behaves exactly as before.
    //
    // WHY: the shared PanelSettings renders in OVERLAY mode, so a tip left on NGUI draws UNDER
    // every toolkit view — in a round that is the toolkit HUD, which is where "Weapon Loaded"
    // shows. See contexts/games/action-bots/ui-toolkit/context-notification-overlay-sort.md.
    //
    // WHAT THE LEGACY TIP ACTUALLY DRAWS: the scene instance wires ONLY the tip fields
    // (tipTitle/tipDescription/tipScore/tipContinue, notificationPanel, notificationContainerTip).
    // The achievement/point/info/error containers exist in the scene but are inactive and their
    // fields are unbound, so a gameQueueTipInfo/Error/Achievement/Point toast slides up the band
    // with ContainerTip HIDDEN and nothing on it. The view reproduces that: it carries the band and
    // ContainerTip only, and ShowToolkitContainerType hides ContainerTip for the other four types.
    // A product whose view also carries ContainerInfo etc. gets them shown and written by name.

    private bool toolkitLoadRequested = false;

    // The last item pushed at the toolkit view, kept for the replay when the view lands after the
    // first tip was already processed.
    private UINotificationTipItem toolkitItem = null;
    private readonly List<GameObject> toolkitSuppressed = new List<GameObject>();

    // Element names are the WIRE CONTRACT — the legacy GameObject names. The label/container names
    // are shared with the sibling toast; these three are the tip's own.
    public const string elementButtonIcon = "ButtonIcon";
    public const string elementBackground = "SpriteBackground";
    public const string elementNote = "LabelNote";

    public bool isToolkitPanel {
        get {
            return viewRoot != null && viewRoot.alive;
        }
    }

    public virtual string toolkitViewKey {
        get {
            return BaseUIPanel.panelNotificationTip;
        }
    }

    // One above the sibling toast's band: legacy draws the tip on LoadCamera (depth 69), above
    // the toast's OverlayCamera (55), so a tip and a toast on screen together keep that order.
    public virtual int toolkitSortOrder {
        get {
            return UILayers.notification + 1;
        }
    }

    public virtual string toolkitShowPreset {
        get {
            return "panel-show";
        }
    }

    public virtual string toolkitHidePreset {
        get {
            return "panel-hide";
        }
    }

    // PRELOADED, not lazy — the tip interrupts a live round, and a view built on the first
    // ShowDialog would arrive a frame or two late and pop in flat. Deferred one frame ON PURPOSE:
    // UIToolkitHost publishes the shared PanelSettings from its own OnEnable, and Unity does not
    // order OnEnable between scene objects (the sibling's note, same trap).
    protected virtual void PreloadToolkitView() {

        if(!gameObject.activeInHierarchy) {
            return;
        }

        StartCoroutine(PreloadToolkitViewCo());
    }

    IEnumerator PreloadToolkitViewCo() {

        yield return new WaitForEndOfFrame();

        EnsureToolkitView();
    }

    protected virtual void EnsureToolkitView() {

        if(!UIPlatform.toolkitViewsEnabled) {
            return;
        }

        if(isToolkitPanel || string.IsNullOrEmpty(toolkitViewKey)) {
            return;
        }

        LoadToolkitView(toolkitViewKey);
    }

    public virtual void LoadToolkitView(string viewKey) {

        IUIBackend backend = UIPlatform.viewBackend;

        if(backend == null || string.IsNullOrEmpty(viewKey) || toolkitLoadRequested) {
            return;
        }

        toolkitLoadRequested = true;

        backend.LoadView(viewKey, toolkitSortOrder, (UIRef view) => {

            if(view == null || !view.alive) {
                // No UXML for this key: stay on NGUI, and allow a later retry.
                toolkitLoadRequested = false;
                return;
            }

            if(!toolkitLoadRequested) {
                // Freed while the deferred PanelRenderer build was still pending.
                backend.DestroyView(view);
                return;
            }

            viewRoot = view;

            SuppressLegacyView();

            // Match the state we are actually in: the load lands whenever the PanelRenderer gets
            // round to it, and the tip is hidden far more often than it is shown.
            if(notificationState == UINotificationTipState.Showing) {
                backend.Show(view);
                ApplyToolkitItem(null);
                TweenUtil.ShowObjectBottom(viewRoot, toolkitShowPreset);
            }
            else {
                backend.Hide(view);
            }
        });
    }

    // Hides the legacy widgets so they cannot draw underneath the view. The tip has no 3D coin
    // to keep alive (its Coin and Icon are inactive in the scene), so unlike the sibling it puts
    // away the whole Containers node — band, all five containers and the ButtonIcon collider.
    // This component sits on the panel ROOT, above Containers, so its Invokes and queue survive.
    //
    // tipContinue survives too: hiding a GameObject does not clear the reference, so the name
    // compare in OnButtonClickEventHandler still matches a toolkit "ButtonIcon" click.
    protected virtual void SuppressLegacyView() {

        if(notificationPanel == null || toolkitSuppressed.Count > 0) {
            return;
        }

        SuppressLegacyObject(notificationPanel.transform.Find("Containers"));
    }

    void SuppressLegacyObject(Transform t) {

        if(t == null || !t.gameObject.activeSelf) {
            return;
        }

        toolkitSuppressed.Add(t.gameObject);
        t.gameObject.SetActive(false);
    }

    protected virtual void FreeToolkitView() {

        // Symmetric restore, so flipping UIPlatform.toolkitViewsEnabled back off returns a working
        // legacy tip rather than an invisible one.
        for(int i = 0; i < toolkitSuppressed.Count; i++) {

            if(toolkitSuppressed[i] != null) {
                toolkitSuppressed[i].SetActive(true);
            }
        }

        toolkitSuppressed.Clear();

        if(!isToolkitPanel) {
            toolkitLoadRequested = false;
            return;
        }

        // Stop any in-flight slide before the VisualElement is detached, or the tween writes style
        // on a panel-less element.
        TweenUtil.Cancel(viewRoot);

        IUIBackend backend = UIPlatform.For(viewRoot);

        if(backend != null) {
            backend.DestroyView(viewRoot);
        }

        viewRoot = UIRef.none;
        toolkitLoadRequested = false;
    }

    public UIRef ToolkitContainer(UINotificationTipType type) {

        if(!isToolkitPanel) {
            return UIRef.none;
        }

        return UIUtil.ResolveDeep(viewRoot, ToolkitContainerName(type));
    }

    // The sibling's container names: both legacy panels name their five containers alike.
    public static string ToolkitContainerName(UINotificationTipType type) {

        if(type == UINotificationTipType.Achievement) {
            return UINotificationDisplay.elementContainerAchievement;
        }
        else if(type == UINotificationTipType.Point) {
            return UINotificationDisplay.elementContainerPoint;
        }
        else if(type == UINotificationTipType.Tip) {
            return UINotificationDisplay.elementContainerTip;
        }
        else if(type == UINotificationTipType.Error) {
            return UINotificationDisplay.elementContainerError;
        }

        return UINotificationDisplay.elementContainerInfo;
    }

    static readonly UINotificationTipType[] toolkitTypes = new UINotificationTipType[] {
        UINotificationTipType.Achievement,
        UINotificationTipType.Point,
        UINotificationTipType.Info,
        UINotificationTipType.Tip,
        UINotificationTipType.Error
    };

    // Mirrors ShowNotificationContainerType one-for-one: show the one, hide the other four. A
    // container the view does not carry resolves to UIRef.none and both calls no-op on it.
    protected virtual void ShowToolkitContainerType(UINotificationTipType type) {

        if(!isToolkitPanel) {
            return;
        }

        for(int i = 0; i < toolkitTypes.Length; i++) {

            UIRef container = ToolkitContainer(toolkitTypes[i]);

            if(toolkitTypes[i] == type) {
                UIUtil.ShowObject(container);
            }
            else {
                UIUtil.HideObject(container);
            }
        }
    }

    // The label writes, replayed onto the toolkit view by ELEMENT NAME. They cannot ride the
    // existing UIUtil.SetLabelValue(tipTitle, ...) calls: in the NGUI build those fields are
    // legacy UILabels (rule 26), so every write would land on the label SuppressLegacyView just
    // hid. Kept as a REPLAY (it takes the item, not the widget state) because the view may still
    // be building on the first tip of a session.
    //
    // tipTitle is LabelDisplayName in the scene (there is no LabelTitle under ContainerTip), and
    // tipScore is an INACTIVE CoinContainer/LabelScore — the view carries no score, so it is not
    // written. LabelNote ("- TAP TO DISMISS -") is static @loc text in the view.
    protected virtual void ApplyToolkitItem(UINotificationTipItem item) {

        if(item != null) {
            toolkitItem = item;
        }

        if(!isToolkitPanel || toolkitItem == null) {
            return;
        }

        ShowToolkitContainerType(toolkitItem.notificationType);

        UIRef container = ToolkitContainer(toolkitItem.notificationType);

        UIUtil.SetLabelValue(UIUtil.ResolveDeep(container, UINotificationDisplay.elementDisplayName), toolkitItem.title);
        UIUtil.SetLabelValue(UIUtil.ResolveDeep(container, UINotificationDisplay.elementDescription), toolkitItem.description);
    }

    // ==========================================================================================
}