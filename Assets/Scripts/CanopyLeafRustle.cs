using UnityEngine;

/// <summary>
/// Animates hanging canopy leaf clusters with a natural rustling motion in the wind.
/// Attached to hanging foliage clusters on tree branches.
/// </summary>
[ExecuteAlways]
public class CanopyLeafRustle : MonoBehaviour
{
    [Header("Rustle Motion Settings")]
    public float rustleSpeed = 2.2f;
    public float maxTiltAngle = 4.5f;
    public float waveOffset = 0f;

    [Header("Sprite Animation (Optional)")]
    public Sprite[] animFrames;
    public float frameRate = 4f;

    private SpriteRenderer sr;
    private Quaternion baseRotation;
    private float startTime;

    void Start()
    {
        Init();
    }

    void OnEnable()
    {
        Init();
    }

    private void Init()
    {
        sr = GetComponent<SpriteRenderer>();
        baseRotation = transform.localRotation;
        startTime = Random.Range(0f, 10f);
        if (waveOffset == 0f)
        {
            waveOffset = transform.position.x * 0.15f;
        }

#if UNITY_EDITOR
        if ((animFrames == null || animFrames.Length == 0) && sr != null)
        {
            var loaded = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/VFX/canopy_leaf_cluster.png");
            var list = new System.Collections.Generic.List<Sprite>();
            foreach (var o in loaded)
            {
                if (o is Sprite s) list.Add(s);
            }
            if (list.Count > 0) animFrames = list.ToArray();
        }
#endif
    }

    void Update()
    {
        float t = (Application.isPlaying ? Time.time : (float)System.DateTime.Now.TimeOfDay.TotalSeconds) + startTime;

        // 1. Gentle natural branch swing
        float wave = Mathf.Sin(t * rustleSpeed + waveOffset) * 0.7f + Mathf.Sin(t * rustleSpeed * 1.5f + waveOffset) * 0.3f;
        float angle = wave * maxTiltAngle;
        transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, angle);

        // 2. Frame animation if frames exist
        if (sr != null && animFrames != null && animFrames.Length > 0 && frameRate > 0f)
        {
            int frameIdx = Mathf.FloorToInt(t * frameRate) % animFrames.Length;
            sr.sprite = animFrames[frameIdx];
        }
    }
}
