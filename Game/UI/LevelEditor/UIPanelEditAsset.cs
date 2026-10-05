using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

using UnityEngine;
using Engine.Game.App.BaseApp;
using Engine.Game.App;


#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
#else
using UnityEngine.UI;
#endif

using Engine.Events;

public enum UIPanelEditAssetActionState {
	NONE,
	SELECT_ITEM,
	SELECT_EFFECT,
	SELECT_AUDIO
}

public class UIPanelEditAsset : UIAppPanel, IUIPanelLevelEditorView {

    public static UIPanelEditAsset Instance;

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
    public UIImageButton buttonGameEditAssetDelete;
    public UIImageButton buttonGameEditAssetDeselect;
    public UIImageButton buttonGameEditAssetSave;
    public UIImageButton buttonGameEditAssetSprite;
    public UIImageButton buttonGameEditAssetSpriteEffect;

    public UICheckbox checkboxEditAssetDestructable;
    public UICheckbox checkboxEditAssetKinematic;
    public UICheckbox checkboxEditAssetReactive;
    public UICheckbox checkboxEditAssetGravity;

    public UIInput inputSprite;
    public UIInput inputSpriteEffect;

    public UILabel labelAssetEdit;
    public UILabel labelGameEditAssetSprite;
    public UILabel labelGameEditAssetSpriteEffect;

    // Rotation
    public UIInput inputRotationSpeed;
    public UISlider sliderRotationSpeed;
    public UIImageButton buttonGameEditAssetRotationReset;
#else
    // B11.1: agnostic UIRef handles (was UGUI Button/Toggle/InputField/Text/Slider), the
    // BaseGameHUD pattern. Not serialized, so nothing comes from the prefab: they are bound BY
    // ELEMENT NAME in OnToolkitViewReady (BindLegacyFieldsByName) and reset on free. Initialised
    // to UIRef.none so the name compares in the event handlers below read "" instead of throwing
    // before a view is up.
    public Engine.UI.UIRef buttonGameEditAssetDelete = Engine.UI.UIRef.none;
    public Engine.UI.UIRef buttonGameEditAssetDeselect = Engine.UI.UIRef.none;
    public Engine.UI.UIRef buttonGameEditAssetSave = Engine.UI.UIRef.none;
    public Engine.UI.UIRef buttonGameEditAssetSprite = Engine.UI.UIRef.none;
    public Engine.UI.UIRef buttonGameEditAssetSpriteEffect = Engine.UI.UIRef.none;

    public Engine.UI.UIRef checkboxEditAssetDestructable = Engine.UI.UIRef.none;
    public Engine.UI.UIRef checkboxEditAssetKinematic = Engine.UI.UIRef.none;
    public Engine.UI.UIRef checkboxEditAssetReactive = Engine.UI.UIRef.none;
    public Engine.UI.UIRef checkboxEditAssetGravity = Engine.UI.UIRef.none;

    public Engine.UI.UIRef inputSprite = Engine.UI.UIRef.none;
    public Engine.UI.UIRef inputSpriteEffect = Engine.UI.UIRef.none;

    public Engine.UI.UIRef labelAssetEdit = Engine.UI.UIRef.none;
    public Engine.UI.UIRef labelGameEditAssetSprite = Engine.UI.UIRef.none;
    public Engine.UI.UIRef labelGameEditAssetSpriteEffect = Engine.UI.UIRef.none;

    // Rotation
    public Engine.UI.UIRef inputRotationSpeed = Engine.UI.UIRef.none;
    public Engine.UI.UIRef sliderRotationSpeed = Engine.UI.UIRef.none;
    public Engine.UI.UIRef buttonGameEditAssetRotationReset = Engine.UI.UIRef.none;
#endif

    public GameObject listItemPrefab;

    public UIPanelEditAssetActionState actionState = UIPanelEditAssetActionState.NONE;

    // Tools	

    float MIN_ROTATION_SPEED = -1000f;
    float MAX_ROTATION_SPEED = 1000f;

    public GameLevelItemAsset itemAsset;

    public override void Awake() {
        base.Awake();

        if(Instance != null && this != Instance) {
            //There is already a copy of this script running
            Destroy(this);
            return;
        }

        Instance = this;
    }

    // B11.1 (defect 3): these were PRIVATE `void OnEnable/OnDisable`. Nothing above them defines
    // either (UIAppPanel / GameObjectBehavior have none), so nothing was hidden -- but a product
    // subclass had no chain to call into. Now public virtual, the UIPanelBase shape, so a subclass
    // can override and chain base.OnEnable(). The listener set is unchanged and still registered
    // exactly once (see AttachListeners).
    public virtual void OnEnable() {
        AttachListeners();
    }

    public virtual void OnDisable() {

        // Symmetric with the view's lifetime: release the view and put the legacy widgets back
        // (UINotificationDisplayTip.OnDisable). A no-op when no view was ever loaded.
        FreeEditorView();

        DetachListeners();
    }

