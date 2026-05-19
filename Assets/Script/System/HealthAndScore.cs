
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthAndScore : MonoBehaviour
{
    private HealthManager HP;
    public int score;
    public TextMeshProUGUI scoreUI;
    public Image healthBar;
    private float HpNormalize;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        HP = GameObject.FindGameObjectWithTag("Player").GetComponent<HealthManager>();
    }

    // Update is called once per frame
    void Update()
    {
        scoreUI.SetText(score.ToString());
        HpNormalize = HP.currenthealth/HP.maxHP;
        healthBar.fillAmount = HpNormalize;
    }
}
