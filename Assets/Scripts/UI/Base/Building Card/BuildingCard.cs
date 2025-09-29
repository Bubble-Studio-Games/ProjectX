using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Poolable))]
public class BuildingCard : UI_Base,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    GameEntity m_GameEntity;
    public Image m_objectImage;
    public TextMeshProUGUI cardName;
    public TextMeshProUGUI m_Type;
    public TextMeshProUGUI m_Atk; // 기본 공격력
    public TextMeshProUGUI m_Def; // 기본 방어력
    public TextMeshProUGUI m_SpawnCost; // 소환 비용

    public RectTransform m_RectTransform;

    [SerializeField] private RectTransform m_BGRectTransform;

    [Header("Drag And Drop")]
    [SerializeField]  private Vector2 m_OriginalPosition;
    private Transform m_OriginalParent;
    private bool m_IsDragging;
    [SerializeField] private bool m_IsChange = true;

    [Header("Pointer")]
    [SerializeField] private float m_fUpYOffset = 100f;
    [SerializeField] private float m_fUpTime = 0.2f;
    [SerializeField] private AudioClip m_CardPointerAudio;


    public void Init(GameEntity gameEntity)
    {
        m_GameEntity = gameEntity;
        var stat = gameEntity.m_StatSystem.m_Stat;
        m_objectImage.sprite = stat.sprite;
        cardName.text = stat.Name;
        m_SpawnCost.text = (stat as ControllableObjectStat).m_iSpawnCost.ToString();
        m_Type.text = $"Type : {gameEntity.m_ObjectType.ToString()}";
        m_Atk.text = $"ATK : 0";
        m_Def.text = $"DEF : {stat.m_iPhysicalDefence.ToString()}";
    }
    // 드래그 시작
    public void OnBeginDrag(PointerEventData eventData)
    {
        m_IsDragging = true;
        //m_OriginalPosition = m_RectTransform.anchoredPosition;
        m_OriginalParent = transform.parent;
        transform.SetParent(m_OriginalParent.root); // 최상단 레이어로 올리기

        BuildingTypeSelectUI.Instance.m_ActiveBuildingCard = this;
    }

    // 드래그 중
    public void OnDrag(PointerEventData eventData)
    {
        var isInside = RectTransformUtility.RectangleContainsScreenPoint(BuildingTypeSelectUI.Instance.m_RectTransform, eventData.position);

        m_RectTransform.position = eventData.position;

        // 안으로
        if(isInside)
        {
            if (m_IsChange == false)
            {
                //Debug.Log("카드를 가지고 안으로");
                m_IsChange = true;

                m_BGRectTransform.gameObject.SetActive(true);
                //var obj = Managers.Resource.Instantiate<GameEntity>(m_GameEntity.gameObject);
                GridBuildingSystem.Instance.ChangePlaceObject(null);
            }
        }
        // 밖으로
        else
        {
            if(m_IsChange == true)
            {
                //Debug.Log("카드를 가지고 밖으로");
                m_IsChange = false;

                m_BGRectTransform.gameObject.SetActive(false);
                GridBuildingSystem.Instance.ChangePlaceObject(m_GameEntity);
            }

        }
    }

    // 드래그 종료
    public void OnEndDrag(PointerEventData eventData)
    {
        m_IsDragging = false;
        transform.SetParent(m_OriginalParent);
        m_BGRectTransform.gameObject.SetActive(true);

        BuildingTypeSelectUI.Instance.m_ActiveBuildingCard = null;

        var isInside = RectTransformUtility.RectangleContainsScreenPoint(BuildingTypeSelectUI.Instance.m_RectTransform, eventData.position);
        if (isInside)
        {
            // 영역 안 → 그냥 원위치 복귀
            m_RectTransform.anchoredPosition = m_OriginalPosition;

        }
        else
        {
            // 영역 밖 → 소환 시도
            BuildingTypeSelectUI.Instance.TrySummonEntity(this, m_GameEntity, m_OriginalPosition);
        }

    }

    public void ResetTransform(Vector2 xOffset)
    {
        m_OriginalPosition = xOffset;
    }

    // 마우스 진입/이탈
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (InputManager.Instance.MouseRightClickHold())
            return;

        if (m_RectTransform.position.x == m_OriginalPosition.x && m_RectTransform.position.y == m_OriginalPosition.y)
        {
            Managers.Sound.Play(m_CardPointerAudio);
            m_RectTransform.DOMoveY(m_OriginalPosition.y + m_fUpYOffset, m_fUpTime);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (InputManager.Instance.MouseRightClickHold())
            return;

        //if (m_RectTransform.position.x == m_OriginalPosition.x && m_RectTransform.position.y == m_OriginalPosition.y)
        {
            m_RectTransform.DOMoveY(m_OriginalPosition.y, m_fUpTime);
        }
    }
}
