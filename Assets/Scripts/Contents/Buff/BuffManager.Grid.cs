using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using static Define;

public partial class BuffManager
{
    private readonly Dictionary<GridPosition, CellBuffContainer> _activeContainers = new();

    #region Unity Lifecycle
    private void OnAwakeGrid()
    {
        // LevelGrid 이벤트 구독
        LevelGrid.Instance.OnChangeGrid += HandleGridChange;
    }
    #endregion

    // 합본
    private void HandleGridChange(object _, LevelGrid.OnChangeGridAgrs e)
    {
        if (e.type != E_GridCheckType.HasUnit || e.ListGridPosition == null || e.ListGridPosition.Count == 0) return;

        // 가상 중심 좌표 계산, Unit이 1x1 이상의 셀 점유시
        Vector2 center = Vector2.zero;
        foreach (var pos in e.ListGridPosition)
            center += new Vector2(pos.x, pos.z);
        center /= e.ListGridPosition.Count;

        GridPosition centerPos = new GridPosition(
            Mathf.RoundToInt(center.x),
            Mathf.RoundToInt(center.y),
            e.ListGridPosition[0].floor
        );

        var entity = LevelGrid.Instance.GetObjectAtGridPosition(centerPos);
        if (entity == null) return;

        if (e.isNotGrid)    // 해당 셀 점유중인 Entity 존재하면
        {
            if (entity is IBuffSender sender && sender.BuffID?.Count > 0)
                SenderAdded(sender, centerPos);
            else if (entity is IBuffReceiver receiver) 
                ReceiverAdded(receiver, e.ListGridPosition);
        }
        else // 셀 점유자가 없어지면
        {
            if (entity is IBuffSender sender)
                SenderRemoved(sender, centerPos);
            else if (entity is IBuffReceiver receiver)
                ReceiverRemoved(receiver, e.ListGridPosition);
        }
    }

    private void SenderAdded(IBuffSender sender, GridPosition centerPos)
    {
        var offset = new GridPosition(sender.ShapeOffset.x, sender.ShapeOffset.y, centerPos.floor);
        var affectedPositions = GetGridPositionsAtGridShape(offset, centerPos, sender.Shape);

        foreach (var pos in affectedPositions)
        {
            // 유효 검사
            if (!LevelGrid.Instance.TryGetGridCellInfo(pos, out var cell)) continue;
            if (cell.CheckType == E_GridCheckType.Obstacle) continue;

            // 컨테이너 객체 있는지
            if (!_activeContainers.TryGetValue(pos, out var container))
            {
                container = _cellPool.Get();
                container.Init();
                container.GridPosition = pos;
                _activeContainers[pos] = container;
            }

            // 컨테이너 객체에 sender 등록
            container.AddOccupant(sender);

            // 혹시 버프 받을 사람 있나?
            var receiverEntity = LevelGrid.Instance.GetObjectAtGridPosition(pos);
            if (receiverEntity is IBuffReceiver receiver) // 널체크 ok
            {
                foreach (var id in sender.BuffID)
                    sender.IssueRequest(RequestType.BuffApply, receiver, id);
            }
        }
    }

    private void SenderRemoved(IBuffSender sender, GridPosition centerPos)
    {
        var offset = new GridPosition(sender.ShapeOffset.x, sender.ShapeOffset.y, centerPos.floor);
        var affectedPositions = GetGridPositionsAtGridShape(offset, centerPos, sender.Shape);

        // Sender 해제 전에 적용중인 범위 버프 거두기
        var affectedReceivers = GetReceiversAtPositions(affectedPositions);

        if (affectedReceivers != null)
        {
            foreach (var receiver in affectedReceivers)
            {
                foreach(var buff in receiver.TakeBuffs)
                {
                    if(sender == buff.Sender) continue;
                    buff.Sender.IssueRequest(RequestType.BuffRemove, receiver, buff.Data.ID, buff); // 회수 요청 작성
                }
            }
        }

        foreach (var pos in affectedPositions)
        {
            if (!_activeContainers.TryGetValue(pos, out var container)) continue;
            container.RemoveOccupant(sender);   // sender 제거

            if (!container.IsOccupied)
            {
                _cellPool.Release(container);   // 컨테이너 회수
                _activeContainers.Remove(pos);  // 비활성 처리
            }
        }
    }

    private void ReceiverAdded(IBuffReceiver receiver, List<GridPosition> positions)
    {
        foreach (var pos in positions)  // ReceiverAdded는 실제로 positions[0]으로, 1번만 돌아야 함.
        {
            if (!_activeContainers.TryGetValue(pos, out var container)) continue;
            if (container.IsDisposed) continue;

            for (int i = 0; i < container.Count; i++)
            {
                var sender = container.GetBuffSender(i);
                foreach (var id in sender.BuffID)
                    sender.IssueRequest(RequestType.BuffApply, receiver, id);
            }
        }
    }

    private void ReceiverRemoved(IBuffReceiver receiver, List<GridPosition> positions)
    {
        // 이동 처리 or 죽었을 때 버프 객체도 돌려주는 요청 넣으면서 내 버프 지워버리기. 
    }

