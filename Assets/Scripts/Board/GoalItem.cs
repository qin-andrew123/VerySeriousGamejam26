using UnityEngine;
using System.Collections.Generic;

public class GoalItem : MonoBehaviour
{
    public HashSet<TurnOrder> SatisfyingActors => _satisfyingActors;

    [SerializeField] private string _name;
    HashSet<TurnOrder> _satisfyingActors = new HashSet<TurnOrder>();

    public GoalItem(HashSet<TurnOrder> satisfyingActors)
    {
        _satisfyingActors = satisfyingActors;
    }
    public GoalItem()
    {
        _satisfyingActors = new HashSet<TurnOrder>();
    }
    public void AddActorAsSatisfying(TurnOrder actor)
    {
        if (!_satisfyingActors.Add(actor))
        {
            Debug.LogError($"Tried to add {actor} to {_name} but it already was part of set");
        }
        
        Debug.Log($"{_name} added {actor} as satisfying");
    }

    public bool DoesActorSatisfy(TurnOrder actor)
    {
        return _satisfyingActors.Contains(actor);
    }
}
