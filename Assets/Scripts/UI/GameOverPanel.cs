using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameOverPanel : UIPanel
{

    GameFramework.GameFramework Framework =>GameFramework.GameFramework.Instance;
    Button TryAgain;
    Button BackMenu;

    public override void Initialize()
    {
        base.Initialize();
        TryAgain = transform.Find(nameof(TryAgain)).GetComponent<Button>();
        BackMenu = transform.Find(nameof(BackMenu)).GetComponent<Button>();


        if(TryAgain!=null) TryAgain.onClick.AddListener(()=> Framework.ChangeState(GameFramework.GameState.Playing));
        if(BackMenu!=null) BackMenu.onClick.AddListener(()=>Framework.ChangeState(GameFramework.GameState.MainMenu));
    }

    public override void OnShow()
    {
        base.OnShow();
        EventSystem.current.firstSelectedGameObject=TryAgain.gameObject;
    }
}