    // ONE subscription, from two owners. OnEnable is the legacy owner. The SHOWN toolkit sheet is
    // the other: in the shipping scene this panel sits under the INACTIVE EditorContainer, so
    // OnEnable never runs, yet the view's buttons still broadcast ButtonEvents.EVENT_BUTTON_CLICK
    // and must reach OnButtonClickEventHandler. Messenger is a plain multicast delegate with no
    // dedupe (rule 109), so the flag is what keeps the handler at one registration whichever
    // owner attached first.
    //
    // The toolkit owner holds it only WHILE THE SHEET IS SHOWN (ShowEditorView/HideEditorView),
    // not for the view's lifetime: the Messenger handlers are game-wide (any NGUI checkbox in any
    // screen raises CheckboxEvents.EVENT_ITEM_CHANGE), and OnCheckboxChangeEventHandler reads
    // checkboxEditAssetGravity.name, which is unwired in UIEditor.prefab -- a dark panel listening
    // all session would throw from a Settings toggle once an asset had been selected.
    bool listenersAttached = false;

    void AttachListeners() {

        if(listenersAttached) {
            return;
        }

        listenersAttached = true;

        Messenger<string, bool>.AddListener(CheckboxEvents.EVENT_ITEM_CHANGE, OnCheckboxChangeEventHandler);

        Messenger<string>.AddListener(ButtonEvents.EVENT_BUTTON_CLICK, OnButtonClickEventHandler);

        Messenger<string, int>.AddListener(InputEvents.EVENT_ITEM_CLICK, OnInputClickEventHandler);
        Messenger<string, string>.AddListener(InputEvents.EVENT_ITEM_CHANGE, OnInputChangeEventHandler);

        Messenger<string, float>.AddListener(SliderEvents.EVENT_ITEM_CHANGE, OnSliderChangeEventHandler);
    }

    void DetachListeners() {

        if(!listenersAttached) {
            return;
        }

        listenersAttached = false;

        Messenger<string, bool>.RemoveListener(CheckboxEvents.EVENT_ITEM_CHANGE, OnCheckboxChangeEventHandler);

        Messenger<string>.RemoveListener(ButtonEvents.EVENT_BUTTON_CLICK, OnButtonClickEventHandler);

        Messenger<string, int>.RemoveListener(InputEvents.EVENT_ITEM_CLICK, OnInputClickEventHandler);
        Messenger<string, string>.RemoveListener(InputEvents.EVENT_ITEM_CHANGE, OnInputChangeEventHandler);

        Messenger<string, float>.RemoveListener(SliderEvents.EVENT_ITEM_CHANGE, OnSliderChangeEventHandler);
    }

    public override void Start() {
        Init();
    }

    public override void Init() {
        base.Init();

        LoadData();

        UIUtil.SetSliderValue(sliderRotationSpeed, .5f);
    }

    public void LoadData() {
        LoadDataAsset();
    }

    public void LoadDataAsset() {

        SyncCurrenItemAsset();

        toolkitSyncDepth++;

        try {

            if(itemAsset != null) {
                //UIUtil.SetLabelValue(labelAssetEdit, itemAsset.asset_code);
                UIUtil.SetInputValue(inputSprite, itemAsset.code);
                UIUtil.SetLabelValue(labelGameEditAssetSprite, GetItemAssetDisplayName(itemAsset.code));
                UIUtil.SetInputValue(inputSpriteEffect, itemAsset.destroy_effect_code);
                UIUtil.SetLabelValue(labelGameEditAssetSpriteEffect, GetItemAssetDisplayName(itemAsset.destroy_effect_code));

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
                UIUtil.SetCheckboxValue(checkboxEditAssetDestructable, itemAsset.destructable);
                UIUtil.SetCheckboxValue(checkboxEditAssetKinematic, itemAsset.kinematic);
                UIUtil.SetCheckboxValue(checkboxEditAssetReactive, itemAsset.reactive);
                UIUtil.SetCheckboxValue(checkboxEditAssetGravity, itemAsset.gravity);
#else
                UIUtil.SetToggleValue(checkboxEditAssetDestructable, itemAsset.destructable);
                UIUtil.SetToggleValue(checkboxEditAssetKinematic, itemAsset.kinematic);
                UIUtil.SetToggleValue(checkboxEditAssetReactive, itemAsset.reactive);
                UIUtil.SetToggleValue(checkboxEditAssetGravity, itemAsset.gravity);
#endif

                UpdateRotation((float)itemAsset.speed_rotation.z);
            }

            UpdateDisplay();

            ApplyToolkitAsset();
        }
        finally {
            toolkitSyncDepth--;
        }
    }

    void UpdateDisplay() {
        if(itemAsset != null) {
            if(itemAsset.destructable) {
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
                UIUtil.ShowInput(inputSpriteEffect);
                UIUtil.ShowLabel(labelGameEditAssetSpriteEffect);
                UIUtil.ShowButton(buttonGameEditAssetSpriteEffect);
#else
                UIUtil.ShowObject(inputSpriteEffect);
                UIUtil.ShowLabel(labelGameEditAssetSpriteEffect);
                UIUtil.ShowObject(buttonGameEditAssetSpriteEffect);
#endif
            }
            else {
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
                UIUtil.HideInput(inputSpriteEffect);
                UIUtil.HideLabel(labelGameEditAssetSpriteEffect);
                UIUtil.HideButton(buttonGameEditAssetSpriteEffect);
#else
                UIUtil.HideObject(inputSpriteEffect);
                UIUtil.HideLabel(labelGameEditAssetSpriteEffect);
                UIUtil.HideObject(buttonGameEditAssetSpriteEffect);
#endif
            }
        }

        ApplyToolkitDisplay();
    }

