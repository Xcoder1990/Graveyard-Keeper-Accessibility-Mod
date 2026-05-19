namespace BeamMeUpGerry;

public static class Menus
{
    private static Dictionary<string, Action> MenuActions => new()
    {
        {Constants.Page1, () => ShowMultiAnswer(LocationLists.Locations[0])},
        {Constants.Page2, () => ShowMultiAnswer(LocationLists.Locations[1])},
        {Constants.Page3, () => ShowMultiAnswer(LocationLists.Locations[2])},
        {Constants.Page4, () => ShowMultiAnswer(LocationLists.Locations[3])},
        {Constants.Page5, () => ShowMultiAnswer(LocationLists.Locations[4])},
        {Constants.Page6, () => ShowMultiAnswer(LocationLists.Locations[5])},
        {Constants.Page7, () => ShowMultiAnswer(LocationLists.Locations[6])},
        {Constants.Page8, () => ShowMultiAnswer(LocationLists.Locations[7])},
        {Constants.Page9, () => ShowMultiAnswer(LocationLists.Locations[8])},
        {Constants.Page10, () => ShowMultiAnswer(LocationLists.Locations[9])},
        {Constants.Cancel, Helpers.EnablePlayerControl}
    };

    internal static void ShowMultiAnswer(List<AnswerVisualData> answers)
    {
        Helpers.DisablePlayerControl();

        MainGame.me.player.ShowMultianswer(answers, MenuOnOnChosen, talker: MainGame.me.player);
    }

    private static void MenuOnOnChosen(string chosen)
    {
        if (MenuActions.TryGetValue(chosen, out var action))
        {
            action.Invoke();
            return;
        }

        var allLocations = new[] {LocationLists.AllLocations};
        var chosenLocation = allLocations.SelectMany(list => list).FirstOrDefault(loc => loc.zone == chosen);

        if (chosenLocation != null)
        {
            if (chosenLocation.defaultLocation)
            {
                Teleport.TryTeleport(chosenLocation);
            }
            else
            {
                if (Helpers.HasTheMoney())
                {
                    Teleport.TryTeleport(chosenLocation);
                }
            }
        }

        Helpers.EnablePlayerControl();
    }
}