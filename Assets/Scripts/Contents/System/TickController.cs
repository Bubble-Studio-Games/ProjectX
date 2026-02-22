using System.Collections;
using UnityEngine;

// 게임 내 틱을 관리
[EditorShowInfo("플레이어의 공격, 이동, 버프의 타이밍 등의 틱을 관리")]
public class TickController : MonoBehaviour
{
    [Header("Check Timer")]
    public float TickIntervalTime = 0.5f;

    private void Start()
    {
        // 코루틴 시작
        StartCoroutine(ActionTickCoroutine());
    }

    private IEnumerator ActionTickCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(TickIntervalTime);

            // 지정 위치 전달
            Managers.Tick.Tick();
        }
    }
}
