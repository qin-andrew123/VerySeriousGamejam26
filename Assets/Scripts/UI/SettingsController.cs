using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public struct Settings
{
    public int MusicVolume { get; set; }
    public int SfxVolume { get; set; }
}

public class SettingsController : MonoBehaviour
{
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

    #region Initialization
    private void OnEnable()
    {
        _uiDocument = GetComponent<UIDocument>();

        if (_uiDocument is null ||
            _uiDocument.rootVisualElement is null)
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
        _musicVolumeSlider?.SetValueWithoutNotify(PlayerPrefs.GetInt("MusicVolume", 100));
        _sfxVolumeSlider?.SetValueWithoutNotify(PlayerPrefs.GetInt("SfxVolume", 100));
        _lastSavedSettings = new Settings()
        {
            MusicVolume = _musicVolumeSlider.value,
            SfxVolume = _sfxVolumeSlider.value
        };
    }

    private void OnDisable()
    {
        UnsubscribeButtons();
    }

    private void SubscribeButtons()
    {
        if (_backButton is not null)
            _backButton.clicked += OnClickBack;
        if (_resetToDefaultsButton is not null)
            _resetToDefaultsButton.clicked += OnClickResetToDefaults;
        if (_saveButton is not null)
            _saveButton.clicked += OnClickSave;
    }

    private void UnsubscribeButtons()
    {
        if (_backButton is not null)
            _backButton.clicked -= OnClickBack;
        if (_resetToDefaultsButton is not null)
            _resetToDefaultsButton.clicked -= OnClickResetToDefaults;
        if (_saveButton is not null)
            _saveButton.clicked -= OnClickSave;
    }
    #endregion

    #region Button events
    private void OnClickBack()
    {
        if (HasUnsavedChanges())
        {
            Debug.Log("Yoink! Say bye-bye to your changes i guess");
            // TODO BRIEZ: raise conf dialog => Cancel returns
        }

        // Discard any unsaved changes
        RollbackLastSavePoint();

        // Hide settings page
        _uiDocument.rootVisualElement.style.display = DisplayStyle.None;
    }

    private void OnClickResetToDefaults()
    {
        // TODO BRIEZ: raise conf dialog
        /*
         * Would ideally want to reuse some visual for "Back" confirmation dialog
         * though w/ minor tweaks to the message
         * i.e. "I see you have unsaved changes that you will lose" vs.
         * "I haven't bothered to check, but everything will be nuked"
         */

        _sfxVolumeSlider?.SetValueWithoutNotify(DefaultSfxVolume);
        _musicVolumeSlider?.SetValueWithoutNotify(DefaultMusicVolume);

        SetSavePoint(raiseSaveFlag: false);
    }

    private void OnClickSave() => SetSavePoint();

    // TODO BRIEZ: Saving should have some visual indicator
    // Unsaved changes diff colour in UI? Graying out when no changes? Perchance.
    private void SetSavePoint(bool raiseSaveFlag = true)
    {
        // Save all settings at current value
        // TODO/NOTE BRIEZ: should these pref names be consts? aaaughhhahahaaha maybe

        PlayerPrefs.SetInt("MusicVolume", _musicVolumeSlider.value);
        _lastSavedSettings.MusicVolume = _musicVolumeSlider.value;
        Debug.Log($"Set MusicVolume to {_musicVolumeSlider.value}%");

        PlayerPrefs.SetInt("SfxVolume", _sfxVolumeSlider.value);
        _lastSavedSettings.SfxVolume = _sfxVolumeSlider.value;
        Debug.Log($"Set SfxVolume to {_sfxVolumeSlider.value}%");
    }

    private void RollbackLastSavePoint()
    {
        _musicVolumeSlider?.SetValueWithoutNotify(_lastSavedSettings.MusicVolume);
        _sfxVolumeSlider?.SetValueWithoutNotify(_lastSavedSettings.SfxVolume);
    }

    private bool HasUnsavedChanges() =>
        _lastSavedSettings.MusicVolume != _musicVolumeSlider.value ||
        _lastSavedSettings.SfxVolume != _sfxVolumeSlider.value;
    #endregion
}
