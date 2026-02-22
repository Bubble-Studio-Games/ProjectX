using UnityEngine;
using CodeMonkey.Utils;
using static Define;

[EditorShowInfo("클릭 효과 주기")]
public class CommandActionClickEffectPresenter : MonoBehaviour
{
    [Header("Click Effect")]
    [SerializeField] private Transform worldUITransform;
    [SerializeField] private GameObject commandActionAtGridEffectPrefab;
    [SerializeField] private float defaultHeight = 4f;

    private GameObject _activeEffect;

    private void OnEnable()
    {
        Managers.Command.OnCommandAction += HandleCommandAction;
    }

    private void OnDisable()
    {
         Managers.Command.OnCommandAction -= HandleCommandAction;
    }

    private void HandleCommandAction(CommandManager.OnCommandActionEventArgs e)
    {
        // 안전장치
        if (commandActionAtGridEffectPrefab == null || worldUITransform == null)
            return;

        float height = defaultHeight;

        // 공격이면 대상 높이에 맞춰 올려주기
        if (e.action == typeof(CommandAttackAction))
        {
            var target = Managers.Grid.GetUnitAt(e.GridPosition);
            if (target == null) return;

            height += target.m_HitCollider.bounds.max.y;
        }

        // 기존 이펙트 제거
        if (_activeEffect != null)
            Managers.Resource.Destroy(_activeEffect);

        // 이펙트 생성
        _activeEffect = Managers.Resource.Instantiate(commandActionAtGridEffectPrefab, worldUITransform);
        _activeEffect.transform.position = Managers.Grid.GetWorldPosition(e.GridPosition) + new Vector3(0, height, 0);

        // 5초 후 제거
        FunctionTimer.Create(() =>
        {
            if (_activeEffect != null)
                Managers.Resource.Destroy(_activeEffect.gameObject);
        }, 5f);
    }
}
