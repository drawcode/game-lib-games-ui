using System.Collections.Generic;

using Engine.Game.App;
using Engine.Game.App.BaseApp;

// Arcade and created-level rows for any toolkit list that has the matching template: the worlds
// panel's list, and the ARCADE and CREATE screens. The caller clears and scrolls the list.
public static class GameWorldsListRows {

    // One row per arcade play type in the current world, gated by GameContentGate with the next
    // locked ones as "more to come" teasers. PLAY is renamed ButtonGamePlay$<arcade state>$<code>.
    public static void FillArcade(Engine.UI.UIRef view, string list, string template) {

        GameWorld world = GameWorlds.Current;
        string worldCode = world != null ? world.code : null;

        List<AppContentCollect> teasers = new List<AppContentCollect>();

        int i = 0;

        // a row behind a FeatureGates key that is off (zombie waves, P5) is not listed at all
        List<AppContentCollect> rows = new List<AppContentCollect>();

        foreach(AppContentCollect row in AppContentCollects.GetByTypeAndWorld(GameArcadeLevelKeys.arcade, worldCode)) {
            if(!GameArcadeLevels.IsGatedOff(row)) {
                rows.Add(row);
            }
        }

        foreach(AppContentCollect row in GameContentGate.Visible(rows, teasers)) {

            Engine.UI.UIRef item = UIUtil.AddListItem(view, list, template, "ArcadeItem" + i);

            i++;

            UIUtil.UpdateLabelObject(item, "LabelName", row.display_name);

            Engine.UI.UIRef play = UIUtil.ResolveDeep(item, "ButtonAction");

            // locked: greyed, no play payload, no input (opacity alone still clicks)
            if(teasers.Contains(row)) {
                UIUtil.SetClass(item, "ar-row--teaser", true);
                UIUtil.UpdateLabelObject(item, "LabelGoal",
                    L10n.TrOrDefault("game_ui_worlds_more_to_come", "More to come"));
                UIUtil.UpdateLabelObject(item, "LabelStage", "");
                UIUtil.SetElementName(play, "ButtonActionTeaser");
                UIUtil.SetElementEnabled(play, false);
                continue;
            }

            int stage = GameArcadeLevels.Stage(row, worldCode);

            GameArcadeLevels.ApplyGoal(row, stage);

            List<AppContentCollectItem> actions = row.GetItemsData();

            UIUtil.UpdateLabelObject(item, "LabelGoal",
                actions.Count > 0 && actions[0].data != null ? actions[0].data.display_name : "");
            UIUtil.UpdateLabelObject(item, "LabelStage",
                L10n.TrOrDefault("game_ui_arcade_stage", "STAGE {0}", stage + 1));

            UIUtil.SetElementName(play,
                BaseUIButtonNames.buttonGamePlay + "$" + AppContentStateMeta.appContentStateGameArcade + "$" + row.code);
        }
    }

    // One row per saved created level, newest first; onlyWorld > 0 keeps that world's levels.
    // Returns the rows added. The delete button is renamed <deleteButton>$<code>: each list has
    // its own name so a tap reaches only the panel showing that list.
    public static int FillCreated(Engine.UI.UIRef view, string list, string template, string deleteButton, int onlyWorld) {

        List<string> codes = GameLevelCodes.ListCreated();

        int added = 0;

        for(int i = 0; i < codes.Count; i++) {

            int world;
            int level;

            if(!GameLevelCodes.TryParseCreated(codes[i], out world, out level)) {
                continue;
            }

            if(onlyWorld > 0 && world != onlyWorld) {
                continue;
            }

            Engine.UI.UIRef item = UIUtil.AddListItem(view, list, template, "CreateLevelItem" + i);

            added++;

            string code = codes[i];

            // value first, handler after: setting the text must not commit it as a name
            Engine.UI.UIRef input = UIUtil.ResolveDeep(item, BaseGameUIPanelGameModeCreate.elementInputName);
            UIUtil.SetInputValue(input, BaseGameUIPanelGameModeCreate.RowName(code, world, level));
            Engine.UI.UIInputChange.SetInputHandlerChange(
                input, value => BaseGameUIPanelGameModeCreate.RenameRow(code, value, input, world, level));

            // the click bus carries only the element name, so the row's buttons carry the code
            UIUtil.SetElementName(
                UIUtil.ResolveDeep(item, "ButtonAction"),
                BaseUIButtonNames.buttonGameModePlayEditLevel + "$" + code);

            UIUtil.SetElementName(
                UIUtil.ResolveDeep(item, "ButtonDelete"), deleteButton + "$" + code);
        }

        return added;
    }
}
