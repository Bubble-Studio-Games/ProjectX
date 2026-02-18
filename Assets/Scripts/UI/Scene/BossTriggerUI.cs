using UnityEngine;
using Data;

/// <summary>
/// 보스 테스트용 UI
/// </summary>
public class BossTriggerUI : UI_Base
{
    private enum Buttons { SpawnBossButton, SuccessBossButton, FailBossButton }

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        BindButton(typeof(Buttons));
        GetButton((int)Buttons.SpawnBossButton).onClick.AddListener(OnSpawnBossClicked);
        GetButton((int)Buttons.SuccessBossButton).onClick.AddListener(OnSuccessBossClicked);
        GetButton((int)Buttons.FailBossButton).onClick.AddListener(OnFailBossClicked);
        return true;
    }

    public void OnSpawnBossClicked()
    {
        var bossSystem = Managers.SceneServices.Get<BossSystem>();
        if (bossSystem == null)
            return;

        bossSystem.StartClosestBossRoom();
    }

    public void OnSuccessBossClicked()
    {
        var bossSystem = Managers.SceneServices.Get<BossSystem>();
        if (bossSystem == null)
        {
            Debug.LogWarning("[BossTriggerUI] BossSystem을 찾을 수 없습니다");
            return;
        }

        if (bossSystem.IsBossActive == false)
        {
            Debug.LogWarning("[BossTriggerUI] 활성화된 보스가 없습니다");
            return;
        }

        var activeBossRoom = bossSystem.ActiveBossRoom;
        if (activeBossRoom == null)
        {
            Debug.LogWarning("[BossTriggerUI] ActiveBossRoom이 없습니다");
            return;
        }

        var currentBoss = activeBossRoom.GetCurrentBoss();
        if (currentBoss == null)
        {
            Debug.LogWarning("[BossTriggerUI] CurrentBoss가 없습니다");
            return;
        }

        currentBoss.SetHealthToZero();
    }

    public void OnFailBossClicked()
    {
        var bossSystem = Managers.SceneServices.Get<BossSystem>();
        if (bossSystem == null)
        {
            Debug.LogWarning("[BossTriggerUI] BossSystem을 찾을 수 없습니다");
            return;
        }

        if (bossSystem.IsBossActive == false)
        {
            Debug.LogWarning("[BossTriggerUI] 활성화된 보스가 없습니다");
            return;
        }

        var activeBossRoom = bossSystem.ActiveBossRoom;
        if (activeBossRoom == null)
        {
            Debug.LogWarning("[BossTriggerUI] ActiveBossRoom이 없습니다");
            return;
        }

        var currentBoss = activeBossRoom.GetCurrentBoss();
        if (currentBoss == null)
        {
            Debug.LogWarning("[BossTriggerUI] CurrentBoss가 없습니다");
            return;
        }

        currentBoss.SetHealthToZero();
    }
}
