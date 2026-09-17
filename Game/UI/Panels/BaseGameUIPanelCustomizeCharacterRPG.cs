using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Engine.Game.App.BaseApp;

using Engine.Events;

#if ENABLE_FEATURE_CHARACTER_CUSTOMIZE

public class BaseGameUIPanelCustomizeCharacterRPG : GameUIPanelBase {
    
    public static GameUIPanelCustomizeCharacterRPG Instance;

    public Camera cameraCustomize;

    public UICustomizeCharacterRPG customizeCharacterRPG;
    
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
     
        //currentColors = GameProfiles.Current.GetCustomColorsRunner();
        //UpdateControls();
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

        // Chain to base so UIPanelBase.OnDisable -> FreeToolkitView runs when this panel is pooled
        // away, else the toolkit view leaks once this panel gets a toolkitViewKey. Standing
        // Phase-3 migration prerequisite; latent until then.
        //
        // OnDisable ONLY: UIPanelBase.OnEnable re-adds EVENT_BUTTON_CLICK ->
        // OnButtonClickEventHandler, which this panel already subscribes itself, so chaining
        // OnEnable would fire every button click twice. RemoveListener is idempotent, so the
        // one-sided chain is safe.
        base.OnDisable();

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
     
    // ------------------------------------------------------------------
    // TOOLKIT PATH (3J) — the last screen off NGUI.
    //
    // This screen was DEFERRED (iter 17/18) on two grounds, both softer than they
    // read. "Rows are built at runtime": loadDataRPG() builds a FIXED list of four
    // attributes, so they are authored statically in the view. "Clicks resolve by
    // GameObject identity": UICustomizeCharacterRPGItem compared
    // `go == buttonRPGItemUp.gameObject`, which a toolkit element cannot satisfy
    // because it has no GameObject — so the toolkit path routes by element NAME
    // instead, the same way every other converted screen does.
    //
    // The legacy driver (UICustomizeCharacterRPG + UICustomizeCharacterRPGItem) is
    // left completely untouched: it is still what runs with the kill switch off,
    // and this path must not be able to break it.

    public Engine.UI.UIRef LabelUpgradesValue;

    public Engine.UI.UIRef LabelRPGSpeedName;
    public Engine.UI.UIRef LabelRPGSpeedValue;
    public Engine.UI.UIRef RPGSpeedFill;

    public Engine.UI.UIRef LabelRPGEnergyName;
    public Engine.UI.UIRef LabelRPGEnergyValue;
    public Engine.UI.UIRef RPGEnergyFill;

    public Engine.UI.UIRef LabelRPGHealthName;
    public Engine.UI.UIRef LabelRPGHealthValue;
    public Engine.UI.UIRef RPGHealthFill;

    public Engine.UI.UIRef LabelRPGAttackName;
    public Engine.UI.UIRef LabelRPGAttackValue;
    public Engine.UI.UIRef RPGAttackFill;

    // Display names are the LEGACY captions, which do not all match their codes:
    // the attack row is labelled "Power" on screen.
    public const string rpgCodeSpeed = "speed";
    public const string rpgCodeEnergy = "energy";
    public const string rpgCodeHealth = "health";
    public const string rpgCodeAttack = "attack";

    // What the player has dialled in but not yet saved, and what the profile
    // already holds. A step may never take a value BELOW the saved one — that is
    // the legacy rule (`val < profileValue` returns) and it is what stops a player
    // refunding upgrades they already committed.
    private readonly Dictionary<string, double> rpgCurrent = new Dictionary<string, double>();
    private readonly Dictionary<string, double> rpgProfile = new Dictionary<string, double>();

    private double upgradesAvailableToolkit = 0;

