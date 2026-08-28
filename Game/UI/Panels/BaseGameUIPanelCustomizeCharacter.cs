using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Engine.Events;

#if ENABLE_FEATURE_CHARACTER_CUSTOMIZE

public class BaseGameUIPanelCustomizeCharacter : GameUIPanelBase {

    public static GameUIPanelCustomizeCharacter Instance;
    public Camera cameraCustomize;
    public int currentSelectedItem = 0;
    public GameObject playerObject;
    public GameObject playerContainerObject;
    public UICustomizeProfileCharacters customProfileCharacters;

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

        UpdateControls();
    }

    public override void OnEnable() {

        Messenger<string>.AddListener(ButtonEvents.EVENT_BUTTON_CLICK, OnButtonClickEventHandler);

        Messenger<string>.AddListener(
            UIControllerMessages.uiPanelAnimateIn,
            OnUIControllerPanelAnimateIn);

        Messenger<string>.AddListener(
            UIControllerMessages.uiPanelAnimateOut,
            OnUIControllerPanelAnimateOut);

        Messenger<string, string>.AddListener(
            UIControllerMessages.uiPanelAnimateType,
            OnUIControllerPanelAnimateType);

        Messenger<string, int>.AddListener(InputEvents.EVENT_ITEM_CLICK, OnInputClicked);
    }

    public override void OnDisable() {

        Messenger<string>.RemoveListener(ButtonEvents.EVENT_BUTTON_CLICK, OnButtonClickEventHandler);

        Messenger<string>.RemoveListener(
            UIControllerMessages.uiPanelAnimateIn,
            OnUIControllerPanelAnimateIn);

        Messenger<string>.RemoveListener(
            UIControllerMessages.uiPanelAnimateOut,
            OnUIControllerPanelAnimateOut);

        Messenger<string, string>.RemoveListener(
            UIControllerMessages.uiPanelAnimateType,
            OnUIControllerPanelAnimateType);

        Messenger<string, int>.RemoveListener(InputEvents.EVENT_ITEM_CLICK, OnInputClicked);

        // Chain to base so UIPanelBase.OnDisable -> FreeToolkitView runs when this pooled panel is
        // put away; without it the toolkit view leaks on every navigation away. Phase-3 migration
        // prerequisite, same fix the list/results bases got.
        //
        // OnDisable ONLY — UIPanelBase.OnEnable re-adds EVENT_BUTTON_CLICK, which this panel already
        // subscribes itself, so chaining OnEnable would fire every click TWICE. RemoveListener is
        // idempotent, so the OnDisable-only chain is safe.
        base.OnDisable();
    }

    void OnInputClicked(string controlName, int data) {

        Debug.Log("OnInputClicked:" + " controlName:" + controlName + " data:" + data);

        if(customProfileCharacters == null) {
            customProfileCharacters = GetComponentInChildren<UICustomizeProfileCharacters>();
        }

        if(customProfileCharacters == null) {
            return;
        }

        if(customProfileCharacters.inputCurrentDisplayName != null
            && controlName == customProfileCharacters.inputCurrentDisplayName.name) {

            GameUIPanelHeader.CharacterLargeZoomIn();
            GameUIPanelHeader.CharacterLargeShowBack();
        }
        else if(customProfileCharacters.inputCurrentDisplayCode != null
            && controlName == customProfileCharacters.inputCurrentDisplayCode.name) {

            GameUIPanelHeader.CharacterLargeZoomIn();
            GameUIPanelHeader.CharacterLargeShowFront();
        }
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

    // 3G SUPPRESSION — selective, NOT the default whole-panelContainer hide.
    //
    // Two things under panelContainer must stay ALIVE, and the default hide killed both:
    //
    // 1. BEHAVIOUR. Controls/CharacterPreset carries UICustomizeProfileCharacters, whose
    //    OnButtonClickEventHandler is what actually cycles the bot
    //    (UIUtil.IsButtonClicked(buttonCycleLeft/Right, buttonName) -> ChangePresetPrevious/Next).
    //    Deactivating its GameObject unregisters that listener, so the toolkit arrows broadcast the
    //    right NAME and nobody is home — user 2026-08-25: "hitting left/right does not change bot
    //    anymore". Hiding a container here is not just a visual change; it silences a handler.
    //
    // 2. LAYERING. The dark backer must keep rendering. Toolkit views composite ABOVE the whole
    //    NGUI stack, so a toolkit-drawn card sits in front of the legacy 3D bot — user: "bot is
    //    behind the panel". Keeping the LEGACY backer puts the bot in front of it again (both are
    //    NGUI) with the toolkit chrome above both, which is the legacy stacking order.
    //
    // So we hide only the flat widgets the toolkit view actually replaces.
    private const string controls = "Container/AnchorCenter/Center/Container/Controls/";

    private static readonly string[] legacySuppressPaths = {
        // The arrows, the 3/4 plate and the name/code inputs. Suppressed at PresetContainer, one
        // level BELOW CharacterPreset, precisely so CharacterPreset itself stays active and
        // UICustomizeProfileCharacters keeps listening (see above).
        controls + "CharacterPreset/PresetContainer",
        // Caption + the legacy name label.
        controls + "CharacterMeta",
        // CUSTOMIZE/CHANGE BOT (serialized inactive; activated at runtime).
        controls + "Buttons",
    };

    private readonly System.Collections.Generic.List<GameObject> suppressedLegacy
        = new System.Collections.Generic.List<GameObject>();

    protected override void SuppressLegacyView() {
        ReassertLegacySuppression();
        ReplayCharacterDisplay();
    }

    // Re-run the preset control's initial load once the view EXISTS.
    //
    // UICustomizeProfileCharacters populates the plate and the info card from ChangePreset, and it
    // does that first from Start() — which fires a frame or two BEFORE LoadToolkitView's
    // continuation lands. Its toolkit writes therefore no-op on the very first show and the screen
    // sits on the placeholders authored into the view ("Bot #1", "3/4", "0/10") until the player
    // touches an arrow. Same replay problem, and same hook, as the header band title.
    protected virtual void ReplayCharacterDisplay() {

        if(!isToolkitPanel) {
            return;
        }

        UICustomizeProfileCharacters presets =
            GetComponentInChildren<UICustomizeProfileCharacters>(true);

        if(presets != null) {
            presets.ShowCurrentProfileCharacter();
        }
    }

    // CONTINUOUS, never one-shot (iter-7 rule 1). Controls/Buttons/ButtonGameProductsCharacter is
    // serialized INACTIVE and gets activated at RUNTIME, so a single sweep at view-load time misses
    // it entirely and the legacy CUSTOMIZE/CHANGE BOT draws under the toolkit one (user 2026-08-25
    // screenshot: the label rendered twice). Re-swept every frame from Update.
    private void ReassertLegacySuppression() {

        if(!isToolkitPanel) {
            return;
        }

        foreach(string path in legacySuppressPaths) {

            Transform t = transform.Find(path);

            if(t == null || !t.gameObject.activeSelf) {
                continue;
            }

            t.gameObject.Hide();

            if(!suppressedLegacy.Contains(t.gameObject)) {
                suppressedLegacy.Add(t.gameObject);
            }
        }
    }

    // Restore-on-free so the UIPlatform.toolkitViewsEnabled kill switch returns a working legacy panel.
    protected override void FreeToolkitView() {

        foreach(GameObject go in suppressedLegacy) {
            if(go != null) {
                go.Show();
            }
        }

        suppressedLegacy.Clear();

        base.FreeToolkitView();
    }

    public override void OnButtonClickEventHandler(string buttonName) {
        //LogUtil.Log("OnButtonClickEventHandler: " + buttonName);
    }

    public virtual void OnCheckboxChangedEventHandler(string buttonName, bool selected) {

    }

    public virtual void UpdateControls() {

    }

    public static void LoadData() {
        if(GameUIPanelCustomizeCharacter.Instance != null) {
            GameUIPanelCustomizeCharacter.Instance.loadData();
        }
    }

    public virtual void loadData() {
        StartCoroutine(loadDataCo());
    }

    IEnumerator loadDataCo() {

        LogUtil.Log("LoadDataCo");

        if(listGridRoot != null) {
            listGridRoot.DestroyChildren();

            yield return new WaitForEndOfFrame();

            //loadDataPowerups();

            yield return new WaitForEndOfFrame();
            listGridRoot.GetComponent<UIGrid>().Reposition();
            yield return new WaitForEndOfFrame();
        }
    }

    public virtual void ClearList() {
        if(listGridRoot != null) {
            listGridRoot.DestroyChildren();
        }
    }

    public override void HandleShow() {
        base.HandleShow();

        buttonDisplayState = UIPanelButtonsDisplayState.None;
        characterDisplayState = UIPanelCharacterDisplayState.CharacterLarge;
        backgroundDisplayState = UIPanelBackgroundDisplayState.PanelBacker;
    }

    public override void HandleHide() {
        base.HandleHide();
    }

    public override void AnimateIn() {

        base.AnimateIn();

        loadData();
    }

    public override void AnimateOut() {

        base.AnimateOut();
        ClearList();
    }

    public virtual void Update() {

        // Before every early-return below: the legacy chrome must stay suppressed for as long as
        // the toolkit view owns this panel, and the runtime-activated change-bot button will
        // re-appear the moment this stops being re-asserted.
        ReassertLegacySuppression();

        if(GameConfigs.isGameRunning) {
            return;
        }

        if(!isVisible) {
            return;
        }

        if(cameraCustomize == null) {
            return;
        }

        /*
        if(Input.GetMouseButtonDown(0)) {
            Ray screenRay = cameraCustomize.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if(Physics.Raycast(screenRay, out hit, Mathf.Infinity) && hit.transform != null) {
                
                LogUtil.Log("hit:" + hit.transform.name);
                
                if(hit.transform.gameObject == colorWheelPanel) {
                                                        
                    Texture2D tex = (Texture2D)hit.collider.gameObject.renderer.material.mainTexture;
                    Color color = tex.GetPixelBilinear(hit.textureCoord.x, hit.textureCoord.y); // GetPixelBilinear oh how I love thee.
                                                    
                    GameAudio.PlayEffect(GameAudioEffects.audio_effect_ui_button_1);
                                        
                    //LoadSelectedItem(0, false);
                    //SetColorProperties(color);
                    //SetMaterialColors();
                    
                    LogUtil.Log("hit.point:" + hit.point);
                    LogUtil.Log("hit.textureCoord:" + hit.textureCoord);
                    LogUtil.Log("hit.textureCoord2:" + hit.textureCoord2);
                }
            }
        }
        */
    }

    public virtual void LateUpdate() {

        //if (playerContainerObject) {
        //playerContainerObject.transform.Rotate(0f, -50 * Time.deltaTime, 0f);
        //}
    }
}
#endif
