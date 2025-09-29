using RootMotion.FinalIK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


[RequireComponent(typeof(UnitRagdoll), typeof(FullBodyBipedIK), typeof(BodyTilt))]
public class UnitAnimationManager : ControllableObjectAnimator
{
    Unit m_Unit;
    BodyTilt m_BodyTilt;
    FullBodyBipedIK m_FullBodyBipedIK;

    protected override void Awake()
    {
        base.Awake();
        m_Unit = GetComponentInParent<Unit>();
        m_BodyTilt = GetComponent<BodyTilt>();
        m_FullBodyBipedIK = GetComponent<FullBodyBipedIK>();
    }

    public void EnableTwoHandIK()
    {

    }

    public void DisEnableTwoHandIK()
    {

    }


    public virtual void SetHandIKForWeapon(RightHandIKTarget rightHandTarget, LeftHandIKTarget leftHandTarget, bool isTwoHandingWeapon)
    {
        // 두 손의 경우 왼 손 무기는 집어 넣고, 오른 손 무기를 두 손으로 잡기
        if(isTwoHandingWeapon)
        {
            if (rightHandTarget != null)
            {
                m_FullBodyBipedIK.solver.rightHandEffector.target = rightHandTarget.transform;
                m_FullBodyBipedIK.solver.rightHandEffector.positionWeight = 1;
                m_FullBodyBipedIK.solver.rightHandEffector.rotationWeight = 1;
            }

            if (leftHandTarget != null)
            {
                m_FullBodyBipedIK.solver.leftHandEffector.target = leftHandTarget.transform;
                m_FullBodyBipedIK.solver.leftHandEffector.positionWeight = 1;
                m_FullBodyBipedIK.solver.leftHandEffector.rotationWeight = 1;
            }

            if(rightHandTarget != null && leftHandTarget != null)
            {
                m_FullBodyBipedIK.solver.spineMapping.twistWeight = 1;
            }
        }
        else
        {
            m_FullBodyBipedIK.solver.rightHandEffector.target = null;
            m_FullBodyBipedIK.solver.leftHandEffector.target = null;
        }
    }
}
