using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance { get; private set; }
    public bool TurningClockwise { get; private set; } = true;
    public bool IsBoardRotating => m_isRotating;
    // UI Information
    [SerializeField] private GameplayUIControl _gameplayControl;
    [SerializeField] private List<WantsUIController> _characterWants;
    [SerializeField] private WinLossController _winLossController;

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
    private List<GameObject> _actorPositions;
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

            foreach (GameObject iter in _setCurrentlyActivePrefabs)
            {
                possibleItems.Remove(iter);
            }

            int randomItemIndex = Random.Range(0, possibleItems.Count);

            GameObject go = Instantiate(possibleItems[randomItemIndex]);
            GoalItem prefabGoalItem = go.GetComponent<GoalItem>();
            prefabGoalItem.PrefabID = possibleItems[randomItemIndex];
#if UNITY_EDITOR
            Assert.IsNotNull(prefabGoalItem);
#endif
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
        AudioManager.Instance.PlayMoveSFX(moveType);

        switch (moveType)
        {
            case MoveType.MOVE_TYPE_PLUS_ONE:
                RotateBoardByAmount(1);
                break;
            case MoveType.MOVE_TYPE_REVERSE:
                TurningClockwise = !TurningClockwise;
                break;
            case MoveType.MOVE_TYPE_CHAOS:
                RandomizeRotation();
                break;
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
            foreach (TurnOrder turn in item.SatisfyingActors)
            {
                if (turn == TurnOrder.TURN_ORDER_PLAYER)
                {
                    containsPlayerConstraint = true;
                }

                int turnToIndex = (int)turn;
                float distSqr = Vector2.SqrMagnitude(_actorPositions[turnToIndex].transform.position - item.gameObject.transform.position);
                if (distSqr < bestDistanceSqr)
                {
                    Debug.Log($"Distance sqr to {item.name}: {bestDistanceSqr}, threshold: {_obtainDistance * _obtainDistance}");

                    bestDistanceSqr = distSqr;
                    closestIndex = turnToIndex;
                }
            }

            if (bestDistanceSqr < (_obtainDistance * _obtainDistance))
            {
                TurnOrder bestAsTurn = (TurnOrder)closestIndex;
                Debug.Log($"Item was obtained by {bestAsTurn}");
                item.MoveAndShrinkTowardsTargetLocation(_actorPositions[closestIndex].transform.position);

                // TODO AQIN: UI Here to describe success or loss
                ++_actorScores[closestIndex];

                elementsToRemove.Add(item);

                if (containsPlayerConstraint)
                {
                    _isPlayerConstraintSatisfied = false;
                    bool wasPlayerSuccess = bestAsTurn == TurnOrder.TURN_ORDER_PLAYER;
                    string audioToPlay = wasPlayerSuccess ? "ObtainItem" : "LostItem";
                    AudioManager.Instance.PlayAudioOneShot(audioToPlay);
                    if (!wasPlayerSuccess)
                    {
                        --_currentLives;

                        if (_currentLives < 0)
                        {
                            AudioManager.Instance.PlayAudioOneShot("GameOver");
                            _winLossController.UpdateGameState("YOU LOSE");
                            TurnManager.Instance.IsGameOver = true;
                        }
                    }
                }

                foreach (TurnOrder turn in item.SatisfyingActors)
                {
                    int turnAsIndex = (int)turn;

                    ActorAnimator animator = _actorPositions[turnAsIndex].GetComponent<ActorAnimator>();
                    if (animator != null)
                    {
                        if (turn == bestAsTurn)
                        {
                            animator.PlaySuccess();
                        }
                        else
                        {
                            animator.PlayFail();
                        }
                    }

                    _characterWants[turnAsIndex].gameObject.SetActive(false);
                }

                --_numActiveItems;
            }
        }

        foreach (GoalItem i in elementsToRemove)
        {
            _currentlyAvailableItems.Remove(i);
            _setCurrentlyActivePrefabs.Remove(i.PrefabID);
            i.TryDestroy();
        }

        _gameplayControl.UpdateScoreForActor(_actorScores);

        if (_currentlyAvailableItems.Count == 0)
        {
            TurnManager.Instance.IsRoundOver = true;
        }
    }

    private void DetermineSpawnPointForItem(GoalItem item)
    {
        Dictionary<TurnOrder, Vector3> satisfyingPositions = new Dictionary<TurnOrder, Vector3>();
        List<BoardNode> possibleIndicies = new List<BoardNode>();
        foreach (BoardNode node in _boardPoints)
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
                if (possibleIndicies[j] == null)
                {
                    continue;
                }

                if (_currentlyAvailableItems[i].ParentNode == possibleIndicies[j])
                {
                    possibleIndicies[j] = null;
                    continue;
                }

                float distSqr = Vector2.SqrMagnitude(item.gameObject.transform.position - possibleIndicies[j].SpawnPoint.position);
                if (distSqr <= 2.5f)
                {
                    possibleIndicies[j] = null;
                    continue;
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
                Debug.Log($"{item} valid location: {possibleIndicies[i].AssociatedActor}");
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
#if UNITY_EDITOR

                Assert.IsTrue(validStartingLocations[i].AssociatedActor != turn, "There is a valid location that is equal to a constraint. this shouldn't be possible");
#endif

                Vector3 satisfyingSpawnPointPosition;
                if (!satisfyingPositions.TryGetValue(turn, out satisfyingSpawnPointPosition))
                {
#if UNITY_EDITOR
                    Assert.IsTrue(false, $"Failed to lookup satisfying constraint {turn}");
#endif
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

        item.gameObject.transform.parent = gameObject.transform;
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
                if (possibleIndicies[j] == null)
                {
                    continue;
                }

                if (possibleIndicies[j].ActorPoint != null)
                {
                    if (possibleIndicies[j].AssociatedActor == TurnOrder.TURN_ORDER_INVALID)
                    {
                        possibleIndicies[j] = null;
                    }
                    else if (_currentlyAvailableItems[i].SatisfyingActors.Contains(possibleIndicies[j].AssociatedActor))
                    {
                        possibleIndicies[j] = null;
                    }
                }
                else
                {
                    possibleIndicies[j] = null;
                }
            }
        }

        List<BoardNode> validIndicies = new List<BoardNode>();
        for (int i = 0; i < possibleIndicies.Count; ++i)
        {
            if (possibleIndicies[i] != null && possibleIndicies[i].AssociatedActor != TurnOrder.TURN_ORDER_INVALID)
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
            if (validIndicies.Count == 0)
            {
                break;
            }


            int randIndex = Random.Range(0, validIndicies.Count);
            TurnOrder randomActor = validIndicies[randIndex].AssociatedActor;

            if (randomActor == TurnOrder.TURN_ORDER_INVALID)
            {
                validIndicies.RemoveAt(randIndex);
                continue;
            }

            item.AddActorAsSatisfying(randomActor);
            validIndicies.RemoveAt(randIndex); 
            --numActorsToPick;
        }

        foreach (TurnOrder turn in item.SatisfyingActors)
        {
            if (turn == TurnOrder.TURN_ORDER_INVALID)
            {
                continue;
            }

            int turnAsIndex = (int)turn;
            _characterWants[turnAsIndex].gameObject.SetActive(true);

            SpriteRenderer itemSpriteRenderer = item.gameObject.GetComponent<SpriteRenderer>();
#if UNITY_EDITOR
            Assert.IsNotNull(itemSpriteRenderer);
#endif
            _characterWants[turnAsIndex].SetWant(itemSpriteRenderer.sprite.texture);
        }
    }

    private void GenerateConstraintsForItem(GoalItem item)
    {
        PickActorConstraint(item, _isPlayerConstraintSatisfied ? 1 : 2);
#if UNITY_EDITOR
        Assert.IsTrue(_isPlayerConstraintSatisfied, "Validating Items but Player Constraint isn't satisfied yet");
#endif
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
        AudioManager.Instance.PlayRepeatingOneShot("TableTurnStart");
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

        AudioManager.Instance.StopPlayingRepeatingOneShot();
        transform.rotation = endRot;

        AudioManager.Instance.PlayAudioOneShot("TableTurnEnd");
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
        for (int i = 0; i < _foodSpawnPoint.Count; ++i)
        {
            bool validActorPosition = i >= _actorPositions.Count;

            BoardNode node = new BoardNode(_foodSpawnPoint[i],
                validActorPosition ? null : _actorPositions[i].transform,
                validActorPosition ? TurnOrder.TURN_ORDER_INVALID : (TurnOrder)i);

            _boardPoints.Add(node);
        }
    }
}
