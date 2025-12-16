using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class StartMenu : MonoBehaviour
{
    [Header("UI References")]
    public Button startButton;
    public Button quitButton;
    
    [Header("Scene Settings")]
    public string gameSceneName = "SampleScene";
    
    [Header("Optional - Background")]
    public SpriteRenderer backgroundRenderer;
    
    [Header("Optional - Fade Transition")]
    public float fadeTime = 0.5f;
    public CanvasGroup fadePanel;

    private void Start()
    {
        // Auto-find buttons if not assigned
        if (startButton == null)
            startButton = GameObject.Find("StartButton")?.GetComponent<Button>();
        
        if (quitButton == null)
            quitButton = GameObject.Find("QuitButton")?.GetComponent<Button>();
        
        // Add button listeners
        if (startButton != null)
            startButton.onClick.AddListener(OnStartClicked);
        
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
        
        // Ensure fade panel starts invisible
        if (fadePanel != null)
            fadePanel.alpha = 0;
    }

    public void OnStartClicked()
    {
        Debug.Log("Start button clicked - Loading game scene...");
        
        if (fadePanel != null)
        {
            StartCoroutine(FadeAndLoadScene());
        }
        else
        {
            LoadGameScene();
        }
    }

    public void OnQuitClicked()
    {
        Debug.Log("Quit button clicked");
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    private void LoadGameScene()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    private System.Collections.IEnumerator FadeAndLoadScene()
    {
        // Fade to black
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            fadePanel.alpha = Mathf.Clamp01(elapsed / fadeTime);
            yield return null;
        }
        
        fadePanel.alpha = 1f;
        LoadGameScene();
    }

    // Keyboard shortcuts using new Input System
    private void Update()
    {
        if (Keyboard.current == null) return;
        
        if (Keyboard.current.enterKey.wasPressedThisFrame || 
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            OnStartClicked();
        }
        
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            OnQuitClicked();
        }
    }
}