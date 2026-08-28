using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
#else
using UnityEngine.UI;
#endif

// using Engine.Data.Json;
using Engine.Events;
using Engine.Utility;
using Engine.Game.App.BaseApp;

public class UICustomizeProfileCharacters : UICustomizeSelectObject {
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
    public UIInput inputCurrentDisplayCode;
    public UIImageButton buttonSave;
#else
    public InputField inputCurrentDisplayCode;
    public Button buttonSave;
#endif

    public string type = "character";

    GameProfileCharacterItem profileCharacterItem;

    public override void OnEnable() {
        base.OnEnable();

        Messenger<string, string>.AddListener(InputEvents.EVENT_ITEM_CHANGE, OnInputChanged);
        //Messenger<string, string>.AddListener(InputEvents.EVENT_ITEM_CLICK, OnInputClicked);
    }

    public override void OnDisable() {
        base.OnDisable();

        Messenger<string, string>.RemoveListener(InputEvents.EVENT_ITEM_CHANGE, OnInputChanged);
        //Messenger<string, string>.RemoveListener(InputEvents.EVENT_ITEM_CLICK, OnInputClicked);
    }

    void OnInputChanged(string controlName, string data) {

        Debug.Log("OnInputChanged:" + " controlName:" + controlName + " data:" + data);

        if(inputCurrentDisplayName != null
           && controlName == inputCurrentDisplayName.name) {

            ChangeCharacterDisplayName(data);
        }
        else if(inputCurrentDisplayCode != null
                && controlName == inputCurrentDisplayCode.name) {

            ChangeCharacterDisplayCode(data);
        }
    }

    void OnInputClicked(string controlName, string data) {

        //Debug.Log("OnInputClicked:" + " controlName:" + controlName + " data:" + data);


        //if(inputCurrentDisplayName != null 
        //   && controlName == inputCurrentDisplayName.name) {
        //
        //}
        //else if(inputCurrentDisplayCode != null 
        //       && controlName == inputCurrentDisplayCode.name) {
        //
        //}
    }

    public override void Start() {
        Load();
    }

    public override void Load() {
        base.Load();

        ShowCurrentProfileCharacter();
    }

    public void ShowCurrentProfileCharacter() {

        GameProfileCharacterItems gameProfileCharacterItems =
            GameProfileCharacters.Current.GetCharacters();

        int countPresets = gameProfileCharacterItems.items.Count;
        int index = 0;

        string currentCharacterCode =
            GameProfileCharacters.Current.GetCurrentCharacterProfileCode();

        foreach(GameProfileCharacterItem gameProfileCharacterItem
                in gameProfileCharacterItems.items) {
            if(gameProfileCharacterItem.code == currentCharacterCode) {
                ChangePreset(index);
                break;
            }
            index++;
        }

        if(index == countPresets - 1) {
            ChangePreset(0);
        }

    }

    public override void OnButtonClickEventHandler(string buttonName) {

        if(UIUtil.IsButtonClicked(buttonCycleLeft, buttonName)) {
            ChangePresetPrevious();
        }
        else if(UIUtil.IsButtonClicked(buttonCycleRight, buttonName)) {
            ChangePresetNext();
        }
        else if(UIUtil.IsButtonClicked(buttonSave, buttonName)) {
            SaveInputs();
        }
    }

    public virtual void SaveInputs() {
        SaveCharacterDisplayNameInput();
        SaveCharacterDisplayCodeInput();

        GameCustomController.BroadcastCustomCharacterDisplayChanged();
    }

    public virtual void SaveCharacterDisplayNameInput() {

        if(inputCurrentDisplayName == null) {
            return;
        }

        ChangeCharacterDisplayName(inputCurrentDisplayName.text);
    }

    public virtual void SaveCharacterDisplayCodeInput() {

        if(inputCurrentDisplayCode == null) {
            return;
        }

        ChangeCharacterDisplayCode(inputCurrentDisplayCode.text);
    }

