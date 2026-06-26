using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/*
 * TODO BRIEZ:
 * 
 * Confirmation dialog for "Back" and "Reset to default"
 * => Shown if unsaved changes for former; always for latter. Cancel returns.
 * => Should have some var text setting to tweak message to context
 * 
 * Visual indicator for unsaved changes
 * => Grayed-out "Save" button? Maybe
 * => Highlight/colour altered settings? Perchance.
 */

public enum DIALOG_RESULT
{
    None = -1,
    Waiting,
    OK,
    Cancel
}

public struct Settings
{
    public int MusicVolume { get; set; }
    public int SfxVolume { get; set; }
}

public class SettingsController : MonoBehaviour
{
    [SerializeField] private GameObject DialogPrefab;
    [SerializeField] private GameObject OverlayPrefab;

    // Settings UI document
    private UIDocument _uiDocument;

    // Buttons
    private Button _backButton;
    private Button _resetToDefaultsButton;
    private Button _saveButton;

    // Volume sliders
    private SliderInt _musicVolumeSlider;
    private SliderInt _sfxVolumeSlider;

    // Default settings
    [SerializeField] private int DefaultMusicVolume = 100;
    [SerializeField] private int DefaultSfxVolume = 100;

    // Last saved values — used to check for unsaved changes
    private Settings _lastSavedSettings;

    private DIALOG_RESULT _dialogResult = DIALOG_RESULT.None;

    private GameObject _overlayInstance;

