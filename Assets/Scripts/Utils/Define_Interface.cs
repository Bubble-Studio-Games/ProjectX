using Data;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public partial class Define
{
    // 플레이어가 마우스 클릭으로 상호작용이 가능한 오브젝트에 부착할 용도
    public interface IInteractable
    {
        // TODO 삭제 예정 => OnSelected로 대체
        void Interact(Action onInteractionComplete);

        public event EventHandler OnSelectedEvent;
        public event EventHandler OnDeselectedEvent;

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
