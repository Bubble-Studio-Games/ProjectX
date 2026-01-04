using UnityEngine;
using UnityEngine.UI;

public class UI_Popup : UI_Base
{
    [SerializeField] private RectTransform _barrier;

    private void OnValidate()
    {
        if (_barrier == null)
        {
            var barrierTransform = transform.Find("Barrier");
            if (barrierTransform != null)
                _barrier = barrierTransform as RectTransform;
        }
    }

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        Managers.UI.SetCanvas(gameObject, true);
        gameObject.GetOrAddComponent<GraphicRaycaster>();

        return true;
    }

    public void SetBarrierActive(bool active)
    {
        if (_barrier != null)
            _barrier.gameObject.SetActive(active);
    }

    public virtual void ClosePopupUI()
    {
        Managers.UI.ClosePopupUI(this);
    }
}