    #region Initialization
    private void OnEnable()
    {
        _uiDocument = GetComponent<UIDocument>();

        if (_uiDocument == null ||
            _uiDocument.rootVisualElement == null)
        {
            Debug.LogError("Unable to find Settings UIDocument or its rot visual element!");
            return;
        }

        // Hide Settings page by default
        _uiDocument.rootVisualElement.style.display = DisplayStyle.None;

        // Get visual elements
        _backButton = _uiDocument.rootVisualElement.Q("Back") as Button;
        _resetToDefaultsButton = _uiDocument.rootVisualElement.Q("ResetToDefaults") as Button;
        _saveButton = _uiDocument.rootVisualElement.Q("Save") as Button;
        _musicVolumeSlider = _uiDocument.rootVisualElement.Q("MusicVolume") as SliderInt;
        _sfxVolumeSlider = _uiDocument.rootVisualElement.Q("SfxVolume") as SliderInt;

        SubscribeButtons();

        // Restore settings from saved PlayerPrefs
        _lastSavedSettings = new Settings()
        {
            MusicVolume = PlayerPrefs.GetInt("MusicVolume", 100),
            SfxVolume = PlayerPrefs.GetInt("SfxVolume", 100)
        };

        if (_musicVolumeSlider != null)
            _musicVolumeSlider.SetValueWithoutNotify(_lastSavedSettings.MusicVolume);

        if (_sfxVolumeSlider != null)
            _sfxVolumeSlider.SetValueWithoutNotify(_lastSavedSettings.SfxVolume);

        // Create and hide reusable overlay prefab
        if (OverlayPrefab == null)
        {
            Debug.LogError("Unable to create overlay prefab for Settings");
        }
        else
        {
            _overlayInstance = Instantiate(OverlayPrefab, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
            if (_overlayInstance == null)
                Debug.LogError("Overlay prefab for Settings is null");
            else
                ToggleOverlay(show: false);
        }
    }

    private void OnDisable()
    {
        UnsubscribeButtons();
    }

    private void SubscribeButtons()
    {
        if (_backButton != null)
            _backButton.clicked += OnClickBack;
        if (_resetToDefaultsButton != null)
            _resetToDefaultsButton.clicked += OnClickResetToDefaults;
        if (_saveButton != null)
            _saveButton.clicked += OnClickSave;
    }

    private void UnsubscribeButtons()
    {
        if (_backButton != null)
            _backButton.clicked -= OnClickBack;
        if (_resetToDefaultsButton != null)
            _resetToDefaultsButton.clicked -= OnClickResetToDefaults;
        if (_saveButton != null)
            _saveButton.clicked -= OnClickSave;
    }

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
    #endregion

    public void DisplaySettings(bool showSettingsOverlay = false)
    {
        Debug.Log($"Requested to show Settings with overlay display: {showSettingsOverlay}");

        // Display settings UI
        _uiDocument.rootVisualElement.style.display = DisplayStyle.Flex;

        // Display overlay if requested
        if (showSettingsOverlay)
            ToggleOverlay(showSettingsOverlay);
    }

    #region Button events
    private void OnClickBack()
    {
        Action backAction = () =>
        {
            Debug.Log("Executing Back callback");

            // Discard any unsaved changes
            RollbackLastSavePoint();

            // Hide settings page
            _uiDocument.rootVisualElement.style.display = DisplayStyle.None;
            ToggleOverlay(show: false);
        };

        if (HasUnsavedChanges())
        {
            StartCoroutine(RaiseDialogAndWait(
                titleText: "Exit without saving?",
                bodyText: "You have unsaved changes to Settings that will be lost!",
                okCallback: backAction));
        }
        else
        {
            backAction();            
        }
    }

    private void OnClickResetToDefaults()
    {
        StartCoroutine(RaiseDialogAndWait(
            titleText: "Reset settings?",
            bodyText: "All customised settings will be lost!",
            okCallback: () =>
            {
                Debug.Log("Executing ResetToDefaults callback");

                // Reset volume sliders
                if (_musicVolumeSlider != null)
                    _musicVolumeSlider.SetValueWithoutNotify(DefaultMusicVolume);

                if (_sfxVolumeSlider != null)
                    _sfxVolumeSlider.SetValueWithoutNotify(DefaultSfxVolume);

                // Save as defaults
                SetSavePoint(raiseSaveFlag: false);
            }));
    }

    private void OnClickSave() => SetSavePoint();

    private void SetSavePoint(bool raiseSaveFlag = true)
    {
        // Save all settings at current value

        PlayerPrefs.SetInt("MusicVolume", _musicVolumeSlider.value);
        _lastSavedSettings.MusicVolume = _musicVolumeSlider.value;
        float clampedMusicPrefsValue = Mathf.Clamp01((float)PlayerPrefs.GetInt("MusicVolume") / _musicVolumeSlider.highValue);
        AudioManager.Instance.SetAudioSourceVolume(AudioSourceType.AST_MAIN, clampedMusicPrefsValue);
        Debug.Log($"Set MusicVolume to {_musicVolumeSlider.value}%");

        PlayerPrefs.SetInt("SfxVolume", _sfxVolumeSlider.value);
        _lastSavedSettings.SfxVolume = _sfxVolumeSlider.value;
        float clampedSFXPrefsValue = Mathf.Clamp01((float)PlayerPrefs.GetInt("SfxVolume") / _sfxVolumeSlider.highValue);
        AudioManager.Instance.SetAudioSourceVolume(AudioSourceType.AST_ONESHOT, clampedSFXPrefsValue);
        Debug.Log($"Set SfxVolume to {_sfxVolumeSlider.value}%");

        PlayerPrefs.Save();
    }

    private void RollbackLastSavePoint()
    {
        if (_musicVolumeSlider != null)
            _musicVolumeSlider.SetValueWithoutNotify(_lastSavedSettings.MusicVolume);

        if (_sfxVolumeSlider != null)
            _sfxVolumeSlider.SetValueWithoutNotify(_lastSavedSettings.SfxVolume);
    }

    private bool HasUnsavedChanges() =>
        _lastSavedSettings.MusicVolume != _musicVolumeSlider.value ||
        _lastSavedSettings.SfxVolume != _sfxVolumeSlider.value;
    #endregion

    #region Warning Dialog
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
