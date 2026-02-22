using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GridDebugger : MonoBehaviour
{
    [SerializeField] private Transform debugPrefab;
    private Dictionary<GridPosition, GridDebugObject> _debugObjects = new();

    private void Start()
    {
        Setup();
    }

    public void Setup()
    {
        // LevelGrid의 데이터를 바탕으로 디버그 오브젝트들 생성
        foreach (var gridSystem in Managers.Grid.LevelGrid.GridSystemList)
        {
            _debugObjects.AddRange(gridSystem.CreateDebugObjects(debugPrefab, this.transform));
        }

        // LevelGrid의 값이 변할 때마다 디버그 텍스트 갱신을 위해 이벤트 구독
        Managers.Grid.OnGridChanged += UpdateVisual;
    }

    private void UpdateVisual(GridPosition g)
    {
        if (_debugObjects.TryGetValue(g, out var debugObj))
            debugObj.UpdateGridObject(); // 텍스트나 색상 갱신
    }
}