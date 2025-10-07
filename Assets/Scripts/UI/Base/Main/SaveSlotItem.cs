using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.UI;

public class SaveSlotItem : UI_Base
{
    public int slotID;
    public Image m_SaveImage;
    public Image m_SaveBGImage;
    public TextMeshProUGUI m_CreateTimeText;
    public TextMeshProUGUI m_PlayTimeText;

    public MenuUI m_MenuUI;
    public bool m_havingData { get; private set; }

    [Header("Sounds")]
    public AudioClip uiButtonClick;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        m_SaveBGImage.gameObject.BindEvent(() => Click());
        m_SaveImage.gameObject.BindEvent(() => Click());

        return true;
    }

    public void SetData(Sprite image, string createTime, double seconds)
    {
        m_SaveImage.sprite = image;

        m_CreateTimeText.text = createTime;

        TimeSpan t = TimeSpan.FromSeconds(seconds);
        m_PlayTimeText.text = $"{(int)t.TotalHours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";

        RefreshUI();
    }

    private async void Click()
    {
        // 복붙 기능이 켜져 있음. 원하는 슬롯을 클릭하면 복붙을 함.
        if(m_MenuUI.m_IsCopying)
        {
            await Managers.Data.Copy(m_MenuUI.m_iselectSlotID, slotID);
            m_MenuUI.RefreshUI(); // 슬롯 데이터 갱신
            m_MenuUI.CopyComplete();
        }

        m_MenuUI.SlotsClickCancel();
        m_MenuUI.IsClickingSlot(m_havingData);
        m_MenuUI.m_iselectSlotID = slotID;

        m_SaveBGImage.color = Color.red;

        RefreshUI();
    }

    public void ClickCancle()
    {
        m_SaveBGImage.color = Color.white;
    }

    public void ClearData()
    {
        m_CreateTimeText.text = null;
        m_PlayTimeText.text = null;
        m_SaveImage.sprite = null;
        m_havingData = false;
    }

    public override void RefreshUI()
    {
        //Debug.Log("Save Slot Item Refresh");

        // Image
        if (m_SaveImage.sprite == null)
            m_havingData = false;
        else
            m_havingData = true;

        // Create Time & Play Time

        if (string.IsNullOrEmpty(m_CreateTimeText.text))
        {
            m_CreateTimeText.gameObject.SetActive(false);
            m_PlayTimeText.gameObject.SetActive(false);
        }
        else
        {
            m_CreateTimeText.gameObject.SetActive(true);
            m_PlayTimeText.gameObject.SetActive(true);
        }
    }

}
