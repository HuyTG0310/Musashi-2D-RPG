using UnityEngine;
using UnityEngine.SceneManagement;

public class MapTransition : MonoBehaviour
{

    [Header("Tên Map tiếp theo")]
    public string nextMapName = "Map2_Oak Forest";

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Debug.Log("Đang chuyển sang: " + nextMapName);
            SceneManager.LoadScene(nextMapName);
        }
    }
}
