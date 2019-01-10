using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// using Engine.Data.Json;
using Engine.Events;
using Engine.Networking;

public class GameCommunityUIPanelStatistics : UIAppPanelBaseList {


    public GameObject listItemPrefab;
    public List<GameStatistic> currentStatistics = null;
    public static GameCommunityUIPanelStatistics Instance;

    public override void Awake() {
        base.Awake();

        if(Instance != null && this != Instance) {
            //There is already a copy of this script running
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public override void OnEnable() {
        Messenger.AddListener(GameCommunityMessages.gameCommunityReady, OnGameCommunityReady);
    }

    public override void OnDisable() {
        Messenger.RemoveListener(GameCommunityMessages.gameCommunityReady, OnGameCommunityReady);
    }

    void OnGameCommunityReady() {
        Init();
    }

    public override void Start() {

    }

    public override void Init() {
        currentStatistics = new List<GameStatistic>();
        LoadData();
    }

    public void LoadData() {

        currentStatistics = new List<GameStatistic>();

        StartCoroutine(LoadDataCo());
    }

    public List<GameStatistic> GetStatistics() {

        if(currentStatistics == null) {
            currentStatistics = new List<GameStatistic>();
        }
        else {
            currentStatistics.Clear();
        }

        currentStatistics = GameStatistics.Instance.GetAll();

        return currentStatistics;
    }

    IEnumerator LoadDataCo() {

        if(listGridRoot != null) {
            listGridRoot.DestroyChildren();

            yield return new WaitForEndOfFrame();
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
            listGridRoot.transform.parent.gameObject.GetComponent<UIDraggablePanel>().ResetPosition();
            yield return new WaitForEndOfFrame();
#endif

            GetStatistics();

            int i = 0;

            foreach(GameStatistic statistic in currentStatistics) {

#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
                GameObject item = NGUITools.AddChild(listGridRoot, listItemPrefab);
#else
                GameObject item = GameObjectHelper.CreateGameObject(
                    listItemPrefab, Vector3.zero, Quaternion.identity, false);
                // NGUITools.AddChild(listGridRoot, listItemPrefab);
                item.transform.parent = listGridRoot.transform;
                item.ResetLocalPosition();
#endif

                item.name = "AStatisticItem" + i;

                UIUtil.UpdateLabelObject(item.transform, "LabelName", statistic.display_name);
                UIUtil.UpdateLabelObject(item.transform, "LabelDescription", statistic.description);

                double statValue = GameProfileStatistics.Current.GetStatisticValue(statistic.code);
                string displayValue = GameStatistics.Instance.GetStatisticDisplayValue(statistic, statValue);

                UIUtil.UpdateLabelObject(item.transform, "LabelValue", displayValue);

                i++;
            }

            yield return new WaitForEndOfFrame();
#if USE_UI_NGUI_2_7 || USE_UI_NGUI_3
            listGridRoot.GetComponent<UIGrid>().Reposition();
            yield return new WaitForEndOfFrame();
            listGridRoot.transform.parent.gameObject.GetComponent<UIDraggablePanel>().ResetPosition();
            yield return new WaitForEndOfFrame();
#endif

        }
    }
}