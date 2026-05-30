using System.Collections;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class MenuFunction : MonoBehaviour
{
    public GameObject menubtn;
    public GameObject credit;
    public GameObject highscore;
    public string FirstVideoName;
    public string secondLoadVideoName;
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
            string videopath = System.IO.Path.Combine(Application.streamingAssetsPath,FirstVideoName);
            Vidplayer.url = videopath;
            GameManager.Instance.isfirstLoad = false;
        }
        else
        {
            string videopath = System.IO.Path.Combine(Application.streamingAssetsPath,secondLoadVideoName);
            Vidplayer.url = videopath;
        }

        double videoLength = Vidplayer.length;
        ImagePlayer.SetActive(true);
        Vidplayer.Play();

        

        yield return new WaitForSeconds((float)videoLength);

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

    public void playBoss()
    {
        SceneManager.LoadScene(4);    
    }

    public void quit()
    {
        Application.Quit();
    }
}