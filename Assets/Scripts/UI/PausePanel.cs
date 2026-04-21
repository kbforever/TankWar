using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GameFramework;
public class PausePanel : UIPanel
{
    private Button BackGame;
    private Button BackMenu;

    private GameFramework.GameFramework Framework => GameFramework.GameFramework.Instance;

    public override void Initialize()
    {
        base.Initialize();

        BackGame = transform.Find("BackGame").GetComponent<Button>();
        BackMenu = transform.Find("BackMenu").GetComponent<Button>();

        if(BackMenu!=null)
        {
            BackMenu.onClick.AddListener(OnBackMenu);
        }
        if(BackGame!=null)
        {
            BackGame.onClick.AddListener(OnBackGame);
        }

    }


    private void OnBackGame()
    {
        Framework.ChangeState(GameState.Playing);
    }

    private void OnBackMenu()
    {
        Framework.ChangeState(GameState.MainMenu);
    }



}
