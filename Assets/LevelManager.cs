using System.Collections;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [Header("Tham chiếu lối ra")]
    public GameObject mapExitTrigger;

    [Header("Tham chiếu Boss (Map 4)")]
    public GameObject bossToSpawn;


    private void Start()
    {
        if (mapExitTrigger != null)
        {
            mapExitTrigger.SetActive(false);
        }

        if (bossToSpawn != null)
        {
            bossToSpawn.SetActive(false);
        }

        StartCoroutine(CheckEnemiesRoutine());
    }

    private IEnumerator CheckEnemiesRoutine()
    {
        yield return null;

        while (true)
        {
            EnemyBase[] remainingEnemies = FindObjectsOfType<EnemyBase>();

            if (remainingEnemies.Length == 0)
            {
                if (mapExitTrigger != null)
                {
                    mapExitTrigger.SetActive(true);
                }

                if (bossToSpawn != null)
                {
                    bossToSpawn.SetActive(true);
                }


                Debug.Log("Clear Map!. Lối ra đã mở");
                break;
            }

            yield return new WaitForSeconds(1f);
        }
    }
}
