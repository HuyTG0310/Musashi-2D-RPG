using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns and animates ambient oak leaves drifting, fluttering and tumbling through the forest wind.
/// Gives Map 2 a lively, natural forest atmosphere with animated fluttering leaves.
/// </summary>
[ExecuteAlways]
public class OakLeafWindEffect : MonoBehaviour
{
    [Header("Camera Tracking")]
    public Camera targetCamera;

    [Header("Leaf Counts & Bounds")]
    [Range(10, 80)]
    public int leafCount = 45;
    public float spawnAreaWidth = 36f;
    public float spawnAreaHeight = 24f;

    [Header("Wind & Physics")]
    public float windSpeedX = 2.4f;
    public float fallSpeedY = 1.1f;
    public float flutterSpeed = 7.0f;
    public float waveAmplitude = 0.35f;
    public float waveFrequency = 1.6f;

    [Header("Visuals & Sprites")]
    public Sprite[] leafSprites;

    private class LeafParticle
    {
        public GameObject gameObject;
        public Transform transform;
        public SpriteRenderer spriteRenderer;
        public Vector3 position;
        public float baseScale;
        public float speedMultiplier;
        public float wavePhase;
        public float flutterPhase;
        public float rotationOffset;
        public bool isGolden;
        public int spriteIndexOffset;
    }

    private List<LeafParticle> leaves = new List<LeafParticle>();
    private Transform poolRoot;
    private bool initialized = false;

    void Start()
    {
        InitializeLeaves();
    }

    void OnEnable()
    {
        InitializeLeaves();
    }

    void OnDisable()
    {
        CleanupLeaves();
    }

    void OnDestroy()
    {
        CleanupLeaves();
    }

