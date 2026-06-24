using UnityEngine;

public class BoardNode
{
    public BoardNode(Transform spawnPoint, Transform actorPoint, TurnOrder associatedActor)
    {
        SpawnPoint = spawnPoint;
        ActorPoint = actorPoint;
        AssociatedActor = associatedActor;
    }

    public Transform SpawnPoint { get; set; } = null;
    public Transform ActorPoint { get; set; } = null;
    public TurnOrder AssociatedActor { get; set; } = TurnOrder.TURN_ORDER_INVALID;
}
