using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// using Engine.Data.Json;
using Engine.Events;
using Engine.Utility;
using Engine.Game.App.BaseApp;

public class UICustomizeSelectObject : UICustomizeObject {
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
    public UIImageButton buttonCycleLeft;
    public UIImageButton buttonCycleRight;
    public UILabel labelCurrentDisplayName;
    public UILabel labelCurrentType;
    public UILabel labelCurrentStatus;
    public UIInput inputCurrentDisplayName;
#else
    // B10: agnostic UIRef handles (was UGUI), the BaseGameHUD pattern. Unbound (null) until
    // something binds them by name; every UIUtil call no-ops on a null ref.
    public Engine.UI.UIRef buttonCycleLeft;
    public Engine.UI.UIRef buttonCycleRight;
    public Engine.UI.UIRef labelCurrentDisplayName;
    public Engine.UI.UIRef labelCurrentType;
    public Engine.UI.UIRef labelCurrentStatus;
    public Engine.UI.UIRef inputCurrentDisplayName;
#endif

    // NGUI-free identity of the selector, set by the owning panel at bind time. The arrows match by
    // NAME as well as by the legacy refs above (a toolkit element carries the legacy GameObject's
    // name), and the current preset name is kept as a plain string so a view can show it without
    // reading a legacy label back.
    public string buttonCycleLeftName;
    public string buttonCycleRightName;
    public string currentDisplayName;

    public int currentIndex = -1;
    public GameProfileCustomItem currentProfileCustomItem;
    public GameProfileCustomItem initialProfileCustomItem;

    public override void Start() {
        Load();
    }

    public override void Load() {
        currentIndex = -1;
        initialProfileCustomItem = null;
        currentProfileCustomItem = null;
    }

    public override void Update() {

    }

    protected bool IsCycleLeftClicked(string buttonName) {
        return UIUtil.IsButtonClicked(buttonCycleLeft, buttonName)
            || (!string.IsNullOrEmpty(buttonCycleLeftName) && buttonName == buttonCycleLeftName);
    }

    protected bool IsCycleRightClicked(string buttonName) {
        return UIUtil.IsButtonClicked(buttonCycleRight, buttonName)
            || (!string.IsNullOrEmpty(buttonCycleRightName) && buttonName == buttonCycleRightName);
    }

    protected void SetCurrentDisplayName(string value) {
        currentDisplayName = value;
        UIUtil.SetLabelValue(labelCurrentDisplayName, value);
    }
}