    public virtual void ChangeCharacterDisplayName(string val) {

        if(inputCurrentDisplayName == null) {
            return;
        }

        if(profileCharacterItem == null) {
            return;
        }

        if(string.IsNullOrEmpty(val)) {
            return;
        }

        Debug.Log("ChangeCharacterDisplayName:" + " val:" + val);

        UIUtil.SetInputValue(inputCurrentDisplayName, val);

        profileCharacterItem.characterDisplayName = val;
        GameProfileCharacters.Current.SetCharacter(profileCharacterItem);

        GameState.SaveProfile();
    }

    public virtual void ChangeCharacterDisplayCode(string val) {

        if(inputCurrentDisplayCode == null) {
            return;
        }

        if(profileCharacterItem == null) {
            return;
        }

        if(string.IsNullOrEmpty(val)) {
            return;
        }

        Debug.Log("ChangeCharacterDisplayCode:" + " val:" + val);

        UIUtil.SetInputValue(inputCurrentDisplayCode, val);

        profileCharacterItem.characterDisplayCode = val;
        GameProfileCharacters.Current.SetCharacter(profileCharacterItem);

        GameState.SaveProfile();
    }

    public void ChangePresetNext() {
        ChangePreset(currentIndex + 1);
    }

    public void ChangePresetPrevious() {
        ChangePreset(currentIndex - 1);
    }

    public void ChangePreset(int index) {

        GameProfileCharacterItems gameProfileCharacterItems
            = GameProfileCharacters.Current.GetCharacters();

        int countPresets = gameProfileCharacterItems.items.Count;

        if(index < 0) {
            index = countPresets - 1;
        }

        if(index > countPresets - 1) {
            index = 0;
        }

        currentIndex = index;

        if(index > -1 && index < countPresets) {

            if(initialProfileCustomItem == null) {
                initialProfileCustomItem = GameProfileCharacters.currentCustom;
            }

            currentProfileCustomItem = GameProfileCharacters.currentCustom;

            if(index == -1) {

                UIUtil.SetLabelValue(labelCurrentDisplayName, "Previous");
                UIUtil.SetLabelValue(labelCurrentType, "");


                //GameCustomController.UpdateTexturePresetObject(
                //    initialProfileCustomItem, currentObject, type);
            }
            else {

                profileCharacterItem =
                    gameProfileCharacterItems.items[currentIndex];

                //GameCustomController.SaveCustomItem(currentProfileCustomItem);

                GameProfileCharacters.Current.SetCurrentCharacterProfileCode(profileCharacterItem.code);

                Messenger<string>.Broadcast(
                    GameCustomMessages.customCharacterPlayerChanged, profileCharacterItem.code);

                string characterType = "";
                GameCharacter gameCharacter = GameCharacters.Instance.GetById(profileCharacterItem.characterCode);
                if(gameCharacter != null) {
                    characterType = gameCharacter.display_name;
                    characterType = "- TYPE: " + characterType + " -";
                }

                UIUtil.SetInputValue(inputCurrentDisplayName, profileCharacterItem.characterDisplayName);
                UIUtil.SetLabelValue(labelCurrentDisplayName, profileCharacterItem.characterDisplayName);
                UIUtil.SetLabelValue(labelCurrentType, characterType);

                UIUtil.SetInputValue(inputCurrentDisplayCode, profileCharacterItem.characterDisplayCode);

                UIUtil.SetLabelValue(labelCurrentStatus, string.Format("{0}/{1}", index + 1, countPresets));

                // ...and again for the toolkit, by ELEMENT NAME. labelCurrentDisplayName /
                // labelCurrentType / labelCurrentStatus are declared inside
                // UICustomizeSelectObject's `#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3` branch — the
                // branch actually compiled here — so they are legacy UILabels that BindElements
                // can never rebind. Every write above lands on the NGUI widget SuppressLegacyView
                // has already hidden, which is why cycling bots moved the 3D model but left the
                // plate's name/type/status frozen at their authored placeholders (user, 2026-08-28).
                // Third instance of this trap after the worlds labels and the header band title.
                UpdateToolkitDisplay(
                    profileCharacterItem, characterType,
                    string.Format("{0}/{1}", index + 1, countPresets));
            }
        }
    }

