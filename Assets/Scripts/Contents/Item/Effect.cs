using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Poolable))]
public class Effect : MonoBehaviour
{
    [SerializeField] float m_destroyTimeDelay = 3f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(Destory());
    }

    private IEnumerator Destory()
    {
        yield return new WaitForSeconds(m_destroyTimeDelay);
        Managers.Resource.Destroy(gameObject);
    }
}
