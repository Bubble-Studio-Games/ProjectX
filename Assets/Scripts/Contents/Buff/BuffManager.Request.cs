using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class BuffManager
{
    #region Fields
    private static Queue<Request> request;

    #endregion

    #region Unity Lifecycle
    private void OnAwakeRequest()
    {
        if (request == null)
        {
            request = new();
        }

        Debug.Log("BuffManaer :  Request Init");
    }

    // 요청처리 Update
    private void OnUpdateRequest()
    {
        if(request?.Count > 0)
        {
            ExecuteRequest();
        }
    }
    #endregion

    #region Methods
    // Dequeue 추적용
    private Request? RequestDequeue()
    {
        if (request.Count > 0)
            return request.Dequeue();
        return null;
    }
    
    /// <summary>
    /// Request 생성하여 참조를 전달하면, BuffManager가 관리하는 Queue에 요청 등록.
    /// </summary>
    /// <param name="req"></param>
    public void SubmitRequest(in Request req)
    {
        request.Enqueue(req);   // 복사 저장이므로 ref로 받음
    }
    #endregion

}

#region Request Enum & Struct
public enum RequestType
{
    BuffLoad,
    BuffUnLoad,
    BuffApply,
    BuffRemove
}

/// <summary>
/// Request 생성시 Controlable이 가진 데이터 넣어서 요청
/// </summary>
public struct Request
{
    public RequestType type;    // 요청 타입
    public IBuffSender sender;      // 보내는 사람
    public IBuffReceiver receiver;    // 받는 사람
    public int id;  // 요청과 관련된 id
    public IBuff instance;

    public Request(RequestType _type, IBuffSender _sender, in IBuffReceiver _receiver, int _id, in IBuff _instance)
    {
        type = _type;
        sender = _sender;
        receiver = _receiver;
        id = _id;
        instance = _instance;
    }
}
#endregion
