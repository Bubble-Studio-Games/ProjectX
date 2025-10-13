using System.Collections.Generic;
using UnityEngine;
using static BuffManager;



//-------------------------------------------------------------------
public class Buff : IBuff
{
    #region Fields
    private float _elapsed;  // 경과
    private float _tickTimer;
    #endregion

    public IBuffSender Sender { get; private set; }
    public BuffData Data { get; private set; }
    public bool IsExpired { get; set; }

    public Buff(BuffData data)
    {
        Data = data;
        IsExpired = false;
        _tickTimer = 0f;
        _elapsed = 0f;

        Debug.Log("Buff 객체 생성되었음");
    }
    public void SetOwner(IBuffSender owner)
    {
        if (Sender != null) return;
        Sender = owner;
    }
    public virtual void Update(float deltaTime)
    {
        if (IsExpired) return;
        if (Data.Duration < 0) return;  // 무한정 유지

        deltaTime *= Time.timeScale;
        _elapsed += deltaTime;
        _tickTimer += deltaTime;

        // 틱 간격이 지났다면 틱 발동
        if (_tickTimer >= Data.TickInterval)
        {
            // 처리 코드 들어가면 됌
            _tickTimer -= Data.TickInterval;
        }

        // 지속 시간 만료
        if (_elapsed >= Data.Duration)
        {
            IsExpired = true;
        }
    }
    public virtual void Reset(BuffData data)
    {
        Data = data;
        IsExpired = false;
        _tickTimer = 0f;
        _elapsed = 0f;
    }
}
public class PeriodicBuff : IBuff
{
    #region Fields
    private float _elapsed;  // 경과
    private float _tickTimer;
    #endregion
    
    #region Properties
    public IBuffSender Sender { get; private set; }
    public BuffData Data { get; private set; }
    public bool IsExpired { get; set; } // 만료 여부

    #endregion

    #region Methods
    public PeriodicBuff(BuffData data)
    {
        Data = data;
        IsExpired = false;
        _tickTimer = 0f;
        _elapsed = 0f;

        Debug.Log("PeriodicBuff 객체 생성되었음");
    }
    public void SetOwner(IBuffSender owner)
    {
        if (Sender != null) return;
        Sender = owner;
    }
    public virtual void Update(float deltaTime)
    {
        if (IsExpired) return;
        if (Data.Duration < 0) return;  // 무한정 유지

        deltaTime *= Time.timeScale;
        _elapsed += deltaTime;
        _tickTimer += deltaTime;

        // 틱 간격이 지났다면 틱 발동
        if (_tickTimer >= Data.TickInterval)
        {
            // 처리 코드 들어가면 됌
            _tickTimer -= Data.TickInterval;
        }

        // 지속 시간 만료
        if (_elapsed >= Data.Duration)
        {
            IsExpired = true;
        }
    }
    public virtual void Reset(BuffData data)
    {
        Data = data;
        IsExpired = false;
        _tickTimer = 0f;
        _elapsed = 0f;
    }
    

    #endregion
}

//-------------------------------------------------------------------
public class DeBuff : Buff
{
    public DeBuff(BuffData buffData) : base(buffData){}
}
public class CustomBuff : Buff
{
    public CustomBuff(BuffData buffData) : base(buffData){}
}
public class PeriodicDeBuff : PeriodicBuff
{
    public PeriodicDeBuff(BuffData buffData) : base(buffData){}
}
public class PeriodicCustomBuff : PeriodicBuff
{
    public PeriodicCustomBuff(BuffData buffData) : base(buffData){}
}
