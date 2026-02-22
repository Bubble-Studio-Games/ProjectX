using Data;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;

public class BuildingCardSelectUI :  MonoBehaviour, ISaveable, IBuildingCardUI
{
    public RectTransform m_CanvaseRect { get; private set; }
    public Canvas m_Canvas { get; private set; }
    public RectTransform m_RectTransform { get; private set; }

    private HashSet<BuildingCard> ShowGameEntityCardDic = new();
    private const int m_iMaxHaveCard = 10;

    [SerializeField] private BuildingCard m_BuildingCardPrefab;

    [Header("Card Interval And Move Time")]
    [SerializeField] private float m_fXInteraval = 260;
    [SerializeField] private float m_fXStartOffset = 160;
    [SerializeField] private float m_fYOffset = 130;
    private float m_fXLastOffset => m_fXInteraval * (m_iMaxHaveCard - 1) + m_fXStartOffset; // 마지막 10번째 위치

    [Header("Move Animation")]
    [SerializeField] private float m_fXMoveTime = 3f;

    private void Awake()
    {
        m_RectTransform = GetComponent<RectTransform>();

        m_CanvaseRect = transform.parent.GetComponentInParent<RectTransform>();
        m_Canvas = GetComponentInParent<Canvas>();

        // ✅ 서비스 등록
        Managers.SceneServices.Register<IBuildingCardUI>(this);
    }

    public void AddCard(GameEntity addUnit, Vector3 worldPosition = default, bool isInit = false)
    {
        // 초과 지급시 제일 첫 장은 버린다.
        if (ShowGameEntityCardDic.Count >= m_iMaxHaveCard)
        {
            var firstEntry = ShowGameEntityCardDic.First();
            RemoveCard(firstEntry);
        }

        BuildingCard card = Managers.Resource.Instantiate<BuildingCard>(m_BuildingCardPrefab.gameObject, transform.parent);

        bool isInside = true;

        if (isInit)
        {
            // 첫 시작지
            // 목적지 & 애니메이션
            card.m_RectTransform.anchoredPosition = new Vector2(m_fXLastOffset, m_fYOffset);
        }
        else
        {
            // 해당 범위가 카메라 안에 있는가? 있으면 이동 애니메이션, 없으면 그냥 바로 넣기
            Vector3 screenPos = Camera.main.WorldToViewportPoint(worldPosition);


            // 카메라 뷰포트 내부(0 ~ 1) 범위인지 확인
            isInside = screenPos.z > 0 &&       // 카메라 앞에 있는가?
                            screenPos.x >= 0 && screenPos.x <= 1 &&
                            screenPos.y >= 0 && screenPos.y <= 1;

            // 현재 보고 있는 카메라 안에 있다면 이동 연출
            if(isInside)
            {
                // 몬스터 위치를 기준으로
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    m_CanvaseRect,   // UI가 있는 Canvas의 RectTransform
                    screenPos,             // 월드 → 스크린 변환된 좌표
                    m_Canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : m_Canvas.worldCamera,
                    out Vector2 uiPos      // 변환된 UI 로컬 좌표
                );

                //now you can set the position of the ui element
                card.m_RectTransform.anchoredPosition = uiPos * -1f;

            }
            else
            {
                card.m_RectTransform.anchoredPosition = new Vector2(m_fXLastOffset, m_fYOffset);
            }
        }

        float xinterval = ShowGameEntityCardDic.Count * m_fXInteraval + m_fXStartOffset;
        card.m_RectTransform.DOMove(new Vector3(xinterval, m_fYOffset, 0), m_fXMoveTime);
        card.SetTransform(new Vector2(xinterval, m_fYOffset));

        card.Init(addUnit, this);

        ShowGameEntityCardDic.Add(card);
    }

    public void RemoveCard(BuildingCard removeCard)
    {
        if (removeCard == null || !ShowGameEntityCardDic.Contains(removeCard)) return;

        ShowGameEntityCardDic.Remove(removeCard);

        // 카드들 위치 재정렬 (Dictionary 순회하며 정렬)
        ReorderCards();
    }

    private void ReorderCards()
    {
        int index = 0;
        foreach (var card in ShowGameEntityCardDic)
        {
            float interval = index * m_fXInteraval + m_fXStartOffset;
            card.m_RectTransform.DOMoveX(interval, m_fXMoveTime);
            card.SetTransform(new Vector2(interval, m_fYOffset));
            index++;
        }
    }

    #region Data Save & Load

    BaseData ISaveable.CaptureSaveData() => null;
    public void RestoreSaveData(BaseData data) { }

    public List<BaseData> CaptureSaveData()
    {
        List<BaseData> datas = new();


        foreach (var card in ShowGameEntityCardDic)
        {
            BuildingCardData carddata = new BuildingCardData();
            //carddata.gameEntitySaveData = card.m_GameEntity.CaptureSaveData() as GameEntityData;
            datas.Add(carddata);
        }

        return datas;
    }

    public void RestoreSaveDatas(IEnumerable<BaseData> datas) 
    { 
        foreach (BuildingCardData data in datas)
        {
            BuildingCard card = Managers.Resource.Instantiate<BuildingCard>(m_BuildingCardPrefab.gameObject, transform.parent);

            float xinterval = ShowGameEntityCardDic.Count * m_fXInteraval + m_fXStartOffset;

            card.m_RectTransform.anchoredPosition = new Vector2(xinterval, m_fYOffset);
            //card.m_RectTransform.DOMove(new Vector3(xinterval, m_fYOffset, 0), m_fXMoveTime);
            card.SetTransform(new Vector2(xinterval, m_fYOffset));
            Debug.Log($"{card.name} 카드의 위치 : {xinterval} {m_fYOffset}");

            GameEntity addUnit = Managers.Object.GetPrefabByName(data.gameEntitySaveData.prefabName).GetComponent<GameEntity>();
            card.Init(addUnit, this);

            //ShowGameEntityCardDic.Add(card);
        }
    }

    #endregion
}
