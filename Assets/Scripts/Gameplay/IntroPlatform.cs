using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class IntroPlatform : MonoBehaviour
{
    [SerializeField] BallController ball;

    [Header("Custom Model (pivot at disc center)")]
    [SerializeField] GameObject tilePrefab;     // Wedge sector con pivot en origen

    [Header("Fallback Cubes (sin prefab)")]
    [SerializeField] Material tileMaterial;

    [Header("UI")]
    [SerializeField] TextMeshProUGUI tapLabel;

    const int   SegCount     = 10;
    const float PlatRadius   = 0.88f;   // solo usado en modo cubo
    const float TileWidth    = 1.22f;
    const float TileThick    = 0.22f;
    const float TileDepth    = 0.42f;
    const float BallRadius   = 0.5f;
    const float AnimDuration = 0.6f;

    Rigidbody   ballRb;
    Transform[] tiles;
    Vector3[]   outDirs;
    bool        started;
    bool        usePrefab;
    GameObject  autoCanvas;

    void Awake()
    {
        if (ball == null) ball = FindObjectOfType<BallController>();
        ballRb    = ball.GetComponent<Rigidbody>();
        usePrefab = tilePrefab != null;
    }

    void Start()
    {
        ballRb.isKinematic = true;
        BuildPlatform();
        ShowTapLabel();
    }

    void BuildPlatform()
    {
        tiles   = new Transform[SegCount];
        outDirs = new Vector3[SegCount];

        float y         = ball.transform.position.y - BallRadius - TileThick * 0.5f + 0.05f;
        float angleStep = 360f / SegCount;

        for (int i = 0; i < SegCount; i++)
        {
            float angle = i * angleStep;
            float rad   = angle * Mathf.Deg2Rad;

            Transform t;
            if (usePrefab)
            {
                // Pivot en centro del disco: todas las piezas en (0,y,0), cada una rotada
                t          = Instantiate(tilePrefab).transform;
                t.position = new Vector3(0f, y, 0f);
                t.rotation = Quaternion.Euler(0f, angle, 0f);

                // Deshabilitar colisores: la bola es kinematic durante el intro
                foreach (var col in t.GetComponentsInChildren<Collider>())
                    col.enabled = false;
            }
            else
            {
                // Fallback: cubos posicionados a radio
                t            = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                t.position   = new Vector3(Mathf.Sin(rad) * PlatRadius, y, Mathf.Cos(rad) * PlatRadius);
                t.localScale = new Vector3(TileWidth, TileThick, TileDepth);
                t.rotation   = Quaternion.Euler(0f, angle, 0f);

                var rend = t.GetComponent<Renderer>();
                if (tileMaterial != null) rend.material = tileMaterial;
                else rend.material.color = new Color(0.42f, 0.37f, 0.32f);
            }

            // Direccion de salida calculada desde el angulo (valida para ambos modos)
            outDirs[i] = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
            tiles[i]   = t;
        }
    }

    void ShowTapLabel()
    {
        if (tapLabel != null) { tapLabel.gameObject.SetActive(true); return; }

        autoCanvas = new GameObject("IntroPlatformCanvas");
        var canvas = autoCanvas.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        autoCanvas.AddComponent<CanvasScaler>();
        autoCanvas.AddComponent<GraphicRaycaster>();

        var textGO = new GameObject("TapToStartText");
        textGO.transform.SetParent(autoCanvas.transform, false);

        var rt = textGO.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.22f);
        rt.anchorMax        = new Vector2(0.5f, 0.22f);
        rt.sizeDelta        = new Vector2(500f, 80f);
        rt.anchoredPosition = Vector2.zero;

        tapLabel           = textGO.AddComponent<TextMeshProUGUI>();
        tapLabel.text      = "Tap to Play";
        tapLabel.alignment = TextAlignmentOptions.Center;
        tapLabel.fontSize  = 40f;
        tapLabel.color     = Color.white;
        tapLabel.fontStyle = FontStyles.Bold;
    }

    void Update()
    {
        if (started) return;

        var pointer = Pointer.current;
        bool tapped = pointer != null && pointer.press.wasPressedThisFrame;
#if UNITY_EDITOR
        var kb = UnityEngine.InputSystem.Keyboard.current;
        tapped |= kb != null && kb.spaceKey.wasPressedThisFrame;
#endif
        if (tapped) TriggerStart();
    }

    void TriggerStart()
    {
        started = true;
        if (autoCanvas != null) Destroy(autoCanvas);
        else if (tapLabel != null) tapLabel.gameObject.SetActive(false);
        StartCoroutine(LaunchSequence());
    }

    IEnumerator LaunchSequence()
    {
        StartCoroutine(AnimatePlatform());
        yield return new WaitForSeconds(0.15f);
        ballRb.isKinematic = false;
        GameManager.Instance.StartGame();
    }

    IEnumerator AnimatePlatform()
    {
        var startPos = new Vector3[tiles.Length];
        for (int i = 0; i < tiles.Length; i++)
            startPos[i] = tiles[i].position;

        float elapsed = 0f;
        while (elapsed < AnimDuration)
        {
            elapsed += Time.deltaTime;
            float t     = Mathf.Clamp01(elapsed / AnimDuration);
            float slide = Mathf.SmoothStep(0f, 3.8f, t);
            float fall  = t * t * 5f;
            for (int i = 0; i < tiles.Length; i++)
                tiles[i].position = startPos[i] + outDirs[i] * slide - Vector3.up * fall;
            yield return null;
        }

        foreach (var tile in tiles)
            if (tile != null) Destroy(tile.gameObject);
    }
}
