using UnityEngine;
using System;
using static Define;
using System.Linq;

public class CommandManager : IManager
{
    public void Init()
    {
    }

    public void Clear()
    {
    }

    public event Action<OnCommandActionEventArgs> OnSelectedActionChanged;
    public event Action<OnCommandActionEventArgs> OnCommandAction;
    public class OnCommandActionEventArgs : EventArgs
    {
        public GridPosition GridPosition;
        public Type action;
    }

    public BaseAction m_SelectAction { get; private set; }

    public void ClickSelectCommand()
    {
        var selectedUnits = Managers.Selection.SelectedUnits;
        if (selectedUnits.Count == 0)
            return;

        // 클릭 지점 & 대상Object 체크
        if (RaycastToWorld(out GameEntity target, out GridPosition gridPos))
        {

            if (target == null)
            {
                //Debug.Log($"커맨드 무브 {gridPos}");
                CommandMove(gridPos);
                return;
            }

            //Debug.Log($"대상 선택 {target.name}");
            switch (target.m_EObjectType)
            {
                case E_ObjectType.Unit:
                case E_ObjectType.Building:
                    if (target.m_TeamId == E_TeamId.Monster)
                        CommandAttack(target, gridPos);
                    break;

                case E_ObjectType.Interact:
                    CommandInteract(target);
                    break;

                default:
                    CommandMove(gridPos);
                    break;
            }
        }

        bool RaycastToWorld(out GameEntity obj, out GridPosition gp)
        {
            obj = null;

            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition),
                                out RaycastHit hit, GameConfig.Layer.HitColLayerMask))
            {
                obj = hit.collider.GetComponentInParent<GameEntity>();
            }

            gp = Util.Mouse.GetMouseWorldGridPosition(); 
            return true;
        }
    }

    public void CommandMove(GridPosition gridPos)
    {
        ExecuteCommand<CommandMoveAction>(gridPos);
    }

    public void CommandAttack(GameEntity target, GridPosition gridPos)
    {
        ExecuteCommand<CommandAttackAction>(gridPos);
    }

    public void CommandInteract(GameEntity target)
    {
        var pos = target.GetGridPosition();
        ExecuteCommand<CommandInteractAction>(pos);
    }

    private void ExecuteCommand<TAction>
        (GridPosition gridPosition)
        where TAction : BaseAction
    {
        // 조작 가능 유닛만 가져오기
        var selectedEntities = Managers.Selection.SelectedUnits.OfType<ControllableObject>();  // ISelectable -> GameEntity 로 필터링/캐스팅
        
        // ✔ 액션 가능 유닛만 가져오기
        var filtered = Util.FilterGameEntityHasAction<TAction, ControllableObject>(selectedEntities);

        if (filtered.Count() == 0)
            return;

        bool executedAny = false;

        foreach (var (unit, action) in filtered)
        {
            // ✔ 개별 유닛의 유효성만 체크하고 invalid면 skip
            if (!action.IsValidActionGridPosition(gridPosition))
                continue;

            executedAny = true;

            // ✔ 개별 유닛에 명령 실행
            unit.DirectCommand(action, gridPosition);
        }

        // ✔ 하나라도 실행된 경우에만 이벤트 보내기
        if (executedAny)
        {
            OnCommandAction?.Invoke(new OnCommandActionEventArgs
            {
                action = typeof(TAction),
                GridPosition = gridPosition,
            });

            DiectActionSoundPlay<TAction>();
        }
    }

    private void DiectActionSoundPlay<TAction>() where TAction : BaseAction
    {
        if (typeof(TAction) == typeof(CommandMoveAction))
        {
            Managers.Sound.Play(GameConfig.Sound.m_CommandAction_CommandMoveAudioClip);
        }

        if (typeof(TAction) == typeof(CommandAttackAction))
        {
            Managers.Sound.Play(GameConfig.Sound.m_CommandAction_CommandAttackAudioClip);
        }
    }

    // 커맨드 액션 선택
    public void SetSelectedAction(BaseAction baseAction)
    {
        m_SelectAction = baseAction;

        OnSelectedActionChanged?.Invoke(new OnCommandActionEventArgs
        {
            action = baseAction.GetType()
        });

        // CommandMove, CommandAttack은 별도 선택이 없다.
        if (baseAction.GetType() == typeof(CommandMoveAction))
        {
            Managers.Sound.Play(GameConfig.Sound.m_SelectAction_CommandMoveAudioClip);
        }

        if (baseAction.GetType() == typeof(CommandAttackAction))
        {
            Managers.Sound.Play(GameConfig.Sound.m_SelectAction_CommandAttackAudioClip);
        }

        //Debug.Log($"({m_SelectedAction.GetActionName()}) Action 이 선택됨");
    }
}
