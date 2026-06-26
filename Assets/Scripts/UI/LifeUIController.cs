using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Image = UnityEngine.UIElements.Image;

public class LifeUIController : MonoBehaviour
{
    // Life VT
    private UIDocument _uiDocument;

    // Images
    private Image _lifeImage1;
    private Image _lifeImage2;
    private Image _lifeImage3;

    // Score text. Did I mention there's score text?
    // Sorry I don't feel like renaming the classes
    private Label _scoreLabel;

    #region Initialization
    private void OnEnable()
    {
        _uiDocument = GetComponent<UIDocument>();
        if (_uiDocument == null)
        {
            Debug.LogError("Failed to initialize life UI: root element is null");
            return;
        }

        // Acquire life icons
        _lifeImage1 = _uiDocument.rootVisualElement.Q("Life1") as Image;
        _lifeImage2 = _uiDocument.rootVisualElement.Q("Life2") as Image;
        _lifeImage3 = _uiDocument.rootVisualElement.Q("Life3") as Image;

        if (_lifeImage1 == null ||
            _lifeImage2 == null ||
            _lifeImage3 == null)
        {
            Debug.LogError("Failed to initialize life UI: at least one Image is null");
        }

        // Acquire score text
        _scoreLabel = _uiDocument.rootVisualElement.Q("Score") as Label;
        if (_scoreLabel == null)
        {
            Debug.LogError("Failed to initialize life UI: score text is null");
        }
        else
        {
            _scoreLabel.text = $"Score: 0";
        }
    }
    #endregion

    // Sets visual icons for lives
    // Actual life count det. by caller
    public void SetLives(int lives)
    {
        if (_lifeImage1 == null ||
            _lifeImage2 == null ||
            _lifeImage3 == null)
        {
            Debug.LogError("Failed to set lives: at least one Image is null");
            return;
        }

        Debug.Log($"Set lives to {lives}");

        _lifeImage1.style.display = (lives > 0) ? DisplayStyle.Flex : DisplayStyle.None;
        _lifeImage2.style.display = (lives > 1) ? DisplayStyle.Flex : DisplayStyle.None;
        _lifeImage3.style.display = (lives > 2) ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public void SetScore(int score)
    {
        if (_scoreLabel == null)
        {
            Debug.LogError("Failed to set score: Text element is null");
            return;
        }

        _scoreLabel.text = $"Score: {score}";
    }
}
