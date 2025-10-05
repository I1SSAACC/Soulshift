using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class SpriteCoverFitter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera = null;

    [Header("Settings")]
    [SerializeField, Min(0f)] private float zDistanceFromCamera = 10f;
    [SerializeField] private bool matchCameraPositionXY = true;
    [SerializeField] private bool updateEveryFrame = false;

    private SpriteRenderer _sr;
    private Camera _cam;
    private Vector3 _initialLocalScale;
    private Vector2 _spriteSizeWorld; // sprite size in world units at localScale = (1,1,1)
    private int _lastScreenW;
    private int _lastScreenH;
    private float _lastOrthoSize;

    private void Reset() => targetCamera = Camera.main;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr == null)
        {
            Debug.LogError("SpriteCoverFitter requires a SpriteRenderer.", this);
            enabled = false;
            return;
        }

        _cam = targetCamera != null ? targetCamera : Camera.main;
        if (_cam == null || !_cam.orthographic)
        {
            Debug.LogError("SpriteCoverFitter requires an orthographic Camera assigned or Camera.main to be orthographic.", this);
            enabled = false;
            return;
        }

        _initialLocalScale = transform.localScale;
        CacheSpriteSize();
        FitToScreen();

        _lastScreenW = Screen.width;
        _lastScreenH = Screen.height;
        _lastOrthoSize = _cam.orthographicSize;
    }

    private void Update()
    {
        if (updateEveryFrame)
        {
            FitToScreen();
            return;
        }

        if (Screen.width != _lastScreenW || Screen.height != _lastScreenH || !Mathf.Approximately(_cam.orthographicSize, _lastOrthoSize))
        {
            _lastScreenW = Screen.width;
            _lastScreenH = Screen.height;
            _lastOrthoSize = _cam.orthographicSize;
            FitToScreen();
        }
    }

    private void CacheSpriteSize()
    {
        var sprite = _sr.sprite;
        if (sprite == null)
        {
            _spriteSizeWorld = Vector2.one;
            return;
        }

        Vector2 spritePixels = sprite.rect.size;
        float ppu = Mathf.Max(1f, sprite.pixelsPerUnit);
        _spriteSizeWorld = spritePixels / ppu; // world units at localScale = 1
    }

    public void FitToScreen()
    {
        if (_sr == null || _cam == null) return;

        // world size of camera view (orthographic)
        float worldHeight = _cam.orthographicSize * 2f;
        float worldWidth = worldHeight * _cam.aspect;

        // desired world size to cover
        float desiredW = worldWidth;
        float desiredH = worldHeight;

        Vector2 spriteWorldSize = _spriteSizeWorld;
        if (spriteWorldSize.x <= 0f || spriteWorldSize.y <= 0f)
            spriteWorldSize = Vector2.one;

        float scaleX = desiredW / spriteWorldSize.x;
        float scaleY = desiredH / spriteWorldSize.y;

        // Cover mode: choose max so sprite covers the whole view
        float finalScale = Mathf.Max(scaleX, scaleY);

        Vector3 newScale = new Vector3(finalScale * _initialLocalScale.x, finalScale * _initialLocalScale.y, _initialLocalScale.z);
        transform.localScale = newScale;

        if (matchCameraPositionXY)
        {
            Vector3 camPos = _cam.transform.position;
            Vector3 newPos = camPos + _cam.transform.forward.normalized * zDistanceFromCamera;
            transform.position = newPos;
        }
    }

    public void Refresh()
    {
        CacheSpriteSize();
        FitToScreen();
    }
}