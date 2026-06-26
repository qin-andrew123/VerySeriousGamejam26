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

    // Main menu Scene name
    [SerializeField] private string MainMenuName = "MainMenu";

    // Settings controller
    [SerializeField] SettingsController SettingsController;

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
        // Hide pause menu and overlay
        ToggleOverlay(show: false);
        _uiDocument.rootVisualElement.style.display = DisplayStyle.None;
        
        Debug.Log("Pause menu --> Resume");
    }

    private void OnClickSettings()
    {
        if (SettingsController == null)
        {
            Debug.LogError("Unable to open settings: controller not found!");
            return;
        }

        // Display settings (without additional overlay)
        SettingsController.DisplaySettings();

        Debug.Log("Pause menu --> Settings");
    }

    private void OnClickExit()
    {
        // Exit callback
        Action exitAction = () =>
        {
            Debug.Log("Executing Exit callback");
            StartCoroutine(LoadMainMenuAsync());
        };

        // Raise confirmation dialog
        StartCoroutine(RaiseDialogAndWait(
            titleText: "Exit to main menu?",
            bodyText: "Any unsaved progress will be lost!",
            okCallback: exitAction));
        
        Debug.Log("Pause menu --> Exit");
    }

    private void OnClickQuitGame()
    {
        // QuitGame callback
        Action exitGameCallback = () =>
        {
            Application.Quit();
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#endif
        };

        // Raise confirmation dialog
        StartCoroutine(RaiseDialogAndWait(
            titleText: "Quit application?",
            bodyText: "Any unsaved progress will be lost!",
            okCallback: exitGameCallback));

        Debug.Log("Pause menu --> QuitGame");

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

    private IEnumerator LoadMainMenuAsync()
    {
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(MainMenuName);
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

    #region Warning Dialog
    // Copied from SettingsController.cs with minor tweaks
    // Duping is messier but faster ATP sorry
    
    private DIALOG_RESULT _dialogResult = DIALOG_RESULT.None;

    private IEnumerator RaiseDialogAndWait(
        string titleText, string bodyText, Action okCallback)
    {
        // Check for already-active dialogs
        if (_dialogResult != DIALOG_RESULT.None)
        {
            Debug.Log($"Another waiting dialog is already open!");
            yield break;
        }

        GameObject dialogInstance = InstantiateDialog(titleText, bodyText);
        if (dialogInstance == null)
        {
            Debug.LogError($"Failed to instantiate dialog!");
            yield break;
        }

        // Wait for user response
        _dialogResult = DIALOG_RESULT.Waiting;
        yield return new WaitUntil(() => _dialogResult != DIALOG_RESULT.Waiting);

        // Execute callback if user elected to continue
        if (_dialogResult == DIALOG_RESULT.OK)
        {
            okCallback();
        }

        CleanUpDialog(dialogInstance);
        _dialogResult = DIALOG_RESULT.None;
    }

    private GameObject InstantiateDialog(string titleText, string bodyText)
    {
        // Create dialog GameObject
        if (DialogPrefab == null)
            return null;

        GameObject dialogInstance = Instantiate(DialogPrefab, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
        if (dialogInstance == null)
            return null;

        // Get visual tree
        UIDocument dialogDocument = dialogInstance.GetComponent<UIDocument>();
        if (dialogDocument == null)
            return null;

        // Set text fields
        Label titleField = dialogDocument.rootVisualElement.Q("Title") as Label;
        Label bodyField = dialogDocument.rootVisualElement.Q("Body") as Label;

        if (titleField != null)
            titleField.text = titleText;
        if (bodyField != null)
            bodyField.text = bodyText;

        // Listen to buttons
        Button dialogCancelButton = dialogDocument.rootVisualElement.Q("Cancel") as Button;
        Button dialogOkButton = dialogDocument.rootVisualElement.Q("OK") as Button;

        if (dialogCancelButton != null)
            dialogCancelButton.clicked += OnDialogCancel;
        if (dialogOkButton != null)
            dialogOkButton.clicked += OnDialogOk;

        return dialogInstance;
    }

    private void CleanUpDialog(GameObject dialogInstance)
    {
        if (dialogInstance == null)
            return;

        UIDocument dialogDocument = dialogInstance.GetComponent<UIDocument>();
        if (dialogDocument != null)
        {
            // Unsubscribe from buttons
            Button dialogCancelButton = dialogDocument.rootVisualElement.Q("Cancel") as Button;
            Button dialogOkButton = dialogDocument.rootVisualElement.Q("OK") as Button;

            if (dialogCancelButton != null)
                dialogCancelButton.clicked -= OnDialogCancel;
            if (dialogOkButton != null)
                dialogOkButton.clicked -= OnDialogOk;
        }

        // BEGONE BACK TO THE DARKNESS
        Destroy(dialogInstance);
    }

    private void OnDialogCancel()
    {
        Debug.Log("User selected dialog option: Cancel");
        _dialogResult = DIALOG_RESULT.Cancel;
    }

    private void OnDialogOk()
    {
        Debug.Log("User selected dialog option: OK");
        _dialogResult = DIALOG_RESULT.OK;
    }
    #endregion
}
