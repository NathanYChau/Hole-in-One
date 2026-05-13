using UnityEngine;
using UnityEngine.SceneManagement;

public class KillPlane3D : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<BallController3D>() == null)
            return;

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}