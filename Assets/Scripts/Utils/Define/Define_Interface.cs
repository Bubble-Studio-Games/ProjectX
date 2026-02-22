using Data;
using GoogleSheet;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Define
{
    public interface INullServiceProxy<T> where T : class
    {
        /// <summary>
        /// 진짜 서비스가 Register 될 때 SceneServices가 한 번 호출해 줌.
        /// 여기서 Null 쪽에 쌓인 이벤트/상태를 real로 옮기고, 자기 쪽은 비워버리면 됨.
        /// </summary>
        void TransferTo(T real);
    }


    #region MapChange
    /// <summary>
    /// New Input System 액션맵 스택 / 상태를 관리하는 컨트롤러
    /// (예: Lobby / Game / Dialogue / Tutorial)
    /// </summary>
    [GenerateNullService]
    public interface IInputActionMapController
    {
        /// <summary>현재 활성화된 그룹 이름 (예: "Game", "Lobby", "Dialogue")</summary>
        string CurrentActionMapGroup { get; }

        /// <summary>타입 세이프 버전 (없으면 null)</summary>
        Define.E_InputActionMap? CurrentActionMapType { get; }

        /// <summary>ActionMap 변경 시 문자열 그룹 이름을 알림</summary>
        event Action<string> OnActionMapChanged;

        /// <summary>타입 기반 Push</summary>
        void PushActionMapGroup(Define.E_InputActionMap mapType);

        /// <summary>문자열 기반 Push (호환용)</summary>
        void PushActionMapGroup(string groupName);

        /// <summary>마지막으로 Push한 그룹 Pop</summary>
        void PopActionMapGroup();
    }


    #endregion
    public interface IGridTerrainScanner
    {
        E_TerrainCellType Scan(GridPosition pos, Vector3 worldPos);
    }

    /// <summary>
    /// 비동기 닫힘 대기 인터페이스 - UI가 닫힐 때 비동기적으로 대기
    /// </summary>
    public interface IAsyncCloseable
    {
        public Action OnClose { get; set; }
    }

    /// <summary>
    /// 아이템 호버 정책 인터페이스 - 부모 UI가 호버 패널 표시 여부 및 방식 결정
    /// </summary>
    public interface IItemHoverPolicy
    {
        /// <summary>
        /// 아이템 호버 패널 표시
        /// </summary>
        public void ShowItemHover(Item.Data data, UnityEngine.Vector2 screenPos);

        /// <summary>
        /// 아이템 호버 패널 숨김
        /// </summary>
        public void HideItemHover();
    }

    public interface IItemBoxUI
    {
        public ITable Table { get; }
    }

    public interface IUpgradeble
    {
        public event Action<OnChangeGradeEventArgs> OnChangeGrade;
        public E_ObjectGrade m_EObjectGrade { get; set; } //조정된 등급
        public void TryEnhanceGrade();
    }

    public class OnChangeGradeEventArgs : EventArgs
    {
        public E_ObjectGrade objGrade;
        public E_ObjectEnhanceType gradeEnhanceType;
        public float enhanceValue;
        public bool isSuccessGrade;
    }

    #region Scene Services

    [GenerateNullService]
    public interface ICoroutineRunner
    {
        Coroutine Run(IEnumerator routine);
        void Stop(Coroutine coroutine);
    }


    public interface IDungeonCore
    {
        GridPosition GetGridPosition();
    }


    [GenerateNullService]
    public interface IBuildingCardUI
    {
        void AddCard(GameEntity addUnit, Vector3 worldPosition = default, bool isInit = false);
        void RestoreSaveDatas(IEnumerable<Data.BaseData> datas);
        List<Data.BaseData> CaptureSaveData();
    }


    [GenerateNullService]
    public interface ICameraInfoProvider
    {
        Vector3 Position { get; }
        Quaternion Rotation { get; }

        void SetPositionAndRotation (Vector3 position, Quaternion rotation);
    }

    [GenerateNullService]
    public interface ICameraRig
    {
        event Action<int> OnChangeLookFloor;
        int CurrentLookFloor { get; }

        float GetCameraHeight();
    }

    [GenerateNullService]
    public interface ICameraShakeSettings
    {
        void SetImpulseReactionDuration(float duration);
    }

    #region Input


    [GenerateNullService]
    public interface ICameraInput
    {
        Vector2 GetCameraMoveVector();
    }
    
    [GenerateNullService]
    public interface IInputQuery
    {
        bool IsRightClick { get; }
    }
    #endregion

    #endregion

    public interface ICommandAction
    {
    }

    public interface IInteractable
    {
        bool CanInteract(GameEntity interactor);
        void Interact(GameEntity interactor);
        int GetInteractRange();
        public event Action OnInteracted;
    }

    // 플레이어가 마우스 클릭으로 상호작용이 가능한 오브젝트에 부착할 용도
    public interface ISelectable
    {
        public event Action OnSelectedEvent;
        public event Action OnDeselectedEvent;

        public void OnDeselected();
        public void OnSelected();
    }

    public interface IGuidObject
    {
        public void SetGUID(string inputGuid);
        public string guid { get;}
    }

    // 1. 제네릭이 없는 버전 (저장 시 범용적으로 사용)
    public interface ISaveable
    {
        // 단일 저장
        BaseData CaptureSaveData();

        // 여러 개 저장 (기본은 null)
        IEnumerable<BaseData> CaptureSaveDatas() => null;

        // 단일 복원
        void RestoreSaveData(BaseData data);

        // 여러 개 복원
        void RestoreSaveDatas(IEnumerable<BaseData> datas) { }
    }
}
