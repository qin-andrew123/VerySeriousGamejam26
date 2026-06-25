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
 * 
 * Want the volume values shown next to slider.
 * 
 * Un/subscription null-checking
 * => Frankly gets repetitive as-is. Not high priority. Just thinking.
 */

public enum DialogResult
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

    private DialogResult _dialogResult = DialogResult.None;

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
    #endregion

    #region Button events
    private void OnClickBack()
    {
        Action backAction = () => {
            Debug.Log("Executing Back callback");

            // Discard any unsaved changes
            RollbackLastSavePoint();

            // Hide settings page
            _uiDocument.rootVisualElement.style.display = DisplayStyle.None;
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
            okCallback: () => {
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
        Debug.Log($"Set MusicVolume to {_musicVolumeSlider.value}%");

        PlayerPrefs.SetInt("SfxVolume", _sfxVolumeSlider.value);
        _lastSavedSettings.SfxVolume = _sfxVolumeSlider.value;
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
        if (_dialogResult != DialogResult.None)
        {
            Debug.Log($"Another waiting dialog is already open!");
            yield return null;
        }

        GameObject dialogInstance = InstantiateDialog(titleText, bodyText);
        if (dialogInstance == null)
        {
            Debug.LogError($"Failed to instantiate dialog!");
            yield return null;
        }

        // Wait for user response
        _dialogResult = DialogResult.Waiting;
        yield return new WaitUntil(() => _dialogResult != DialogResult.Waiting);

        // Execute callback if user elected to continue
        if (_dialogResult == DialogResult.OK)
        {
            okCallback();
        }

        CleanUpDialog(dialogInstance);
        _dialogResult = DialogResult.None;
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
        _dialogResult = DialogResult.Cancel;
    }

    private void OnDialogOk()
    {
        Debug.Log("User selected dialog option: OK");
        _dialogResult = DialogResult.OK;
    }
    #endregion
}