    private List<IBuffReceiver> GetReceiversAtPositions(List<GridPosition> gridPositions)
    {
        if (gridPositions == null || gridPositions.Count == 0) return null;

        List<IBuffReceiver> list = new();
        foreach (var pos in gridPositions)
        {
            var receiverEntity = LevelGrid.Instance.GetObjectAtGridPosition(pos);
            if (receiverEntity is IBuffReceiver receiver) // 널체크 ok
            {
                list.Add(receiver);
            }
        }
        return list;
    }

    // Shape 기준, 북쪽 방향 기준으로 Position Get, dir 집어넣으면 회전
    private List<GridPosition> GetGridPositionsAtGridShape(GridPosition offset, GridPosition origin, E_GridShape shape, E_Dir dir = E_Dir.North)
    {
        List<GridPosition> list = new();
        switch (shape)
        {
            case E_GridShape.Cross:
                for (int i = 0; i < 4; i++)
                {
                    var pos = LevelGrid.Instance.ToGridPosition(offset, origin, (E_Dir)i);
                    if (LevelGrid.Instance.IsValidGridPosition(pos))
                        list.Add(pos);
                }
                break;
            case E_GridShape.XShaped:
                for (int i = 4; i < 8; i++)
                {
                    var pos = LevelGrid.Instance.ToGridPosition(offset, origin, (E_Dir)i);
                    if (LevelGrid.Instance.IsValidGridPosition(pos))
                        list.Add(pos);
                }
                break;
            case E_GridShape.Square:
                for (int x = -offset.x; x <= offset.x; x++)
                {
                    for (int z = -offset.z; z <= offset.z; z++)
                    {
                        var pos = new GridPosition(origin.x + x, origin.z + z, origin.floor);
                        if (LevelGrid.Instance.IsValidGridPosition(pos))
                            list.Add(pos);
                    }
                }
                break;
        }

        // 옵젝이 바라보는 방향이 있고 정비례 아닐 경우 반영.
        if (offset.x != offset.z && dir != E_Dir.North)
        {
            for (int i = 0; i < list.Count; i++)
            {
                // offset 계산: origin 기준 상대 좌표
                var relative = list[i] - origin;
                list[i] = LevelGrid.Instance.ToGridPosition(relative, origin, dir);
            }
        }

        return list;
    }

}

public enum E_GridShape
{
    Square,
    Cross,
    XShaped
}

// [MethodImpl(MethodImplOptions.AggressiveInlining)]
// 인라인 명시, 자주 사용이 예상되는 함수나 호출비용이 더 비쌀 것 같을 때 추가하되, 컴파일러의 조건을 만족하지 못할 것 같을 때

/// <summary>
/// 셀 정보 버프용, ArrayPool Rent 하는 쪽으로 구현했는데 Buff ID 등록이 빈번하지 않으면 안써도 됌.
/// </summary>
public class CellBuffContainer : IDisposable
{
    private IBuffSender[] _occupants; // Rent, Return 싱글톤 배열, 점유중인 객체
    private int _count; // 배열 신뢰 범위
    private int _capacity;  // 배열 크기
    private bool _isDisposed;   // 반납 여부
    public GridPosition GridPosition { get; set; } // 연결된 Cell Position

    public bool IsOccupied => _count > 0;   // 점유하는 객체가 있어?
    public int Count => _count;
    public bool IsDisposed => _isDisposed;

    public void Init(int capacity = 4)
    {
        if (!_isDisposed && _occupants != null)
        {
            // 이전 세션이 남아 있으면 정리
            ArrayPool<IBuffSender>.Shared.Return(_occupants, clearArray: true);
        }

        _capacity = capacity;
        _occupants = ArrayPool<IBuffSender>.Shared.Rent(_capacity);
        _count = 0;
        _isDisposed = false;
    }

    // ID 추가, 버퍼 크기 작으면 재할당
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddOccupant(IBuffSender sender)
    {
        if (_isDisposed || _occupants == null) return;

        if (_count >= _capacity)
        {
            int newCap = _capacity * 2;
            IBuffSender[] newArr = ArrayPool<IBuffSender>.Shared.Rent(newCap);
            Array.Copy(_occupants, newArr, _capacity);
            ArrayPool<IBuffSender>.Shared.Return(_occupants);
            _occupants = newArr;
            _capacity = newCap;
        }

        _occupants[_count++] = sender;
    }
    // 초기화
    public void Clear()
    {
        if (_isDisposed || _occupants == null) return;

        Array.Clear(_occupants, 0, _count);
        _count = 0;
    }
    // 반납
    public void Dispose()
    {
        if (_isDisposed) return;

        if (_occupants != null)
        {
            ArrayPool<IBuffSender>.Shared.Return(_occupants, clearArray: true);
            _occupants = null;
        }
        _capacity = 0;
        _count = 0;
        _isDisposed = true;
    }

    public IBuffSender GetBuffSender(int index)
    {
        return _occupants[index];
    }
    public void RemoveOccupant(IBuffSender sender)
    {
        if (_isDisposed || _occupants == null || _count == 0)
            return;

        for (int i = 0; i < _count; i++)
        {
            if (_occupants[i] == sender)
            {
                // Swap back
                _occupants[i] = _occupants[_count - 1];
                _occupants[_count - 1] = null;
                _count--;
                return;
            }
        }
    }
}