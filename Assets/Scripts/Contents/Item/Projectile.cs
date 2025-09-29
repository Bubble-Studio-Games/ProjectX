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

        // 콜라이더 끄기
        m_Collider.enabled = false;
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
            // 자식들 모두 setactieve false로 바꾸기
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(false);
            }

            StartCoroutine(ObjectDestroy());
        }
    }

    public void AttackReady(ControllableObject owner, AttackPattern attack)
    {
        // Audio
        m_AudioSource.PlayOneShot(m_ProjectileFlyingAudioClip);

        // 콜라이더 켜기
        m_Collider.enabled = true;
        m_Owner = owner;
        m_AttackPattern = attack;
    }

    private void HitEffect(Vector3 hitPos)
    {
        m_AudioSource.PlayOneShot(m_ProjectileHitAudioClip);
        if (m_AfterProjectileHitPrefab != null)
        {
            GameObject go = Managers.Resource.Instantiate(m_AfterProjectileHitPrefab);
            go.transform.position = hitPos;
            go.transform.rotation = Quaternion.identity;
        }

        m_Rigidbody.velocity = Vector3.zero;
        m_Rigidbody.angularVelocity = Vector3.zero;
        m_Rigidbody.isKinematic = true; // 필요시
    }

    private void OnCollisionEnter(Collision col)
    {
        // 일반 사물
        if (((1 << col.gameObject.layer) & LayerManager.Instance.m_StructLayer) != 0)
        {
            HitEffect(col.contacts[0].point);

            Destroy();
        }
        else if (((1 << col.gameObject.layer) & LayerManager.Instance.HitColLayerMask) != 0)
        {
            // 목표 타겟

            // 적에게 부딪혔거나 지형 지물에 부딪혔을 경우에 한하여
            ControllableObject target = col.gameObject.GetComponentInParent<ControllableObject>();
            if (target != null && m_Owner.IsEnemy(target))
            {
                // 타격 처리
                target.m_StatSystem.Hit(m_AttackPattern, m_Owner);
                HitEffect(col.contacts[0].point);

                Destroy();
            }
        }
    }
}
