using System.Collections.Generic;
using UnityEngine;

public class AIManager : MonoBehaviour
{
    // In order of turn order
    [SerializeField] List<AIEvaluator> m_AIBehaviors;

    // Returns true once a value is obtained
    public ActionType EvaluateAction(TurnOrder AIType)
    {
        // TODO AQIN: IMPLEMENT
        int properIndex = (int)AIType - (int)TurnOrder.TURN_ORDER_NPC1;
        if (m_AIBehaviors[properIndex] == null)
        {
            Debug.LogError("AIEvaluator at index " + properIndex + " is null");
            return ActionType.ACTION_TYPE_INVALID;
        }

        return m_AIBehaviors[properIndex].GetAction();
    }
    public void PerformAction(int AIType, ActionType actionType)
    {
        // TODO AQIN: Implement
    }
}
