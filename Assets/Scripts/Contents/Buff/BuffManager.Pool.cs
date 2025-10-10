using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public partial class BuffManager
{
    #region Fields
    private BuffPool _buffPool;
    private CellBuffOccupancyPool _cellPool;
    #endregion

    #region Unity Lifecycle
    private void OnAwakePool()
    {
        _buffPool = new(this);
        _cellPool = new(this);
    }
    #endregion

    #region InnerClass & Interface
    public class BuffPool
    {
        private BuffManager _owner;
        private readonly Dictionary<int, Queue<IBuff>> _pool;

        public BuffPool(BuffManager owner)
        {
            _owner = owner;
            _pool = new();
        }

        #region Private Methods
        public IBuff Get(int id)
        {
            if (_pool.TryGetValue(id, out var queue) && queue.Count > 0)
                return queue.Dequeue();

            return Create(id);
        }
        private IBuff Create(int id)
        {
            if (!_owner._useableBuffs.TryGetValue(id, out BuffData data))
            {
                Debug.LogWarning($"유효하지 않은 Buff ID: {id}");
                return null;
            }

            IBuff buff = data.Type switch
            {
                E_BuffType.Buff => data.IsPeriodic ? new PeriodicBuff(data) : new Buff(data),
                E_BuffType.Debuff => data.IsPeriodic ? new PeriodicDeBuff(data) : new DeBuff(data),
                E_BuffType.All => data.IsPeriodic ? new PeriodicCustomBuff(data) : new CustomBuff(data),
                _ => null
            };

            return buff;
        }
        public void Release(IBuff buff)
        {
            int id = buff.Data.ID;

            if (!_pool.TryGetValue(id, out var queue))
            {
                queue = new Queue<IBuff>();
                _pool.Add(id, queue);
            }

            // 초기값으로 Reset
            if (_owner._useableBuffs.TryGetValue(id, out BuffData data))
            {
                buff.Reset(data);
            }
            queue.Enqueue(buff);
        }
        public void Prewarm(int id, int count)
        {
            if (!_owner._useableBuffs.TryGetValue(id, out BuffData data))
            {
                Debug.LogWarning($"Prewarm 실패: {id}");
                return;
            }

            if (!_pool.ContainsKey(id))
                _pool[id] = new Queue<IBuff>();

            for (int i = 0; i < count; i++)
            {
                IBuff buff = Create(id);
                _pool[id].Enqueue(buff);
            }

            Debug.Log($"{id} 버프 {count}개 Prewarm 완료");
        }
        
        // ID별 풀 제거
        public void Destroy(int id)
        {
            if (_pool.TryGetValue(id, out var queue))
            {
                queue.Clear();
                _pool.Remove(id);
                Debug.Log($"{id} 버프 풀 제거 완료");
            }
        }
        public void DestroyAll()
        {
            foreach (var kvp in _pool)
            {
                kvp.Value.Clear();
            }
            _pool.Clear();
            Debug.Log($"모든 버프 풀 Destroy 완료");
        }
        #endregion
    }
    public class CellBuffOccupancyPool
    {
        private BuffManager _owner;
        private readonly Stack<CellBuffContainer> _pool;
        public CellBuffOccupancyPool(BuffManager owner)
        {
            _owner = owner;
            _pool = new();
        }
        public CellBuffContainer Get()
        {
            if (_pool.Count > 0)
            {
                var container = _pool.Pop();
                container.Init();
                return container;
            }

            return new CellBuffContainer();
        }

        public void Release(CellBuffContainer container)
        {
            if (container == null || container.IsDisposed) return;

            container.Dispose();
            _pool.Push(container);
        }

        public void DestroyAll()
        {
            while (_pool.Count > 0)
            {
                var c = _pool.Pop();
                c.Dispose(); // 안전하게 내부 버퍼 반납
            }
            Debug.Log("모든 CellBuffContainer 풀 삭제 완료");
        }
    }
    #endregion

    // 매니저 삭제 시 호출
    private void OnDestroy()
    {
        if (LevelGrid.Instance == null) return;

    }
}
