using UnityEngine;
using UnityEngine.SceneManagement;

// Detects when the ball falls out of the level
public class KillPlane3D : MonoBehaviour
{
    // Called automatically when another collider enters this trigger
    private void OnTriggerEnter(Collider other)
    {
        // Ignore objects that are not the ball
        if (other.GetComponent<BallController3D>() == null)
            return;

        // Ensure game is not paused before reloading
        Time.timeScale = 1f;

        // Reload the current level
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}