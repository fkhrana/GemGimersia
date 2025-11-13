using UnityEngine;

/// <summary>
/// Static session data carried between scenes.
/// Logs when reset or when lastKeyPosition/hasKey are updated elsewhere.
/// </summary>
public static class GameSession
{
    public static Vector3 lastKeyPosition = Vector3.zero;
    public static bool hasKey = false;
    public static bool hasPlayedIntro = false;

    public static void Reset()
    {
        Debug.Log("[GameSession] Resetting session state.");
        lastKeyPosition = Vector3.zero;
        hasKey = false;
    }
}