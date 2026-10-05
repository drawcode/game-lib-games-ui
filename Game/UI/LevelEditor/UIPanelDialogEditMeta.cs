using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

public class UIPanelDialogEditMeta : UIAppPanel, IUIPanelLevelEditorView {


    public GameObject listItemPrefab;
    
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
    public UIInput inputName;
    public UIInput inputAmmo;
#else
    // B11.1: agnostic UIRef handles (was a bare GameObject, written through the GameObject
    // overload of UIUtil.SetInputValue). Not serialized; bound by element name when the view
    // loads, UIRef.none otherwise, so the null checks in LoadDataCo still read "unbound".
    public Engine.UI.UIRef inputName = Engine.UI.UIRef.none;
    public Engine.UI.UIRef inputAmmo = Engine.UI.UIRef.none;
#endif

    public static UIPanelDialogEditMeta Instance;

    public override void Awake() {
        base.Awake();

        if(Instance != null && this != Instance) {
            //There is already a copy of this script running
            Destroy(this);
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

        LoadData();
    }

    public void LoadData() {

        // Recorded and pushed at the view first: GameDraggableEditor may call this on a panel
        // that never woke up (the dark editor), where the coroutine below cannot start.
        ApplyToolkitMeta();

        // StartCoroutine on an inactive GameObject only logged an error and ran nothing.
        if(!gameObject.activeInHierarchy) {
            return;
        }

        StartCoroutine(LoadDataCo());
    }

    IEnumerator LoadDataCo() {

        GameLevel currentLevel = GameLevels.Current;

        if(inputName != null) {
            UIUtil.SetInputValue(inputName, currentLevel.display_name);
            //inputName.text = currentLevel.display_name;
        }


        if(inputAmmo != null) {
            UIUtil.SetInputValue(inputAmmo, "90");
            //inputAmmo.text = "90";//currentLevel.display_name;
        }
        
        yield break;
        /*
		
		LogUtil.Log("Load GameWorlds: LoadDataCo");
		
		
		LogUtil.Log("Load GameWorlds: LoadDataCo 2");
		
		if (listGridRoot != null) { 
            foreach (Transform item in listGridRoot.transform) {
                Destroy(item.gameObject);
            }
			
			LogUtil.Log("Load GameWorlds: LoadDataCo 3");
		
			List<GameWorld> worlds = GameWorlds.Instance.GetAll();
		
	        LogUtil.Log("Load GameWorlds: worlds.Count: " + worlds.Count);
			
			int i = 0;
			
	        foreach(GameWorld world in worlds) {
				
	            GameObject item = NGUITools.AddChild(listGridRoot, listItemPrefab);
	            item.name = "WorldItem" + world.sort_order;
	            item.transform.FindChild("LabelWorld").GetComponent<UILabel>().text = world.name;
	            item.transform.FindChild("LabelWorldDisplayName").GetComponent<UILabel>().text = world.display_name;
	            item.transform.FindChild("LabelWorldDescription").GetComponent<UILabel>().text = world.description;
				
				//GameObject iconObject = item.transform.FindChild("Icon").gameObject;	
				//UISprite iconSprite = iconObject.GetComponent<UISprite>();
				
				
				//bool completed = GameProfiles.Current.CheckIfAttributeExists(world.code);
				
				//if(completed) {
				//	completed = GameProfiles.Current.GetAchievementValue(world.code);
				//}
				
				//string points = "";
				
				item.transform.FindChild("LabelPoints").GetComponent<UILabel>().text = points;				
			
				// Get trophy icon
				
				i++;
	        }
			
	        //yield return new WaitForEndOfFrame();
	        listGridRoot.GetComponent<UIGrid>().Reposition();
	        yield return new WaitForEndOfFrame();	
			
        }
        */
    }

    // B11.1 (defect-3 shape, additive): this panel had no OnEnable/OnDisable at all, so nothing
    // released a view when a product's (lit) editor container was put away.
    public virtual void OnDisable() {
        FreeEditorView();
    }

    // ==========================================================================================
    // UI TOOLKIT (B11.1 -- the Level Settings dialog)
    //
    // Lifecycle: UIPanelLevelEditorView (see the notes there; it works on an inactive panel).
    // The dialog has no handlers of its own: SAVE and CLOSE (ButtonGameEditMetaSave/-Close) are
    // GameDraggableEditor's, and they reach it by name from the view exactly as from NGUI. The
    // legacy dialog never saves the two fields either (MetaSave only hides), so neither does
    // this -- the view shows the level name and the placeholder ammo "90", nothing more.
    //
    // Writes go BY ELEMENT NAME (rule 26: in the NGUI build the fields are legacy UIInputs) and
    // are replayed from state when the view lands.

    public const string elementInputName = "InputName";
    public const string elementInputAmmo = "InputAmmo";

    // The legacy dialog's only literal value (LoadDataCo).
    public const string toolkitAmmoPlaceholder = "90";

    UIPanelLevelEditorView _editorView;

    UIPanelLevelEditorView editorView {
        get {
            if(_editorView == null) {
                _editorView = new UIPanelLevelEditorView(this, this);
            }
            return _editorView;
        }
    }

    bool toolkitMetaPending = false;

    public bool isToolkitPanel {
        get {
            return editorView.isLoaded;
        }
    }

    public virtual string toolkitViewKey {
        get {
            return BaseUIPanel.panelLevelEditorMeta;
        }
    }

    // A dialog: above the toolbar (+1) and the asset sheet (+2).
    public virtual int toolkitSortOrder {
        get {
            return Engine.UI.UILayers.overlay + 3;
        }
    }

    // AnchorTop/TopContainer/GameEditActions in the prefab.
    public virtual bool toolkitSlidesFromBottom {
        get {
            return false;
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

    public virtual void ShowEditorView() {
        editorView.Show();
    }

    public virtual void HideEditorView() {
        editorView.Hide();
    }

    public virtual void FreeEditorView() {
        editorView.Free();
    }

    public virtual void OnToolkitViewReady(Engine.UI.UIRef view) {

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
#else
        inputName = editorView.Resolve(elementInputName);
        inputAmmo = editorView.Resolve(elementInputAmmo);
#endif

        if(toolkitMetaPending) {
            ApplyToolkitMeta();
        }
    }

    public virtual void OnToolkitViewFreed() {

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
#else
        inputName = Engine.UI.UIRef.none;
        inputAmmo = Engine.UI.UIRef.none;
#endif
    }

    // LoadDataCo's two writes, on the view. Remembered as "pending" (not as values: the level is
    // read again at replay time, so a replay never shows a stale name).
    void ApplyToolkitMeta() {

        toolkitMetaPending = true;

        if(!isToolkitPanel) {
            return;
        }

        GameLevel currentLevel = GameLevels.Current;

        if(currentLevel != null) {
            UIUtil.SetInputValue(editorView.Resolve(elementInputName), currentLevel.display_name);
        }

        UIUtil.SetInputValue(editorView.Resolve(elementInputAmmo), toolkitAmmoPlaceholder);
    }

    // ==========================================================================================
}