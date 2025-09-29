using CodeMonkey.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.iOS;
using static Define;

public class GridBuildingSystem : MonoBehaviour
{
    public static GridBuildingSystem Instance { get; private set; }

    public event EventHandler<E_SetupObjectOffsetChange> OnSelectedChanged;
    public event EventHandler<OnPlacedEventArgs> OnObjectPlaced;
    public class OnPlacedEventArgs : EventArgs
    {
        public GridPosition PivotGridPosition;
    }

    public event EventHandler<E_SetupObjectOffsetChange> OnRotateObject;
    public event EventHandler OnObjectPlacedCancel;

    [SerializeField] private List<GameEntity> placedObjectList;
    public GameEntity m_PlacedObject { get; private set; }
    private GameEntity beforePlacedObject;


    int tempSize = 10;

    private void Awake()
    {
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        //DeleteObject();

        RotateSelectObject();
    }

    public bool SetUpGridObject()
    {
        if (m_PlacedObject == null)
            return false;

        Vector3 mousePlanePos = UtilsClass.GetMouseWorldPositionByRaycast(LayerManager.Instance.mousePlaneLayerMask);
        GridPosition pivotGridPos = LevelGrid.Instance.GetGridPosition(mousePlanePos);
        List<GridPosition> gridPositions = m_PlacedObject.GetGridPositionListAtSelectPosition(pivotGridPos);
        Vector3 baseWorldPos = LevelGrid.Instance.GetWorldPosition(pivotGridPos);

        // 조건 검사
        foreach (GridPosition gp in gridPositions)
        {
            if (!LevelGrid.Instance.IsValidGridPosition(gp) ||
                !Pathfinding.Instance.IsWalkableGridPosition(gp) ||
                LevelGrid.Instance.HasAnyUnitOnGridPosition(gp) ||
                LevelGrid.Instance.IsReservedGridPosition(gp))
            {
                //UtilsClass.CreateWorldTextPopup("Cannot building here!", baseWorldPos, tempSize);
                return false;
            }
        }

        OnObjectPlaced?.Invoke(this, new OnPlacedEventArgs 
        { PivotGridPosition = pivotGridPos });

        return true;
    }

    private void RotateSelectObject()
    {
        if (m_PlacedObject == null)
            return;

        if (Input.GetKeyDown(KeyCode.R))
        {
            m_PlacedObject.m_CurrentEDir = m_PlacedObject.GetNextDir();
            //UtilsClass.CreateWorldTextPopup("" + m_PlacedObject.m_CurrentEDir, UtilsClass.GetMouseWorldPositionByRaycast(1 << LayerMask.NameToLayer("MousePlane")), tempSize);
            
            if(m_PlacedObject.m_IsRotateSymmetry)
                OnRotateObject?.Invoke(this, E_SetupObjectOffsetChange.None);
            else
                OnRotateObject?.Invoke(this, E_SetupObjectOffsetChange.XZOffset);
        }
    }


    // 설치된 오브젝트 삭제
    // 나중에 삭제하고 삭제 UI 띄울 거임
    private void DeleteObject()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Vector3 mousePosition = UtilsClass.GetMouseWorldPositionByRaycast(LayerManager.Instance.mousePlaneLayerMask);

            if (!LevelGrid.Instance.IsValidGridPosition(LevelGrid.Instance.GetGridPosition(mousePosition)))
                return;

            // Valid Grid Position
            var placedObject = LevelGrid.Instance.GetObjectAtGridPosition(LevelGrid.Instance.GetGridPosition(mousePosition));
            if (placedObject != null)
            {
                // Demolish
                placedObject.DeSpawnComplete();
            }
        }
    }

    public void ChangePlaceObject(GameEntity toChangeObject, bool isInputNumberPad = false)
    {
        beforePlacedObject = m_PlacedObject;
        m_PlacedObject = toChangeObject;

        Vector3 mousePosition = UtilsClass.GetMouseWorldPositionByRaycast(LayerManager.Instance.mousePlaneLayerMask);
        //if (m_PlacedObject != null)
        //    UtilsClass.CreateWorldTextPopup("Select : " + m_PlacedObject.name, mousePosition, 10);

        if (beforePlacedObject == m_PlacedObject)
            return;


        // 취소 했을 때는 x
        if (toChangeObject == null)
        {
            OnObjectPlacedCancel?.Invoke(this, null);
            return;
        }

        E_SetupObjectOffsetChange state = E_SetupObjectOffsetChange.All;

        if(isInputNumberPad)
        {
            if (beforePlacedObject != null)
            {
                if (beforePlacedObject.m_GridPositionOffsets != m_PlacedObject.m_GridPositionOffsets)
                {
                    if (beforePlacedObject.GetGridPositionYOffset() != m_PlacedObject.GetGridPositionYOffset())
                        state = E_SetupObjectOffsetChange.All;
                    else
                        state = E_SetupObjectOffsetChange.XZOffset;
                }
                else
                    state = E_SetupObjectOffsetChange.None;
            }
            else
            {
                // 제일 처음 가져 왔을 때
                state = E_SetupObjectOffsetChange.All;
            }
        }

        OnSelectedChanged?.Invoke(this, state);
    }

    public Quaternion GetPlacedObjectRotation()
    {
        if (m_PlacedObject != null)
        {
            return Quaternion.Euler(0, m_PlacedObject.GetRotationAngle(), 0);
        }
        else
        {
            return Quaternion.identity;
        }
    }

    public Vector3 GetMouseWorldSnappedPosition()
    {
        Vector3 mousePlanePos = UtilsClass.GetMouseWorldPositionByRaycast(LayerManager.Instance.mousePlaneLayerMask);
        if (LevelGrid.Instance.IsValidGridPosition(mousePlanePos) == false)
            return mousePlanePos;

        Vector3 baseWorldPos = LevelGrid.Instance.GetWorldPositionNormalize(mousePlanePos);
        
        if (m_PlacedObject != null)
        {
            return baseWorldPos;
        }
        else
        {
            return mousePlanePos;
        }
    }


    public GameEntity GetPlacedObject()
    {
        return m_PlacedObject;
    }



}
