using UnityEngine;

public enum ActionType
{
    ACTION_TYPE_INVALID = -1,
    ACTION_TYPE_DEFAULT = 0,
    ACTION_TYPE_FALLBACK = 1,
    ACTION_TYPE_PASS
}
public class AIEvaluator : MonoBehaviour
{
    [SerializeField] private TurnOrder m_AIType;
    public ActionType GetAction()
    {
        switch (m_AIType)
        {
            default:
                Debug.LogError("AIEvalualtor.GetAction(): Not a value AI Type");
                return ActionType.ACTION_TYPE_INVALID;
        }
    }
}
