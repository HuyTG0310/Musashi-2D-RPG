using UnityEngine;

public class HealthBarFix : MonoBehaviour
{
    private float originalScaleX;

    void Start()
    {
        if (originalScaleX <= 0f)
        {
            originalScaleX = Mathf.Abs(transform.localScale.x);
        }
    }

    public void SetOriginalScale(float scaleX)
    {
        originalScaleX = Mathf.Abs(scaleX);
    }

    void Update()
    {
        if (transform.parent == null) return;

        float targetScale = originalScaleX > 0f ? originalScaleX : Mathf.Abs(transform.localScale.x);
        if (targetScale <= 0f) targetScale = 1f;

        // Nếu quái vật cha bị lật âm, ta lật ngược lại Canvas để nó luôn hiển thị đúng chiều
        if (transform.parent.lossyScale.x < 0)
        {
            transform.localScale = new Vector3(-targetScale, transform.localScale.y, transform.localScale.z);
        }
        else
        {
            transform.localScale = new Vector3(targetScale, transform.localScale.y, transform.localScale.z);
        }
    }
}
