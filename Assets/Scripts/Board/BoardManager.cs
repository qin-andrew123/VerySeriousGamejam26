using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering.Universal;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance { get; private set; }
    public bool TurningClockwise { get; private set; } = true;
    public bool IsBoardRotating => m_isRotating;
    // UI Information
    [SerializeField] private GameplayUIControl _gameplayControl;

    // Player Fields. Gameplay Information
    [SerializeField] private int _startingLives = 3;
    private int _currentLives = 0;
    private List<int> _actorScores = new List<int>();

    // Board Fields. Turning Fields and duration
    [SerializeField] private float _angleOfRotation = 60.0f;
    [SerializeField] private float _rotationDuration = 1.0f;
    [SerializeField] private float _delayTime = 0.5f;
    private int m_numRotations = 1;
    private bool m_isRotating = false;

    // Item Fields. Spawning and logicstics
    [SerializeField] private int _numMaxActiveItems = 3;
    private int _numActiveItems = 0;
    [SerializeField] private List<GameObject> _itemSpawnPrefabs;
    [SerializeField]
    [Tooltip("Where the actor is positioned. Requred for determining if the actor can take item")]
    private List<Transform> _actorPositions;
    [SerializeField] private List<Transform> _foodSpawnPoint;
    [SerializeField]
    [Tooltip("How close the item must be to goal to be considered as obtained")]
    private float _obtainDistance = 1.5f;
    private List<GoalItem> _currentlyAvailableItems = new List<GoalItem>();
    private HashSet<GameObject> _setCurrentlyActivePrefabs = new HashSet<GameObject>();
    private List<BoardNode> _boardPoints = new List<BoardNode>();
    private bool _isPlayerConstraintSatisfied = false;

    public List<Vector3> GetItemCurrentPositions()
    {
        List<Vector3> result = new List<Vector3>();
        for (int i = 0; i < _currentlyAvailableItems.Count; ++i)
        {
            result.Add(_currentlyAvailableItems[i].gameObject.transform.position);
        }

        return result;
    }

    public List<Vector3> SimulateRotateBoard(int numRotations, bool isClockwise)
    {
        List<Vector3> result = new List<Vector3>();
        float totalAngle = _angleOfRotation * numRotations * (isClockwise ? -1f : 1f);

        Quaternion startRot = transform.rotation;
        Quaternion endRot = startRot * Quaternion.Euler(0f, 0f, totalAngle);
        for (int i = 0; i < _currentlyAvailableItems.Count; ++i)
        {
            Vector3 predicted = CalculateItemLocation(gameObject.transform, _currentlyAvailableItems[i].transform, endRot); ;
            result.Add(predicted);
        }

        return result;
    }

    
    public void InitializeBoard()
    {
        while (_numActiveItems < _numMaxActiveItems)
        {
            int randomPrefabIndex = Random.Range(0, _itemSpawnPrefabs.Count);
            List<GameObject> possibleItems = new List<GameObject>();
            for (int i = 0; i < _itemSpawnPrefabs.Count; ++i)
            {
                possibleItems.Add(_itemSpawnPrefabs[i]);
            }

            foreach(GameObject iter in _setCurrentlyActivePrefabs)
            {
                possibleItems.Remove(iter);
            }

            int randomItemIndex = Random.Range(0, possibleItems.Count);

            GameObject go = Instantiate(possibleItems[randomItemIndex]);
            GoalItem prefabGoalItem = go.GetComponent<GoalItem>();
            prefabGoalItem.PrefabID = possibleItems[randomItemIndex];
            Assert.IsNotNull(prefabGoalItem);
            _currentlyAvailableItems.Add(prefabGoalItem);
            _setCurrentlyActivePrefabs.Add(possibleItems[randomItemIndex]);

            GenerateConstraintsForItem(prefabGoalItem);
            DetermineSpawnPointForItem(prefabGoalItem);
            prefabGoalItem.ToDebugString(_numActiveItems);
            ++_numActiveItems;
        }
    }

    public void RotateBoardByAmount(int numTurns)
    {
        m_numRotations += numTurns;
        RotateBoard();
    }

    public void RotateBoard()
    {
        if (m_isRotating)
        {
            return;
        }
        StartCoroutine(RotateBoardCoroutine());
    }

    public void RandomizeRotation()
    {
        int numRotations = Random.Range(0, 100);
        Debug.Log($"Rotating {numRotations} times");
        RotateBoardByAmount(numRotations);
    }

    public void PerformBoardMove(MoveType moveType)
    {
        if (moveType == MoveType.MOVE_TYPE_PLUS_ONE)
        {
            RotateBoardByAmount(1);
        }
        else if (moveType == MoveType.MOVE_TYPE_REVERSE)
        {
            TurningClockwise = !TurningClockwise;
        }
        else if (moveType == MoveType.MOVE_TYPE_CHAOS)
        {
            RandomizeRotation();
        }
        StartCoroutine(ActionCompleteDelay());
    }
    
    public void CheckItemsForObtain()
    {
        List<GoalItem> elementsToRemove = new List<GoalItem>();
        for (int i = 0; i < _currentlyAvailableItems.Count; ++i)
        {
            GoalItem item = _currentlyAvailableItems[i];

            float bestDistanceSqr = float.MaxValue;
            int closestIndex = 0;

            bool containsPlayerConstraint = false;
            foreach(TurnOrder turn in item.SatisfyingActors)
            {
                if (turn == TurnOrder.TURN_ORDER_PLAYER)
                {
                    containsPlayerConstraint = true;
                }

                int turnToIndex = (int)turn;
                float distSqr = Vector3.SqrMagnitude(_actorPositions[turnToIndex].position - item.gameObject.transform.position);

                if (distSqr < bestDistanceSqr)
                {
                    bestDistanceSqr = distSqr;
                    closestIndex = turnToIndex;
                }
            }

            if (bestDistanceSqr < (_obtainDistance * _obtainDistance))
            {
                TurnOrder bestAsTurn = (TurnOrder)closestIndex;
                Debug.Log($"Item was obtained by {bestAsTurn}");

                // TODO AQIN: UI Here to describe success or loss
                ++_actorScores[closestIndex];
                _gameplayControl.UpdateScoreForActor(closestIndex, _actorScores[closestIndex]);
                
                elementsToRemove.Add(item);
                Destroy(item.gameObject);
                if (containsPlayerConstraint)
                {
                    _isPlayerConstraintSatisfied = false;
                    if (bestAsTurn != TurnOrder.TURN_ORDER_PLAYER)
                    {
                        --_currentLives;

                        if (_currentLives <= 0)
                        {
                            Debug.Log("Game has ended!");
                            TurnManager.Instance.IsGameOver = true;
                        }
                    }
                }
                --_numActiveItems;
            }
        }

        foreach (GoalItem i in elementsToRemove)
        {
            _currentlyAvailableItems.Remove(i);
            _setCurrentlyActivePrefabs.Remove(i.PrefabID);
        }
    }

    private void DetermineSpawnPointForItem(GoalItem item)
    {
        Dictionary<TurnOrder, Vector3> satisfyingPositions = new Dictionary<TurnOrder, Vector3>();
        List<BoardNode> possibleIndicies = new List<BoardNode>();
        foreach(BoardNode node in _boardPoints)
        {
            if (item.SatisfyingActors.Contains(node.AssociatedActor))
            {
                satisfyingPositions.Add(node.AssociatedActor, node.SpawnPoint.position);
            }
            possibleIndicies.Add(node);
        }
        
        // Cannot spawn in positions that have something there already
        for (int i = 0; i < _currentlyAvailableItems.Count; ++i)
        {
            // Disregard if item is self
            if (_currentlyAvailableItems[i] == item)
            {
                continue;
            }

            for (int j = 0; j < possibleIndicies.Count; ++j)
            {
                if (_currentlyAvailableItems[i].ParentNode == possibleIndicies[j])
                {
                    possibleIndicies[j] = null;
                }
            }
        }

        // Cannot spawn on locations that match our constraints
        foreach (var turn in item.SatisfyingActors)
        {
            for (int i = 0; i < possibleIndicies.Count; ++i)
            {
                if (possibleIndicies[i] != null)
                {
                    if (possibleIndicies[i].AssociatedActor == turn)
                    {
                        possibleIndicies[i] = null;
                    }
                }
            }
        }

        List<BoardNode> validStartingLocations = new List<BoardNode>();
        for (int i = 0; i < possibleIndicies.Count; ++i)
        {
            if (possibleIndicies[i] != null)
            {
                validStartingLocations.Add(possibleIndicies[i]);
            }
        }

        // Same size as validStartingLocations
        List<float> weights = new List<float>();
        float totalWeight = 0.0f;

        // Calculate distance from a valid starting loc to each spawn point (summed)
        for (int i = 0; i < validStartingLocations.Count; ++i)
        {
            float calculatedWeight = 0.0f;
            Vector3 spawnPointPosition = validStartingLocations[i].SpawnPoint.position;

            foreach (TurnOrder turn in item.SatisfyingActors)
            {
                Assert.IsTrue(validStartingLocations[i].AssociatedActor != turn, "There is a valid location that is equal to a constraint. this shouldn't be possible");

                Vector3 satisfyingSpawnPointPosition;
                if (!satisfyingPositions.TryGetValue(turn, out satisfyingSpawnPointPosition))
                {
                    Assert.IsTrue(false, $"Failed to lookup satisfying constraint {turn}");
                }
                calculatedWeight += Vector3.SqrMagnitude(satisfyingSpawnPointPosition - spawnPointPosition);
            }

            weights.Add(calculatedWeight);
            totalWeight += calculatedWeight;
        }

        float randValue = Random.Range(0, totalWeight);
        int randIndex = 0;
        for (int i = 0; i < validStartingLocations.Count; ++i)
        {
            if (weights[i] > randValue)
            {
                randIndex = i;
                break;
            }
            randValue -= weights[i];
        }

        item.gameObject.transform.parent = validStartingLocations[randIndex].SpawnPoint;
        item.gameObject.transform.position = validStartingLocations[randIndex].SpawnPoint.position;
        item.SetParentNode(validStartingLocations[randIndex]);
    }

    private void PickActorConstraint(GoalItem item, int numActorsToPick)
    {
        List<BoardNode> possibleIndicies = new List<BoardNode>();
        foreach (BoardNode node in _boardPoints)
        {
            possibleIndicies.Add(node);
        }

        for (int i = 0; i < _currentlyAvailableItems.Count; ++i)
        {
            // Disregard self
            if (_currentlyAvailableItems[i] == item)
            {
                continue;
            }

            for (int j = 0; j < possibleIndicies.Count; ++j)
            {
                if (possibleIndicies[j] != null)
                {
                    if (_currentlyAvailableItems[i].SatisfyingActors.Contains(possibleIndicies[j].AssociatedActor))
                    {
                        possibleIndicies[j] = null;
                    }
                }
            }
        }

        List<BoardNode> validIndicies = new List<BoardNode>();
        for (int i = 0; i < possibleIndicies.Count; ++i)
        {
            if (possibleIndicies[i] != null)
            {
                validIndicies.Add(possibleIndicies[i]);
            }
        }

        if (!_isPlayerConstraintSatisfied)
        {
            _isPlayerConstraintSatisfied = true;
            item.AddActorAsSatisfying(TurnOrder.TURN_ORDER_PLAYER);
            --numActorsToPick;
        }

        // Must remove the player from valid indicies to prevent actor constraint overload
        foreach (BoardNode node in validIndicies)
        {
            if (node.AssociatedActor == TurnOrder.TURN_ORDER_PLAYER)
            {
                validIndicies.Remove(node);
                break;
            }
        }

        while (numActorsToPick > 0)
        {
            int randIndex = Random.Range(0, validIndicies.Count);
            if (randIndex >= validIndicies.Count)
            {
                return;
            }
            TurnOrder randomActor = validIndicies[randIndex].AssociatedActor;
            item.AddActorAsSatisfying(randomActor);
            foreach (BoardNode node in validIndicies)
            {
                if (node.AssociatedActor == randomActor)
                {
                    validIndicies.Remove(node);
                    break;
                }
            }
            --numActorsToPick;
        }
    }

    private void GenerateConstraintsForItem(GoalItem item)
    {
        PickActorConstraint(item, _isPlayerConstraintSatisfied ? 1 : 2);
        Assert.IsTrue(_isPlayerConstraintSatisfied, "Validating Items but Player Constraint isn't satisfied yet");
    }

    private Vector3 CalculateItemLocation(Transform parent, Transform obj, Quaternion simulatedParentRotation)
    {
        Vector3 localOffset = Vector3.zero;
        Quaternion localRot = Quaternion.identity;

        var chain = new List<Transform>();
        Transform t = obj;
        while (t != parent)
        {
            chain.Add(t);
            t = t.parent;

            if (chain.Count > 3)
            {
                Debug.LogError("Item could not be calculated because it is not a parent of the child");
                return Vector3.zero;
            }
        }

        chain.Reverse();

        Vector3 pos = Vector3.zero;
        Quaternion rot = Quaternion.identity;
        foreach (var node in chain)
        {
            pos = rot * node.localPosition + pos;
            rot = rot * node.localRotation;
        }

        return parent.position + simulatedParentRotation * pos;
    }

    private IEnumerator RotateBoardCoroutine()
    {
        m_isRotating = true;

        float totalAngle = _angleOfRotation * m_numRotations * (TurningClockwise ? -1f : 1f);
        
        Quaternion startRot = transform.rotation;
        Quaternion endRot = startRot * Quaternion.Euler(0f, 0f, totalAngle);
        float elapsed = 0f;
        while (elapsed < _rotationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _rotationDuration);
            float currentAngle = Mathf.Lerp(0f, totalAngle, t);
            transform.rotation = startRot * Quaternion.Euler(0f, 0f, currentAngle);
            yield return null;
        }

        transform.rotation = endRot;
        yield return new WaitForSeconds(0.3f);
        m_isRotating = false;
        m_numRotations = 1;

        CheckItemsForObtain();
    }

    private IEnumerator ActionCompleteDelay()
    {
        yield return new WaitForSeconds(_delayTime);
        TurnManager.Instance.MarkActionComplete();
    }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Initialize Lives and score
        _actorScores = Enumerable.Repeat(0, (int)TurnOrder.TURN_ORDER_SIZE).ToList();
        _currentLives = _startingLives;

        // Initialize Board information
        Assert.IsTrue(_foodSpawnPoint.Count == (int)TurnOrder.TURN_ORDER_SIZE);
        Assert.IsTrue(_actorPositions.Count == (int)TurnOrder.TURN_ORDER_SIZE);

        for (int i = 0; i <  _actorPositions.Count; ++i)
        {
            BoardNode node = new BoardNode(_foodSpawnPoint[i], _actorPositions[i], (TurnOrder)i);
            _boardPoints.Add(node);
        }
    }
}
