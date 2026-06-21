using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using System;

public class FadeElement
{
    public event Action OnBannerComplete;
    private VisualElement _root;
    private Coroutine m_activeCoroutine;
    private readonly MonoBehaviour m_runner;
    private float m_visibleDuration = 0.0f;

    public FadeElement(MonoBehaviour runner, VisualElement root, float duration)
    {
        m_runner = runner;
        _root = root;
        m_visibleDuration = duration;
    }

    public void Show()
    {
        if (m_activeCoroutine != null)
        {
            m_runner.StopCoroutine(m_activeCoroutine);
        }

        m_activeCoroutine = m_runner.StartCoroutine(FadeInOut());
    }

    private IEnumerator FadeInOut()
    {
        _root.AddToClassList("notify--visible");
        yield return new WaitForSecondsRealtime(m_visibleDuration);
        _root.RemoveFromClassList("notify--visible");
        m_activeCoroutine = null;
        OnBannerComplete?.Invoke();
    }
}
