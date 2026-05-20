    using System;
    using TMPro;
    using UnityEngine;
using UnityEngine.UI;

public class LevelUpManager : MonoBehaviour
    {   

        [System.Serializable]
        public struct MenuContent
        {
            public TextMeshProUGUI Title;
            public Image img;
            public TextMeshProUGUI Desc;
        }
        public BuffData[] allBuffs;

        public BuffData[] currentChoices = new BuffData[3];
        public GameObject buffMenu;
        public MenuContent[] Menu;
        private GameObject player;
        private PlayerStat stat;
        private Exp level;
        void Awake()
        {
            player = GameObject.FindGameObjectWithTag("Player");
            stat = player.GetComponent<PlayerStat>();
            level = player.GetComponent<Exp>();
            level.OnLevelUp += openBuffMenu;
        }

        void OnEnable()
        {
            
        }
        void OnDisable()
        {
            level.OnLevelUp -= openBuffMenu;
        }

        public void GenerateChoices()
        {
            for(int i = 0; i < 3; i++)
            {
                int rand = UnityEngine.Random.Range(0, allBuffs.Length);

                currentChoices[i] = allBuffs[rand];
            }
        }

        void openBuffMenu()
        {
            GenerateChoices();

            for (int i = 0; i < Menu.Length; i++)
            {
                Menu[i].Title.SetText(currentChoices[i].buffName);
                Menu[i].Desc.SetText(currentChoices[i].description);
                Menu[i].img.sprite = currentChoices[i].Image;
            }
            Time.timeScale = 0f;
            buffMenu.SetActive(true);
        }

        public void buttonbuffA()
        {
            stat.ApplyBuff(currentChoices[0]);
            Time.timeScale = 1f;
            buffMenu.SetActive(false);
        }
        public void buttonbuffB()
        {
            stat.ApplyBuff(currentChoices[1]);
            Time.timeScale = 1f;
            buffMenu.SetActive(false);
        }
        public void buttonbuffC()
        {
            stat.ApplyBuff(currentChoices[2]);
            Time.timeScale = 1f;
            buffMenu.SetActive(false);
        }
    }
