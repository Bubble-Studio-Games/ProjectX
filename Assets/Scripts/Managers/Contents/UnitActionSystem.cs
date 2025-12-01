using CodeMonkey.Utils;
using System;
using System.Collections;
using UnityEngine;
using static Define;

public class UnitActionSystem : MonoBehaviour
{
    public static UnitActionSystem Instance { get; private set; }

    public event EventHandler OnUpdateActionTick;

    [Header("Check Timer")]
    public float checkInterval = 0.5f;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("There's more than one UnitActionSystem! " + transform + " - " + Instance);
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        // 코루틴 시작
        StartCoroutine(ActionTickCoroutine());
    }

    private void OnDestroy()
    {
        OnUpdateActionTick = null;
    }

    private IEnumerator ActionTickCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);

            // 지정 위치 전달
            OnUpdateActionTick?.Invoke(this, EventArgs.Empty);
        }
    }
}
