using UnityEngine;
using UnityEngine.SceneManagement;

// Handles loading the next level in the build order
public class NextLevelLoader : MonoBehaviour
{
    // Called by UI button when player presses "Next Level"
    public void LoadNextLevel()
    {
        // Ensure game is unpaused before changing scenes
        Time.timeScale = 1f;

        // Get next scene index
        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;

        // Check if another level exists
        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            // Load next level
            SceneManager.LoadScene(nextIndex);
        }
        else
        {
            // If no more levels exist,
            // return to first scene (usually main menu or Level 1)
            SceneManager.LoadScene(0);
        }
    }
}