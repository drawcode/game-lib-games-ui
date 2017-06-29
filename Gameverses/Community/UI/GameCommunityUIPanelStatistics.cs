using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Engine.Data.Json;
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
            listGridRoot.transform.parent.gameObject.GetComponent<UIDraggablePanel>().ResetPosition();
            yield return new WaitForEndOfFrame();

            GetStatistics();

            int i = 0;

            foreach(GameStatistic statistic in currentStatistics) {

                GameObject item = NGUITools.AddChild(listGridRoot, listItemPrefab);
                item.name = "AStatisticItem" + i;

                item.transform.Find("LabelName").GetComponent<UILabel>().text = statistic.display_name;
                item.transform.Find("LabelDescription").GetComponent<UILabel>().text = statistic.description;

                double statValue = GameProfileStatistics.Current.GetStatisticValue(statistic.code);
                string displayValue = GameStatistics.Instance.GetStatisticDisplayValue(statistic, statValue);

                item.transform.Find("LabelValue").GetComponent<UILabel>().text = displayValue;

                i++;
            }

            yield return new WaitForEndOfFrame();
            listGridRoot.GetComponent<UIGrid>().Reposition();
            yield return new WaitForEndOfFrame();
            listGridRoot.transform.parent.gameObject.GetComponent<UIDraggablePanel>().ResetPosition();
            yield return new WaitForEndOfFrame();

        }
    }
}