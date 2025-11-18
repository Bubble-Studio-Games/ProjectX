using Data;
using System;
using System.Collections;
using UnityEngine;
using static Define;

[RequireComponent(typeof(AudioSource), typeof(Animator))]
public class Item : MonoBehaviour, ISaveable, IGuidObject
{
    [Header("Ref")]
    protected Animator animator;
    protected Transform spawnTransform;
    protected GameEntity m_Owner;

    public string _guid { get; private set; } = string.Empty; // private field로 변경하고 프로퍼티로 접근
    public string guid => _guid;

    public void SetGUID(string inputGuid)
    {
        _guid = inputGuid;
    }

    public virtual void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        Managers.Object.Add(gameObject);

        // 씬에 배치된 오브젝트인 경우, GUID가 없으면 새로 생성
        if (string.IsNullOrEmpty(_guid))
        {
            _guid = Guid.NewGuid().ToString(); // System.Guid를 사용하여 새 GUID 생성
        }
    }

    public virtual void OnEnable()
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(true);
        }

        spawnTransform = transform;
    }

    public virtual void OnDestroy()
    {
        Managers.Object.Remove(gameObject);
    }


    public virtual void OnDisable()
    {
    }

    public virtual void Destroy(float seconds = 3.0f)
    {
        // 모델은 숨기고
        // 사운드 때문에 이렇게 해 놓은거
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }

        StartCoroutine(ObjectDestroy(seconds));
    }

    protected IEnumerator ObjectDestroy(float seconds = 3.0f)
    {
        yield return new WaitForSeconds(seconds);

        // UnityEngine.Object는 == 연산자 오버라이드로 "Missing"도 null로 판단함
        if (gameObject == null)
            yield break;

        Managers.Resource.Destroy(gameObject);
    }


    public virtual BaseData CaptureSaveData()
    {
        return new ItemData()
        {
            prefabName = name,
            spawnPosition = spawnTransform.position,
            spawnRotation = spawnTransform.rotation,
            position = transform.position,
            rotation = transform.rotation,
            guid = _guid,
            onwerGuid = m_Owner?._guid,
        };
    }

    public virtual void RestoreSaveData(BaseData data)
    {
        ItemData iData = data as ItemData;
        _guid = iData.guid;
        spawnTransform.position = iData.position;
        spawnTransform.rotation = iData.rotation;
        transform.SetPositionAndRotation(iData.position, iData.rotation);
        m_Owner = Managers.Object.FindByGuidObject<GameEntity>(iData.onwerGuid);

        if(m_Owner is ControllableObject cObj)
        {
            if(cObj.m_ControllableObjectCombatManager != null)
            {
                cObj.m_ControllableObjectCombatManager.m_AttackReadyItemObject.Add((this, spawnTransform));
            }
        }
    }
}
