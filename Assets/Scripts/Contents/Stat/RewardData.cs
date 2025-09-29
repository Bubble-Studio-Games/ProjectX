using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

#region Reward Data
[CreateAssetMenu(menuName = "Reward/Base Reward")]
public class RewardData : ScriptableObject
{
    [Header("Down Jam")]
    public int downJamMin;
    public int downJamMax;
    public float downJamProb;

    [Header("Card")]
    public List<RewardCard> rewardCards = new List<RewardCard>();
    public float CardProb;
    
    [Header("Buff")]
    public List<RewardBuff> rewardBuffs = new List<RewardBuff>();
    public float BuffProb;
    
    [Header("Effect")]
    public List<RewardEffect> rewardEffects = new List<RewardEffect>();
    public float EffectProb;

    public bool m_ISCheckChange = false;

#if UNITY_EDITOR
    // 인스펙터에서 값 바뀔 때 자동 호출
    public void Init()
    {
        NormalizeProbabilities(rewardCards, "RewardCard");
        NormalizeProbabilities(rewardBuffs, "RewardBuff");
        NormalizeProbabilities(rewardEffects, "RewardEffect");
    }

    private void OnApplicationQuit()
    {

    }

    private void NormalizeProbabilities<T>(List<T> list, string label) where T : BaseRewardItem
    {
        if (list == null || list.Count == 0) return;
        
        float sum = 0f;
        foreach (var item in list) sum += item.Probability;
        
        if (sum <= 0f)
        {
            Debug.LogWarning($"[{label}] 확률 값이 전부 0입니다. 정규화가 스킵됩니다.");
            return;
        }
        
        for (int i = 0; i < list.Count; i++)
            list[i].Probability /= sum;
    }
#endif
}

[System.Serializable]
public class BaseRewardItem
{
    public float Probability;
}

[System.Serializable]
public class RewardCard : BaseRewardItem
{
    //public string cardId;
    public GameEntity m_GameEntity;
}

[System.Serializable]
public class RewardBuff : BaseRewardItem
{
    public string buffId;
}

[System.Serializable]
public class RewardEffect : BaseRewardItem
{
    public string effectId;
}
#endregion
