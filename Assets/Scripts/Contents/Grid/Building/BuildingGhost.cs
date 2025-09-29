using CodeMonkey.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using UnityEngine;
using static GridBuildingSystem;
using static UnityEditor.Experimental.GraphView.GraphView;

public class BuildingGhost : MonoBehaviour 
{
    public static BuildingGhost Instance { get; private set; }

    private GameEntity visual;

    private Dictionary<Transform, int> layerDic = new Dictionary<Transform, int>();

    public float floatingHeight = 1f;

    public Vector3 m_PivotPosition { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start() {
        RefreshVisual();

        GridBuildingSystem.Instance.OnObjectPlacedCancel += (s, e) => RefreshVisual();
        GridBuildingSystem.Instance.OnSelectedChanged += (s, e) => RefreshVisual();
        GridBuildingSystem.Instance.OnObjectPlaced += ObjectPlaced;
        //GridBuildingSystem.Instance.OnRotateObject += (s, e) => UpdateVisualObjectRotation();
    }

    private void LateUpdate() {
        if (visual == null)
            return;

        Vector3 targetPosition = GridBuildingSystem.Instance.GetMouseWorldSnappedPosition();
        targetPosition.y += floatingHeight;
        visual.transform.position = Vector3.Lerp(visual.transform.position, targetPosition, Time.deltaTime * 15f);
        visual.transform.rotation = Quaternion.Lerp(visual.transform.rotation, GridBuildingSystem.Instance.GetPlacedObjectRotation(), Time.deltaTime * 15f);
    }

    private void RefreshVisual() {
        if (visual != null) {
            Restorelayer(visual.transform);
            Managers.Resource.Destroy(visual.gameObject);
            visual = null;
        }

        GameEntity placedObject = GridBuildingSystem.Instance.GetPlacedObject();

        Vector3 mousePosition = UtilsClass.GetMouseWorldPositionByRaycast(LayerManager.Instance.mousePlaneLayerMask);
        if (LevelGrid.Instance.IsValidGridPosition(mousePosition) == false)
            return;
        
        m_PivotPosition = LevelGrid.Instance.GetWorldPositionNormalize(mousePosition);

        if (placedObject != null) {
            visual = Managers.Resource.Instantiate<GameEntity>(placedObject.gameObject, Vector3.zero, Quaternion.identity);
            visual.transform.parent = transform;
            visual.transform.localPosition = m_PivotPosition;
            visual.transform.rotation = Quaternion.Euler(0, placedObject.GetRotationAngle(), 0);
            visual.m_CurrentEDir = placedObject.m_CurrentEDir;

            visual.SelectSpawnObject();
            foreach (var t in visual.m_ModelTransforms)
            {
                SetLayerRecursive(t, LayerMask.NameToLayer("Ghost"));
            }
        }
    }

    private void ObjectPlaced(object s, GridBuildingSystem.OnPlacedEventArgs e)
    {
        visual.transform.SetParent(null);
        Restorelayer(visual.transform);

        //Level grid Set Reserve
        LevelGrid.Instance.SetReserveGridPosition(visual.GetGridPositionListAtSelectPosition(e.PivotGridPosition) , true, visual);

        StartCoroutine(visual.m_SetupAnimation.PlacedSpawnAnimation());

        visual = null;
    }

    private void SetLayerRecursive(Transform target, int layer)
    {
        // MeshRenderer가 있으면 레이어 백업 후 변경
        if (target.TryGetComponent<Renderer>(out Renderer mChild))
        {
            layerDic[target] = target.gameObject.layer;
            target.gameObject.layer = layer;
        }

        // 자식들도 반드시 재귀 탐색
        foreach (Transform child in target)
        {
            SetLayerRecursive(child, layer);
        }
    }


    private void Restorelayer(Transform targetGameObject, bool isRoot = true)
    {
        foreach (Transform child in targetGameObject.transform)
        {
            if(layerDic.TryGetValue(child, out int layer))
            {
                child.gameObject.layer = layerDic[child];
            }
            Restorelayer(child, false);
        }

        // 재귀가 전부 끝난 후, 루트에서만 딕셔너리 초기화
        if (isRoot)
        {
            layerDic.Clear();
        }
    }

}