    public void UpdateRotation(float val) {
        UpdateRotation(val, false, false);
    }

    public void UpdateRotation(float val, bool deferSlider, bool deferInput) {
        if(itemAsset != null) {

            val = Mathf.Clamp(val, MIN_ROTATION_SPEED, MAX_ROTATION_SPEED);
            float sliderVal = NormalizeRotationSlider(val);

            toolkitSyncDepth++;

            try {

                if(!deferInput) {
                    UIUtil.SetInputValue(inputRotationSpeed, val.ToString());
                }
                if(!deferSlider) {
                    UIUtil.SetSliderValue(sliderRotationSpeed, sliderVal);
                }

                ApplyToolkitRotation(val, deferSlider, deferInput);
            }
            finally {
                toolkitSyncDepth--;
            }

            Vector3 posFrom = Vector3.zero;

            if(GameDraggableEditor.GetCanvasType() == GameDraggableCanvasType.CANVAS_2D) {
                posFrom = posFrom.WithZ(-val);
            }
            else {
                posFrom = posFrom.WithY(-val);
            }

            itemAsset.speed_rotation.FromVector3(posFrom);
        }
    }

    public float NormalizeRotation(float actualRotation) {
        return actualRotation / MAX_ROTATION_SPEED;
    }

    public float DenormalizeRotation(float normalizedRotation) {
        return normalizedRotation * MAX_ROTATION_SPEED;
    }

    public float NormalizeRotationSlider(float actualRotation) {
        float adjusted = NormalizeRotation(actualRotation);

        // when 	1000/1000 	= 		1 	= 	1
        // when 	0/1000 		= 		0 	= 	.5
        // when 	-1000/1000 	= 		-1 	= 	0
        // 1/-1 = .5/x

        adjusted = adjusted * .5f + .5f;

        return adjusted;
    }

    public float DenormalizeRotationSlider(float normalizedRotation) {
        float adjusted = (normalizedRotation - .5f) / .5f;
        adjusted = DenormalizeRotation(adjusted);
        return adjusted;
    }

    public void UpdateSprite(string assetCode) {
        if(itemAsset != null) {
            itemAsset.code = assetCode;

            toolkitSyncDepth++;

            try {
                UIUtil.SetInputValue(inputSprite, itemAsset.code);
                UIUtil.SetLabelValue(labelAssetEdit, itemAsset.code);
                UIUtil.SetLabelValue(labelGameEditAssetSprite, GetItemAssetDisplayName(itemAsset.code));

                // LabelAssetEdit is only ever written here (LoadDataAsset leaves the authored
                // "EDIT SELECTED ASSET"), so it is kept for the replay rather than derived.
                toolkitLabelAssetEdit = itemAsset.code;

                ApplyToolkitAsset();
            }
            finally {
                toolkitSyncDepth--;
            }

            GameDraggableLevelItem levelItem = GameDraggableEditor.GetCurrentDraggableLevelItem();
            if(levelItem != null) {
                levelItem.LoadSprite(itemAsset.code);
            }
        }
    }

    public void UpdateSpriteEffect(string assetCode) {
        if(itemAsset != null) {
            itemAsset.destroy_effect_code = assetCode;

            toolkitSyncDepth++;

            try {
                UIUtil.SetInputValue(inputSpriteEffect, itemAsset.destroy_effect_code);
                UIUtil.SetLabelValue(labelGameEditAssetSpriteEffect, GetItemAssetDisplayName(itemAsset.destroy_effect_code));

                ApplyToolkitAsset();
            }
            finally {
                toolkitSyncDepth--;
            }
        }
    }

    public void SyncCurrenItemAsset() {
        if(GameDraggableEditor.Instance != null) {
            itemAsset = GameDraggableEditor.GetCurrentLevelItemAsset();
        }
    }

