using UnityEngine;

public class GoalZone3D : MonoBehaviour
{
    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered)
            return;

        BallController3D ball = other.GetComponent<BallController3D>();

        if (ball == null)
            return;

        triggered = true;

        if (EndScreenUI3D.Instance != null)
            EndScreenUI3D.Instance.ShowEndScreen();
    }
}