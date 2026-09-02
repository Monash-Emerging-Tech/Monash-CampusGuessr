using System;

public static class ZLevelFormatter
{
    /// <summary>
    /// Converts z-level number to readable name
    /// </summary>
    /// <param name="zLevel">Z-level number</param>
    /// <returns>Readable level name</returns>
    public static string GetName(int zLevel)
    {
        if (zLevel == -4) return "P4 (Parking Level 4)";
        if (zLevel == -3) return "P3 (Parking Level 3)";
        if (zLevel == -2) return "P2 (Parking Level 2)";
        if (zLevel == -1) return "P1 (Parking Level 1)";
        if (zLevel == 0) return "LG (Lower Ground)";
        if (zLevel == 1) return "G (Ground)";
        if (zLevel == 2) return "1 (First Floor)";
        if (zLevel == 3) return "2 (Second Floor)";
        if (zLevel == 4) return "3 (Third Floor)";
        if (zLevel == 5) return "4 (Fourth Floor)";
        if (zLevel == 6) return "5 (Fifth Floor)";
        if (zLevel == 7) return "6 (Sixth Floor)";
        if (zLevel == 8) return "7 (Seventh Floor)";
        if (zLevel == 9) return "8 (Eighth Floor)";
        if (zLevel == 10) return "9 (Ninth Floor)";
        if (zLevel == 11) return "10 (Tenth Floor)";
        if (zLevel == 12) return "11 (Eleventh Floor)";
        if (zLevel < -4) return $"B{Math.Abs(zLevel)} (Basement {Math.Abs(zLevel)})";

        return $"{zLevel} (Level {zLevel})";
    }
}
