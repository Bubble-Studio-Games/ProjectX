using UnityEngine;
using static Define;

[CreateAssetMenu(menuName = "Stat/NPC Stat")]
public class NPCStat : BaseStat
{
    [Header("NPC Identification")]
    public string NPC_ID;
    public E_NPC NPCType = E_NPC.None;

    [Header("NPC State & Relationship")]
    public E_NPCState InitialState = E_NPCState.Neutral;

    [Header("NPC Movement")]
    [Tooltip("던전 코어를 향해 자동으로 이동할지 여부")]
    public bool MoveTowardsDungeonCore = false;

    [Tooltip("목표 도달 후 원래 위치로 복귀할지 여부 (상점 NPC 등)")]
    public bool ReturnToSpawnAfterGoal = false;

    [Header("NPC Detection & Interaction")]
    [Tooltip("NPC 감지 범위 (m)")]
    [Range(0, 10)]
    public int DetectionRange = 5;

    [Tooltip("상호작용 범위 (m)")]
    [Range(0, 10)]
    public int InteractionRange = 2;

    [Header("NPC Timers")]
    [Tooltip("재소환 대기 시간 (초)")]
    public float RespawnTime = 300f;

    [Tooltip("상점 운영 시간 (초, 상점 NPC 전용)")]
    public float ShopDuration = 180f;

    [Header("NPC Reputation")]
    [Range(0, 100)]
    [Tooltip("NPC 초기 우호도")]
    public int ReputationValue = 0;

    [Header("Dialogue & Content")]
    [Tooltip("대화 스크립트 파일 경로")]
    public string DialogueScriptPath;
}
