using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Define;

public class StatBarUI : MonoBehaviour
{
    private AttributeSystem StatSystem;
    private GameEntity m_GameEntity;

    [SerializeField] private Image healthBarImage;
    [SerializeField] private Image ManaBarImage;
    [SerializeField] private GameObject ManaBar;
    [SerializeField] private GameObject healthBar;
    [SerializeField] private TextMeshProUGUI ObjectNameText;

    private void Awake()
    {
        m_GameEntity = GetComponentInParent<GameEntity>();
        StatSystem = GetComponentInParent<AttributeSystem>();
    }

    private void Start()
    {
        // 장애물은 이름을 나타내지 않는다.
        if (m_GameEntity.m_EObjectType == E_ObjectType.Obstacle)
            ObjectNameText.enabled = false;

        if (m_GameEntity is IUpgradeble cobj)
        {
            ObjectNameText.text = StatSystem.m_Stat.Name;
            switch (cobj.m_EObjectGrade)
            {
                case E_ObjectGrade.Normal:
                    ObjectNameText.gameObject.SetActive(true);
                    ObjectNameText.color = Color.white;
                    break;
                case E_ObjectGrade.Elite:
                    ObjectNameText.gameObject.SetActive(true);
                    ObjectNameText.color = Color.yellow;
                    break;
                case E_ObjectGrade.Boss:
                    ObjectNameText.gameObject.SetActive(true);
                    ObjectNameText.color = Color.red;
                    break;
                default:
                    break;
            }
        }

        if (!m_GameEntity.m_IsSpawning)
            Init();
    }

    private void OnEnable()
    {
        // 이름/등급 표시
        if (m_GameEntity is IUpgradeble upgradable)
            upgradable.OnChangeGrade += UpdateGrade;

        m_GameEntity.OnObjectSpawnStart += Init;
        m_GameEntity.OnSpawnObjectSelected += HideBars;

        // Event
        StatSystem.OnUpdateStat += UpdateHealthBar;
        StatSystem.OnUpdateStat += UpdateManaBar;

        StatSystem.OnDead += HideBars;
        StatSystem.OnRevived += Init;
    }

    private void OnDisable()
    {
        if (m_GameEntity is IUpgradeble cobj)
            cobj.OnChangeGrade -= UpdateGrade;

        m_GameEntity.OnObjectSpawnStart -= Init;
        m_GameEntity.OnSpawnObjectSelected -= HideBars;

        // Event
        StatSystem.OnUpdateStat -= UpdateHealthBar;
        StatSystem.OnUpdateStat -= UpdateManaBar;

        StatSystem.OnDead -= HideBars;
        StatSystem.OnRevived -= Init;
    }

    /// <summary>
    /// 스폰/부활 시점 초기화. 이때의 체력/마나를 기준값으로 저장.
    /// 기준값과 다를 때만 바를 보여준다.
    /// </summary>
    private void Init()
    {
        if (StatSystem == null) return;

        // 현재 상태 반영
        UpdateHealthBar();
        UpdateManaBar();
    }

    private void UpdateHealthBar()
    {
        // UI Fill
        healthBarImage.fillAmount = StatSystem.GetHealthNormalized();

        // 표시 조건: 현재 != 최대
        bool visible = !StatSystem.GetFillHeath();

        if (healthBar != null && healthBar.activeSelf != visible)
            healthBar.SetActive(visible);
    }

    private void UpdateManaBar()
    {
        // 마나 없는 유닛이면 그냥 꺼둔다
        if (!StatSystem.IsManaCharacter())
        {
            if (ManaBar != null && ManaBar.activeSelf)
                ManaBar.SetActive(false);
            return;
        }

        // UI Fill
        ManaBarImage.fillAmount = StatSystem.GetManaNormalized();

        // 표시 조건: 현재 != 최대
        bool visible = !StatSystem.GetFillMana();

        if (ManaBar != null && ManaBar.activeSelf != visible)
            ManaBar.SetActive(visible);
    }


    private void HideBars()
    {
        if (ManaBar != null) ManaBar.SetActive(false);
        if (healthBar != null) healthBar.SetActive(false);

        if (ObjectNameText != null)
            ObjectNameText.gameObject.SetActive(false);
    }

    private void HideBars(OnAttackInfoEventArgs _) => HideBars();


    private void UpdateGrade(OnChangeGradeEventArgs args)
    {
        if (ObjectNameText == null)
            return;

        if (!args.isSuccessGrade)
            ObjectNameText.text = StatSystem.m_Stat.Name;
        else
        {
            string prefix = args.gradeEnhanceType switch
            {
                E_ObjectEnhanceType.Health => "Iron",
                E_ObjectEnhanceType.Magic => "Arcane",
                E_ObjectEnhanceType.Physical => "Brutal",
                E_ObjectEnhanceType.Defense => "Bulwark",
                E_ObjectEnhanceType.Speed => "Swift",
                E_ObjectEnhanceType.Critical => "Deadeye",
                E_ObjectEnhanceType.Range => "Longshot",
                E_ObjectEnhanceType.Skill => "Ascendant",
                _ => ""
            };

            ObjectNameText.text = string.IsNullOrEmpty(prefix)
                ? StatSystem.m_Stat.Name
                : $"{prefix} {StatSystem.m_Stat.Name}";
        }


        // 표시와 등급
        switch (args.objGrade)
        {
            case E_ObjectGrade.Normal:
                ObjectNameText.gameObject.SetActive(true);
                ObjectNameText.color = Color.white;
                break;
            case E_ObjectGrade.Elite:
                ObjectNameText.gameObject.SetActive(true);
                ObjectNameText.color = Color.yellow;
                break;
            case E_ObjectGrade.Boss:
                ObjectNameText.gameObject.SetActive(true);
                ObjectNameText.color = Color.red;
                break;
            default:
                break;
        }

    }
}
