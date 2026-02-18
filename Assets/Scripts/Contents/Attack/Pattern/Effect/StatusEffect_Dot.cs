using UnityEngine;

/// <summary>
/// 지속 데미지 상태 효과 - 일정 시간 동안 반복적으로 대미지를 적용하는 런타임 상태 관리
/// </summary>
public class StatusEffect_Dot : IStatusEffect
{
	private GameEntity _target;
	private GameEntity _source;
	private AttackData_Dot _data;

	private int _remainingTicks;
	private float _tickElapsedTime;

	public GameEntity Target => _target;
	public GameEntity Source => _source;

	public void Init(GameEntity target, GameEntity source, AttackData data)
	{
		_target = target;
		_source = source;
		_data = data as AttackData_Dot;

		if (_data == null)
		{
			Debug.LogError("[StatusEffect_Dot] AttackData_Dot로 캐스트 실패");
			return;
		}

		_remainingTicks = _data.DotTickCount;
		_tickElapsedTime = 0f;
	}

	public bool Tick(float deltaTime)
	{
		if (_target == null)
			return false;

		_tickElapsedTime += deltaTime;

		while (_tickElapsedTime >= _data.DotTickInterval && _remainingTicks > 0)
		{
			ApplyDamage();
			_remainingTicks--;
			_tickElapsedTime -= _data.DotTickInterval;
		}

		return _remainingTicks > 0;
	}

	private void ApplyDamage()
	{
		if (_target != null && _target.m_AttributeSystem != null)
		{
			_target.m_AttributeSystem.Hit(_data, _source);
		}
	}
}
