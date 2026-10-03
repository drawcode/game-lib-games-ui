//#define USE_UI_NGUI_2_7

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using Engine.Events;
using Engine.Game.App.BaseApp;
using Engine.UI;

public class UILocalizedLabel : GameObjectBehavior {

    public string gameLocalizationCode = "";

    public GameObject labelLocalized = null;

    // B1: reads/writes go through the UIRef facade (CurrentLabel), so the same component drives an
    // NGUI UILabel, a uGUI Text, or -- once a panel binds one with SetLabelRef -- a UI Toolkit
    // label. This field holds only a bound toolkit element (UIRef is never serialized). On a
    // prefab it stays null and CurrentLabel wraps the GameObject FindLabel found, which dispatches
    // to the same backend SetLabelValue/GetLabelValue the GameObject overloads always reached.
    private UIRef labelRef = null;

    // Point this component at an element (e.g. from a panel's BindElements). A toolkit element
    // wins over the GameObject probe until cleared with UIRef.none; a GameObject ref just becomes
    // labelLocalized, so it behaves exactly like one assigned in the inspector.
    public void SetLabelRef(UIRef r) {

        labelRef = null;

        if (r == null || !r.alive) {
            return;
        }

        if (r.gameObject != null) {
            labelLocalized = r.gameObject;
            return;
        }

        labelRef = r;
    }

    private bool hasElementRef {
        get {
            return labelRef != null && labelRef.alive;
        }
    }

    public void Start() {
        FindLabel();
        UpdateContent();
    }

    public void OnEnable() {

        Messenger<string>.AddListener(
            GameLocalizationMessages.gameLocalizationChanged,
            OnGameLocalizationChanged);
    }

    public void OnDisable() {

        Messenger<string>.RemoveListener(
            GameLocalizationMessages.gameLocalizationChanged,
            OnGameLocalizationChanged);
    }

    public void FindLabel() {

        // An explicitly bound element owns the label; nothing to probe.
        if (hasElementRef) {
            return;
        }

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
        if(labelLocalized == null) {
            labelLocalized = gameObject.GetAsGameObject<UILabel>();
        }
#endif
        if(labelLocalized == null) {
            labelLocalized = gameObject.GetAsGameObject<Text>();
        }
    }

    // The ref to write through: a bound toolkit element, else the probed GameObject (which may
    // be null -- UIRef.none, and every facade op no-ops on it, matching the null GameObject the
    // old SetLabelValue(GameObject) call silently ignored).
    private UIRef CurrentLabel() {

        if (hasElementRef) {
            return labelRef;
        }

        return UIRef.Of(labelLocalized);
    }

    public void SetContent(string content) {

        FindLabel();

        UIUtil.SetLabelValue(CurrentLabel(), content);

    }

    public string GetContent() {

        FindLabel();

        return UIUtil.GetLabelValue(CurrentLabel());
    }

    public void OnGameLocalizationChanged(string localeTo) {
        UpdateContent();
    }

    public void UpdateContent() {

        if(string.IsNullOrEmpty(gameLocalizationCode)) {
            return;
        }

        // Get from code
        string content = Locos.GetString(gameLocalizationCode);

        if(string.IsNullOrEmpty(content)) {

            // try lookup from current content
            string currentContent = GetContent();
            string currentContentCode = Locos.GetCodeFromContent(currentContent);

            content = Locos.GetString(currentContentCode);
        }

        if(!string.IsNullOrEmpty(content)) {
            SetContent(content);
        }
    }
}