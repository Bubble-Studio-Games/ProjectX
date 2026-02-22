using UnityEngine;

[CreateAssetMenu(fileName = "GridSettings", menuName = "Game/Config/Grid")]
public class GridSettings : ScriptableObject
{
    [Header("Grid Settings")]
    [SerializeField] private int width = 35;
    public int Width => width;
    [SerializeField] private int height = 30;
    public int Height => height;
    [SerializeField] private float cellSize = 2f;
    public float CellSize => cellSize;
    [SerializeField] private int floorAmount = 1;
    public int FloorAmount => floorAmount;
}
