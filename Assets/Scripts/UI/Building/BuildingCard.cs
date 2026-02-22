using DG.Tweening;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Define;

[RequireComponent(typeof(Poolable))]
public class BuildingCard : UI_Base,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    private IInputQuery _input;
    private BuildingCardSelectUI _buildingCardUI;
    private string _cardGuid; // 카드 고유 guid

    [Header("Card")]
    public Image m_objectImage;
    public TextMeshProUGUI cardName;
    public TextMeshProUGUI m_Type;
    public TextMeshProUGUI m_Atk; // 기본 공격력
    public TextMeshProUGUI m_Def; // 기본 방어력
    public TextMeshProUGUI m_SpawnCost; // 소환 비용
    [SerializeField] private GameEntity m_GameEntity;

    public RectTransform m_RectTransform;
    private CanvasGroup _cg; // 카드 루트 or 카드 그래픽 루트에 붙은 CanvasGroup

    [Header("Drag And Drop")]
    private Vector2 m_OriginalPosition;
    private Transform m_OriginalParent;
    private bool m_IsChange = true;

    [Header("Pointer")]
    [SerializeField] private float m_fUpYOffset = 100f;
    [SerializeField] private float m_fUpTime = 0.2f;
    [SerializeField] private AudioClip m_CardPointerAudio;

    public void Init(GameEntity gameEntity, BuildingCardSelectUI ui)
    {
        m_GameEntity = gameEntity;
        _cardGuid = System.Guid.NewGuid().ToString(); // 카드 자체 guid

        _buildingCardUI = ui;

        var stat = gameEntity.GetComponent<AttributeSystem>().m_Stat;

        if (stat.sprite == null)
        {
            int RandomValue = Random.Range(1, 5);
            m_objectImage.sprite = Managers.Resource.Load<Sprite>($"Art/UI/Card/Game Entity/Unreleased_{gameEntity.m_EObjectType.ToString()}_0{RandomValue}");
        }
        else
        {
            m_objectImage.sprite = stat.sprite;
        }

        cardName.text = stat.Name;
        m_SpawnCost.text = (stat as ControllableObjectStat).m_iSpawnCost.ToString();
        m_Type.text = $"Type : {gameEntity.m_EObjectType.ToString()}";
        m_Atk.text = $"ATK : 0";
        m_Def.text = $"DEF : {stat.m_iPhysicalDefence.ToString()}";
    }


    private void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
        _input = Managers.SceneServices.InputQuery;
    }

    private void OnEnable()
    {
        Managers.Building.OnPlaced += ConfirmSummon;
        Managers.Building.OnPlaceFailed += PlaceCancleCard;
        Managers.Building.OnCanceled += PlaceCancleCard;
    }

    private void OnDisable()
    {
        Managers.Building.OnPlaced -= ConfirmSummon;
        Managers.Building.OnPlaceFailed -= PlaceCancleCard;
        Managers.Building.OnCanceled -= PlaceCancleCard;
    }


    // 드래그 시작
    public void OnBeginDrag(PointerEventData eventData)
    {
        m_OriginalParent = transform.parent;
        transform.SetParent(m_OriginalParent.root); // 최상단으로
    }


    // 드래그 중
    public void OnDrag(PointerEventData eventData)
    {
        var isInside = RectTransformUtility.RectangleContainsScreenPoint(_buildingCardUI.m_RectTransform, eventData.position);

        m_RectTransform.position = eventData.position;

        // 카드를 가지고 안으로
        if (isInside)
        {
            if (m_IsChange == false)
            {
                m_IsChange = true;

                // 카드 보여주기
                SetCardVisible(true);

                // 요청
                Managers.Building.Reqeust(new BuildContext(BuildRequestType.Cancel, null));

                // 그리드 비쥬얼 갱신
                Managers.Grid.RequestVisualRefresh();
            }
        }
        // 카드를 가지고 밖으로
        else
        {
            if(m_IsChange == true)
            {
                m_IsChange = false;

                // 카드 안 보여주기
                SetCardVisible(false);

                // 요청
                Managers.Building.Reqeust(new BuildContext(BuildRequestType.Select, m_GameEntity));

                // 그리드 비쥬얼 갱신
                Managers.Grid.RequestVisualRefresh();
            }
        }
    }

    // 드래그 종료
    public void OnEndDrag(PointerEventData eventData)
    {
        transform.SetParent(m_OriginalParent);

        var isInside = RectTransformUtility.RectangleContainsScreenPoint(_buildingCardUI.m_RectTransform, eventData.position);

        // 영역 안 → 그냥 원위치 복귀
        if (isInside)
        {
            m_RectTransform.anchoredPosition = m_OriginalPosition;

        }
        // 영역 밖 → 소환 요청
        else
            Managers.Building.Reqeust(new BuildContext(BuildRequestType.Place, m_GameEntity, _cardGuid));

        Managers.Grid.RequestVisualRefresh();
    }

    public void SetTransform(Vector2 xOffset) => m_OriginalPosition = xOffset;

    // 마우스 진입/이탈
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_input.IsRightClick)
            return;

        if (m_RectTransform.position.x == m_OriginalPosition.x && m_RectTransform.position.y == m_OriginalPosition.y)
        {
            Managers.Sound.Play(m_CardPointerAudio);
            m_RectTransform.DOMoveY(m_OriginalPosition.y + m_fUpYOffset, m_fUpTime);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_input.IsRightClick)
            return;

        m_RectTransform.DOMoveY(m_OriginalPosition.y, m_fUpTime);
    }

    /// 소환 확정
    private void ConfirmSummon(GridPosition g, BuildContext e)
    {
        if (_cardGuid != e.CardGuid) return;

        _buildingCardUI.RemoveCard(this);

        Managers.Resource.Destroy(gameObject);
    }

    /// 소환 실패 or 취소 → 카드 제자리 복귀
    private void PlaceCancleCard(BuildContext e)
    {
        if (_cardGuid != e.CardGuid) return;

        // Reset Transform
        m_RectTransform.anchoredPosition = m_OriginalPosition;

        SetCardVisible(true); 
    }

    private void SetCardVisible(bool visible)
    {
        _cg.alpha = visible ? 1f : 0f;         // 화면에서 안 보이게
        _cg.blocksRaycasts = visible;          // 밖으로 나가면 클릭/레이캐스트도 안 먹게
        _cg.interactable = visible;
    }
}
