using UnityEngine;
using UnityEngine.SceneManagement;

// Requires an AudioSource so button sound effects can play
[RequireComponent(typeof(AudioSource))]
public class GameUIManager3D : MonoBehaviour
{
    // Static instance so other scripts can check GameUIManager3D.Instance
    public static GameUIManager3D Instance;

    // -------------------------
    // UI REFERENCES
    // -------------------------

    [Header("UI")]

    // Start menu shown before the first level begins
    public GameObject startScreen;

    // Popup panel that shows controls
    public GameObject controlsPopup;

    // Button used to open/close controls
    public GameObject controlsButton;

    // Button used to restart the current level
    public GameObject restartButton;

    // -------------------------
    // SOUND EFFECTS
    // -------------------------

    [Header("SFX")]

    // AudioSource used for UI sounds
    public AudioSource audioSource;

    // Sound played when buttons are pressed
    public AudioClip buttonClickSFX;

    // Tracks whether player has started the level
    public bool GameStarted { get; private set; } = false;

    // -------------------------
    // UNITY AWAKE
    // -------------------------

    private void Awake()
    {
        // Store this script as the global instance
        Instance = this;

        // Auto-find AudioSource if not assigned in Inspector
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    // -------------------------
    // UNITY START
    // -------------------------

    private void Start()
    {
        // Game begins as not started
        GameStarted = false;

        // Scene index 0 acts like the first/start level
        if (SceneManager.GetActiveScene().buildIndex == 0)
        {
            // Show start screen only on first scene
            startScreen.SetActive(true);

            // Hide gameplay UI until player presses start
            controlsPopup.SetActive(false);
            controlsButton.SetActive(false);
            restartButton.SetActive(false);

            // Pause game while start screen is open
            Time.timeScale = 0f;
        }
        else
        {
            // Other levels start immediately
            StartGame();
        }
    }

    // -------------------------
    // START GAME BUTTON
    // -------------------------

    public void StartGame()
    {
        // Play button click sound
        PlayButtonSFX();

        // Mark game as started so player controls are enabled
        GameStarted = true;

        // Hide menus and show gameplay buttons
        startScreen.SetActive(false);
        controlsPopup.SetActive(false);
        controlsButton.SetActive(true);
        restartButton.SetActive(true);

        // Resume gameplay
        Time.timeScale = 1f;
    }

    // -------------------------
    // CONTROLS POPUP
    // -------------------------

    public void ShowControls()
    {
        PlayButtonSFX();

        // Show controls panel
        controlsPopup.SetActive(true);
    }

    public void HideControls()
    {
        PlayButtonSFX();

        // Hide controls panel
        controlsPopup.SetActive(false);
    }

    public void ToggleControls()
    {
        PlayButtonSFX();

        // Switch controls popup between visible/hidden
        controlsPopup.SetActive(!controlsPopup.activeSelf);
    }

    // -------------------------
    // RESTART LEVEL BUTTON
    // -------------------------

    public void RestartLevel()
    {
        PlayButtonSFX();

        // Make sure time is unpaused before reloading
        Time.timeScale = 1f;

        // Reload current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // -------------------------
    // PLAY BUTTON SOUND
    // -------------------------

    private void PlayButtonSFX()
    {
        // Only play if AudioSource and clip are assigned
        if (audioSource != null && buttonClickSFX != null)
            audioSource.PlayOneShot(buttonClickSFX);
    }
}