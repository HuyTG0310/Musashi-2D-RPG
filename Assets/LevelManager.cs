using System.Collections;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [Header("Tham chiếu lối ra")]
    public GameObject mapExitTrigger;


    private void Start()
    {
        if (mapExitTrigger != null)
        {
            mapExitTrigger.SetActive(false);
        }

        StartCoroutine(CheckEnemiesRoutine());
    }

    private IEnumerator CheckEnemiesRoutine()
    {
        while (true)
        {
            EnemyBase[] remainingEnemies = FindObjectsOfType<EnemyBase>();

            if (remainingEnemies.Length == 0)
            {
                if (mapExitTrigger != null)
                {
                    mapExitTrigger.SetActive(true);
                }


                Debug.Log("Clear Map!. Lối ra đã mở");
                break;
            }

            yield return new WaitForSeconds(1f);
        }
    }
}