    private static string RPGCodeForButton(string buttonName, out double delta) {

        delta = 0;

        if (string.IsNullOrEmpty(buttonName)) {
            return null;
        }

        if (buttonName.EndsWith("Up")) {
            delta = .1;
        }
        else if (buttonName.EndsWith("Down")) {
            delta = -.1;
        }
        else {
            return null;
        }

        if (buttonName.StartsWith("ButtonRPGSpeed")) { return rpgCodeSpeed; }
        if (buttonName.StartsWith("ButtonRPGEnergy")) { return rpgCodeEnergy; }
        if (buttonName.StartsWith("ButtonRPGHealth")) { return rpgCodeHealth; }
        if (buttonName.StartsWith("ButtonRPGAttack")) { return rpgCodeAttack; }

        delta = 0;
        return null;
    }

    public virtual void LoadRPGToolkit() {

        rpgCurrent.Clear();
        rpgProfile.Clear();

        upgradesAvailableToolkit = GameProfileRPGs.Current.GetUpgrades();

        GameProfileCharacterItem characterItem =
            GameProfileCharacters.Current.GetCurrentCharacter();

        if (characterItem == null || characterItem.profileRPGItem == null) {
            SyncRPGToolkit();
            return;
        }

        GameProfileRPGItem rpg = characterItem.profileRPGItem;

        SeedRPG(rpgCodeSpeed, rpg.GetSpeed());
        SeedRPG(rpgCodeEnergy, rpg.GetEnergy());
        SeedRPG(rpgCodeHealth, rpg.GetHealth());
        SeedRPG(rpgCodeAttack, rpg.GetAttack());

        SyncRPGToolkit();
    }

    private void SeedRPG(string code, double val) {
        rpgProfile[code] = Math.Round(val, 1);
        rpgCurrent[code] = Math.Round(val, 1);
    }

    private double RPGValue(string code) {
        double val;
        return rpgCurrent.TryGetValue(code, out val) ? val : 0;
    }

    public virtual void StepRPGToolkit(string code, double delta) {

        if (string.IsNullOrEmpty(code) || !rpgCurrent.ContainsKey(code)) {
            return;
        }

        // Same three gates the legacy item applied, in the same order: an increase
        // needs an upgrade in hand, the value may not drop below what is already
        // saved, and it may not exceed 1.0 (rendered as 10/10).
        if (delta > 0 && upgradesAvailableToolkit <= 0) {
            return;
        }

        double next = Math.Round(rpgCurrent[code] + delta, 1);

        double floorValue;
        if (!rpgProfile.TryGetValue(code, out floorValue)) {
            floorValue = 0;
        }

        if (next < floorValue || next > 1.0) {
            return;
        }

        rpgCurrent[code] = next;
        upgradesAvailableToolkit -= delta > 0 ? 1 : -1;

        SyncRPGToolkit();
    }

    public virtual void SyncRPGToolkit() {

        UIUtil.SetLabelValue(LabelUpgradesValue, upgradesAvailableToolkit.ToString("N0", Engine.Game.App.BaseApp.L10n.NumberFormat));

        SyncRPGRow(rpgCodeSpeed, LabelRPGSpeedName, L10n.TrOrDefault("game_ui_customize_character_rpg_speed", "Speed"), LabelRPGSpeedValue, RPGSpeedFill);
        SyncRPGRow(rpgCodeEnergy, LabelRPGEnergyName, L10n.TrOrDefault("game_ui_customize_character_rpg_energy", "Energy"), LabelRPGEnergyValue, RPGEnergyFill);
        SyncRPGRow(rpgCodeHealth, LabelRPGHealthName, L10n.TrOrDefault("game_ui_customize_character_rpg_health", "Health"), LabelRPGHealthValue, RPGHealthFill);
        SyncRPGRow(rpgCodeAttack, LabelRPGAttackName, L10n.TrOrDefault("game_ui_customize_character_rpg_power", "Power"), LabelRPGAttackValue, RPGAttackFill);
    }

