using UnityEngine;

public class BackgroundController : MonoBehaviour
{
    private float startPos;
    public GameObject cam;
    public float parallaxEffect;


    // Start is called before the first frame update
    void Start()
    {
        startPos = transform.position.x;
        FindCameraIfNeeded();
    }

    void Update()
    {
        if (cam == null)
        {
            FindCameraIfNeeded();
            if (cam == null) return;
        }

        // khoảng cách mà background phải di chuyển theo camera
        float bgTravel = cam.transform.position.x * parallaxEffect;

        // cập nhật vị trí của background (mốc ban đầu + quảng đường phải di chuyển)
        transform.position = new Vector3(startPos + bgTravel, transform.position.y, transform.position.z);
    }

    private void FindCameraIfNeeded()
    {
        if (cam != null) return;

        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            mainCam = FindFirstObjectByType<Camera>();
#if !UNITY_2023_1_OR_NEWER
            if (mainCam == null)
            {
                mainCam = FindObjectOfType<Camera>();
            }
#endif
        }

        if (mainCam != null)
        {
            cam = mainCam.gameObject;
        }
    }
}