    public void SaveDataAsset() {

        SyncCurrenItemAsset();

        if(itemAsset != null) {

            // With the view up the legacy widgets are stale (the toolkit toggles write the asset
            // directly and nothing mirrors them back onto the suppressed NGUI checkboxes), so
            // reading them here would undo the player's edits. The view is the source.
            if(isToolkitPanel) {
                SaveDataAssetToolkit();
                return;
            }

            itemAsset.destroy_effect_code = UIUtil.GetInputValue(inputSpriteEffect);
            itemAsset.code = UIUtil.GetInputValue(inputSprite);

            // B11.1 (defect 4): was `-float.Parse(...)`, which THREW on an empty or non-numeric
            // field and abandoned the save half-done (codes written, rotation and the four flags
            // not). TryParse has the same culture and styles as Parse, so every value that parsed
            // before parses identically; one that threw now keeps the asset's current rotation.
            float rotationSpeed = 0f;

            if(float.TryParse(UIUtil.GetInputValue(inputRotationSpeed), out rotationSpeed)) {

                rotationSpeed = -rotationSpeed;

                Vector3 vectorSpeed = Vector3.zero;

                if(GameDraggableEditor.GetCanvasType() == GameDraggableCanvasType.CANVAS_2D) {
                    vectorSpeed = Vector3.zero.WithZ(rotationSpeed);
                }
                else {
                    vectorSpeed = Vector3.zero.WithY(rotationSpeed);
                }

                itemAsset.speed_rotation.FromVector3(vectorSpeed);
            }

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
            itemAsset.destructable = UIUtil.GetCheckboxValue(checkboxEditAssetDestructable);
            itemAsset.kinematic = UIUtil.GetCheckboxValue(checkboxEditAssetKinematic);
            itemAsset.reactive = UIUtil.GetCheckboxValue(checkboxEditAssetReactive);
            itemAsset.gravity = UIUtil.GetCheckboxValue(checkboxEditAssetGravity);
#else
            itemAsset.destructable = UIUtil.GetToggleValue(checkboxEditAssetDestructable);
            itemAsset.kinematic = UIUtil.GetToggleValue(checkboxEditAssetKinematic);
            itemAsset.reactive = UIUtil.GetToggleValue(checkboxEditAssetReactive);
            itemAsset.gravity = UIUtil.GetToggleValue(checkboxEditAssetGravity);
#endif
        }
    }

    public string GetItemAssetDisplayName(string code) {
        AppContentAsset asset = AppContentAssets.Instance.GetById(code);
        if(asset != null) {
            return asset.display_name;
        }
        return code;
    }


    /*
	IEnumerator LoadDataCo() {
		
		yield return new WaitForSeconds(.1f);
		
		if (listGridRoot != null) {
            foreach (Transform item in listGridRoot.transform) {
                Destroy(item.gameObject);
            }
		
			List<AppContentAsset> assets = AppContentAssets.Instance.GetAll();
		
	        LogUtil.Log("Load AppContentAsset: assets.Count: " + assets.Count);
			
			int i = 0;
			
			int totalPoints = 0;
			
	        foreach(AppContentAsset asset in assets) {
				
	            GameObject item = NGUITools.AddChild(listGridRoot, listItemPrefab);
	            item.name = "AssetItem" + i;
	            item.transform.FindChild("LabelName").GetComponent<UILabel>().text = asset.display_name;
	            //item.transform.FindChild("LabelDescription").GetComponent<UILabel>().text = achievement.description;
				
				GameObject gameLevelItemObject = item.transform.FindChild("GameLevelItemObject").gameObject;	
				
				// clear current items
				
				foreach(Transform t in gameLevelItemObject.transform) {
					Destroy(t.gameObject);
				}
				
				GameObject go = GameShooterController.Instance.LoadSpriteUI(
					gameLevelItemObject, asset.code, Vector3.one);
				gameLevelItemObject.ChangeLayersRecursively("UILayer");
				
				float maxSize = 3;
				
				if(go != null) {
					PackedSprite sprite = go.GetComponent<PackedSprite>();
					if(sprite != null) {
						
						float adjust = 1;
						
						if(sprite.height > sprite.width) {
							if(sprite.height > maxSize) {
								adjust = maxSize/sprite.height;
							}
						}
						else {
							if(sprite.width > maxSize) {
								adjust = maxSize/sprite.width;
							}
						}
						
						go.transform.localScale = go.transform.localScale.WithX(adjust).WithY(adjust).WithZ(adjust);
					}
				}
				
				item.transform.FindChild("ButtonGameLevelItemObject").GetComponent<UIButton>().name 
						= "ButtonGameLevelItemObject$" + asset.code; ///levels[y].name;
				
				i++;
	        }
			
	        yield return new WaitForEndOfFrame();
	        listGridRoot.GetComponent<UIGrid>().Reposition();
	        yield return new WaitForEndOfFrame();	
			
        }
	}
	*/

    void OnInputClickEventHandler(string inputName, int cam) {
        LogUtil.Log("OnInputClickEventHandler: inputName:" + inputName);

        if(inputName == inputSprite.name) {
            actionState = UIPanelEditAssetActionState.SELECT_ITEM;
            GameDraggableEditor.ShowUIPanelDialogItems();
        }
        else if(inputName == inputSpriteEffect.name) {
            actionState = UIPanelEditAssetActionState.SELECT_EFFECT;
            GameDraggableEditor.ShowUIPanelDialogItems();
        }
    }

    void OnInputChangeEventHandler(string inputName, string val) {
        LogUtil.Log("OnInputChangeEventHandler: val:" + val);

        if(itemAsset != null) {
            if(inputName == inputRotationSpeed.name) {

                float rotationValue = 0f;
                string inputValue = UIUtil.GetInputValue(inputRotationSpeed);
                if(!string.IsNullOrEmpty(inputValue)) {
                    bool converted = float.TryParse(inputValue, out rotationValue);
                    if(!converted) {
                        rotationValue = 0f;
                    }
                }

                if(itemAsset != null) {
                    UpdateRotation(rotationValue, false, false);
                }
            }
        }
    }

    void OnSliderChangeEventHandler(string sliderName, float val) {
        //LogUtil.Log("SliderEvents:OnSliderChange: sliderName: " + sliderName + " changeValue:" + val);

        if(itemAsset != null) {
            if(sliderName == sliderRotationSpeed.name) {
                UpdateRotation(DenormalizeRotationSlider(val), true, false);
            }
        }
    }

