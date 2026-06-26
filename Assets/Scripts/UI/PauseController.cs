using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class PauseController : MonoBehaviour
{
    // Prefabs to raise
    [SerializeField] GameObject OverlayPrefab;
    [SerializeField] GameObject DialogPrefab;

    // Visual elements
    private UIDocument _uiDocument;
    private Button _resumeButton;
    private Button _settingsButton;
    private Button _exitButton;
    private Button _quitGameButton;

    private GameObject _overlayInstance;

    #region Initialization
    private void OnEnable()
    {
        _uiDocument = GetComponent<UIDocument>();
        if (_uiDocument == null)
        {
            Debug.LogError("Failed to initialize Pause menu: UIDocument is null");
            return;
        }

        // Hide pause menu by default
        _uiDocument.rootVisualElement.style.display = DisplayStyle.None;

        // Get buttons
        _resumeButton = _uiDocument.rootVisualElement.Q("Resume") as Button;
        _settingsButton = _uiDocument.rootVisualElement.Q("Settings") as Button;
        _exitButton = _uiDocument.rootVisualElement.Q("Exit") as Button;
        _quitGameButton = _uiDocument.rootVisualElement.Q("QuitGame") as Button;

        if (_resumeButton == null ||
            _settingsButton == null ||
            _exitButton == null ||
            _quitGameButton == null)
        {
            Debug.LogError("Failed to initialize Pause menu: at least one button is null");
        }
        else
        {   
            // Subscribe to buttons
            _resumeButton.clicked += OnClickResume;
            _settingsButton.clicked += OnClickSettings;
            _exitButton.clicked += OnClickExit;
            _quitGameButton.clicked += OnClickQuitGame;
        }

        if (OverlayPrefab == null)
        {
            Debug.LogError("Failed to initialize Pause overlay: prefab is null");
        }
        else
        {
            // Create reusable overlay
            InitializeAndHideOverlay();
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from buttons
        if (_resumeButton != null)
            _resumeButton.clicked -= OnClickResume;
        if (_settingsButton != null)
            _settingsButton.clicked -= OnClickSettings;
        if (_exitButton != null)
            _exitButton.clicked -= OnClickExit;
        if (_quitGameButton != null)
            _quitGameButton.clicked -= OnClickQuitGame;
    }

    private void InitializeAndHideOverlay()
    {
        _overlayInstance = Instantiate(OverlayPrefab, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
        if (_overlayInstance == null)
            Debug.LogError("Failed to initialize overlay for Pause");

        ToggleOverlay(show: false);
    }
    #endregion

    public void DoPause()
    {
        // Show pause menu and overlay
        ToggleOverlay(show: true);
        _uiDocument.rootVisualElement.style.display = DisplayStyle.Flex;
    }

    #region Button commands
    private void OnClickResume()
    {
        // todo briez: close pause menu (and hide overlay)
        Debug.Log("DO RESUME");
    }

    private void OnClickSettings()
    {
        // todo briez: open settings (and hide pause?)
        Debug.Log("DO SETTINGS");
    }

    private void OnClickExit()
    {
        // todo briez: conf dialog and return to main menu
        Debug.Log("DO EXIT");
    }

    private void OnClickQuitGame()
    {
        // todo briez: close application
        Debug.Log("DO QUITGAME");
    }
    #endregion

    private void ToggleOverlay(bool show)
    {
        if (_overlayInstance == null)
        {
            Debug.LogError($"Failed to toggle overlay ({show}): instance is null");
            return;
        }

        UIDocument overlayDocument = _overlayInstance.GetComponent<UIDocument>();
        if (overlayDocument == null)
        {
            Debug.LogError($"Failed to toggle overlay ({show}: UIDocument is null");
        }

        overlayDocument.rootVisualElement.style.display =
            show ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
