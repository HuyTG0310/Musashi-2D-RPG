using UnityEngine;

/// <summary>
/// Controls wind sway and parallax movement for 2D background layers.
/// Gives background trees, branches and leaves a natural, organic swaying motion in the wind.
/// </summary>
[ExecuteAlways]
public class BackgroundWindSway : MonoBehaviour
{
    [Header("Camera & Parallax")]
    public Camera targetCamera;
    public float parallaxFactor = 0.2f;
    private float startPosX;
    private bool initialized = false;

    [Header("Wind Sway Settings")]
    public bool enableWindSway = true;
    public float swaySpeed = 1.2f;
    public float swayAmountX = 0.18f;
    public float swayAmountY = 0.04f;
    public float swayRotationAngle = 0.75f; // degrees of gentle swaying tilt
    public float waveFrequency = 0.08f;    // wave spread across parts

    [Header("Constant Drift (e.g. Clouds / Sky)")]
    public float constantDriftSpeed = 0f;

    [System.Serializable]
    public struct PartInfo
    {
        public Transform transform;
        public Vector3 baseLocalPos;
        public float phaseOffset;
    }

    [SerializeField]
    private PartInfo[] parts;
    private float driftOffset = 0f;

    void Start()
    {
        Init();
    }

    public void Init()
    {
        startPosX = transform.position.x;
        FindCameraIfNeeded();

        int childCount = transform.childCount;
        parts = new PartInfo[childCount];
        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);
            parts[i] = new PartInfo
            {
                transform = child,
                baseLocalPos = child.localPosition,
                phaseOffset = child.position.x * waveFrequency
            };
        }
        initialized = true;
    }

    void Update()
    {
        if (!initialized || parts == null || parts.Length == 0)
        {
            Init();
        }

        if (targetCamera == null)
        {
            FindCameraIfNeeded();
        }

        // 1. Parallax movement along X
        if (targetCamera != null && parallaxFactor != 0f)
        {
            float camX = targetCamera.transform.position.x;
            float targetX = startPosX + camX * parallaxFactor;
            Vector3 pos = transform.position;
            pos.x = targetX;
            transform.position = pos;
        }

        // 2. Constant drift (if any)
        if (constantDriftSpeed != 0f)
        {
            driftOffset += constantDriftSpeed * (Application.isPlaying ? Time.deltaTime : 0.016f);
        }

        // 3. Natural wind sway on children parts
        if (enableWindSway && parts != null)
        {
            float currentTime = Application.isPlaying ? Time.time : (float)(System.DateTime.Now.TimeOfDay.TotalSeconds);
            float t = currentTime * swaySpeed;

            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].transform == null) continue;

                float phase = parts[i].phaseOffset;
                // Dual-sine wave for organic wind feeling
                float wave = Mathf.Sin(t + phase) * 0.75f + Mathf.Sin(t * 1.37f + phase * 1.5f) * 0.25f;

                float offsetX = wave * swayAmountX + driftOffset;
                float offsetY = Mathf.Cos(t * 0.8f + phase) * swayAmountY;
                float rotZ = wave * swayRotationAngle;

                parts[i].transform.localPosition = parts[i].baseLocalPos + new Vector3(offsetX, offsetY, 0f);
                parts[i].transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
            }
        }
    }

    private void FindCameraIfNeeded()
    {
        if (targetCamera != null) return;
        targetCamera = Camera.main;
        if (targetCamera == null)
        {
            targetCamera = FindFirstObjectByType<Camera>();
        }
    }
}
