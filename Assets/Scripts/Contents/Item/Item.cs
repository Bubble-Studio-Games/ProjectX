using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource), typeof(Animator))]
public class Item : MonoBehaviour
{
    [Header("Ref")]
    protected Animator animator;

    public virtual void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    public virtual void OnEnable()
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(true);
        }
    }

    public virtual void OnDisable()
    {
    }

    public virtual void Destroy(float seconds = 3.0f)
    {
        // 모델은 숨기고
        // 사운드 때문에 이렇게 해 놓은거
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }

        StartCoroutine(ObjectDestroy(seconds));
    }

    protected IEnumerator ObjectDestroy(float seconds = 3.0f)
    {
        yield return new WaitForSeconds(seconds);
        Managers.Resource.Destroy(gameObject);
    }
}
