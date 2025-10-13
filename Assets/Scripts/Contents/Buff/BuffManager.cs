using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class BuffManager : MonoBehaviour
{
    public static BuffManager Instance { get; private set; }

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else 
        { 
            if(Instance != this) Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(this);


        OnAwakeProcess();
        OnAwakePool();
        OnAwakeRequest();
        OnAwakeGrid();
    }

    private void Update()
    {
        OnUpdateRequest();
    }
    #endregion

    #region Public Methods

    #endregion

}