    void OnCheckboxChangeEventHandler(string checkboxName, bool selected) {
        LogUtil.Log("OnCheckboxChangeEventHandler: checkboxName:" + checkboxName + " selected:" + selected);

        if(itemAsset != null) {
            if(checkboxName == checkboxEditAssetDestructable.name) {
                if(itemAsset != null) {
                    itemAsset.destructable = selected;
                    UpdateDisplay();
                }
            }
            else if(checkboxName == checkboxEditAssetKinematic.name) {
                if(itemAsset != null) {
                    itemAsset.kinematic = selected;
                }
            }
            else if(checkboxName == checkboxEditAssetReactive.name) {
                if(itemAsset != null) {
                    itemAsset.reactive = selected;
                }
            }
            else if(checkboxName == checkboxEditAssetGravity.name) {
                if(itemAsset != null) {
                    itemAsset.gravity = selected;
                }
            }
        }
    }

    void OnButtonClickEventHandler(string buttonName) {

        if(itemAsset != null) {
            if(buttonName == buttonGameEditAssetSave.name) {
                SaveDataAsset();
                ////GameDraggableEditor.ResetAssetPanelRemoveDeselect();
                actionState = UIPanelEditAssetActionState.NONE;
            }
            else if(buttonName == buttonGameEditAssetDelete.name) {

                GameDraggableLevelItem levelItem = GameDraggableEditor.GetCurrentDraggableLevelItem();
                if(levelItem != null) {
                    levelItem.DestroyMeAnimated();
                }

                ////GameDraggableEditor.ResetAssetPanelRemoveDeselect();
                actionState = UIPanelEditAssetActionState.NONE;
            }
            else if(buttonName == buttonGameEditAssetDeselect.name) {
                ////GameDraggableEditor.ResetAssetPanelRemoveDeselect();
                actionState = UIPanelEditAssetActionState.NONE;
            }
            else if(buttonName == buttonGameEditAssetSprite.name) {
                actionState = UIPanelEditAssetActionState.SELECT_ITEM;
                GameDraggableEditor.ShowUIPanelDialogItems();
            }
            else if(buttonName == buttonGameEditAssetSpriteEffect.name) {
                actionState = UIPanelEditAssetActionState.SELECT_EFFECT;
                GameDraggableEditor.ShowUIPanelDialogItems();
            }

            else if(buttonName == buttonGameEditAssetRotationReset.name) {
                UpdateRotation(0f, false, false);
            }

            // Toolkit only: the view draws the two sprite fields as BUTTONS (a tap opens the item
            // picker, the legacy UIInput's only use), so they arrive here as clicks rather than
            // as InputEvents.EVENT_ITEM_CLICK. Gated on a loaded view; no NGUI widget broadcasts
            // these names as a button click.
            else if(isToolkitPanel && buttonName == elementInputSprite) {
                actionState = UIPanelEditAssetActionState.SELECT_ITEM;
                GameDraggableEditor.ShowUIPanelDialogItems();
            }
            else if(isToolkitPanel && buttonName == elementInputSpriteEffect) {
                actionState = UIPanelEditAssetActionState.SELECT_EFFECT;
                GameDraggableEditor.ShowUIPanelDialogItems();
            }
        }
    }

    // ==========================================================================================
    // UI TOOLKIT (B11.1 -- the Edit Asset sheet)
    //
    // The lifecycle is UIPanelLevelEditorView (load / suppress / show / hide / free -- see the
    // notes there for why it works on an INACTIVE panel). This half is the panel's own: the view
    // key, the element-name wire contract, the toolkit change handlers, and the writes.
    //
    // WRITES GO BY ELEMENT NAME, in BOTH builds. In the NGUI build the fields above are legacy
    // UICheckbox/UIInput/UILabel (rule 26) and every UIUtil call on them lands on the widget
    // SuppressLegacyView just hid. So each write method ends in an Apply* that pushes the same
    // value at the view, and OnToolkitViewReady REPLAYS them all from the panel's state
    // (itemAsset + the last rotation), because GameDraggableEditor calls LoadData before the
    // async view has landed on the first show.
    //
    // REENTRANCY: a toolkit Slider/Toggle/TextField raises its change event on a PROGRAMMATIC
    // value write too, so writing the slider would re-enter the slider handler, which writes the
    // field, whose handler writes the slider... Every write path holds toolkitSyncDepth, and the
    // toolkit handlers return while it is held. The NGUI Messenger handlers never read it.

