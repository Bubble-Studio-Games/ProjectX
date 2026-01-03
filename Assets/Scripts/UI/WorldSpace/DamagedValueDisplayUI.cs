using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using static Define;
using Random = UnityEngine.Random;

public class DamagedValueDisplayUI : MonoBehaviour
{
    public E_DamagedValueTextDisplayType m_EDamagedValueTextDisplayType;

    GameEntity m_GameEntity;
    AttributeSystem StatSystem;

    [SerializeField] TextMeshProUGUI m_DamageValuePrefab;

    [Header("Up")]
    [SerializeField] float m_fUpHeight = 0.3f;

    [Header("Bounds")]
    [SerializeField] float duration1 = 0.3f;
    [SerializeField] float duration2 = 0.25f;
    [SerializeField] float duration3 = 0.2f;

    [SerializeField] float height1 = 1.0f;
    [SerializeField] float height2 = 0.5f;

    [Header("Heal Animation Settings")]
    [SerializeField] float m_fHealUpHeight = 0.5f;
    [SerializeField] float m_fHealDuration = 1.0f;
    private float m_fHealMaxScale = 0.4f;

    // Start is called before the first frame update
    void Awake()
    {
        m_GameEntity = GetComponentInParent<GameEntity>();
        StatSystem = GetComponentInParent<AttributeSystem>();
    }

    private void Start()
    {
    }

    private void OnEnable()
    {
        StatSystem.OnStatDelta += DisplayStatDelta;

        foreach (Transform child in transform)
        {
            Managers.Resource.Destroy(child.gameObject);
        }
    }

    private void OnDisable()
    {
        StatSystem.OnStatDelta -= DisplayStatDelta;
    }

    private void DisplayStatDelta(OnStatDeltaEventArgs e)
    {
        // 0 데미지 + HitDecision 있으면 Miss/Evasion/Counter 텍스트
        if (e.Amount <= 0 && 
            e.HitDecision != E_HitDecisionType.Hit && 
            e.HitDecision != E_HitDecisionType.CriticalHit)
        {
            DisplayTextOnly(e);
            return;
        }

        // 0 회복은 표시 안 함
        //if (e.Amount <= 0)
        //    return;

        string text = BuildText(e);
        Color color = ResolveColor(e);

        var prefab = Managers.Resource.Instantiate<TextMeshProUGUI>(
            m_DamageValuePrefab.gameObject, transform);

        prefab.text = text;
        prefab.color = color;

        var col = m_GameEntity.m_HitCollider;

        // 표시 타입 랜덤 보정
        if (m_EDamagedValueTextDisplayType == E_DamagedValueTextDisplayType.None)
        {
            m_EDamagedValueTextDisplayType =
                Random.value > 0.5f
                ? E_DamagedValueTextDisplayType.Up
                : E_DamagedValueTextDisplayType.MiddleBounce;
        }

        // TODO 나중에 더 개선해야 됨
        if (e.Sign == E_StatDeltaSign.Plus)
        {
            PlayHealStyle(prefab, col);
        }
        else
        {
            PlayDamageStyle(prefab, col, e);
        }
    }

    private string BuildText(OnStatDeltaEventArgs e)
    {
        if (e.Sign == E_StatDeltaSign.Plus)
            return $"+{e.Amount}";

        return e.Amount.ToString();
    }

    private Color ResolveColor(OnStatDeltaEventArgs e)
    {
        // 힐 계열
        if (e.Sign == E_StatDeltaSign.Plus)
        {
            return e.Cause == E_StatDeltaCause.LifeSteal
                ? Util.ColorUtil.GetHeal() * 0.9f
                : Util.ColorUtil.GetHeal();
        }

        // 데미지 계열
        return e.HitDecision switch
        {
            E_HitDecisionType.CriticalHit => Util.ColorUtil.GetCriticalHit(),
            E_HitDecisionType.AttackMiss => Util.ColorUtil.GetMissOrEvasion(),
            E_HitDecisionType.Evasion => Util.ColorUtil.GetMissOrEvasion(),
            E_HitDecisionType.Counter => Util.ColorUtil.GetMissOrEvasion(),
            _ => Util.ColorUtil.GetNormalDamage(),
        };
    }

