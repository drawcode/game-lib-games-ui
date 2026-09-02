using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Engine.Utility;
using Engine.Game.App;

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
#else
using UnityEngine.UI;
#endif

using Engine.Events;
using Engine.UI;

public enum UINotificationState {
    Showing,
    Hidden
}

public enum UINotificationType {
    Info,
    Achievement,
    Tip,
    Error,
    Point
}

public class UINotificationItem {
    public string code = "";
    public string title = "";
    public string description = "";
    public string score = "";
    public string icon = "";
    public UINotificationType notificationType = UINotificationType.Info;

    public UINotificationItem() {

    }
}

public class UINotificationDisplay
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

    // Achievement
    public Text achievementTitle;
    public Text achievementDescription;
    public Text achievementScore;
    public Button achievementIcon;

    // Point
    public Text pointTitle;
    public Text pointDescription;
    public Text pointScore;
    public Button pointContinue;

    // Error
    public Text errorTitle;
    public Text errorDescription;
    public Text errorScore;
    public Button errorContinue;

    // Info
    public Text infoTitle;
    public Text infoDescription;
    public Text infoScore;
    public Button infoContinue;

    // Tip
    public Text tipTitle;
    public Text tipDescription;
    public Text tipScore;
    public Button tipContinue;
#endif

    public GameObject notificationPanel;
    public GameObject notificationContainerAchievement;
    public GameObject notificationContainerPoint;
    public GameObject notificationContainerInfo;
    public GameObject notificationContainerTip;
    public GameObject notificationContainerError;

    //UINotificationItem notificationItem;
    float positionYOpenInGame = 0;
    float positionYClosedInGame = 900;
    public static UINotificationDisplay Instance;
    public double currentScore = 0;
    public double lastScore = 0;
    UINotificationItem currentItem;
    UINotificationState notificationState = UINotificationState.Hidden;
    public bool paused = false;
    Queue<UINotificationItem> notificationQueue = new Queue<UINotificationItem>();

    public bool IsHidden {
        get {
            if (notificationState == UINotificationState.Hidden)
                return true;

            return false;
        }
    }

    public override void Awake() {

        base.Awake();

        if (Instance != null && this != Instance) {
            //There is already a copy of this script running
            Destroy(this);
            return;
        }

        Instance = this;

        //DontDestroyOnLoad(gameObject);
    }

    public override void Start() {

        base.Start();

        notificationState = UINotificationState.Hidden;
        HideDialog();
    }

    void OnEnable() {
        Messenger<string>.AddListener(ButtonEvents.EVENT_BUTTON_CLICK, OnButtonClickEventHandler);

        Messenger<string>.AddListener(GameNotificationMessages.gameQueueAchievement, OnQueueAchievement);
        Messenger<string, string>.AddListener(GameNotificationMessages.gameQueueError, OnQueueError);
        Messenger<string, string>.AddListener(GameNotificationMessages.gameQueueInfo, OnQueueInfo);
        Messenger<string, string>.AddListener(GameNotificationMessages.gameQueueTip, OnQueueTip);
        Messenger<string, string, double>.AddListener(GameNotificationMessages.gameQueuePoint, OnQueuePoint);

        // Warm the view now rather than at the first toast — see PreloadToolkitView.
        PreloadToolkitView();
    }

    void OnDisable() {
        Messenger<string>.RemoveListener(ButtonEvents.EVENT_BUTTON_CLICK, OnButtonClickEventHandler);

        Messenger<string>.RemoveListener(GameNotificationMessages.gameQueueAchievement, OnQueueAchievement);
        Messenger<string, string>.RemoveListener(GameNotificationMessages.gameQueueError, OnQueueError);
        Messenger<string, string>.RemoveListener(GameNotificationMessages.gameQueueInfo, OnQueueInfo);
        Messenger<string, string>.RemoveListener(GameNotificationMessages.gameQueueTip, OnQueueTip);
        Messenger<string, string, double>.RemoveListener(GameNotificationMessages.gameQueuePoint, OnQueuePoint);

        // Symmetric with the preload above: release the view so its PanelRenderer is reclaimed,
        // put the legacy widgets back, and detach the coin stage.
        FreeToolkitView();
    }

    void OnButtonClickEventHandler(string buttonName) {

        if (UIUtil.IsButtonClicked(achievementIcon, buttonName)) {
            HideDialog();
        }
        else if (UIUtil.IsButtonClicked(pointContinue, buttonName)) {
            HideDialog();
        }
        else if (UIUtil.IsButtonClicked(errorContinue, buttonName)) {
            HideDialog();
        }
        else if (UIUtil.IsButtonClicked(infoContinue, buttonName)) {
            HideDialog();
        }
        else if (UIUtil.IsButtonClicked(tipContinue, buttonName)) {
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
        QueueTip(title, description);
    }

    void OnQueuePoint(string title, string description, double points) {
        QueuePoint(title, description, points);
    }

    //void OnQueueInfo(string title, string message) {
    //    QueueNotification(title, description, score, notificationType);
    //}

    // NOTIFICATION

    public static void QueueNotification(
            string title,
            string description,
            double score,
            UINotificationType notificationType) {
        if (Instance != null) {
            Instance.queueNotification(
                title,
                description,
                score,
                notificationType);
        }
    }

    public void queueNotification(
        string title,
        string description,
        double score,
        UINotificationType notificationType) {

        UINotificationItem notification = new UINotificationItem();
        notification.title = title;
        notification.description = description;
        notification.notificationType = notificationType;
        notification.score = score.ToString("N0");
        QueueNotification(notification);
    }

    // ACHIEVEMENT

    public static void QueueAchievement(string title, string description, double points) {
        if (Instance != null) {
            Instance.queueAchievement(title, description, points);
        }
    }

    public void queueAchievement(string title, string description, double points) {
        queueNotification(title, description, points, UINotificationType.Achievement);
    }

    // POINT

    public static void QueuePoint(string title, string description, double points) {
        if (Instance != null) {
            Instance.queuePoint(title, description, points);
        }
    }

    public void queuePoint(string title, string description, double points) {
        queueNotification(title, description, points, UINotificationType.Point);
    }

    // INFO

    public static void QueueInfo(string title, string description) {
        if (Instance != null) {
            Instance.queueInfo(title, description);
        }
    }

    public void queueInfo(string title, string description) {
        QueueNotification(title, description, 0, UINotificationType.Info);
    }

    // ERROR

    public static void QueueError(string title, string description) {
        if (Instance != null) {
            Instance.queueError(title, description);
        }
    }

    public void queueError(string title, string description) {
        QueueNotification(title, description, 0, UINotificationType.Error);
    }

    // TIP

    public static void QueueTip(string title, string description) {
        if (Instance != null) {
            Instance.queueTip(title, description);
        }
    }

    public void queueTip(string title, string description) {
        QueueNotification(title, description, 0, UINotificationType.Tip);
    }

    // NOTIFICATION MAIN

    public void QueueNotification(UINotificationItem notificationItem) {
        if (Instance != null) {
            Instance.queueNotification(notificationItem);
        }
    }

    public void queueNotification(UINotificationItem notificationItem) {

        foreach (UINotificationItem item in notificationQueue) {
            if (item.title == notificationItem.title) {
                return;
            }
        }

        if (currentItem != null) {
            if (currentItem.title == notificationItem.title) {
                return;
            }
        }

        notificationQueue.Enqueue(notificationItem);

        LogUtil.Log("Notification Queue("
            + notificationQueue.Count + ") "
            + "Notification Added:title:"
            + notificationItem.title
            + " notificationType:"
            + notificationItem.notificationType

        );

        ProcessNotifications();
    }

    public void QueueAchievement(string achievementCode) {

        LogUtil.Log("Queueing Achievement:achievementCode:" + achievementCode);
        string packCode = GamePacks.Current.code;
        string app_state = AppStates.Current.code;
        string app_content_state = AppContentStates.Current.code;

        string achievementBaseCode = achievementCode;
        achievementBaseCode = achievementBaseCode.Replace("-" + app_state, "");
        achievementBaseCode = achievementBaseCode.Replace("_" + GameAchievementCodes.formatAchievementCode(app_state), "");
        achievementBaseCode = achievementBaseCode.Replace("-" + app_content_state, "");
        achievementBaseCode = achievementBaseCode.Replace("_" + GameAchievementCodes.formatAchievementCode(app_content_state), "");
        achievementBaseCode = achievementBaseCode.Replace("-" + packCode, "");
        achievementBaseCode = achievementBaseCode.Replace("_" + GameAchievementCodes.formatAchievementCode(packCode), "");

        GameAchievement achievement
            = GameAchievements.Instance.GetByCodeAndPack(
                achievementCode,
                packCode//,
                        //app_content_state
        );


        if (achievement != null) {
            //achievement.description = GameAchievements.Instance.FormatAchievementTags(
            //  app_state,
            //  app_content_state, 
            //  achievement.description);
            //LogUtil.Log("Queueing Achievement display:" + achievement.display_name);

        }
        else {
            LogUtil.Log("Achievement not found:" + achievementCode);
        }

        if (achievement != null) {
            UINotificationItem item = new UINotificationItem();
            item.code = achievement.code;
            item.description = achievement.description;
            item.icon = "";
            item.notificationType = UINotificationType.Achievement;
            item.score = achievement.data.points.ToString();
            item.title = achievement.display_name;
            QueueNotification(item);
        }

        if (achievementCode == "achieve_test1") {

            UINotificationItem item = new UINotificationItem();
            item.code = achievementCode;
            item.description = "This is an achievement test, you did awesome!";
            item.icon = "";
            item.notificationType = UINotificationType.Achievement;
            item.score = 3.ToString();
            item.title = "First Achievement Tested";
            QueueNotification(item);
        }
    }

    public void ToggleDialog() {
        if (notificationState == UINotificationState.Hidden) {
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

        // Unconditional, and BEFORE the branch: it also RECORDS the item, so a view that is
        // still building on the very first toast can replay it from LoadToolkitView's continuation.
        ApplyToolkitItem(currentItem);

        if(isToolkitPanel) {

            // Deliberately NOT MoveToObject. The legacy panel stays parked at its open position
            // because the 3D coin that feeds the UIRenderStage is parented under it and the stage
            // frames its camera once — sliding the panel away would slide the coin out of frame
            // and the toast would draw an empty RenderTexture. The VIEW does the sliding instead.
            UIUtil.ShowObject(viewRoot);
            TweenUtil.ShowObjectTop(viewRoot, toolkitShowPreset);
        }
        else {
            TweenUtil.MoveToObject(notificationPanel, Vector3.zero.WithY(positionYOpenInGame), .6f, 0f);
        }

        Invoke("HideDialog", 4.5f);

        SetStateShowing();

        bool audioPlaySuccess = false;

        if (currentItem != null) {
            if (currentItem.notificationType == UINotificationType.Achievement) {
                audioPlaySuccess = true;
            }
            else if (currentItem.notificationType == UINotificationType.Error) {
            }
            else if (currentItem.notificationType == UINotificationType.Info) {
                audioPlaySuccess = true;
            }
            else if (currentItem.notificationType == UINotificationType.Point) {
                audioPlaySuccess = true;
            }
            else if (currentItem.notificationType == UINotificationType.Tip) {
            }
        }

        if (audioPlaySuccess) {
            GameAudio.PlayEffect(GameAudioEffects.audio_effect_pickup_1);
        }

        currentItem = null;
    }

    public void HideDialog() {

        if(isToolkitPanel) {
            TweenUtil.HideObjectTop(viewRoot, toolkitHidePreset);
        }
        else {
            TweenUtil.MoveToObject(notificationPanel, Vector3.zero.WithY(positionYClosedInGame), .2f, 0f);
        }

        Invoke("DisplayNextNotification", 1);
    }

    public void DisplayNextNotification() {

        HideCamera();

        SetStateHidden();
        ProcessNotifications();
    }

    /*
public void Update() {

            if(Input.GetKeyDown(KeyCode.Alpha1)) {
                //achievementNumber++;
                QueueAchievement("achieve_test1");
                //QueueAchievement("achieve_find_first");
                QueueAchievement("Achievement here", "This is an achievement", 10);
            }

            if(Input.GetKeyDown(KeyCode.Alpha2)) {
                //achievementNumber++;
                QueueError("Error Here", "This is an error, oh snap!");
            }       

            if(Input.GetKeyDown(KeyCode.Alpha3)) {
                //achievementNumber++;
                QueueInfo("Info Here", "This is an info, just an FYI!");
            }

            if(Input.GetKeyDown(KeyCode.Alpha4)) {
                //achievementNumber++;
                QueueTip("Tip Here", "This is an tip, do better!");
            }

            if(Input.GetKeyDown(KeyCode.Alpha5)) {
                //achievementNumber++;
                QueuePoint("Point Here", "This is an point, do better!", 1);
            }


}      */

    public bool Paused {
        get {
            return false;
        }
        set {
            paused = value;
        }
    }

    public void ProcessNotifications() {
        if (!Paused) {
            if (notificationQueue.Count > 0)
                if (notificationState == UINotificationState.Hidden)
                    ProcessNextNotification();
        }
    }

    public void ShowNotificationContainerType(UINotificationType type) {

        ShowToolkitContainerType(type);

        if (type == UINotificationType.Achievement) {
            GameObjectHelper.ShowObject(notificationContainerAchievement);
        }
        else {
            GameObjectHelper.HideObject(notificationContainerAchievement);
        }

        if (type == UINotificationType.Point) {
            GameObjectHelper.ShowObject(notificationContainerPoint);
        }
        else {
            GameObjectHelper.HideObject(notificationContainerPoint);
        }

        if (type == UINotificationType.Error) {
            GameObjectHelper.ShowObject(notificationContainerError);
        }
        else {
            GameObjectHelper.HideObject(notificationContainerError);
        }

        if (type == UINotificationType.Tip) {
            GameObjectHelper.ShowObject(notificationContainerTip);
        }
        else {
            GameObjectHelper.HideObject(notificationContainerTip);
        }

        if (type == UINotificationType.Info) {
            GameObjectHelper.ShowObject(notificationContainerInfo);
        }
        else {
            GameObjectHelper.HideObject(notificationContainerInfo);
        }
    }

    public void ProcessNextNotification() {
        if (!Paused) {
            if (notificationQueue.Count > 0) {

                currentItem = notificationQueue.Dequeue();

                bool found = false;


                if (currentItem.notificationType == UINotificationType.Achievement) {

                    ShowNotificationContainerType(currentItem.notificationType);
                    UIUtil.SetLabelValue(achievementTitle, currentItem.title);
                    UIUtil.SetLabelValue(achievementDescription, currentItem.description);

                    if (GameConfigs.useCoinRewardsForAchievements) {
                        double score = Convert.ToDouble(currentItem.score);
                        score *= 50; // 50 coins per   
                        lastScore = 0;
                        currentScore = score;
                        currentItem.score = currentScore.ToString("N0");
                        GameProfileRPGs.Current.AddCurrency(currentScore);
                    }

                    UIUtil.SetLabelValue(achievementScore, "+" + currentItem.score);

                    found = true;
                }
                else if (currentItem.notificationType == UINotificationType.Point) {

                    ShowNotificationContainerType(currentItem.notificationType);
                    UIUtil.SetLabelValue(pointTitle, currentItem.title);
                    UIUtil.SetLabelValue(pointDescription, currentItem.description);
                    UIUtil.SetLabelValue(pointScore, "+" + currentItem.score);

                    found = true;
                }
                else if (currentItem.notificationType == UINotificationType.Info) {

                    ShowNotificationContainerType(currentItem.notificationType);
                    UIUtil.SetLabelValue(infoTitle, currentItem.title);
                    UIUtil.SetLabelValue(infoDescription, currentItem.description);
                    UIUtil.SetLabelValue(infoScore, "");


                    found = true;
                }
                else if (currentItem.notificationType == UINotificationType.Tip) {

                    ShowNotificationContainerType(currentItem.notificationType);
                    UIUtil.SetLabelValue(tipTitle, currentItem.title);
                    UIUtil.SetLabelValue(tipDescription, currentItem.description);
                    UIUtil.SetLabelValue(tipScore, "");

                    found = true;
                }
                else if (currentItem.notificationType == UINotificationType.Error) {

                    ShowNotificationContainerType(currentItem.notificationType);
                    UIUtil.SetLabelValue(errorTitle, currentItem.title);
                    UIUtil.SetLabelValue(errorDescription, currentItem.description);
                    UIUtil.SetLabelValue(errorScore, "");

                    found = true;
                }

                if (found) {

                    LogUtil.Log("Notification Queue("
                        + notificationQueue.Count + ") "
                        + "Notification Removed:title:"
                        + currentItem.title
                        + " notificationType:"
                        + currentItem.notificationType

                    );

                    ShowDialog();
                }
            }
        }
    }

    public void SetStateShowing() {
        notificationState = UINotificationState.Showing;
    }

    public void SetStateHidden() {
        notificationState = UINotificationState.Hidden;
    }

    // ==========================================================================================
    // UI TOOLKIT (wave 3G — the notification toast)
    //
    // This class is a UIAppPanel, NOT a UIPanelBase, so none of the toolkitViewKey /
    // LoadToolkitView / SuppressLegacyView / FreeToolkitView plumbing is inherited. Core
    // game-lib-* are additive-only and shared with other products, so the class is NOT reparented;
    // it carries its own small copy of the seam instead, driving UIPlatform.viewBackend and
    // TweenUtil directly. Every branch below is gated on a LOADED view, so a project with no
    // panel-notification.uxml behaves exactly as it did before.
    //
    // WHY IT HAD TO BE CONVERTED AT ALL: the shared PanelSettings renders in OVERLAY mode, so
    // every toolkit view composites after every camera. A toast left on NGUI draws UNDER the
    // toolkit header whatever its NGUI depth is — the coin count and the FPS readout drew straight
    // across it. That is the "achievements header sort" report. UILayers.notification (30000) puts
    // the view above dialogs, matching the legacy camera order (OverlayCamera 55 > DialogCamera 15).

    private bool toolkitLoadRequested = false;

    // The last item pushed at the toolkit view. ShowDialog clears currentItem on its way out, and
    // the view can still be building on the very first toast, so the replay needs its own copy.
    private UINotificationItem toolkitItem = null;
    private Engine.UI.UIRenderStage toolkitCoinStage;
    private readonly List<GameObject> toolkitSuppressed = new List<GameObject>();

    // Element names are the WIRE CONTRACT — they are the legacy GameObject names, which is what
    // the click bus broadcasts and what UIUtil.IsButtonClicked compares against.
    public const string elementTitle = "LabelTitle";
    public const string elementDisplayName = "LabelDisplayName";
    public const string elementDescription = "LabelDescription";
    public const string elementScore = "LabelScore";
    public const string elementCoin = "Coin";

    public const string elementContainerAchievement = "ContainerAchievement";
    public const string elementContainerPoint = "ContainerPoint";
    public const string elementContainerInfo = "ContainerInfo";
    public const string elementContainerTip = "ContainerTip";
    public const string elementContainerError = "ContainerError";

    public bool isToolkitPanel {
        get {
            return viewRoot != null && viewRoot.alive;
        }
    }

    public virtual string toolkitViewKey {
        get {
            return BaseUIPanel.panelNotification;
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

    // PRELOADED, not lazy. A toast interrupts whatever is on screen, exactly like the 3F pause
    // overlay: if the view were built on the first ShowDialog it would arrive a frame or two late
    // and the first achievement of a session would pop in flat instead of sliding.
    //
    // Deferred one frame ON PURPOSE — UIToolkitHost publishes the shared PanelSettings from its
    // own OnEnable and Unity does not order OnEnable between scene objects, so going straight to
    // LoadView races the host and hits its "no PanelSettings registered" bail, which leaves the
    // toast silently on NGUI.
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

        backend.LoadView(viewKey, UILayers.notification, (UIRef view) => {

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

            // The toast is hidden far more often than it is shown, and the load lands whenever
            // the PanelRenderer gets round to it — so match the state we are actually in.
            if(notificationState == UINotificationState.Showing) {
                backend.Show(view);
                ApplyToolkitItem(null);
                TweenUtil.ShowObjectTop(viewRoot, toolkitShowPreset);
            }
            else {
                backend.Hide(view);
            }
        });
    }

    // Hides the legacy FLAT widgets so they cannot render underneath the view — but NOT the whole
    // panel, because the achievement variant's 3D coin has to keep living: it is the content of
    // the UIRenderStage whose RenderTexture the toolkit view draws. Everything under the Coin
    // subtree is left alone, everything carrying a UIWidget is put away and remembered.
    //
    // The legacy UIImageButton fields (achievementIcon, pointContinue, ...) survive this — hiding
    // a GameObject does not clear the reference, so IsButtonClicked's name compare still matches
    // when a toolkit "ButtonIcon" click arrives.
    protected virtual void SuppressLegacyView() {

        if(notificationPanel == null || toolkitSuppressed.Count > 0) {
            return;
        }

        Transform containers = notificationPanel.transform.Find("Containers");

        if(containers == null) {
            return;
        }

        // The band and the shared M.A.N. card are direct children of Containers.
        SuppressLegacyObject(containers.Find("SpriteBackground"));
        SuppressLegacyObject(containers.Find("Icon"));

        for(int i = 0; i < toolkitContainerNames.Length; i++) {

            Transform container = containers.Find(toolkitContainerNames[i]);

            if(container == null) {
                continue;
            }

            for(int c = 0; c < container.childCount; c++) {

                Transform child = container.GetChild(c);

                // Everything flat goes away — but NOT the coin rig. The achievement variant's
                // CoinContainer holds the 3D coin mesh that feeds the UIRenderStage whose
                // RenderTexture the toolkit view draws, so it is kept and its flat siblings
                // (ButtonIcon, LabelScore) are suppressed individually.
                if(child.name == "CoinContainer") {

                    for(int cc = 0; cc < child.childCount; cc++) {

                        Transform inner = child.GetChild(cc);

                        if(inner.name != "Coin") {
                            SuppressLegacyObject(inner);
                        }
                    }

                    continue;
                }

                SuppressLegacyObject(child);
            }
        }
    }

    void SuppressLegacyObject(Transform t) {

        if(t == null || !t.gameObject.activeSelf) {
            return;
        }

        toolkitSuppressed.Add(t.gameObject);
        t.gameObject.SetActive(false);
    }

    static readonly string[] toolkitContainerNames = new string[] {
        elementContainerAchievement,
        elementContainerPoint,
        elementContainerInfo,
        elementContainerTip,
        elementContainerError
    };

    Transform FindToolkitCoinRoot() {

        if(notificationContainerAchievement == null) {
            return null;
        }

        return notificationContainerAchievement.transform.Find("CoinContainer/Coin");
    }

    // The 3D coin, staged the way the header and HUD coins already are: moved to a widget layer
    // with its own camera and rendered to a RenderTexture the view draws as a plain image.
    //
    // lightIntensity 0 = DO NOT ADD A LIGHT. The stage light is per-LAYER, not per stage (its
    // cullingMask is the whole layer), so a sixth light here would brighten the header coin, the
    // HUD coin and every product-pack coin along with this one.
    void SetupToolkitCoinStage() {

        if(toolkitCoinStage != null || !isToolkitPanel) {
            return;
        }

        Transform coin = FindToolkitCoinRoot();

        // Attached LAZILY, from the first achievement toast, and only while the coin is live:
        // UIRenderStage.Frame() sizes its camera from the content's renderer bounds ONCE, and an
        // inactive mesh has none — staging early would leave the coin framed on nothing for the
        // whole session. ContainerAchievement is inactive whenever another variant is showing.
        if(coin == null || !coin.gameObject.activeInHierarchy) {
            return;
        }

        int layer = LayerMask.NameToLayer("UIWidget3D");

        if(layer < 0) {
            layer = LayerMask.NameToLayer("UI3D");
        }

        // followContent TRUE, and it is load-bearing. The legacy panel is still driven by
        // TweenUtil at boot (Start -> HideDialog runs before the view exists), so the coin is a
        // MOVING target: framed once at attach, the stage camera held the open position while the
        // coin travelled to the closed one, and the RT came back 0/16384 opaque — an invisible
        // coin, measured. This is the case the flag was added for.
        toolkitCoinStage = Engine.UI.UIRenderStage.Attach(coin.gameObject, layer, 128, 1.3f, false, true, 0f);

        if(toolkitCoinStage != null) {
            UIUtil.SetImageTexture(
                UIUtil.ResolveDeep(ToolkitContainer(UINotificationType.Achievement), elementCoin),
                toolkitCoinStage.texture);
        }
    }

    protected virtual void FreeToolkitView() {

        if(toolkitCoinStage != null) {
            toolkitCoinStage.Detach();
            toolkitCoinStage = null;
        }

        // Symmetric restore, so flipping UIPlatform.toolkitViewsEnabled back off returns a working
        // legacy toast rather than an invisible one.
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

    public UIRef ToolkitContainer(UINotificationType type) {

        if(!isToolkitPanel) {
            return UIRef.none;
        }

        return UIUtil.ResolveDeep(viewRoot, ToolkitContainerName(type));
    }

    public static string ToolkitContainerName(UINotificationType type) {

        if(type == UINotificationType.Achievement) {
            return elementContainerAchievement;
        }
        else if(type == UINotificationType.Point) {
            return elementContainerPoint;
        }
        else if(type == UINotificationType.Tip) {
            return elementContainerTip;
        }
        else if(type == UINotificationType.Error) {
            return elementContainerError;
        }

        return elementContainerInfo;
    }

    // Mirrors ShowNotificationContainerType one-for-one: show the one, hide the other four.
    protected virtual void ShowToolkitContainerType(UINotificationType type) {

        if(!isToolkitPanel) {
            return;
        }

        UINotificationType[] all = new UINotificationType[] {
            UINotificationType.Achievement,
            UINotificationType.Point,
            UINotificationType.Info,
            UINotificationType.Tip,
            UINotificationType.Error
        };

        for(int i = 0; i < all.Length; i++) {

            UIRef container = ToolkitContainer(all[i]);

            if(all[i] == type) {
                UIUtil.ShowObject(container);
            }
            else {
                UIUtil.HideObject(container);
            }
        }
    }

    // The five label writes, replayed onto the toolkit view.
    //
    // They cannot go through the existing UIUtil.SetLabelValue(achievementTitle, ...) calls:
    // achievementTitle and its eight siblings are declared inside the USE_UI_NGUI branch, so they
    // compile as legacy UILabel and BindElements can never bind them — every write would land on
    // the NGUI label that SuppressLegacyView just hid. Written by ELEMENT NAME instead, which is
    // the same fix the header's LabelSection and the worlds meta labels needed.
    //
    // Kept as a REPLAY (it takes the item, not the current widget state) because ProcessNext-
    // Notification writes before ShowDialog and the view may still be building on the first toast.
    protected virtual void ApplyToolkitItem(UINotificationItem item) {

        if(item != null) {
            toolkitItem = item;
        }

        if(!isToolkitPanel || toolkitItem == null) {
            return;
        }

        ShowToolkitContainerType(toolkitItem.notificationType);

        UIRef container = ToolkitContainer(toolkitItem.notificationType);

        UIUtil.SetLabelValue(UIUtil.ResolveDeep(container, elementDisplayName), toolkitItem.title);
        UIUtil.SetLabelValue(UIUtil.ResolveDeep(container, elementDescription), toolkitItem.description);

        // Score only where legacy shows one. LabelScore is active in the prefab on the achievement
        // container ALONE — Point writes "+score" onto an object its own container leaves
        // inactive, so a point toast has never shown its points. Reproduced, not corrected: the
        // toolkit view simply does not carry a LabelScore outside the achievement container.
        if(toolkitItem.notificationType == UINotificationType.Achievement) {

            UIUtil.SetLabelValue(UIUtil.ResolveDeep(container, elementScore), "+" + toolkitItem.score);

            // First achievement toast of the session: the coin mesh is live now, so it can be
            // staged and its RenderTexture handed to the view's Coin element.
            SetupToolkitCoinStage();
        }
    }

    // ==========================================================================================
}