    // The panel that hosts this control. Resolved by walking up rather than by asking a
    // specific panel type for its Instance: this control is generic game-lib code and must not
    // learn the name of the one screen that happens to use it today.
    private UIPanelBase hostPanel;

    public UIPanelBase HostPanel() {

        if(hostPanel == null) {
            hostPanel = GetComponentInParent<UIPanelBase>();
        }

        return hostPanel;
    }

    // Mirror of the legacy writes above, addressed by element name. No-ops entirely on the NGUI
    // path (and before the async view lands), because ResolveDeep on a dead ref returns UIRef.none
    // and every backend op no-ops on a ref that is not alive.
    public virtual void UpdateToolkitDisplay(
        GameProfileCharacterItem item, string characterType, string status) {

        UIPanelBase panel = HostPanel();

        if(panel == null || !panel.isToolkitPanel || item == null) {
            return;
        }

        Engine.UI.UIRef root = panel.viewRoot;

        UIUtil.SetLabelValue(UIUtil.ResolveDeep(root, "LabelCharacterNameValue"),
            item.characterDisplayName);
        UIUtil.SetLabelValue(UIUtil.ResolveDeep(root, "LabelType"), characterType);
        UIUtil.SetLabelValue(UIUtil.ResolveDeep(root, "LabelStatus"), status);

        UpdateToolkitInfoCard(item, characterType);
    }

    // The info callout under the plate (user request, 2026-08-28): identity plus the four RPG
    // attributes, so switching bots shows what actually differs between them.
    //
    // The stat scale matches UICustomizeCharacterRPGItem exactly — values are stored 0..1 and
    // displayed against a modifier of 10 — so the same bot reads the same number here and on the
    // skills screen. Deliberately reusing that constant rather than inventing a display range.
    protected virtual void UpdateToolkitInfoCard(
        GameProfileCharacterItem item, string characterType) {

        Engine.UI.UIRef root = HostPanel().viewRoot;

        UIUtil.SetLabelValue(UIUtil.ResolveDeep(root, "LabelCardName"),
            item.characterDisplayName);
        UIUtil.SetLabelValue(UIUtil.ResolveDeep(root, "LabelCardCode"),
            string.IsNullOrEmpty(item.characterDisplayCode) ? "" : "#" + item.characterDisplayCode);

        // The plate already brackets the type as "- TYPE: X -"; the card wants it plain.
        UIUtil.SetLabelValue(UIUtil.ResolveDeep(root, "LabelCardType"),
            characterType.Replace("- ", "").Replace(" -", ""));

        // Read through GetCurrentCharacterRPG, NOT item.profileRPGItem. ChangePreset has already
        // called SetCurrentCharacterProfileCode for this item, so the two ought to agree — but the
        // item pulled out of GetCharacters().items carries no RPG data (measured: its getters all
        // return 0 while the current-character lookup returns the real 0.1), and the authored
        // placeholder "0/10" made that look like a working card with a zeroed bot. Going through
        // the same accessor the skills screen uses also guarantees the two screens can never
        // disagree about the same bot.
        GameProfileRPGItem rpg = GameProfileCharacters.Current.GetCurrentCharacterRPG();

        if(rpg == null) {
            rpg = item.profileRPGItem;
        }

        if(rpg == null) {
            return;
        }

        SetToolkitStat(root, "Speed", rpg.GetSpeed());
        SetToolkitStat(root, "Health", rpg.GetHealth());
        SetToolkitStat(root, "Energy", rpg.GetEnergy());
        SetToolkitStat(root, "Attack", rpg.GetAttack());
    }

    protected virtual void SetToolkitStat(Engine.UI.UIRef root, string code, double val) {

        double modifier = 10;

        UIUtil.SetLabelValue(
            UIUtil.ResolveDeep(root, "Stat" + code + "Value"),
            string.Format("{0}/{1}",
                (val * modifier).ToString("N0"), modifier.ToString("N0")));

        // Falls through the backend to the image-fill path, which sets the element's width as a
        // percentage of its track.
        UIUtil.SetSliderValue(
            UIUtil.ResolveDeep(root, "Stat" + code + "Fill"), (float)val);
    }

    public override void Update() {

    }
}