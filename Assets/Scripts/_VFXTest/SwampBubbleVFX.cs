using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

[ExecuteAlways]
[RequireComponent(typeof(VisualEffect))]
public class SwampBubbleVFX : MonoBehaviour
{
    [SerializeField] private VisualEffect _vfx;
    [SerializeField] private MeshRenderer _meshRenderer;

    private DungeonTiles _dungeonTiles;
    public DungeonTiles DungeonTiles
    {
        get
        {
            if (_dungeonTiles == null)
                _dungeonTiles = FindAnyObjectByType<DungeonTiles>();
            return _dungeonTiles;
        }
    }

    [Header("디버그용")]
    [SerializeField] private Texture2D _maskTexture;
    private Vector2Int _gridMin;
    private Vector2Int _gridMax;

    private Color[] _colors;

    private const string VFX_ASSET_PATH = "VFX/SwampBubble";
    private const string SPAWN_BOX_SIZE_STR = "SpawnBox_size";
    private const string MASK_TEXTURE_STR = "MaskTexture";
    private const string AREA_CENTER_STR = "AreaCenter";

    private Vector3 _preSize;

    public int GridWidth => _gridMax.x - _gridMin.x;
    public int GridHeight => _gridMax.y - _gridMin.y;
    public Vector3 WorldBoundsSize => _meshRenderer != null ? _meshRenderer.bounds.size : Vector3.zero;
    public Vector3 WorldBoundsCenter => _meshRenderer != null ? _meshRenderer.bounds.center : transform.position;

    private void OnValidate()
    {
        InitVFX();
        UpdateSpawnBoxSize();
    }

    private void Awake()
    {
        CalculateGridBounds();
        InitMaskTexture();
    }

    private void Update()
    {
#if UNITY_EDITOR
        UpdateSpawnBoxSize();
#endif
    }

    private void OnDestroy()
    {
        ClearMaskTexture();
    }

    private void InitVFX()
    {
        if (_vfx != null)
            return;

        var vfx = GetComponent<VisualEffect>();
        var vfxAsset = Resources.Load<VisualEffectAsset>(VFX_ASSET_PATH);

        if (vfx == null || vfxAsset == null)
        {
            Debug.LogWarning($"{VFX_ASSET_PATH} 경로에 SwampBubble VFX 에셋이 없습니다.");
            return;
        }

        _vfx = vfx;
        _vfx.visualEffectAsset = vfxAsset;
    }

    /// <summary>
    /// 메쉬 렌더러의 바운드 크기와 중심점 반환
    /// </summary>
    private (Vector3 Size, Vector3 Center) GetMeshBounds()
    {
        if (_meshRenderer == null)
            return ValueTuple.Create(Vector3.zero, Vector3.zero);

        var bounds = _meshRenderer.bounds;
        var center = bounds.center;
        var size = new Vector3(bounds.size.x, bounds.size.y, bounds.size.z);
        return ValueTuple.Create(size, center);
    }


    public void UpdateSpawnBoxSize()
    {
        if (_meshRenderer == null || _vfx == null)
            return;

        var bounds = GetMeshBounds();

        if (_preSize != bounds.Size)
        {
            UpdateSpawnBoxSize(bounds);
            _preSize = bounds.Size;
        }
    }

    private void UpdateSpawnBoxSize((Vector3 Size, Vector3 Center) bounds)
    {
        if (_meshRenderer == null || _vfx == null)
            return;

        this.gameObject.transform.position = bounds.Center;

        _vfx.SetVector3(SPAWN_BOX_SIZE_STR, bounds.Size);
        _vfx.SetVector3(AREA_CENTER_STR, bounds.Center);
        _preSize = bounds.Size;
    }

    /// <summary>
    /// MeshRenderer Bounds를 기반으로 Grid 범위 자동 계산
    /// </summary>
    private void CalculateGridBounds()
    {
        if (_meshRenderer == null || DungeonTiles == null)
            return;

        Bounds bounds = _meshRenderer.bounds;

        // Bounds의 4개 코너 월드 좌표 계산
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        // 각 코너를 Grid 좌표로 변환
        Vector2Int bottomLeft = DungeonTiles.GetGridPosition(new Vector3(min.x, min.y, min.z));
        Vector2Int topRight = DungeonTiles.GetGridPosition(new Vector3(max.x, max.y, max.z));

        // Grid Min/Max 설정
        _gridMin = new Vector2Int(
            Mathf.Min(bottomLeft.x, topRight.x),
            Mathf.Min(bottomLeft.y, topRight.y)
        );

        _gridMax = new Vector2Int(
            Mathf.Max(bottomLeft.x, topRight.x),
            Mathf.Max(bottomLeft.y, topRight.y)
        );
    }

    private void InitMaskTexture()
    {
        ClearMaskTexture();

        int width = GridWidth;
        int height = GridHeight;

        _maskTexture = new Texture2D(width, height, TextureFormat.R8, false);
        _maskTexture.filterMode = FilterMode.Point;
        _maskTexture.wrapMode = TextureWrapMode.Clamp;

        Debug.Log("Mask Texture Size: " + width + " / " + height);

        _colors = new Color[width * height];

        for (int i = 0; i < _colors.Length; i++)
            _colors[i] = Color.black;

        _maskTexture.SetPixels(_colors);
        _maskTexture.Apply();

        UpdateVFXParameters();
    }

    private void UpdateVFXParameters()
    {
        if (_vfx == null)
            return;

        _vfx.SetTexture(MASK_TEXTURE_STR, _maskTexture);
    }

    private void ClearMaskTexture()
    {
        if (_maskTexture == null)
            return;

        _colors = null;

#if UNITY_EDITOR
        // 에디터 모드에서는 DestroyImmediate 사용
        if (Application.isPlaying == false)
        {
            DestroyImmediate(_maskTexture);
        }
        else
        {
            Destroy(_maskTexture);
        }
#else
        // 런타임에서는 Destroy 사용
        Destroy(_maskTexture);
#endif
        _maskTexture = null;
    }

    /// <summary>
    /// Grid 좌표를 MaskTexture 좌표로 변환
    /// </summary>
    private Vector2Int GridToTextureCoord(Vector2Int gridPos)
    {
        int x = gridPos.x - _gridMin.x;
        int y = gridPos.y - _gridMin.y;
        return new Vector2Int(x, y);
    }

    /// <summary>
    /// 특정 Grid 타일 활성화 - DungeonTiles의 Grid 좌표 사용
    /// </summary>
    public void ActiveSwampTile(int gridX, int gridY)
    {
        if (_maskTexture == null)
            return;

        Vector2Int textureCoord = GridToTextureCoord(new Vector2Int(gridX, gridY));

        if (textureCoord.x < 0 || textureCoord.x >= GridWidth ||
            textureCoord.y < 0 || textureCoord.y >= GridHeight)
            return;

        _maskTexture.SetPixel(textureCoord.x, textureCoord.y, Color.red);
        _maskTexture.Apply();

        _vfx.SetTexture(MASK_TEXTURE_STR, _maskTexture);
    }
}
