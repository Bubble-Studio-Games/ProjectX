using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class SwampChunk : MonoBehaviour
{
    [Header("청크 설정")]
    [SerializeField, Range(16, 128)] private int _chunkSize = 64;
    [SerializeField] private float _worldSize = 64f;

    [Header("컴포넌트 참조")]
    [SerializeField] private VisualEffect _chunkVFX;
    [SerializeField] private Transform _swampPlane;
    [SerializeField] private Material _swampMaterial;

    private Texture2D _maskTexture;
    private Color[] _colors;

    public int ChunkSize => _chunkSize;
    public float WorldSize => _worldSize;
    public Vector3 WorldOrigin => transform.position;

    private void Awake()
    {
        InitMaskTexture();
        ApplySettingsToComponents();
    }

    private void OnDestroy()
    {
        _maskTexture = null;
        _colors = null;
    }

    private void OnValidate()
    {
        if (Application.isPlaying == false)
            ApplySettingsToComponents();
    }

    /// <summary>
    /// 에디터 타임에 모든 컴포넌트 설정 동기화
    /// </summary>
    private void ApplySettingsToComponents()
    {
        // Plane 크기 설정
        if (_swampPlane != null)
            _swampPlane.localScale = new Vector3(_worldSize * 0.1f, 1f, _worldSize * 0.1f);

        // Material 설정
        if (_swampMaterial != null && _maskTexture != null)
            _swampMaterial.SetTexture("_MaskTex", _maskTexture);
    }

    private void InitMaskTexture()
    {
        if (_maskTexture != null)
            return;

        _maskTexture = new Texture2D(_chunkSize, _chunkSize, TextureFormat.R8, false);

        _maskTexture.filterMode = FilterMode.Point;
        _maskTexture.wrapMode = TextureWrapMode.Clamp;

        _colors = new Color[_chunkSize * _chunkSize];

        for (int i = 0; i < _colors.Length; i++)
            _colors[i] = Color.black;

        _maskTexture.SetPixels(_colors);
        _maskTexture.Apply();

        _chunkVFX.SetTexture("MaskTexture", _maskTexture);
        _chunkVFX.SetInt("ChunkSize", _chunkSize);
    }

    /// <summary>
    /// 월드 좌표를 그리드 좌표로 변환
    /// </summary>
    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        Vector3 localPos = worldPosition - WorldOrigin;

        int x = Mathf.FloorToInt((localPos.x / _worldSize + 0.5f) * _chunkSize);
        int y = Mathf.FloorToInt((localPos.z / _worldSize + 0.5f) * _chunkSize);

        return new Vector2Int(x, y);
    }

    /// <summary>
    /// 특정 타일 활성화
    /// </summary>
    public void ActiveSwampTile(int x, int y)
    {
        if (x < 0 || _chunkSize <= x || y < 0 || _chunkSize <= y)
            return;

        _maskTexture.SetPixel(x, y, Color.red);
        _maskTexture.Apply();

        if (_swampMaterial != null)
            _swampMaterial.SetTexture("_MaskTex", _maskTexture);
    }

    /// <summary>
    /// 월드 좌표로 타일 활성화
    /// </summary>
    public void ActiveSwampTileAtWorld(Vector3 worldPosition)
    {
        Vector2Int gridPos = WorldToGrid(worldPosition);
        ActiveSwampTile(gridPos.x, gridPos.y);
    }
}
