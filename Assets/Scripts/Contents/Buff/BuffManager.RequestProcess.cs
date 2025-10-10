using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class BuffManager
{
    #region Fields
    private BuffPathCollection _pathCollection;
    private Dictionary<int, BuffData> _useableBuffs;

    #endregion

    #region Unity Lifecycle
    private void OnAwakeProcess()
    {
        _pathCollection = Managers.Resource.Load<BuffPathCollection>("Data/Buff/BuffPathRegistry");
    }

    #endregion

    #region Methods
    private void ExecuteRequest()
    {
        var req = RequestDequeue().Value;

        switch (req.type)
        {
            case RequestType.BuffLoad:
                BuffLoad(req.id);
                break;
            case RequestType.BuffUnLoad:
                BuffUnLoad(req.id);
                break;
            case RequestType.BuffApply:
                BuffApply(req.id, ref req.receiver);
                break;
            case RequestType.BuffRemove:
                BuffRemove(req.instance);
                break;
            default:
                break;
        }
    }
    private void BuffLoad(int id)
    {
        if (_useableBuffs.ContainsKey(id)) return;

        BuffConfig buffConfig = Managers.Resource.Load<BuffConfig>(_pathCollection.GetPath(id));
        BuffData buffData = new(buffConfig);
        _useableBuffs.Add(id, buffData);
    }
    private void BuffUnLoad(int id)
    {
        if (_useableBuffs.ContainsKey(id))
            _useableBuffs.Remove(id);
        else return;
    }
    private void BuffApply(int id, ref IBuffReceiver receiver)
    {
        if (receiver != null)
            receiver.ApplyBuff(_buffPool.Get(id));
    }
    private void BuffRemove(IBuff buff)
    {
        _buffPool.Release(buff);
    }
    #endregion
}
