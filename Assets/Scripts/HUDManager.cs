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





    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(playerScript != null)
        {
            if(appleText != null)
            {
                appleText.text = "x" + playerScript.appleCount.ToString();
            }

            if(shurikenText != null)
            {
                shurikenText.text = "x" + playerScript.shurikenCount.ToString();
            }
        }
    }
}
