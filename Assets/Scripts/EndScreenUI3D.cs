using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class EndScreenUI3D : MonoBehaviour
{
    public static EndScreenUI3D Instance;

    [Header("UI")]
    public GameObject endScreenRoot;
    public TMP_Text titleText;
    public TMP_Text messageText;

    [Header("SFX")]
    public AudioSource audioSource;
    public AudioClip winSFX;
    public AudioClip buttonClickSFX;

    private void Awake()
    {
        Instance = this;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        if (endScreenRoot != null)
            endScreenRoot.SetActive(false);
    }

    public void ShowEndScreen()
    {
        if (endScreenRoot != null)
            endScreenRoot.SetActive(true);

        if (titleText != null)
            titleText.text = "Level Complete!";

        if (messageText != null)
            messageText.text = "You reached the goal!";

        if (audioSource != null && winSFX != null)
            audioSource.PlayOneShot(winSFX);

        Time.timeScale = 0f;
    }

    public void RestartLevel()
    {
        PlayButtonSFX();

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void PlayButtonSFX()
    {
        if (audioSource != null && buttonClickSFX != null)
            audioSource.PlayOneShot(buttonClickSFX);
    }
}