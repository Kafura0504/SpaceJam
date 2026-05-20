using TMPro;
using UnityEngine;

public class MagSize : MonoBehaviour
{
    public PlayerShooting mag;
    public TextMeshProUGUI text;

    void Awake()
    {
        mag = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerShooting>();
        text = GetComponent<TextMeshProUGUI>();
    }
    void Update()
    {
        text.SetText(mag.currentMag+"/"+mag.maxMagazine);
    }
}
