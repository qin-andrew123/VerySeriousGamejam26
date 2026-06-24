using UnityEngine;
using System.Collections.Generic;
using NUnit.Framework;

public class GoalItem : MonoBehaviour
{
    public HashSet<TurnOrder> SatisfyingActors => _satisfyingActors;
    public bool HoldsPlayerConstraint { get; private set; } = false;
    public BoardNode ParentNode { get; private set; }

    [SerializeField] private string _name;
    private HashSet<TurnOrder> _satisfyingActors = new HashSet<TurnOrder>();
    private BoardNode _parentNode = null;

    public void SetParentNode(BoardNode parentNode)
    {
        Assert.IsNotNull(parentNode);
        _parentNode = parentNode;
    }

    public void ToDebugString(int itemIndex)
    {
        string constrainingItems = " ";
        foreach (TurnOrder turn in SatisfyingActors)
        {
            constrainingItems += " " + turn.ToString();
        }

        Debug.Log($"Item {itemIndex}: {_name} spawned at location {transform.position}. Constrainted by{constrainingItems}");
    }

    public void AddActorAsSatisfying(TurnOrder actor)
    {
        if (!_satisfyingActors.Add(actor))
        {
            Debug.LogError($"Tried to add {actor} to {_name} but it already was part of set");
            return;
        }

        HoldsPlayerConstraint = actor == TurnOrder.TURN_ORDER_PLAYER;
        Debug.Log($"{_name} added {actor} as satisfying");
    }

    public bool DoesActorSatisfy(TurnOrder actor)
    {
        return _satisfyingActors.Contains(actor);
    }
}
