
using UnityEngine;
using static Define;

public static class GameEntityEx
{
    /// <summary>
    /// 스탯을 가져오는 함수, 유효성 검증
    /// </summary>
    public static T GetMyStat<T>(this GameEntity entity, string statname = default) where T : BaseStat
    {
        if (entity.m_AttributeSystem != null && entity.m_AttributeSystem.m_Stat is T stat)
            return stat;
        else
            Debug.LogError($"{entity.name} 의 AttributeSystem 에 {typeof(T).Name} 가 존재하지 않습니다.");
        return null;
    }

    public static bool TryGetMyStat<T>(this GameEntity entity, out T stat) where T : BaseStat
    {
        stat = GetMyStat<T>(entity);
        if (stat == null)
            return false;
        return true;
    }
}