    public const string elementButtonDelete = "ButtonGameEditAssetDelete";
    public const string elementButtonDeselect = "ButtonGameEditAssetDeselect";
    public const string elementButtonSave = "ButtonGameEditAssetSave";
    public const string elementButtonSprite = "ButtonGameEditAssetSprite";
    public const string elementButtonSpriteEffect = "ButtonGameEditAssetSpriteEffect";
    public const string elementCheckboxDestructable = "CheckboxEditAssetDestructable";
    public const string elementCheckboxKinematic = "CheckboxEditAssetKinematic";
    public const string elementCheckboxReactive = "CheckboxEditAssetReactive";
    // Not in UIEditor.prefab (the field is unwired there); a view that carries it gets it bound.
    public const string elementCheckboxGravity = "CheckboxEditAssetGravity";
    public const string elementInputSprite = "InputSprite";
    public const string elementInputSpriteEffect = "InputSpriteEffect";
    public const string elementLabelAssetEdit = "LabelAssetEdit";
    // Legacy: the "Label" child of ButtonGameEditAssetSprite(/SpriteEffect). Element names must be
    // unique in a view, so the view names them after the field instead.
    public const string elementLabelSprite = "LabelGameEditAssetSprite";
    public const string elementLabelSpriteEffect = "LabelGameEditAssetSpriteEffect";
    public const string elementInputRotationSpeed = "InputRotationSpeed";
    // The legacy GameObject is "SliderRotation" (the field is sliderRotationSpeed). Range 0..1,
    // like the NGUI slider: NormalizeRotationSlider maps -1000..1000 onto it.
    public const string elementSliderRotation = "SliderRotation";
    public const string elementButtonRotationReset = "ButtonGameEditAssetRotationReset";

    UIPanelLevelEditorView _editorView;

    UIPanelLevelEditorView editorView {
        get {
            if(_editorView == null) {
                _editorView = new UIPanelLevelEditorView(this, this);
            }
            return _editorView;
        }
    }

    int toolkitSyncDepth = 0;

    string toolkitLabelAssetEdit = null;

    bool toolkitHasRotation = false;
    float toolkitRotation = 0f;

    public bool isToolkitPanel {
        get {
            return editorView.isLoaded;
        }
    }

    public virtual string toolkitViewKey {
        get {
            return BaseUIPanel.panelLevelEditorAsset;
        }
    }

    // Overlay band, above the toolbar (+1) and below the dialogs (+3), the legacy depth order:
    // the items picker opened from this sheet must cover it.
    public virtual int toolkitSortOrder {
        get {
            return Engine.UI.UILayers.overlay + 2;
        }
    }