    private void DisplayTextOnly(OnStatDeltaEventArgs e)
    {
        string text = e.HitDecision switch
        {
            E_HitDecisionType.AttackMiss => "Miss",
            E_HitDecisionType.Evasion => "Evasion",
            E_HitDecisionType.Counter => "Counter",
            _ => null
        };

        if (string.IsNullOrEmpty(text))
            return;

        var prefab = Managers.Resource.Instantiate<TextMeshProUGUI>(
            m_DamageValuePrefab.gameObject, transform);

        prefab.text = text;
        prefab.color = Util.ColorUtil.GetMissOrEvasion();

        var col = m_GameEntity.m_HitCollider;
        Vector3 start = col.bounds.center;

        prefab.transform.position = start;
        StartCoroutine(PlayUp(start, prefab.gameObject));
    }

    private void PlayDamageStyle(TextMeshProUGUI prefab, Collider col, OnStatDeltaEventArgs e)
    {
        if (m_EDamagedValueTextDisplayType == E_DamagedValueTextDisplayType.Up)
        {
            Vector3 start = GetRandomTopPosition(col);
            prefab.transform.position = start;
            StartCoroutine(PlayUp(start, prefab.gameObject));
        }
        else
        {
            Vector3 start = col.bounds.center;
            Vector3 dir = -(start - e.Source.transform.position).normalized;
            Vector3 bounceStart = start + dir * 0.5f + Vector3.up * 0.5f;

            prefab.transform.position = bounceStart;
            StartCoroutine(PlayBounceSequence(bounceStart, prefab.gameObject));
        }
    }

    private void PlayHealStyle(TextMeshProUGUI prefab, Collider col)
    {
        Vector3 start = GetRandomTopPosition(col);
        prefab.transform.position = start;
        StartCoroutine(PlayHealUp(start, prefab.gameObject));
    }

    private static Vector3 GetRandomTopPosition(Collider col)
    {
        float minX = col.bounds.min.x;
        float maxX = col.bounds.max.x;
        float maxY = col.bounds.max.y;
        float minZ = col.bounds.min.z;
        float maxZ = col.bounds.max.z;

        return new Vector3(
            Random.Range(minX, maxX),
            maxY,
            Random.Range(minZ, maxZ)
        );
    }

    private IEnumerator PlayUp(Vector3 start, GameObject prefab)
    {
        DG.Tweening.Sequence seq = DOTween.Sequence();
        seq.Append(prefab.transform.DOMoveY(start.y + m_fUpHeight, duration1));
        yield return seq.WaitForCompletion();
        Managers.Resource.Destroy(prefab);
    }

    private IEnumerator PlayBounceSequence(Vector3 start, GameObject prefab)
    {
        float groundY = m_GameEntity.m_HitCollider.bounds.min.y + 0.1f; // 거의 바닥

        DG.Tweening.Sequence seq = DOTween.Sequence();

        seq.Append(prefab.transform.DOMoveY(groundY, duration1).SetEase(Ease.InQuad));           // 1차 낙하
        seq.Append(prefab.transform.DOMoveY(groundY + height1, duration1).SetEase(Ease.OutQuad)); // 1차 반등
        seq.Append(prefab.transform.DOMoveY(groundY, duration2).SetEase(Ease.InQuad));            // 2차 낙하
        seq.Append(prefab.transform.DOMoveY(groundY + height2, duration2).SetEase(Ease.OutQuad)); // 2차 반등
        seq.Append(prefab.transform.DOMoveY(groundY, duration3).SetEase(Ease.InQuad));            // 최종 낙하

        yield return seq.WaitForCompletion();

        Managers.Resource.Destroy(prefab);
    }

    private IEnumerator PlayHealUp(Vector3 start, GameObject prefab)
    {
        var textMesh = prefab.GetComponent<TextMeshProUGUI>();

        Sequence seq = DOTween.Sequence();

        seq.Append(prefab.transform.DOMoveY(start.y + m_fHealUpHeight, m_fHealDuration));
        seq.Join(prefab.transform.DOScale(m_fHealMaxScale, m_fHealDuration * 0.5f).SetEase(Ease.OutQuad));
        seq.Append(prefab.transform.DOScale(m_fHealMaxScale * 1.1f, m_fHealDuration * 0.5f).SetEase(Ease.InQuad));
        seq.Join(textMesh.DOFade(0f, m_fHealDuration));
        yield return seq.WaitForCompletion();

        Managers.Resource.Destroy(prefab);
    }

}
