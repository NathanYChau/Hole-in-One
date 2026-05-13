using UnityEngine;

// Detects when the ball reaches the goal zone
public class GoalZone3D : MonoBehaviour
{
    // Prevents the goal from triggering multiple times
    private bool triggered = false;

    // Called automatically when another collider enters this trigger
    private void OnTriggerEnter(Collider other)
    {
        // Ignore additional trigger events after first completion
        if (triggered)
            return;

        // Check if the object entering is the ball
        BallController3D ball = other.GetComponent<BallController3D>();

        // Ignore anything that is not the ball
        if (ball == null)
            return;

        // Mark goal as completed
        triggered = true;

        // Show level complete screen
        if (EndScreenUI3D.Instance != null)
            EndScreenUI3D.Instance.ShowEndScreen();
    }
}