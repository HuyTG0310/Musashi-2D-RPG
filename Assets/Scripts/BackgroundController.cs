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
    }


    void Update()
    {
        // khoảng cách mà background phải di chuyển theo camera
        float bgTravel = cam.transform.position.x * parallaxEffect;

        // cập nhật vị trí của background (mốc ban đầu + quảng đường phải di chuyển)
        transform.position = new Vector3(startPos + bgTravel, transform.position.y, transform.position.z);
    }
}
