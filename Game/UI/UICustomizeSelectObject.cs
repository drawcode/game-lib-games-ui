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
}