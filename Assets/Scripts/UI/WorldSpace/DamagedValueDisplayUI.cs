using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using static Define;
using Random = UnityEngine.Random;

public class DamagedValueDisplayUI : MonoBehaviour
{
    public E_DamagedValueTextDisplayType m_EDamagedValueTextDisplayType;

    GameEntity m_GameEntity;
    StatSystem StatSystem;

    [SerializeField] TextMeshProUGUI m_DamageValuePrefab;

    [Header("Up")]
    [SerializeField] float m_fUpHeight = 0.3f;

    [Header("Bounds")]
    [SerializeField] float duration1 = 0.3f;
    [SerializeField] float duration2 = 0.25f;
    [SerializeField] float duration3 = 0.2f;

    [SerializeField] float height1 = 1.0f;
    [SerializeField] float height2 = 0.5f;


    // Start is called before the first frame update
    void Awake()
    {
        m_GameEntity = GetComponentInParent<GameEntity>();
        StatSystem = GetComponentInParent<StatSystem>();

        StatSystem.OnDamaged += DisplayDamagedValueText;

        int rand = Random.Range(0, 2);
        if (rand % 2 == 0)
            m_EDamagedValueTextDisplayType = E_DamagedValueTextDisplayType.Up;
        else
            m_EDamagedValueTextDisplayType = E_DamagedValueTextDisplayType.MiddleBounce;
    }

    private void OnDestroy()
    {
        StatSystem.OnDamaged -= DisplayDamagedValueText;
    }

    private void OnEnable()
    {
        foreach (Transform child in transform)
        {
            Managers.Resource.Destroy(child.gameObject);
        }
    }

    private void DisplayDamagedValueText(object sender, StatSystem.OnAttackInfoEventArgs e)
    {
        string text = "";

        switch (e.EHitDeCisionType)
        {
            case E_HitDecisionType.Hit:
                text = e.FinalDamage.ToString();
                break;
            case E_HitDecisionType.CriticalHit:
                text = e.FinalDamage.ToString();
                // TODO 빨간 색으로
                break;
            case E_HitDecisionType.AttackMiss: // 시전자 쪽에서
                text = "Miss";
                break;
            case E_HitDecisionType.Evasion:
                text = "Evasion";
                break;
            case E_HitDecisionType.Counter:
                text = "Counter";
                break;
        }


        m_DamageValuePrefab.text = text;
        var prefab = Managers.Resource.Instantiate(m_DamageValuePrefab.gameObject, transform);
        var col = m_GameEntity.m_HitCollider;

        if (m_EDamagedValueTextDisplayType == E_DamagedValueTextDisplayType.Up)
        {
            float minX = col.bounds.min.x;
            float maxX = col.bounds.max.x;
            float maxY = col.bounds.max.y;
            float minZ = col.bounds.max.z;
            float maxZ = col.bounds.max.z;
            float centerY = col.bounds.center.y;

            Vector3 start = new Vector3(Random.Range(minX, maxX), maxY, Random.Range(minZ, maxZ));
            prefab.transform.position = start;

            StartCoroutine(PlayUp(start, prefab));
        }
        else if (m_EDamagedValueTextDisplayType == E_DamagedValueTextDisplayType.MiddleBounce)
        {

            Vector3 start = col.bounds.center;

            Vector3 attackDir = -(start - e.Attacker.transform.position).normalized;

            // 튀어나가는 방향 (앞 + 위 살짝)
            Vector3 initialOffset = attackDir * 0.5f + Vector3.up * 0.5f;

            Vector3 bounceStart = start + initialOffset;
            prefab.transform.position = bounceStart;

            StartCoroutine(PlayBounceSequence(bounceStart, prefab));
        }
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

}