    // AnchorBottom in the prefab, closed at y -960 by GameDraggableEditor.
    public virtual bool toolkitSlidesFromBottom {
        get {
            return true;
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

    // GameDraggableEditor's entry points, called next to its legacy position tweens. Safe on an
    // inactive panel, and a no-op (beyond remembering the request) while no view exists.
    public virtual void ShowEditorView() {

        // The view's clicks need a listener; a panel that never woke up has none of its own.
        // Only when a view can come (kill switch on, key set) -- see the AttachListeners note.
        if(!isActiveAndEnabled
            && Engine.UI.UIPlatform.toolkitViewsEnabled
            && !string.IsNullOrEmpty(toolkitViewKey)) {
            AttachListeners();
        }

        editorView.Show();
    }

    public virtual void HideEditorView() {

        editorView.Hide();

        if(!isActiveAndEnabled) {
            DetachListeners();
        }
    }

    public virtual void FreeEditorView() {

        editorView.Free();

        // A sheet freed while shown without a view (none authored yet) still holds the toolkit
        // owner's subscription; release it here too (OnToolkitViewFreed only runs for a view).
        if(this == null || !isActiveAndEnabled) {
            DetachListeners();
        }
    }

    public virtual void OnToolkitViewReady(Engine.UI.UIRef view) {

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
#else
        BindLegacyFieldsByName();
#endif

        // Fresh elements per load, so registering here never stacks handlers.
        UIUtil.SetToggleHandlerChange(editorView.Resolve(elementCheckboxDestructable),
            selected => OnToolkitCheckboxChange(elementCheckboxDestructable, selected));
        UIUtil.SetToggleHandlerChange(editorView.Resolve(elementCheckboxKinematic),
            selected => OnToolkitCheckboxChange(elementCheckboxKinematic, selected));
        UIUtil.SetToggleHandlerChange(editorView.Resolve(elementCheckboxReactive),
            selected => OnToolkitCheckboxChange(elementCheckboxReactive, selected));
        UIUtil.SetToggleHandlerChange(editorView.Resolve(elementCheckboxGravity),
            selected => OnToolkitCheckboxChange(elementCheckboxGravity, selected));

        UIUtil.SetSliderHandlerChange(editorView.Resolve(elementSliderRotation), OnToolkitSliderChange);

        Engine.UI.UIInputChange.SetInputHandlerChange(
            editorView.Resolve(elementInputRotationSpeed), OnToolkitRotationInputChange);

        // The replay.
        toolkitSyncDepth++;

        try {
            ApplyToolkitAsset();
        }
        finally {
            toolkitSyncDepth--;
        }
    }

    public virtual void OnToolkitViewFreed() {

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
#else
        UnbindLegacyFields();
#endif

        // No view, no toolkit clicks. `this == null` first: on scene teardown GameDraggableEditor
        // may free a sheet that already reads as destroyed, where isActiveAndEnabled would throw.
        if(this == null || !isActiveAndEnabled) {
            DetachListeners();
        }
    }

    // Everything the sheet shows, from state. Called by every write path and as the replay.
    void ApplyToolkitAsset() {

        if(!isToolkitPanel || itemAsset == null) {
            return;
        }

        toolkitSyncDepth++;

        try {

            // The sprite fields may be authored as a TextField or (the B11.3 plan) a Button: each
            // write no-ops on the wrong element type, so both are issued.
            SetToolkitFieldText(elementInputSprite, itemAsset.code);
            UIUtil.SetLabelValue(editorView.Resolve(elementLabelSprite), GetItemAssetDisplayName(itemAsset.code));
            SetToolkitFieldText(elementInputSpriteEffect, itemAsset.destroy_effect_code);
            UIUtil.SetLabelValue(editorView.Resolve(elementLabelSpriteEffect), GetItemAssetDisplayName(itemAsset.destroy_effect_code));

            if(toolkitLabelAssetEdit != null) {
                UIUtil.SetLabelValue(editorView.Resolve(elementLabelAssetEdit), toolkitLabelAssetEdit);
            }

            UIUtil.SetToggleValue(editorView.Resolve(elementCheckboxDestructable), itemAsset.destructable);
            UIUtil.SetToggleValue(editorView.Resolve(elementCheckboxKinematic), itemAsset.kinematic);
            UIUtil.SetToggleValue(editorView.Resolve(elementCheckboxReactive), itemAsset.reactive);
            UIUtil.SetToggleValue(editorView.Resolve(elementCheckboxGravity), itemAsset.gravity);

            if(toolkitHasRotation) {
                ApplyToolkitRotation(toolkitRotation, false, false);
            }

            ApplyToolkitDisplay();
        }
        finally {
            toolkitSyncDepth--;
        }
    }

    // UpdateDisplay's show/hide of the destroy-effect row, on the view.
    void ApplyToolkitDisplay() {

        if(!isToolkitPanel || itemAsset == null) {
            return;
        }

        Engine.UI.UIRef input = editorView.Resolve(elementInputSpriteEffect);
        Engine.UI.UIRef label = editorView.Resolve(elementLabelSpriteEffect);
        Engine.UI.UIRef button = editorView.Resolve(elementButtonSpriteEffect);

        if(itemAsset.destructable) {
            UIUtil.ShowObject(input);
            UIUtil.ShowObject(label);
            UIUtil.ShowObject(button);
        }
        else {
            UIUtil.HideObject(input);
            UIUtil.HideObject(label);
            UIUtil.HideObject(button);
        }
    }

    // The value UpdateRotation shows (already clamped), remembered for the replay so the view
    // reads exactly what the legacy field does. Formatted invariant and whole-number: the legacy
    // val.ToString() is current-culture ("12,5" in de) and the toolkit field parses invariant
    // first, so a de comma could never round-trip.
    void ApplyToolkitRotation(float val, bool deferSlider, bool deferInput) {

        toolkitHasRotation = true;
        toolkitRotation = val;

        if(!isToolkitPanel) {
            return;
        }

        toolkitSyncDepth++;

        try {

            if(!deferInput) {
                UIUtil.SetInputValue(editorView.Resolve(elementInputRotationSpeed),
                    val.ToString("0", CultureInfo.InvariantCulture));
            }

            if(!deferSlider) {
                UIUtil.SetSliderValue(editorView.Resolve(elementSliderRotation), NormalizeRotationSlider(val));
            }
        }
        finally {
            toolkitSyncDepth--;
        }
    }

    void SetToolkitFieldText(string elementName, string val) {

        Engine.UI.UIRef el = editorView.Resolve(elementName);

        UIUtil.SetInputValue(el, val);
        UIUtil.SetLabelValue(el, val);
    }

    // The toolkit twins of the three Messenger handlers above. Switch on the ELEMENT name, not
    // the field's .name: in the NGUI build checkboxEditAssetGravity is unwired (null) in the
    // prefab, and the Messenger handler's `.name` compare on it would throw.
    void OnToolkitCheckboxChange(string elementName, bool selected) {

        if(toolkitSyncDepth > 0 || itemAsset == null) {
            return;
        }

        if(elementName == elementCheckboxDestructable) {
            itemAsset.destructable = selected;
            UpdateDisplay();
        }
        else if(elementName == elementCheckboxKinematic) {
            itemAsset.kinematic = selected;
        }
        else if(elementName == elementCheckboxReactive) {
            itemAsset.reactive = selected;
        }
        else if(elementName == elementCheckboxGravity) {
            itemAsset.gravity = selected;
        }
    }

    void OnToolkitSliderChange(float val) {

        if(toolkitSyncDepth > 0 || itemAsset == null) {
            return;
        }

        UpdateRotation(DenormalizeRotationSlider(val), true, false);
    }

    // Same contract as OnInputChangeEventHandler: an empty or unparseable value means 0.
    void OnToolkitRotationInputChange(string val) {

        if(toolkitSyncDepth > 0 || itemAsset == null) {
            return;
        }

        UpdateRotation(ParseRotation(val, 0f), false, false);
    }

    // Invariant first (what ApplyToolkitRotation writes), then the player's culture (what they
    // may type), else the fallback.
    public static float ParseRotation(string val, float fallback) {

        float parsed = 0f;

        if(string.IsNullOrEmpty(val)) {
            return fallback;
        }

        if(float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)) {
            return parsed;
        }

        if(float.TryParse(val, NumberStyles.Float, CultureInfo.CurrentCulture, out parsed)) {
            return parsed;
        }

        return fallback;
    }

    // SaveDataAsset with the view up. The two sprite codes are not read back: the view only
    // DISPLAYS them (a tap opens the picker, and UpdateSprite/UpdateSpriteEffect already wrote the
    // pick into itemAsset). The flags and the rotation are read from the view; an element the view
    // does not carry keeps the asset's current value rather than being forced false.
    void SaveDataAssetToolkit() {

        Engine.UI.UIRef rotationInput = editorView.Resolve(elementInputRotationSpeed);

        if(rotationInput.alive) {

            string rotationText = UIUtil.GetInputValue(rotationInput);

            if(!string.IsNullOrEmpty(rotationText)) {

                // Unparseable keeps what the field last showed. Negated like the legacy save
                // (UpdateRotation stores -val).
                float rotationSpeed = -ParseRotation(rotationText, toolkitRotation);

                Vector3 vectorSpeed = Vector3.zero;

                if(GameDraggableEditor.GetCanvasType() == GameDraggableCanvasType.CANVAS_2D) {
                    vectorSpeed = Vector3.zero.WithZ(rotationSpeed);
                }
                else {
                    vectorSpeed = Vector3.zero.WithY(rotationSpeed);
                }

                itemAsset.speed_rotation.FromVector3(vectorSpeed);
            }
        }

        itemAsset.destructable = ReadToolkitToggle(elementCheckboxDestructable, itemAsset.destructable);
        itemAsset.kinematic = ReadToolkitToggle(elementCheckboxKinematic, itemAsset.kinematic);
        itemAsset.reactive = ReadToolkitToggle(elementCheckboxReactive, itemAsset.reactive);
        itemAsset.gravity = ReadToolkitToggle(elementCheckboxGravity, itemAsset.gravity);
    }

    bool ReadToolkitToggle(string elementName, bool current) {

        Engine.UI.UIRef el = editorView.Resolve(elementName);

        if(!el.alive) {
            return current;
        }

        return UIUtil.GetToggleValue(el);
    }

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
#else
    // No-NGUI build: the fields ARE the view's elements. Bound by the wire-contract names rather
    // than through UIAppPanel.BindElements, whose field-name / kebab-case convention matches none
    // of these PascalCase GameObject names (it would warn once per field and bind nothing).
    void BindLegacyFieldsByName() {

        buttonGameEditAssetDelete = editorView.Resolve(elementButtonDelete);
        buttonGameEditAssetDeselect = editorView.Resolve(elementButtonDeselect);
        buttonGameEditAssetSave = editorView.Resolve(elementButtonSave);
        buttonGameEditAssetSprite = editorView.Resolve(elementButtonSprite);
        buttonGameEditAssetSpriteEffect = editorView.Resolve(elementButtonSpriteEffect);

        checkboxEditAssetDestructable = editorView.Resolve(elementCheckboxDestructable);
        checkboxEditAssetKinematic = editorView.Resolve(elementCheckboxKinematic);
        checkboxEditAssetReactive = editorView.Resolve(elementCheckboxReactive);
        checkboxEditAssetGravity = editorView.Resolve(elementCheckboxGravity);

        inputSprite = editorView.Resolve(elementInputSprite);
        inputSpriteEffect = editorView.Resolve(elementInputSpriteEffect);

        labelAssetEdit = editorView.Resolve(elementLabelAssetEdit);
        labelGameEditAssetSprite = editorView.Resolve(elementLabelSprite);
        labelGameEditAssetSpriteEffect = editorView.Resolve(elementLabelSpriteEffect);

        inputRotationSpeed = editorView.Resolve(elementInputRotationSpeed);
        sliderRotationSpeed = editorView.Resolve(elementSliderRotation);
        buttonGameEditAssetRotationReset = editorView.Resolve(elementButtonRotationReset);
    }

    void UnbindLegacyFields() {

        buttonGameEditAssetDelete = Engine.UI.UIRef.none;
        buttonGameEditAssetDeselect = Engine.UI.UIRef.none;
        buttonGameEditAssetSave = Engine.UI.UIRef.none;
        buttonGameEditAssetSprite = Engine.UI.UIRef.none;
        buttonGameEditAssetSpriteEffect = Engine.UI.UIRef.none;

        checkboxEditAssetDestructable = Engine.UI.UIRef.none;
        checkboxEditAssetKinematic = Engine.UI.UIRef.none;
        checkboxEditAssetReactive = Engine.UI.UIRef.none;
        checkboxEditAssetGravity = Engine.UI.UIRef.none;

        inputSprite = Engine.UI.UIRef.none;
        inputSpriteEffect = Engine.UI.UIRef.none;

        labelAssetEdit = Engine.UI.UIRef.none;
        labelGameEditAssetSprite = Engine.UI.UIRef.none;
        labelGameEditAssetSpriteEffect = Engine.UI.UIRef.none;

        inputRotationSpeed = Engine.UI.UIRef.none;
        sliderRotationSpeed = Engine.UI.UIRef.none;
        buttonGameEditAssetRotationReset = Engine.UI.UIRef.none;
    }
#endif

    // ==========================================================================================
}