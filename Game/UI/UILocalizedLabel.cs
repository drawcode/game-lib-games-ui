//#define USE_UI_NGUI_2_7

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using Engine.Events;

public class UILocalizedLabel : GameObjectBehavior {

    public string gameLocalizationCode = "";

    public GameObject labelLocalized = null;

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

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
        if(labelLocalized == null) {
            labelLocalized = gameObject.GetAsGameObject<UILabel>();
        }
#endif
        if(labelLocalized == null) {
            labelLocalized = gameObject.GetAsGameObject<Text>();
        }
    }

    public void SetContent(string content) {

        FindLabel();

        UIUtil.SetLabelValue(labelLocalized, content);

    }

    public string GetContent() {

        FindLabel();

        return UIUtil.GetLabelValue(labelLocalized);
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