using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class GameUIManager3D : MonoBehaviour
{
    public static GameUIManager3D Instance;

    [Header("UI")]
    public GameObject startScreen;
    public GameObject controlsPopup;
    public GameObject controlsButton;
    public GameObject restartButton;

    [Header("SFX")]
    public AudioSource audioSource;
    public AudioClip buttonClickSFX;

    public bool GameStarted { get; private set; } = false;

    private void Awake()
    {
        Instance = this;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        GameStarted = false;

        startScreen.SetActive(true);
        controlsPopup.SetActive(false);
        controlsButton.SetActive(false);
        restartButton.SetActive(false);

        Time.timeScale = 0f;
    }

    public void StartGame()
    {
        PlayButtonSFX();

        GameStarted = true;

        startScreen.SetActive(false);
        controlsPopup.SetActive(false);
        controlsButton.SetActive(true);
        restartButton.SetActive(true);

        Time.timeScale = 1f;
    }

    public void ShowControls()
    {
        PlayButtonSFX();
        controlsPopup.SetActive(true);
    }

    public void HideControls()
    {
        PlayButtonSFX();
        controlsPopup.SetActive(false);
    }

    public void ToggleControls()
    {
        PlayButtonSFX();
        controlsPopup.SetActive(!controlsPopup.activeSelf);
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