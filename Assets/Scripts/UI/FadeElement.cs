using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class FadeElement
{
    private VisualElement m_roundRoot;
    private Coroutine m_activeCoroutine;
    private readonly MonoBehaviour m_runner;
    private float m_visibleDuration = 0.0f;

    public FadeElement(MonoBehaviour runner, VisualElement root, float duration)
    {
        m_runner = runner;
        m_roundRoot = root;
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
        m_roundRoot.AddToClassList("notify--visible");
        yield return new WaitForSecondsRealtime(m_visibleDuration);
        m_roundRoot.RemoveFromClassList("notify--visible");
        m_activeCoroutine = null;
    }
}
