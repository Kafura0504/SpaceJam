using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class MenuFunction : MonoBehaviour
{
    public GameObject menubtn;
    public GameObject credit;
    public GameObject highscore;
    public VideoClip FirstLoad;
    public VideoClip secondLoad;
    public VideoPlayer Vidplayer;
    public GameObject ImagePlayer;
    

    public void pickRandomLevel (){
        StartCoroutine(PlayLevel());
    }

    IEnumerator PlayLevel()
    {
        int rand = UnityEngine.Random.Range(0,3);
        string scenename ="";
        AsyncOperation LoadAsync;
        
        switch (rand)
        {
            case 0:
            scenename = "cryogla";
            break;

            case 1:
            scenename="noir331";
            break;

            case 2:
            scenename="turmos";
            break;
        }
        LoadAsync = SceneManager.LoadSceneAsync(scenename);
        LoadAsync.allowSceneActivation = false;
        

        if (GameManager.Instance.isfirstLoad)
        {
            Vidplayer.clip = FirstLoad;
            GameManager.Instance.isfirstLoad = false;
        }
        else
        {
            Vidplayer.clip = secondLoad;
        }

        ImagePlayer.SetActive(true);
        Vidplayer.Play();

        yield return new WaitForSeconds((float)Vidplayer.clip.length);

        LoadAsync.allowSceneActivation = true;
        
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