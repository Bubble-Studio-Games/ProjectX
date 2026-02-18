using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Data;

public class BossSystem : MonoBehaviour
{
	private BossRoom _activeBossRoom;
	private List<BossRoom> _allBossRooms;

	public event Action<Boss.Data> OnBossStarted;
	public event Action<Boss.Data> OnBossDefeated;
	public event Action OnBossFailed;

	public bool IsBossActive => _activeBossRoom != null;
	public BossRoom ActiveBossRoom => _activeBossRoom;
	private BossUI _bossUI;
	public BossUI BossUI
	{
		get
		{
			if (_bossUI == null)
				_bossUI = FindAnyObjectByType<BossUI>();
			return _bossUI;
		}
	}

	private void Awake()
	{
		Managers.SceneServices.Register<BossSystem>(this);
		BossUI.StopFade();
		_allBossRooms = FindObjectsOfType<BossRoom>().ToList();
	}


	public void StartClosestBossRoom()
	{
		if (IsBossActive)
		{
			Debug.LogWarning("[BossSystem] 보스가 이미 활성화되어 있습니다");
			return;
		}

		if (_allBossRooms == null || _allBossRooms.Count <= 0)
		{
			Debug.LogError("[BossSystem] 사용 가능한 BossRoom이 없습니다");
			return;
		}

		var cameraPosition = Camera.main.transform.position;
		var closestBossRoom = _allBossRooms
			.OrderBy(room => Vector3.Distance(cameraPosition, room.transform.position))
			.First();

		closestBossRoom.TriggerBossRoom();
	}

	public void SetUpAndRun(BossRoom bossRoom, Action onCutsceneRequested = null)
	{
		_activeBossRoom = bossRoom;
		onCutsceneRequested?.Invoke();
		OnBossStarted?.Invoke(bossRoom.GetBossData());
	}

	public async void BossDefeated()
	{
		if (IsBossActive == false)
		{
			Debug.LogWarning("[BossSystem] 활성화된 보스가 없습니다");
			return;
		}

		OnBossDefeated?.Invoke(_activeBossRoom.GetBossData());
		await ShowRewardUIAsync();
		HandleBossDefeated();
	}

	public async void BossFailed()
	{
		if (IsBossActive == false)
		{
			Debug.LogWarning("[BossSystem] 활성화된 보스가 없습니다");
			return;
		}

		OnBossFailed?.Invoke();
		await ShowRewardUIAsync();
		HandleBossFailed();
	}

	public void SetActiveBoss(BossController bossController)
	{
		if (BossUI != null)
		{
			BossUI.gameObject.SetActive(true);
			BossUI.SetBossData(bossController.Data);
		}
	}

	/// <summary>
	/// 보스 엔티티가 사망했을 때 호출 - HP 0 이하 감지
	/// </summary>
	public async void OnBossEntityDead()
	{
		if (IsBossActive == false)
		{
			Debug.LogWarning("[BossSystem] 활성화된 보스가 없습니다");
			return;
		}

		OnBossDefeated?.Invoke(_activeBossRoom.GetBossData());
		await ShowRewardUIAsync();
		HandleBossDefeated();
	}

	private void HandleBossDefeated()
	{
		if (BossUI != null)
			BossUI.StopFade();

		ResetActiveBossRoom();
	}

	private void HandleBossFailed()
	{
		if (BossUI != null)
			BossUI.StopFade();

		ResetActiveBossRoom();
		Debug.Log("[BossSystem] 보스 전투 실패");
	}

	private void ResetActiveBossRoom()
	{
		if (_activeBossRoom != null)
		{
			_activeBossRoom.ResetBossRoom();
			_activeBossRoom = null;
		}
	}

	/// <summary>
	/// RewardUI를 비동기로 표시 - UI가 닫힐 때까지 대기
	/// </summary>
	private async Task ShowRewardUIAsync()
	{
		var tcs = new TaskCompletionSource<bool>();

		var rewardUI = Managers.UI.ShowPopupUI<RewardUI>();
		if (rewardUI == null)
		{
			Debug.LogError("[BossSystem] RewardUI를 생성하지 못했습니다");
			return;
		}

		// 테스트용으로 임시 QuestData 생성 (나중에 실제 보스 보상 데이터로 교체)
		var tempQuestData = new QuestData();

		// RewardUI가 닫힐 때 TCS 완료 처리
		rewardUI.OnClose = () =>
		{
			tcs.TrySetResult(true);
		};

		rewardUI.SetUp(tempQuestData);

		// RewardUI가 닫힐 때까지 대기
		await tcs.Task;
	}
}
