using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameFramework;
using System;
using TMPro;


public class InfoContainer : MonoBehaviour
{


    [SerializeField] private GameObject EnmeyCountContainer;
    [SerializeField] private GameObject itemPrefab;

    [SerializeField] private TextMeshProUGUI P1HealthText;
    [SerializeField] private TextMeshProUGUI P2HealthText;


    private int curEnemyCount;

    private GameFramework.GameFramework Framework=>GameFramework.GameFramework.Instance;

    private List<GameObject> itemobjs;
    private void Awake()
    {
        Framework.SubscribeEvent<GameCoreManager.EnemyCountChangedEvent>(UpdateVisual);
        itemobjs=new List<GameObject>();
        
    }

    private void UpdateVisual(GameCoreManager.EnemyCountChangedEvent countChangedEvent)
    {
        curEnemyCount = countChangedEvent.enemyCount;
        P1HealthText.text = countChangedEvent.P1Health.ToString();
        P2HealthText.text = countChangedEvent.P2Health.ToString();

        
        ClearItems();
        if (itemPrefab != null)
        {
            for (int i = 0; i < curEnemyCount; i++)
            {
                CreateItem();
            }
        }
    }
    

    void CreateItem()
    {
        var ItemObj = Instantiate(itemPrefab);
        ItemObj.transform.SetParent(EnmeyCountContainer.transform);
        itemobjs.Add(ItemObj);
    }


    void ClearItems()
    {
        foreach(var itemobj in itemobjs)
        {
            Destroy(itemobj);
        }
        itemobjs.Clear();
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
