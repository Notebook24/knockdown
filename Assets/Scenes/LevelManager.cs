using UnityEngine;
using UnityEngine.SceneManagement;

// Put on: an empty GameObject called "LevelManager".
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;
    public float nextLevelDelay = 2f;

    bool won;

    void Awake() => Instance = this;

    public void CheckWin()
    {
        if (won) return;

        foreach (Target t in FindObjectsByType<Target>(FindObjectsSortMode.None))
            if (!t.isOut) return;       // still a target on the platform

        won = true;
        Debug.Log("All targets down! Next level...");
        Invoke(nameof(LoadNext), nextLevelDelay);
    }

    void LoadNext()
    {
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        if (next >= SceneManager.sceneCountInBuildSettings) next = 0;   // loop back if last level
        SceneManager.LoadScene(next);
    }
}