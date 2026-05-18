using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuFunction : MonoBehaviour
{
    public GameObject menubtn;
    public GameObject credit;
    public GameObject highscore;
    

    public void pickRandomLevel (){
        int rand = UnityEngine.Random.Range(0,3);
        switch (rand)
        {
            case 0:
            SceneManager.LoadScene("cryogla");
            break;

            case 1:
            SceneManager.LoadScene("noir331");
            break;

            case 2:
            SceneManager.LoadScene("turmos");
            break;
        }
    }

    public void backtomenu()
    {
        menubtn.SetActive(true);
        credit.SetActive(false);
        highscore.SetActive(false);
    }

    public void opencredit()
    {
        menubtn.SetActive(false);
        credit.SetActive(true);
        highscore.SetActive(false);
    }
    public void openhighscore()
    {
        menubtn.SetActive(false);
        credit.SetActive(false);
        highscore.SetActive(true);
    }

    public void quit()
    {
        Application.Quit();
    }
}