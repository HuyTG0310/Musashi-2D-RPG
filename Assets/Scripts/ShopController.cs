using System.Collections;
using TMPro;
using UnityEngine;

public class ShopController : MonoBehaviour
{



    [Header("Shop UI")]
    public GameObject shopUI;
    private bool isPlayerinRange = false;
    private Player playerScript;


    [Header("ShopAudio")]
    public AudioSource audioSource;
    public AudioClip buySuccessSound;
    public AudioClip notEnoughCoinSound;

    [Header("UI Notifications")]
    public TextMeshProUGUI notificationText; // Kéo object NotificationText vào đây


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // nếu Musashi đang đứng gần và nhấn nút E thì mở/tắt shop
        if (isPlayerinRange && Input.GetKeyDown(KeyCode.E))
        {
            ToggleShop();
        }

    }

    public void ToggleShop()
    {
        // bật tắt Canvas
        shopUI.SetActive(!shopUI.activeSelf);
        // dừng thời giang game khi mở shop
        Time.timeScale = shopUI.activeSelf ? 0f : 1f;
    }





    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerinRange = true;
            playerScript = collision.GetComponent<Player>();
            Debug.Log("IN range");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerinRange = false;
            playerScript = null;
            shopUI.SetActive(false);    //tự động đóng shop nếu Musashi đi xa
        }
    }


    public void BuyApple()
    {
        int price = 10;
        if (playerScript != null && playerScript.coins >= price)
        {
            playerScript.coins -= price;
            playerScript.appleCount++;

            if(buySuccessSound != null)
            {
                audioSource.PlayOneShot(buySuccessSound);
            }

            StartCoroutine(ShowNotification("-10 coins: Buy 1 apple!", Color.green));
        }
        else
        {
            Debug.Log("Not enough coin");
            if (buySuccessSound != null)
            {
                audioSource.PlayOneShot(notEnoughCoinSound);
            }
            StartCoroutine(ShowNotification("Coins are not enough!", Color.red));
        }
    }


    public void BuyShurikens()
    {
        int price = 30;
        if (playerScript != null && playerScript.coins >= price)
        {
            playerScript.coins -= price;
            playerScript.shurikenCount += 5;

            if (buySuccessSound != null)
            {
                audioSource.PlayOneShot(buySuccessSound);
            }

            StartCoroutine(ShowNotification("-30 coins: Buy 5 shurikens!", Color.green));
        }
        else
        {
            Debug.Log("Not enough coin");
            if (buySuccessSound != null)
            {
                audioSource.PlayOneShot(notEnoughCoinSound);
            }
            StartCoroutine(ShowNotification("Coins are not enough!", Color.red));
        }
    }


    public void CloseShop()
    {
        shopUI.SetActive(false);
        Time.timeScale = 1f;
    }


    private IEnumerator ShowNotification(string message, Color textColor)
    {
        if (notificationText == null)
        {
            yield break;
        }


        notificationText.text = message;
        notificationText.color = textColor;
        notificationText.gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(1.5f);

        notificationText.gameObject.SetActive(false);
    }


}
