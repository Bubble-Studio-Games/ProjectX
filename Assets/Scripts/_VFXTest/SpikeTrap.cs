using DG.Tweening;
using UnityEngine;

public class SpikeTrap : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float _upDistance = 2f;
    [SerializeField] private float _overShoot = 0.3f;
    [SerializeField] private float _upDuration = 0.15f;
    [SerializeField] private float _downDuration = 0.1f;

    private Vector3 _originalPosition;
    private bool _isInit = false;

    private void Awake()
    {
        InitPosition();
    }

    private void InitPosition()
    {
        if (_isInit == false)
        {
            _originalPosition = transform.localPosition;
            var hiddenPos = _originalPosition - Vector3.up * _upDistance;
            transform.localPosition = hiddenPos;
            _isInit = true;
        }
    }

    public void Shoot()
    {
        if (_isInit == false)
            InitPosition();

        var sequence = DOTween.Sequence();

        sequence.Append(
            transform.DOLocalMoveY(_originalPosition.y + _overShoot, _upDuration)
                .SetEase(Ease.OutQuad)
        );

        sequence.Append(
            transform.DOLocalMoveY(_originalPosition.y, _downDuration)
                .SetEase(Ease.InOutSine)
        );
    }
}
