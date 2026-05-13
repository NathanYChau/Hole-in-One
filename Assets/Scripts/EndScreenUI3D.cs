using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// Controls the level-complete/end screen UI
public class EndScreenUI3D : MonoBehaviour
{
    // Static instance so other scripts can easily call EndScreenUI3D.Instance
    public static EndScreenUI3D Instance;

    // -------------------------
    // UI REFERENCES
    // -------------------------

    [Header("UI")]

    // Root object for the entire end screen panel
    public GameObject endScreenRoot;

    // Main title text, like "Level Complete!"
    public TMP_Text titleText;

    // Smaller message text under the title
    public TMP_Text messageText;

    // -------------------------
    // SOUND EFFECTS
    // -------------------------

    [Header("SFX")]

    // AudioSource used to play UI sounds
    public AudioSource audioSource;

    // Sound played when the level is completed
    public AudioClip winSFX;

    // Sound played when pressing UI buttons
    public AudioClip buttonClickSFX;

    // -------------------------
    // UNITY AWAKE
    // -------------------------

    private void Awake()
    {
        // Store this script as the global instance
        Instance = this;

        // Automatically get AudioSource if not assigned in Inspector
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    // -------------------------
    // UNITY START
    // -------------------------

    private void Start()
    {
        // Hide end screen at the start of the level
        if (endScreenRoot != null)
            endScreenRoot.SetActive(false);
    }

    // -------------------------
    // SHOW END SCREEN
    // -------------------------

    public void ShowEndScreen()
    {
        // Show the end screen panel
        if (endScreenRoot != null)
            endScreenRoot.SetActive(true);

        // Set title text
        if (titleText != null)
            titleText.text = "Level Complete!";

        // Set message text
        if (messageText != null)
            messageText.text = "You reached the goal!";

        // Play win sound
        if (audioSource != null && winSFX != null)
            audioSource.PlayOneShot(winSFX);

        // Pause game while end screen is visible
        Time.timeScale = 0f;
    }

    // -------------------------
    // RESTART LEVEL BUTTON
    // -------------------------

    public void RestartLevel()
    {
        // Play button sound
        PlayButtonSFX();

        // Unpause before reloading
        Time.timeScale = 1f;

        // Reload current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // -------------------------
    // PLAY BUTTON SOUND
    // -------------------------

    private void PlayButtonSFX()
    {
        // Only play if both AudioSource and clip exist
        if (audioSource != null && buttonClickSFX != null)
            audioSource.PlayOneShot(buttonClickSFX);
    }
}