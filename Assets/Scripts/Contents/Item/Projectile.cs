using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Poolable), typeof(Rigidbody))]
public class Projectile : Item
{
    public AudioSource m_AudioSource { get; private set; }
    public Rigidbody m_Rigidbody { get; private set; }
    private Collider m_Collider;

    [Header("Destroy")]
    public bool m_hasDestoryAnimation;
    private ControllableObject m_Owner;
    private AttackPattern m_AttackPattern;

    [Header("Fly")]
    [SerializeField] private AudioClip m_ProjectileFlyingAudioClip;

    [Header("Hit")]
    [SerializeField] private AudioClip m_ProjectileHitAudioClip;
    [SerializeField] private GameObject m_AfterProjectileHitPrefab;

    public override void Awake()
    {
        base.Awake();

        m_AudioSource = GetComponent<AudioSource>();
        m_Rigidbody = GetComponent<Rigidbody>();
        m_Collider = GetComponent<Collider>();

        m_AudioSource.spatialBlend = 1f;
        m_AudioSource.maxDistance = 40f;
    }

    public override void OnEnable()
    {
        base.OnEnable();

        m_AudioSource.clip = null;
        m_AudioSource.playOnAwake = false;

        // Rigidbody 초기화 및  콜라이더 끄기
        m_Rigidbody.isKinematic = false;
        m_Rigidbody.velocity = Vector3.zero;
        m_Rigidbody.angularVelocity = Vector3.zero;
        m_Collider.enabled = false;

        foreach (Transform child in transform)
            child.gameObject.SetActive(true);
    }

    public override void OnDisable()
    {
        base.OnDisable();

        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(true);
        }
        m_Rigidbody.isKinematic = false; // 필요시
    }

    public override void Destroy()
    {
        if (m_hasDestoryAnimation)
        {
            animator.CrossFade("Destroy", 0.2f);
            StartCoroutine(ObjectDestroy());
        }
        else
        {
            foreach (Transform child in transform)
                child.gameObject.SetActive(false);

            StartCoroutine(ObjectDestroy());
        }
    }

    public void AttackReady(ControllableObject owner, AttackPattern attack)
    {
        foreach (Transform child in transform)
            child.gameObject.SetActive(true);

        // Audio
        if (m_ProjectileFlyingAudioClip != null)
            m_AudioSource.PlayOneShot(m_ProjectileFlyingAudioClip);

        // 콜라이더 켜기
        m_Collider.enabled = true;
        
        m_Owner = owner;
        m_AttackPattern = attack;
    }

    private void HitEffect(Vector3 hitPos)
    {
        if (m_ProjectileHitAudioClip != null)
            m_AudioSource.PlayOneShot(m_ProjectileHitAudioClip);

        if (m_AfterProjectileHitPrefab != null)
        {
            GameObject go = Managers.Resource.Instantiate(m_AfterProjectileHitPrefab);
            go.transform.position = hitPos;
            go.transform.rotation = Quaternion.identity;
        }

        // Kinematic이 아닐 때만 velocity 설정
        if (m_Rigidbody.isKinematic == false)
        {
            m_Rigidbody.velocity = Vector3.zero;
            m_Rigidbody.angularVelocity = Vector3.zero;
        }
        
        m_Rigidbody.isKinematic = true; // 필요시
    }

    private void OnCollisionEnter(Collision col)
    {
        if (((1 << col.gameObject.layer) & LayerManager.Instance.HitColLayerMask) != 0)
        {
            // 목표 타겟

            // 적에게 부딪혔거나 지형 지물에 부딪혔을 경우에 한하여
            ControllableObject target = col.gameObject.GetComponentInParent<ControllableObject>();
            if (target != null && m_Owner.IsEnemy(target))
            {
                // 타격 처리
                target.m_AttributeSystem.Hit(m_AttackPattern, m_Owner);
                HitEffect(col.contacts[0].point);

                Destroy();
            }
        }

        // 일반 사물
        if (((1 << col.gameObject.layer) & LayerManager.Instance.m_StructLayer) != 0)
        {
            HitEffect(col.contacts[0].point);

            Destroy();
        }
    }
}
