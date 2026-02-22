using UnityEngine;

public class PathfindingLinkMonoBehaviour : MonoBehaviour
{
    public Vector3 linkPositionA;
    public Vector3 linkPositionB;

    public void Start()
    {
        Managers.Path.PathFindingLinkRegister(GetPathfindingLink());
    }

    public PathfindingLink GetPathfindingLink()
    {
        return new PathfindingLink {
            gridPositionA = Managers.Grid.GetGridPosition(linkPositionA),
            gridPositionB = Managers.Grid.GetGridPosition(linkPositionB)
        };
    }

}