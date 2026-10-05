using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Engine.Game.App.BaseApp;

using Engine.Events;

public class UICustomizeCharacterRPGItem : GameObjectBehavior {
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
    public UISlider sliderProfileValue;
    public UISlider sliderCurrentValue;
    public UIImageButton buttonRPGItemUp;
    public UIImageButton buttonRPGItemDown;
    public UILabel labelName;
    public UILabel labelValue;
#else
    // B10: agnostic UIRef handles (was UGUI), the BaseGameHUD pattern. Unbound (null) until
    // something binds them by name; every UIUtil call no-ops on a null ref.
    public Engine.UI.UIRef sliderProfileValue;
    public Engine.UI.UIRef sliderCurrentValue;
    public Engine.UI.UIRef buttonRPGItemUp;
    public Engine.UI.UIRef buttonRPGItemDown;
    public Engine.UI.UIRef labelName;
    public Engine.UI.UIRef labelValue;
#endif

    public string rpgCode = "energy"; // attack, defense, energy, health, skill, power
    public string characterCode = "default";
    public double currentValue = 0.1;
    public double profileValue = 0.1;
    public string displayName = "";
    public string displayValue = "";

    void Start() {
        LoadData();
    }

    void OnEnable() {
        Messenger<GameObject>.AddListener(ButtonEvents.EVENT_BUTTON_CLICK_OBJECT, OnButtonClickObjectHandler);
        Messenger<string, string, double>.AddListener(UICustomizeCharacterRPGItemMessages.rpgItemCodeChanged, OnRPGItemHandler);
    }

    void OnDisable() {
        Messenger<GameObject>.RemoveListener(ButtonEvents.EVENT_BUTTON_CLICK_OBJECT, OnButtonClickObjectHandler);
        Messenger<string, string, double>.RemoveListener(UICustomizeCharacterRPGItemMessages.rpgItemCodeChanged, OnRPGItemHandler);
    }

    void OnButtonClickObjectHandler(GameObject go) {

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
        if(go == buttonRPGItemUp.gameObject) {
            Up();
        }
        else if(go == buttonRPGItemDown.gameObject) {
            Down();
        }
#else
        // B10: a UIRef is unbound (null) until something binds it, and a toolkit one has no
        // GameObject -- either way it is simply not the clicked object.
        if(buttonRPGItemUp != null && go == buttonRPGItemUp.gameObject) {
            Up();
        }
        else if(buttonRPGItemDown != null && go == buttonRPGItemDown.gameObject) {
            Down();
        }
#endif
    }

    void OnRPGItemHandler(string rpgCodeFrom, string characterCodeFrom, double valFrom) {

        if(rpgCode == rpgCodeFrom && characterCode == characterCodeFrom
            && ((valFrom > 0 && UICustomizeCharacterRPG.Instance.upgradesAvailable > 0)
            || valFrom < 0)) {

            double val = currentValue + valFrom;

            if(val < profileValue
                || val > 1.0) {
                return;
            }

            currentValue += valFrom;

            Messenger<string, string, double>.Broadcast(
                UICustomizeCharacterRPGItemMessages.rpgUpgradesChanged,
                rpgCodeFrom, characterCodeFrom, valFrom);

            UpdateControls();
        }
    }

    public void LoadData() {
        StartCoroutine(loadDataCo());
    }

    public IEnumerator loadDataCo() {

        yield return new WaitForSeconds(1f);

        currentValue = 0.1f;

        GameProfileRPGItem profileItemRPG = GameProfileCharacters.Current.GetCurrentCharacterRPG();

        if(rpgCode.ToLower() == GameDataItemRPGAttributes.attack) {
            displayName = RPGConfigs.displayNameAttack;
            currentValue = profileItemRPG.GetAttack();
        }
        else if(rpgCode.ToLower() == GameDataItemRPGAttributes.defense) {
            displayName = RPGConfigs.displayNameDefense;
            currentValue = profileItemRPG.GetDefense();
        }
        else if(rpgCode.ToLower() == GameDataItemRPGAttributes.defense) {
            displayName = RPGConfigs.displayNameDefense;
            currentValue = profileItemRPG.GetDefense();
        }
        else if(rpgCode.ToLower() == GameDataItemRPGAttributes.energy) {
            displayName = RPGConfigs.displayNameEnergy;
            currentValue = profileItemRPG.GetEnergy();
        }
        else if(rpgCode.ToLower() == GameDataItemRPGAttributes.health) {
            displayName = RPGConfigs.displayNameHealth;
            currentValue = profileItemRPG.GetHealth();
        }
        else if(rpgCode.ToLower() == GameDataItemRPGAttributes.jump) {
            displayName = RPGConfigs.displayNameJump;
            currentValue = profileItemRPG.GetJump();
        }
        else if(rpgCode.ToLower() == GameDataItemRPGAttributes.speed) {
            displayName = RPGConfigs.displayNameSpeed;
            currentValue = profileItemRPG.GetSpeed();
        }

        UpdateControls();
        SetProfileValue(currentValue);
    }

    public void UpdateControls() {
        SetDisplayName(displayName);
        SetDisplayValue();
        SetValues();
    }

    public void SetValues() {
        SetValues(currentValue);
    }

    public void SetValues(double val) {
        SetCurrentValue(val);
        SetProfileValue(profileValue);
    }

    public void SetCurrentValue(double val) {
        currentValue = Math.Round(val, 1);
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
        UIUtil.SetSliderValue(sliderCurrentValue, currentValue);
#else
        UIUtil.SetSliderValue(sliderCurrentValue, (float)currentValue);
#endif
    }

    public void SetProfileValue(double val) {
        profileValue = Math.Round(val, 1);
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
        UIUtil.SetSliderValue(sliderProfileValue, profileValue);
#else
        UIUtil.SetSliderValue(sliderProfileValue, (float)profileValue);
#endif
    }

    public void SetDisplayName(string nameTo) {
        UIUtil.SetLabelValue(labelName, nameTo);
    }

    public void SetDisplayValue() {
        double modifier = 10;
        double currentSliderValue = currentValue * modifier;
        double currentSliderMaxValue = 1 * modifier;

        displayValue = string.Format("{0}/{1}",
            currentSliderValue.ToString("N0", Engine.Game.App.BaseApp.L10n.NumberFormat),
            currentSliderMaxValue.ToString("N0", Engine.Game.App.BaseApp.L10n.NumberFormat));

        UIUtil.SetLabelValue(labelValue, displayValue);
    }

    public void Load(string rpgCodeTo) {
        Load(rpgCodeTo, GameProfileCharacters.Current.GetCurrentCharacterProfileCode());
    }

    public void Load(string rpgCodeTo, string characterCodeTo) {
        rpgCode = rpgCodeTo;
        characterCode = characterCodeTo;

        LoadData();
    }

    public void Up() {
        Messenger<string, string, double>.Broadcast(UICustomizeCharacterRPGItemMessages.rpgItemCodeChanged, rpgCode, characterCode, .1);
    }

    public void Down() {
        Messenger<string, string, double>.Broadcast(UICustomizeCharacterRPGItemMessages.rpgItemCodeChanged, rpgCode, characterCode, -.1);
    }
}