    public void InitializeLeaves()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                targetCamera = FindFirstObjectByType<Camera>();
            }
        }

        LoadSpritesIfNeeded();

        if (poolRoot == null)
        {
            Transform existing = transform.Find("_LeafPool");
            if (existing != null)
            {
                if (Application.isPlaying) Destroy(existing.gameObject);
                else DestroyImmediate(existing.gameObject);
            }

            GameObject poolGo = new GameObject("_LeafPool");
            poolGo.transform.SetParent(transform, false);
            poolGo.hideFlags = HideFlags.DontSave;
            poolRoot = poolGo.transform;
        }

        CleanupLeaves();

        Vector3 camPos = targetCamera != null ? targetCamera.transform.position : transform.position;

        for (int i = 0; i < leafCount; i++)
        {
            var leaf = CreateLeaf(i, camPos);
            leaves.Add(leaf);
        }

        initialized = true;
    }

    private void LoadSpritesIfNeeded()
    {
        if (leafSprites != null && leafSprites.Length > 0) return;

#if UNITY_EDITOR
        var loaded = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/VFX/oak_leaf_flutter.png");
        var list = new List<Sprite>();
        foreach (var obj in loaded)
        {
            if (obj is Sprite s) list.Add(s);
        }
        if (list.Count > 0)
        {
            leafSprites = list.ToArray();
        }
#endif
    }

    private LeafParticle CreateLeaf(int index, Vector3 centerPos)
    {
        GameObject go = new GameObject($"Leaf_{index}");
        go.transform.SetParent(poolRoot, false);
        go.hideFlags = HideFlags.DontSave;

        var sr = go.AddComponent<SpriteRenderer>();

        // Distribute half in Background layer (behind foreground platforms) and half in Default
        if (index % 2 == 0)
        {
            sr.sortingLayerName = "Background";
            sr.sortingOrder = 3; // in front of background trees (0..2)
        }
        else
        {
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 2; // in front of ground/player
        }

        bool gold = (index % 3 == 0);
        int spriteOffset = gold ? 4 : 0; // row 1 in spritesheet has gold variants

        if (leafSprites != null && leafSprites.Length > 0)
        {
            int safeIdx = Mathf.Clamp(spriteOffset, 0, leafSprites.Length - 1);
            sr.sprite = leafSprites[safeIdx];
        }

        float randomScale = Random.Range(0.85f, 1.25f);
        float speedMult = Random.Range(0.75f, 1.30f);

        // Spread initially across the camera area
        float startX = centerPos.x + Random.Range(-spawnAreaWidth * 0.6f, spawnAreaWidth * 0.6f);
        float startY = centerPos.y + Random.Range(-spawnAreaHeight * 0.5f, spawnAreaHeight * 0.7f);

        var leaf = new LeafParticle
        {
            gameObject = go,
            transform = go.transform,
            spriteRenderer = sr,
            position = new Vector3(startX, startY, 0f),
            baseScale = randomScale,
            speedMultiplier = speedMult,
            wavePhase = Random.Range(0f, Mathf.PI * 2f),
            flutterPhase = Random.Range(0f, Mathf.PI * 2f),
            rotationOffset = Random.Range(-25f, 25f),
            isGolden = gold,
            spriteIndexOffset = spriteOffset
        };

        leaf.transform.position = leaf.position;
        leaf.transform.localScale = Vector3.one * leaf.baseScale;
        return leaf;
    }

    void Update()
    {
        if (!initialized || leaves == null || leaves.Count == 0)
        {
            InitializeLeaves();
            return;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null) targetCamera = FindFirstObjectByType<Camera>();
        }

        Vector3 camPos = targetCamera != null ? targetCamera.transform.position : transform.position;
        float dt = Application.isPlaying ? Time.deltaTime : 0.016f;
        float currentTime = Application.isPlaying ? Time.time : (float)System.DateTime.Now.TimeOfDay.TotalSeconds;

        float leftBound = camPos.x - spawnAreaWidth * 0.6f;
        float rightBound = camPos.x + spawnAreaWidth * 0.6f;
        float bottomBound = camPos.y - spawnAreaHeight * 0.6f;
        float topBound = camPos.y + spawnAreaHeight * 0.7f;

        for (int i = 0; i < leaves.Count; i++)
        {
            var leaf = leaves[i];
            if (leaf.transform == null) continue;

            // 1. Move leaf with wind and downward gravity
            float speedX = windSpeedX * leaf.speedMultiplier;
            float speedY = fallSpeedY * leaf.speedMultiplier;

            // Organic sine wave oscillation for fluttering drift
            float wave = Mathf.Sin(currentTime * waveFrequency + leaf.wavePhase) * waveAmplitude;

            leaf.position.x += (speedX + wave * 0.4f) * dt;
            leaf.position.y -= (speedY - wave * 0.6f) * dt;

            // 2. Wrap around when out of bounds
            if (leaf.position.x > rightBound || leaf.position.y < bottomBound)
            {
                // Respawn near top-left of the view
                leaf.position.x = leftBound + Random.Range(0f, spawnAreaWidth * 0.4f);
                leaf.position.y = topBound + Random.Range(0f, 3f);
                leaf.speedMultiplier = Random.Range(0.75f, 1.30f);
                leaf.wavePhase = Random.Range(0f, Mathf.PI * 2f);
                leaf.flutterPhase = Random.Range(0f, Mathf.PI * 2f);
            }

            leaf.transform.position = leaf.position;

            // 3. Leaf flutter animation (tumbling in 2D perspective)
            float flutter = Mathf.Sin(currentTime * flutterSpeed + leaf.flutterPhase);
            float scaleX = leaf.baseScale * Mathf.Abs(flutter);
            if (scaleX < 0.15f) scaleX = 0.15f; // keep minimal visible edge
            leaf.transform.localScale = new Vector3(scaleX, leaf.baseScale, 1f);

            // Sprite frame selection based on flutter cycle
            if (leafSprites != null && leafSprites.Length >= 4)
            {
                int frame = Mathf.FloorToInt(((flutter + 1f) * 0.5f) * 4f) % 4;
                int spriteIdx = leaf.spriteIndexOffset + frame;
                if (spriteIdx >= 0 && spriteIdx < leafSprites.Length)
                {
                    leaf.spriteRenderer.sprite = leafSprites[spriteIdx];
                }
            }

            // Gentle rotation tilt aligned with wind angle
            float rotZ = -15f + wave * 15f + leaf.rotationOffset;
            leaf.transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
        }
    }

    private void CleanupLeaves()
    {
        for (int i = 0; i < leaves.Count; i++)
        {
            if (leaves[i].gameObject != null)
            {
                if (Application.isPlaying) Destroy(leaves[i].gameObject);
                else DestroyImmediate(leaves[i].gameObject);
            }
        }
        leaves.Clear();
    }
}
