using UnityEngine;

public class HealthBarFix : MonoBehaviour
{
    private float originalScaleX;

    void Start()
    {
        originalScaleX = transform.localScale.x;
    }

    void Update()
    {
        // Nếu quái vật cha bị lật âm, ta lật ngược lại Canvas để nó luôn dương
        if (transform.parent.localScale.x < 0)
        {
            transform.localScale = new Vector3(-originalScaleX, transform.localScale.y, transform.localScale.z);
        }
        else
        {
            transform.localScale = new Vector3(originalScaleX, transform.localScale.y, transform.localScale.z);
        }
    }
}
