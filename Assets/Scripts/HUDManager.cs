using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HUDManager : MonoBehaviour
{
    [Header("Tham chiếu đến Musashi")]
    public Player playerScript;

    [Header("Các dòng chữ UI")]
    public TextMeshProUGUI appleText;
    public TextMeshProUGUI shurikenText;
    public TextMeshProUGUI coinText;
    public TextMeshProUGUI sakeText;

    [Header("Combat Stats Text")]
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI defenseText;

    void Start()
    {
        if (playerScript == null)
        {
            playerScript = Player.instance;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (playerScript == null)
        {
            playerScript = Player.instance;
        }

        if (playerScript != null)
        {
            if (appleText != null)
            {
                appleText.text = "x" + playerScript.appleCount.ToString();
            }

            if (shurikenText != null)
            {
                shurikenText.text = "x" + playerScript.shurikenCount.ToString();
            }

            if (coinText != null)
            {
                coinText.text = "x" + playerScript.coins.ToString();
            }

            if (sakeText != null)
            {
                sakeText.text = "x" + playerScript.sakeCount.ToString();
            }

            if (attackText != null)
            {
                attackText.text = "ATK: " + playerScript.attackDamage.ToString();
            }

            if (defenseText != null)
            {
                defenseText.text = "DEF: " + playerScript.defensePower.ToString();
            }
        }
    }
}
