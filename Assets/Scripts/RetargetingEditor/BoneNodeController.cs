using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum BoneType
{
    SOURCE,
    TARGET,
    PAIRED,
}

public class BoneNodeController : MonoBehaviour
{
    public BoneType BoneType = BoneType.SOURCE;
    public int Index = -1;
    public string BoneName ="";

    // Only Used BoneType PAIRED 
    public int Index_T = -1;
    public string BoneName_T = "";

    private Image Image;
    private bool IsPressed = false;
    private Color NormalColor= new Color(1f, 1f, 1f);
    private Color PressedColor = new Color(0.78f, 0.78f, 0.78f);

    // Start is called before the first frame update
    void Start()
    {
        Image = this.GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsPressed) return;

        if ((BoneType == BoneType.SOURCE && BonePairingManager.CurSCtrl != this) ||
            (BoneType == BoneType.TARGET && BonePairingManager.CurTCtrl != this))
        {
            IsPressed = false;
            Image.color = NormalColor;
        }
    }

    // --------------------UI Trigger Function--------------------

    public void BoneNodeOnClick()
    {
        Debug.Log(BoneType + " : " + Index + " : " + BoneName);

        if (BoneType == BoneType.PAIRED)
        {
            if (IsPressed)
            {
                IsPressed = false;
                Image.color = NormalColor;
                BonePairingManager.SelPName.Remove(BoneName);
            }
            else
            {
                IsPressed = true;
                Image.color = PressedColor;
                BonePairingManager.SelPName.Add(BoneName);
            }
        }
        else
        {
            if (BoneType == BoneType.SOURCE) BonePairingManager.CurSCtrl = this;
            else if (BoneType == BoneType.TARGET) BonePairingManager.CurTCtrl = this;
            IsPressed = true;
            Image.color = PressedColor;
        }     
    }
}