    private void SyncRPGRow(
        string code, Engine.UI.UIRef nameRef, string displayName,
        Engine.UI.UIRef valueRef, Engine.UI.UIRef fillRef) {

        double val = RPGValue(code);

        UIUtil.SetLabelValue(nameRef, displayName);

        // Legacy formats as value*10 over 10 — "2/10", not "0.2/1".
        UIUtil.SetLabelValue(valueRef, string.Format("{0}/{1}",
            (val * 10).ToString("N0", Engine.Game.App.BaseApp.L10n.NumberFormat), (10).ToString("N0", Engine.Game.App.BaseApp.L10n.NumberFormat)));

        // Width as a PERCENT of the track, the same contract .cc-stat-fill uses:
        // SetSliderValue falls through to the backend's image-fill path.
        UIUtil.SetSliderValue(fillRef, (float)val);
    }

    public virtual void SaveRPGToolkit() {

        GameProfileCharacterItem characterItem =
            GameProfileCharacters.Current.GetCurrentCharacter();

        if (characterItem == null || characterItem.profileRPGItem == null) {
            return;
        }

        GameProfileRPGItem rpg = characterItem.profileRPGItem;

        rpg.SetSpeed(RPGValue(rpgCodeSpeed));
        rpg.SetEnergy(RPGValue(rpgCodeEnergy));
        rpg.SetHealth(RPGValue(rpgCodeHealth));
        rpg.SetAttack(RPGValue(rpgCodeAttack));

        GameProfileRPGs.Current.SetUpgrades(upgradesAvailableToolkit);

        GameState.SaveProfile();

        // The saved values become the new floor, so the rows cannot be stepped
        // back down past what was just committed.
        SeedRPG(rpgCodeSpeed, RPGValue(rpgCodeSpeed));
        SeedRPG(rpgCodeEnergy, RPGValue(rpgCodeEnergy));
        SeedRPG(rpgCodeHealth, RPGValue(rpgCodeHealth));
        SeedRPG(rpgCodeAttack, RPGValue(rpgCodeAttack));

        SyncRPGToolkit();
    }

    public override void BindElements(Engine.UI.UIRef root) {

        base.BindElements(root);

        // First moment the elements are real — the async LoadView continuation.
        LoadRPGToolkit();
    }

    public override void OnButtonClickEventHandler(string buttonName) {

        if (!isToolkitPanel) {
            return;
        }

        double delta;
        string code = RPGCodeForButton(buttonName, out delta);

        if (code != null) {
            StepRPGToolkit(code, delta);
            return;
        }

        if (buttonName == "ButtonSaveRPG") {
            SaveRPGToolkit();
        }
        else if (buttonName == "ButtonBuyUpgrades") {
            GameUIController.ShowProductCurrency();
        }
    }
     
    public virtual void UpdateControls() {
     
    }
 
    public static void LoadData() {
        if(GameUIPanelCustomizeCharacterRPG.Instance != null) {
            GameUIPanelCustomizeCharacterRPG.Instance.loadData();
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
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
            listGridRoot.GetComponent<UIGrid>().Reposition();
#endif
            yield return new WaitForEndOfFrame();                
        }

        // Only the LEGACY driver needs this. Under the toolkit, SuppressLegacyView has
        // deactivated the NGUI container the driver lives in, so StartCoroutine threw
        // "Coroutine couldn't be started because the game object 'ContainerRPG' is
        // inactive!" on every show — and there was nothing for it to build rows into
        // anyway, because the toolkit path loads its own four values in BindElements.
        //
        // Gated on the view having actually LOADED (isToolkitPanel), not on the switch
        // alone: if the view ever failed to load, suppression never runs, the legacy
        // container stays up, and it must still get its data.
        if(customizeCharacterRPG != null
            && !isToolkitPanel
            && customizeCharacterRPG.gameObject.activeInHierarchy) {
             customizeCharacterRPG.loadData();
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
        characterDisplayState = UIPanelCharacterDisplayState.Character;
        backgroundDisplayState = UIPanelBackgroundDisplayState.PanelBacker;
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

        if(GameConfigs.isGameRunning) {
            return;
        }

        if(!isVisible) {
            return;
        }

        if(cameraCustomize == null) {
            return;
        }
    }
}

#endif