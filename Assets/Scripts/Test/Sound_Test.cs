using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Sound_Test : MonoBehaviour
{
    private List<GameEntity> entities = new List<GameEntity>(); // 로드된 GameEntity 목록
    private int currentIndex = 0;      // 현재 활성화된 엔티티 인덱스
    private GameEntity activeEntity;   // 현재 활성화된 엔티티

    public static System.Action OnActiveEntityChanged;

    void Start()
    {
        LoadGameEntities();
        ActivateEntity(currentIndex);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            ChangeEntity(-1);
        else if (Input.GetKeyDown(KeyCode.RightArrow))
            ChangeEntity(1);
    }

    /// <summary>
    /// Resources/Prefabs/GameEntity 경로에서 GameEntity 프리팹 로드 및 제외 처리
    /// </summary>
    void LoadGameEntities()
    {
        GameEntity[] loaded = Resources.LoadAll<GameEntity>("Prefabs/GameEntity");

        foreach (var entity in loaded)
        {
            if (entity.name.Contains("Base")) continue;
            if (entity.name.Contains("Dummy")) continue;

            GameEntity instance = Instantiate(entity);
            instance.transform.position = new Vector3(10, 0, 10);
            instance.transform.SetParent(transform);
            instance.gameObject.SetActive(false);
            entities.Add(instance);
        }

        if (entities.Count == 0)
            Debug.LogError("❌ GameEntity 프리팹을 찾을 수 없습니다. (Resources/Prefabs/GameEntity)");
    }

    /// <summary>
    /// 좌우 방향키로 활성화된 엔티티 변경
    /// </summary>
    void ChangeEntity(int direction)
    {
        if (entities.Count == 0) return;

        if (activeEntity != null)
            activeEntity.gameObject.SetActive(false);

        currentIndex = (currentIndex + direction + entities.Count) % entities.Count;

        ActivateEntity(currentIndex);

        // 🔔 활성 엔티티 변경 이벤트 발행
        OnActiveEntityChanged?.Invoke();
    }


    /// <summary>
    /// 인덱스에 해당하는 GameEntity를 활성화하고 모든 Animator를 가져옴
    /// </summary>
    void ActivateEntity(int index)
    {
        if (entities.Count == 0) return;

        activeEntity = entities[index];
        activeEntity.gameObject.SetActive(true);
    }
}
