using GoogleSheet.Core.Type;


[UGS(typeof(E_Action))]
public enum E_Action
{
    N,               // 액션 없음
    SHOP_OPEN,       // 상점 열기
    EXIT,            // 대화 즉시 종료
    QUEST_START,     // 퀘스트 시작
    REPAIR_OPEN,     // 수리 창 열기
    CHECK_ITEM,      // 아이템 체크
    CRAFT_OPEN,      // 제작 창 열기
    CHECK_QUEST,     // 퀘스트 체크
    CHECK_OBJECTIVE, // 목표 체크
    QUEST_COMPLETE,  // 퀘스트 완료
    GIVE_REWARD,     // 보상 지급
    UNLOCK_QUEST,    // 퀘스트 잠금 해제
    CHECK_GOLD,      // 골드 체크
    CONSUME_GOLD,    // 골드 소비
    UNLOCK_INFO,     // 정보 잠금 해제
    UNLOCK_MAP,      // 맵 잠금 해제
}

[UGS(typeof(E_Condition))]
public enum E_Condition
{
    QUEST_FINISHED,  // 퀘스트 완료 후
    QUEST_COMPLETE,  // 퀘스트 완료
    QUEST_ACTIVE,    // 퀘스트 진행 중
    QUEST_AVAILABLE, // 퀘스트 수락 가능
    HAS_ITEM,        // 아이템 보유
    FLAG_SET,        // 플래그 설정
    FIRST_MEET,      // 첫 만남
    DEFAULT,         // 기본값
}


/// <summary>
/// 아이템 타입 - 대분류
/// </summary>
public enum E_ItemType
{
    Consumable,     // 소비 아이템
    Equipment,      // 장비 아이템
    Material,       // 재료 아이템
    Currency,       // 화폐
    Misc,           // 기타 아이템
}

/// <summary>
/// 아이템 서브 타입 - 중분류
/// </summary>
public enum E_ItemSubType
{
    // Consumable
    Potion,         // 포션
    Food,           // 음식
    Scroll,         // 스크롤
    Book,           // 책

    // Equipment
    Weapon,         // 무기
    Armor,          // 방어구
    Shield,         // 방패
    Accessory,      // 액세서리

    // Material
    Gem,            // 보석
    Ingot,          // 주괴

    // Currency
    Coin,           // 코인

    // Misc
    Storage,        // 보관 아이템
    Unknown,        // 미확인
}

/// <summary>
/// 아이템 희귀도
/// </summary>
public enum E_ItemRarity
{
    Common,         // 일반
    Uncommon,       // 고급
    Rare,           // 희귀
    Epic,           // 영웅
    Legendary,      // 전설
}

/// <summary>
/// 장비 슬롯 타입
/// </summary>
public enum E_EquipSlot
{
    None,           // 없음
    MainHand,       // 주 손
    OffHand,        // 보조 손
    Head,           // 머리
    Chest,          // 가슴
    Hands,          // 손
    Legs,           // 다리
    Feet,           // 발
    Shoulders,      // 어깨
    Back,           // 등
    Wrist,          // 손목
    Belt,           // 허리
    Ring,           // 반지
    Necklace,       // 목걸이
}

/// <summary>
/// 아이템 효과 타입
/// </summary>
public enum E_ItemEffectType
{
    None,               // 효과 없음
    HP_Percent,         // HP 퍼센트 회복
    MP_Percent,         // MP 퍼센트 회복
    Buff_ATK,           // 공격력 버프
    Buff_INT,           // 지능 버프
    Inventory_Expand,   // 인벤토리 확장
}

/// <summary>
/// 아이템 스탯 타입
/// </summary>
public enum E_ItemStatType
{
    None,           // 스탯 없음
    HP,             // 체력
    MP,             // 마나
    ATK,            // 공격력
    DEF,            // 방어력
}
