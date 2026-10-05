using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

// B11.1: the level editor TOOLBAR (UIEditor.prefab .../AnchorTop/TopContainer/GameEditTools:
// ITEMS / LEVEL / PLAY / SAVE, the EDIT ASSET button, and the ItemTools "<" button).
//
// The legacy toolbar has NO panel class: GameDraggableEditor slides its GameObject
// (gameEditToolsObject) and handles every one of its clicks by name. That stays true on the
// toolkit path -- the view's buttons broadcast the same names onto ButtonEvents.EVENT_BUTTON_CLICK
// and GameDraggableEditor.OnButtonClickEventHandler answers them -- so this class owns only what
// a view needs: the key, the lifecycle (UIPanelLevelEditorView, safe on an inactive panel), and
// the EDIT ASSET button's show/hide state.
//
// NOT in UIEditor.prefab yet: B11.2 adds it to GameEditTools. GameDraggableEditor finds it
// there (GetComponentInChildren, inactive included); a product without it gets no toolbar view
// and keeps the legacy one.
//
// ButtonGameItemLeft has NO handler anywhere, legacy or toolkit (defect 5): nothing in the code
// says what "<" was meant to do, so it is left inert rather than guessed at.
public class UIPanelEditTools : UIAppPanel, IUIPanelLevelEditorView {

    public static UIPanelEditTools Instance;

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

    public virtual void OnDisable() {
        FreeEditorView();
    }

    // ==========================================================================================
    // UI TOOLKIT

    // Wire contract = the legacy GameObject names; GameDraggableEditorButtons holds the click
    // names, these are the ones this class writes.
    public const string elementButtonItems = "ButtonGameEditItems";
    public const string elementButtonMeta = "ButtonGameEditMeta";
    public const string elementButtonPlay = "ButtonGameEditPlay";
    public const string elementButtonSave = "ButtonGameEditSave";
    public const string elementAssetButtonContainer = "GameEditAssetButton";
    public const string elementButtonAssetTools = "ButtonGameEditAssetTools";
    public const string elementButtonItemLeft = "ButtonGameItemLeft";

    UIPanelLevelEditorView _editorView;

    UIPanelLevelEditorView editorView {
        get {
            if(_editorView == null) {
                _editorView = new UIPanelLevelEditorView(this, this);
            }
            return _editorView;
        }
    }

    // The legacy toolbar AUTHORS the EDIT ASSET button visible and nothing in this scene ever
    // moves it (GameDraggableEditor.gameEditAssetButtonObject is wired to the HUD's ButtonGameEdit
    // instead -- see the B11.1 report), so the view starts the same way and then follows
    // Show/HideAssetToolsButton: shown when an object is grabbed, hidden while its sheet is open.
    bool assetToolsButtonVisible = true;

    public bool isToolkitPanel {
        get {
            return editorView.isLoaded;
        }
    }

    public virtual string toolkitViewKey {
        get {
            return BaseUIPanel.panelLevelEditorTools;
        }
    }

    // The bottom of the editor's overlay stack: the asset sheet (+2) and the dialogs (+3) draw
    // over it, and the whole editor over the HUD.
    public virtual int toolkitSortOrder {
        get {
            return Engine.UI.UILayers.overlay + 1;
        }
    }

    // AnchorTop in the prefab, closed at y 960 by GameDraggableEditor.
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

    public virtual void ShowAssetToolsButton() {
        assetToolsButtonVisible = true;
        ApplyAssetToolsButton();
    }

    public virtual void HideAssetToolsButton() {
        assetToolsButtonVisible = false;
        ApplyAssetToolsButton();
    }

    public virtual void OnToolkitViewReady(Engine.UI.UIRef view) {
        ApplyAssetToolsButton();
    }

    public virtual void OnToolkitViewFreed() {
    }

    // Both the wrapper and the button: whichever the view carries. A missing one no-ops.
    void ApplyAssetToolsButton() {

        if(!isToolkitPanel) {
            return;
        }

        Engine.UI.UIRef container = editorView.Resolve(elementAssetButtonContainer);
        Engine.UI.UIRef button = editorView.Resolve(elementButtonAssetTools);

        if(assetToolsButtonVisible) {
            UIUtil.ShowObject(container);
            UIUtil.ShowObject(button);
        }
        else {
            UIUtil.HideObject(container);
            UIUtil.HideObject(button);
        }
    }

    // ==========================================================================================
}
