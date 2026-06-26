using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

public class WantsUIController : MonoBehaviour
{
    private UIDocument _uiDocument;
    private VisualElement _wantsRoot;
    private Image _wantsImage;

    public void SetWant(Texture2D texture)
    {
        _wantsImage.image = texture;
    }

    private void OnEnable()
    {
        _uiDocument = GetComponent<UIDocument>();
#if UNITY_EDITOR
        Assert.IsNotNull(_uiDocument);
#endif
        _wantsRoot = _uiDocument.rootVisualElement;
        _wantsImage = _wantsRoot.Q<Image>("WantsImage");
    }
}
