using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class ASM_Controllable : StateMachineBehaviour
{
    ControllableObject m_ControllableObject;
    ControllableObjectCombatManager m_ControllableObjectCombatManager;

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (Managers.Scene.CurrentScene.SceneType != Define.Scene.Game)
            return;

        // 캐싱: 없으면 GetComponentInParent로 가져오기
        if (m_ControllableObject == null)
            m_ControllableObject = animator.GetComponentInParent<ControllableObject>();

        if (m_ControllableObjectCombatManager == null)
            m_ControllableObjectCombatManager = animator.GetComponentInParent<ControllableObjectCombatManager>();

        if (stateInfo.IsName("Attack"))
        {
            m_ControllableObject.GetAction<CombatAction>().OnEndAttackEventInvoke();
            m_ControllableObject.GetAnimationsManager()[0].AnimatonSpeedRestoreOriginalSpeed();
        }
        else if (stateInfo.IsName("AttackReadyFail"))
        {
            m_ControllableObjectCombatManager.AttackReadyFailEnd();
        }
        else if (stateInfo.IsName("Spawn"))
        {
            m_ControllableObject.SpawnComplete();
        }
        else if (stateInfo.IsName("DeSpawn"))
        {
            m_ControllableObject.DeSpawnComplete();
        }
        else if (stateInfo.IsName("Death"))
        {
            if(m_ControllableObject.m_IsDirectDesawnAtDeath)
            {
                m_ControllableObject.DeSpawnStart();
            }
        }
    }
}
