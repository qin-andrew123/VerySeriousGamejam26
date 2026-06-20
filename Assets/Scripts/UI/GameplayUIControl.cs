using UnityEngine;
using UnityEngine.UIElements;

public class GameplayUIControl : MonoBehaviour
{
    private VisualElement m_rootUI;

    // Round Layout
    private VisualElement m_roundRoot;
    private TextElement m_roundInfo;
    private TextElement m_turnInfo;
    private FadeElement m_roundFade;
    private FadeElement m_turnFade;
    [SerializeField] private float m_roundFadeTime = 1.0f;
    [SerializeField] private float m_turnFadeTime = 1.0f;

    private void InitializeRoundInfo()
    {
        m_roundRoot = m_rootUI.Q<VisualElement>("TurnRoot");
        m_roundInfo = m_rootUI.Q<TextElement>("RoundNum");
        m_turnInfo = m_rootUI.Q<TextElement>("CurrentTurn");
        m_roundFade = new FadeElement(this, m_roundInfo, m_roundFadeTime);
        m_turnFade = new FadeElement(this, m_turnInfo, m_turnFadeTime);
    }

    private void UpdateRoundInformation(int roundNumber)
    {
        m_roundInfo.text = $"Round {roundNumber}";
        m_roundFade.Show();
    }

    private void UpdateTurnInformation(TurnOrder turn)
    {
        switch(turn)
        {
            case TurnOrder.TURN_ORDER_PLAYER:
                m_turnInfo.text = "Player's Turn";
                break;
            case TurnOrder.TURN_ORDER_NPC1:
                m_turnInfo.text = "NPC1's Turn";
                break;
            case TurnOrder.TURN_ORDER_NPC2:
                m_turnInfo.text = "NPC2's Turn";
                break;
            case TurnOrder.TURN_ORDER_NPC3:
                m_turnInfo.text = "NPC3's Turn";
                break;
            case TurnOrder.TURN_ORDER_NPC4:
                m_turnInfo.text = "NPC4's Turn";
                break;
            case TurnOrder.TURN_ORDER_NPC5:
                m_turnInfo.text = "NPC5's Turn";
                break;
        }

        m_turnFade.Show();
    }

    private void OnEnable()
    {
        m_rootUI = GetComponent<UIDocument>().rootVisualElement;

        InitializeRoundInfo();
        TurnManager.OnRoundStartNotify += UpdateRoundInformation;
        TurnManager.OnTurnStartNotify += UpdateTurnInformation;
    }

    private void OnDisable()
    {
        TurnManager.OnRoundStartNotify -= UpdateRoundInformation;
        TurnManager.OnTurnStartNotify -= UpdateTurnInformation;
    }
}
