using System;

public enum BuildRequestType { Select, Rotate, Place, Cancel, Fail}

public readonly struct BuildContext
{
    public readonly BuildRequestType Type;
    public readonly GameEntity Target;
    public readonly string CardGuid;       // 이 요청을 만든 카드

    public BuildContext(BuildRequestType type, GameEntity target = null, string cardGuid = null)
    {
        Type = type;
        Target = target;
        CardGuid = cardGuid;
    }
}

public class GridBuildingManager : IManager
{
    // 외부 구독 이벤트(결과)
    public event Action<BuildContext> OnSelected; // 오브젝트 선택
    public event Action<GridPosition, BuildContext> OnPlaced; // 오브젝트 배치
    public event Action<BuildContext> OnPlaceFailed; // 오브젝트 배치 실패
    public event Action<BuildContext> OnCanceled; // 오브젝트 배치 취소
    public event Action OnRotated; // 오브젝트 회전

    // 외부 요청 구독 이벤트
    public event Action<BuildContext> OnRequestApply;

    public GameEntity PlacingObject => PendingContext.Target;
    public bool IsSetuping => PlacingObject != null;

    public BuildContext PendingContext { get; private set; }  


    // 요청
    public void Reqeust(BuildContext reCotext)
    {
        PendingContext = reCotext;
        OnRequestApply?.Invoke(reCotext);
    }

    // System이 처리 결과로 호출 (여기서 UI 이벤트 등 발사)
    public void RaiseSelected() => OnSelected?.Invoke(PendingContext);
    public void RaiseRotated() => OnRotated?.Invoke();
    public void RaisePlaced(GridPosition gp)
    {
        OnPlaced?.Invoke(gp, PendingContext);
        PendingContext = default;
    }
    public void RaiseCanceled()
    {
        OnCanceled?.Invoke(PendingContext);
        PendingContext = default;
    }

    public void RaiseFailed()
    {
        OnPlaceFailed?.Invoke(PendingContext);
        PendingContext = default;
    }

    public void Init() { }
    public void Clear() { }
}
