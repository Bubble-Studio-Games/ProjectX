using DG.Tweening;
using RootMotion.FinalIK;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class SetupAnimation : MonoBehaviour
{
    GameEntity gameEntity;

    [SerializeField] float upHeight = 0.2f;
    [SerializeField] float upDuration = 0.2f;
    [SerializeField] float downDuration = 0.5f;

    private void Awake()
    {
        gameEntity = GetComponent<GameEntity>();
    }

    public IEnumerator PlacedSpawnAnimation()
    {
        float startY = transform.position.y;

        DG.Tweening.Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOMoveY(startY+upHeight, upDuration).SetEase(Ease.OutBounce));           // 1차 상승
        seq.Append(transform.DOMoveY(startY -BuildingGhost.Instance.floatingHeight, downDuration).SetEase(Ease.OutBounce));           // 1차 낙하

        yield return seq.WaitForCompletion();

        gameEntity.SpawnStart();
    }

}
