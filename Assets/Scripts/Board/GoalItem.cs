using UnityEngine;
using System.Collections.Generic;
using NUnit.Framework;
using System.Collections;

public class GoalItem : MonoBehaviour
{
    public HashSet<TurnOrder> SatisfyingActors => _satisfyingActors;
    public bool HoldsPlayerConstraint { get; private set; } = false;
    public GameObject PrefabID { get; set; } = null;
    public BoardNode ParentNode => _parentNode;

    [SerializeField] private string _name;
    private HashSet<TurnOrder> _satisfyingActors = new HashSet<TurnOrder>();
    private BoardNode _parentNode = null;
    private bool _canDestroy = false;
    public void SetParentNode(BoardNode parentNode)
    {
#if UNITY_EDITOR
        Assert.IsNotNull(parentNode);
#endif
        _parentNode = parentNode;
    }

    public void MoveAndShrinkTowardsTargetLocation(Vector3 targetLocation)
    {
        StartCoroutine(LerpFunction(targetLocation));
    }

    public void TryDestroy()
    {
        StartCoroutine(TryDestroyCoroutine());
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

    private IEnumerator TryDestroyCoroutine()
    {
        yield return new WaitUntil(() => _canDestroy);

        Destroy(gameObject);
    }

    private void Update()
    {
        transform.rotation = Quaternion.identity;
    }

    private IEnumerator LerpFunction(Vector3 targetLocation)
    {
        Vector3 startPosition = transform.position;
        Vector3 startScale = transform.localScale;
        Vector3 endScale = new Vector3(0.01f, 0.01f, 0.01f);

        float elapsed = 0f;
        while (elapsed < 0.1f)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / 0.1f);
            transform.position = Vector3.Lerp(startPosition, targetLocation, t);
            transform.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }

        AudioManager.Instance.StopPlayingRepeatingOneShot();
        transform.position = targetLocation;
        transform.localScale = endScale;

        _canDestroy = true;
    }
}
