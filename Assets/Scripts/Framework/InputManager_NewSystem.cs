using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.PlayerLoop;


public partial class InputManager
{
    private PlayerContorller inputActions;

    private bool isPaused;

    public bool IsPaused=>isPaused;

    public Dictionary<string,InputActionMap> AcitonByName=new Dictionary<string, InputActionMap>();


}
