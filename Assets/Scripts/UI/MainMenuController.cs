using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/* TODO BRIEZ:
 * Progress bar while loading level
 * Leaderboard population
 */

public class MainMenuController : MonoBehaviour
{
    // Inspector properties
    [SerializeField] private string _firstLevelName = "LevelOne";
    [SerializeField] private UIDocument _settingsDocument;

    // Main menu UI document
    private UIDocument _uiDocument;

    // Visual elements
    private Button _newGameButton;
    private Button _settingsButton;
    private Button _exitButton;

    #region Initialization
    private void OnEnable()
    {
        _uiDocument = GetComponent<UIDocument>();
        if (_uiDocument == null)
        {
            Debug.LogError("Unable to locate Main Menu UIDocument!");
            return;
        }

        // Get visual elements
        _newGameButton = _uiDocument.rootVisualElement.Q("NewGame") as Button;
        _settingsButton = _uiDocument.rootVisualElement.Q("Settings") as Button;
        _exitButton = _uiDocument.rootVisualElement.Q("Exit") as Button;

        SubscribeButtons();
    }

    private void OnDisable()
    {
        UnsubscribeButtons();
    }

    private void SubscribeButtons()
    {
        if (_newGameButton != null)
            _newGameButton.clicked += OnClickNewGame;
        if (_settingsButton != null)
            _settingsButton.clicked += OnClickSettings;
        if (_exitButton != null)
            _exitButton.clicked += OnExit;
    }

    private void UnsubscribeButtons()
    {
        if (_newGameButton != null)
            _newGameButton.clicked -= OnClickNewGame;
        if (_settingsButton != null)
            _settingsButton.clicked -= OnClickSettings;
        if (_exitButton != null)
            _exitButton.clicked -= OnExit;
    }
    #endregion

    #region Button events
    private void OnClickNewGame()
    {
        // Load into first level
        StartCoroutine(LoadFirstLevelAsync());
    }

    private void OnClickSettings()
    {
        // Display settings menu

        if (_settingsDocument == null)
        {
            Debug.LogError("Settings UIDocument not found!");
            return;
        }

        _settingsDocument.rootVisualElement.style.display = DisplayStyle.Flex;
    }

    private void OnExit()
    {
        Application.Quit();
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif
    }

    private IEnumerator LoadFirstLevelAsync()
    {
        // Hide main menu UI
        _uiDocument.rootVisualElement.style.display = DisplayStyle.None;

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(_firstLevelName);
        loadOperation.allowSceneActivation = false;

        while (!loadOperation.isDone)
        {
            if (loadOperation.progress >= 0.9f)
            {
                loadOperation.allowSceneActivation = true;
            }

            yield return null;
        }
    }
    #endregion
}
