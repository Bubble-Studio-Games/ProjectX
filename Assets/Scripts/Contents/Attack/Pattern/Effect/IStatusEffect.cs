using UnityEngine;

public interface IStatusEffect
{
    public GameEntity Target { get; }
    public GameEntity Source { get; }

    public void Init(GameEntity target, GameEntity source, AttackData data);

    public bool Tick(float deltaTime);
}
