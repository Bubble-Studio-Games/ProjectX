using UnityEngine;
using static Define;

public class TriggerConverter : MonoBehaviour
{
	private ITriggerTarget _colliderTrigger;

	private void Awake()
	{
		_colliderTrigger = GetComponentInParent<ITriggerTarget>();

		if (_colliderTrigger == null)
			enabled = false;
	}

	private void OnDestroy()
	{
		_colliderTrigger = null;
	}

    private void OnTriggerEnter(Collider other)
    {
        _colliderTrigger?.OnTriggerEnterAction(other);
    }

    // private void OnTriggerStay(Collider other)
    // {
    // 	_colliderTrigger?.OnTriggerStayAction(other);
    // }

    // private void OnTriggerExit(Collider other)
    // {
    // 	_colliderTrigger?.OnTriggerExitAction(other);
    // }
}
