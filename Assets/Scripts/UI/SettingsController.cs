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
        if (HasUnsavedChanges())
        {
            Debug.Log("Yoink! Say bye-bye to your changes i guess");
        }

        // Discard any unsaved changes
        RollbackLastSavePoint();

        // Hide settings page
        _uiDocument.rootVisualElement.style.display = DisplayStyle.None;
    }

    private void OnClickResetToDefaults()
    {
        if (_musicVolumeSlider != null)
            _musicVolumeSlider.SetValueWithoutNotify(DefaultMusicVolume);

        if (_sfxVolumeSlider != null)
            _sfxVolumeSlider.SetValueWithoutNotify(DefaultSfxVolume);

        SetSavePoint(raiseSaveFlag: false);
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
}
