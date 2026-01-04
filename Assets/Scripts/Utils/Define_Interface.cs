using Data;
using GoogleSheet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
}

public static class IAsyncCloseableEx
{
    /// <summary>
    /// UI 닫힘 대기 - 여러 UI가 모두 닫힐 때까지 비동기 대기
    /// </summary>
    public static async Task WaitForUICloseAsync(CancellationToken ct, params Define.IAsyncCloseable[] closeables)
    {
        var registrations = new List<CancellationTokenRegistration>();

        try
        {
            var tasks = closeables.Select(ui =>
            {
                var tcs = new TaskCompletionSource<bool>();

                if (ct.CanBeCanceled)
                {
                    var registration = ct.Register(() => tcs.TrySetCanceled());
                    registrations.Add(registration);
                }

                ui.OnClose = null;
                ui.OnClose = () => tcs.TrySetResult(true);
                return tcs.Task;
            }).ToArray();

            await Task.WhenAll(tasks);
        }
        finally
        {
            foreach (var registration in registrations)
                registration.Dispose();
        }
    }
}
