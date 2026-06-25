using System.Collections.Generic;
using UnityEngine;

public class AIManager : MonoBehaviour
{
    // In order of turn order
    [SerializeField] List<AIEvaluator> m_AIBehaviors;

    public ActionType EvaluateAction(TurnOrder turn)
    {
        int properIndex = (int)turn - (int)TurnOrder.TURN_ORDER_NPC1;
        if (m_AIBehaviors[properIndex] == null)
        {
            Debug.LogError("AIEvaluator at index " + properIndex + " is null");
            return ActionType.ACTION_TYPE_INVALID;
        }

        return m_AIBehaviors[properIndex].GetAction();
    }

    public void PerformAction(TurnOrder turn, ActionType actionType)
    {
        int properIndex = (int)turn - (int)TurnOrder.TURN_ORDER_NPC1;
        if (m_AIBehaviors[properIndex] == null)
        {
            Debug.LogError("AIEvaluator at index " + properIndex + " is null");
            return;
        }

        MoveType moveToPerform = m_AIBehaviors[properIndex].GetMoveType(actionType);
        if (moveToPerform == MoveType.MOVE_TYPE_SKIP)
        {
            TurnManager.Instance.MarkNextTurnAsSkipped(turn);
        }
        BoardManager.Instance.PerformBoardMove(moveToPerform);
    }
}
