using UnityEngine;

namespace Assets.Scripts.Data
{
    [CreateAssetMenu(fileName = "SoundData", menuName = "ScriptableObjects/SoundData")]
    public class SoundData : ScriptableObject
    {
        // Select
        // 액션을 선택했을 때
        [Header("Select")]
        public AudioClip m_SelectUnitAudioClip;
        public AudioClip m_SelectAction_CommandMoveAudioClip;
        public AudioClip m_SelectAction_CommandAttackAudioClip;

        // Command
        // 선택한 액션을 유닛에게 명령 했을 때
        [Header("Command")]
        public AudioClip m_CommandAction_CommandMoveAudioClip;
        public AudioClip m_CommandAction_CommandAttackAudioClip;

    }
}
