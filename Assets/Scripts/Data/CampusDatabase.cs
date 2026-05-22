using System.Collections.Generic;

public enum CampusType
{
    None,
    Clayton,
    Caulfield,
    Peninsula,
    College
}

public static class SceneNames
{
    // Shared
    public const string SAMPLE_SCENE = "SampleScene";
    public const string TESTING = "Testing";

    // Menus
    public const string MAP_SELECTION =
        "Map_selection";

    // Campuses
    public const string CLAYTON =
        "MenuScene_MonashClayton";

    public const string COLLEGE =
        "MenuScene_MonashCollege";

    public const string CAULFIELD =
        "Caulfield_V2";

    public const string PENINSULA =
        "Peninsula_Test";

    // Gameplay
    public const string GAME_SCENE =
        "GameScene";

    public const string BREAKDOWN_SCENE =
        "BreakdownScene";

    // Other
    public const string SCENE_101 =
        "101";
}

public static class CampusDatabase
{
    public static readonly Dictionary<CampusType, string>
        Campuses = new()
    {
        {
            CampusType.Clayton,
            SceneNames.CLAYTON
        },

        {
            CampusType.Caulfield,
            SceneNames.CAULFIELD
        },

        {
            CampusType.Peninsula,
            SceneNames.PENINSULA
        },

        {
            CampusType.College,
            SceneNames.COLLEGE
        }
    };
}