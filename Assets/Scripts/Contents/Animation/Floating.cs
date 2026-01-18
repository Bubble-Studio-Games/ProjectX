using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class Floating : MonoBehaviour
{
    public bool m_IsHitStop = true;
    public bool m_IsRotate = false;
    private bool m_IsHit;

    [Header("Settings")]
    public float m_RotateSpeed = 50f;
    public float m_MoveAmplitude = 0.2f;
    public float m_MoveFrequency = 2f;
    private Vector3 m_OriginalPos;
    public float m_HitStunDuration = 1f;

    // 위상 (sin 계산 기준)
    private float phase;

    Coroutine m_Coroutine;
    GameEntity m_GameEntity;

    private void Hit(OnAttackInfoEventArgs e)
    {
        if (m_Coroutine != null)
            StopCoroutine(m_Coroutine);
        m_Coroutine = StartCoroutine(IOnHitEffect());
    }

    private IEnumerator IOnHitEffect()
    {
        m_IsHit = true;

        yield return new WaitForSeconds(m_HitStunDuration);

        m_IsHit = false;
    }

    private void Awake()
    {
        m_GameEntity = GetComponentInParent<GameEntity>();
    }

    private void Start()
    {
        m_OriginalPos = transform.localPosition;
    }

    private void OnEnable()
    {
        m_GameEntity.OnDamaged += Hit;
        m_GameEntity.OnDead += HitTrue;
        m_GameEntity.OnRevived += HitFalse;
    }

    private void OnDisable()
    {

        m_GameEntity.OnDamaged -= Hit;
        m_GameEntity.OnDead -= HitTrue;
        m_GameEntity.OnRevived -= HitFalse;
    }

    private void LateUpdate()
    {
        if (m_IsHitStop && !m_IsHit)
        {
            // 회전
            if(m_IsRotate)
                transform.Rotate(Vector3.up, m_RotateSpeed * Time.deltaTime);

            // 위상 업데이트
            phase += Time.deltaTime * m_MoveFrequency;

            // 위상 기반 이동
            float yOffset = Mathf.Sin(phase) * m_MoveAmplitude;
            transform.localPosition = m_OriginalPos + new Vector3(0, yOffset, 0);
        }
    }

    private void HitTrue(OnAttackInfoEventArgs e)
    {
        m_IsHit = true;
    }

    private void HitFalse()
    {
        m_IsHit = false;
    }
}
