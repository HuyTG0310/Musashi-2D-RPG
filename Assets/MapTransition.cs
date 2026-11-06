using UnityEngine;
using UnityEngine.SceneManagement;

public class MapTransition : MonoBehaviour
{

    [Header("Map4_Desert")]
    public string nextMapName = "Map4_Desert";

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Debug.Log("Đang chuyển sang: " + nextMapName);
            SceneManager.LoadScene(nextMapName);
        }
    }
}
