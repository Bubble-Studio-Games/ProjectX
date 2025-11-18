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
            Managers.Game.GameEntityModelsSetLayer(visual, LayerMask.NameToLayer("Default"));
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
                Managers.Game.GameEntityModelsSetLayer(visual, LayerMask.NameToLayer("Ghost"));
            }
        }
    }

    private void ObjectPlaced(object s, GridBuildingSystem.OnPlacedEventArgs e)
    {
        visual.transform.SetParent(null);
        Managers.Game.GameEntityModelsSetLayer(visual, LayerMask.NameToLayer("Default"));

        //Level grid Set Reserve
        LevelGrid.Instance.SetGridPositionCellInfo(visual.GetGridPositionListAtSelectPosition(e.PivotGridPosition), Define.E_GridCheckType.Reserve, visual);

        StartCoroutine(visual.m_SetupAnimation.PlacedSpawnAnimation());

        visual = null;
    }

}

