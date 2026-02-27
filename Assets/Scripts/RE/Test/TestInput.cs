using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestInput : MonoBehaviour
{
    private ActionController controller;
    [SerializeField] Animator aaa;
    private void Awake()
    {
        controller = GetComponent<ActionController>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out var hit, 200f))
            {
                var grid = Managers.SceneServices.Grid.GetGridPosition(hit.point);
                //controller.RequestMove(grid);//controller.SetAction(new RE_MoveAction(ctx, grid));
            }
        }

        //if (controller.Current == null)controller.RequestIdle();

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            //Debug.Log($"[AnimView] Animator={aaa.name} Controller={aaa.runtimeAnimatorController?.name} Enabled={aaa.enabled}");
            //aaa.Play("Attack");
            controller.BeTriggered(TriggerActionType.Attack);
        }

    }
}
