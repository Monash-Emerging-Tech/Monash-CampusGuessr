public static class SceneHelper
{
    public static bool IsMenuScene(string sceneName)
    {
        return sceneName == CampusDatabase.Campuses[CampusType.Clayton]
            || sceneName == CampusDatabase.Campuses[CampusType.College]
            || sceneName == SceneNames.MAP_SELECTION;
    }
}