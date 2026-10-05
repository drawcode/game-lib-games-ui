using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Engine.Events;

public class UIPanelSettingsProfile : UIPanelBase {
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
    public UIImageButton buttonClose;
    public UIImageButton buttonProfileFacebook;
    public UIImageButton buttonProfileTwitter;
    public UIImageButton buttonProfileGameNetwork;
    public UIInput inputProfileName;
#else
    // B10: agnostic UIRef handles (was UGUI), the BaseGameHUD pattern. Unbound (null) until
    // something binds them by name; every UIUtil call no-ops on a null ref.
    public Engine.UI.UIRef buttonClose;
    public Engine.UI.UIRef buttonProfileFacebook;
    public Engine.UI.UIRef buttonProfileTwitter;
    public Engine.UI.UIRef buttonProfileGameNetwork;
    public Engine.UI.UIRef inputProfileName;
#endif

    public GameObject listItemPrefab;

    public static UIPanelSettingsProfile Instance;

    public override void Awake() {
        base.Awake();

        if(Instance != null && this != Instance) {
            //There is already a copy of this script running
            //Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public static bool isInst {
        get {
            if(Instance != null) {
                return true;
            }
            return false;
        }
    }

    public override void Start() {
        Init();
    }

    public override void Init() {
        base.Init();

        loadData();
    }

    public override void OnEnable() {
        base.OnEnable();

        Messenger<string, string>.AddListener(InputEvents.EVENT_ITEM_CHANGE, OnProfileInputChanged);
    }

    public override void OnDisable() {
        base.OnDisable();

        Messenger<string, string>.RemoveListener(InputEvents.EVENT_ITEM_CHANGE, OnProfileInputChanged);
    }

    void OnProfileInputChanged(string controlName, string data) {

        if(inputProfileName != null && controlName == inputProfileName.name) {
            ChangeUsername(data);
        }
    }

    public void ChangeUsername(string username) {
        if(inputProfileName == null) {
            return;
        }

        UIUtil.SetInputValue(inputProfileName, username);
        GameProfiles.Current.ChangeUser(username);
        GameProfiles.Current.username = username;
        GameState.SaveProfile();
    }

    public static void LoadData() {
        if(Instance != null) {
            Instance.loadData();
        }
    }

    public void loadData() {
        StartCoroutine(loadDataCo());
    }

    IEnumerator loadDataCo() {

        yield return new WaitForSeconds(1f);

        //ChangeUsername(GameProfiles.Current.username);
    }
}