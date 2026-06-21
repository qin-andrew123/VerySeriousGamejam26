using System.Collections.Generic;
using UnityEngine;

public class AIManager : MonoBehaviour
{
    // In order of turn order
    [SerializeField] List<AIEvaluator> m_AIBehaviors;

    // Returns true once a value is obtained
    public ActionType EvaluateAction(TurnOrder turn)
    {
        int properIndex = (int)turn - (int)TurnOrder.TURN_ORDER_NPC1;
        if (m_AIBehaviors[properIndex] == null)
        {
            Debug.LogError("AIEvaluator at index " + properIndex + " is null");
            return ActionType.ACTION_TYPE_INVALID;
        }

        return ActionType.ACTION_TYPE_DEFAULT;
        // return m_AIBehaviors[properIndex].GetAction();
    }

    public void PerformAction(TurnOrder turn, ActionType actionType)
    {
        BoardManager.Instance.PerformBoardMove(MoveType.MOVE_TYPE_PASS);
        //int properIndex = (int)turn - (int)TurnOrder.TURN_ORDER_NPC1;
        //if (m_AIBehaviors[properIndex] == null)
        //{
        //    Debug.LogError("AIEvaluator at index " + properIndex + " is null");
        //    return;
        //}

        //MoveType moveToPerform = m_AIBehaviors[properIndex].GetMoveType(actionType);
        //if (moveToPerform == MoveType.MOVE_TYPE_SKIP)
        //{
        //    TurnManager.Instance.MarkNextTurnAsSkipped(turn);
        //}
        //else if (moveToPerform == MoveType.MOVE_TYPE_PASS)
        //{
        //    return;
        //}
        //else
        //{
        //    BoardManager.Instance.PerformBoardMove(moveToPerform);
        //}
    }

    private void MarkNextActorAsPassed(int currentTurn)
    {

    }
}
