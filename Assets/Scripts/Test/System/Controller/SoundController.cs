using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundController : MonoBehaviour
{
    // Start is called before the first frame update
    public void OnClick()
    {
        AudioManager.Inst.PlayOneShot("SFX_UI_Click");
    }

    public void OnClickMatching()
    {
        AudioManager.Inst.PlayOneShot("SFX_UI_ClickMatching");
    }

    public void OnClickCancel()
    {
        AudioManager.Inst.PlayOneShot("SFX_UI_ClickCancel");
    }

    public void OnClickMini()
    {
        AudioManager.Inst.PlayOneShot("SFX_UI_ClickMini");
    }

    public void OnSlider()
    {
        AudioManager.Inst.PlayOneShot("SFX_UI_Slider");
    }


    public void OnHotKey()
    {
        AudioManager.Inst.PlayOneShot("SFX_UI_Hotkey");
    }

    public void OnClickError()
    {
        AudioManager.Inst.PlayOneShot("SFX_UI_ClickError");
    }
    
    public void OnClickSelectCharacter()
    {
        AudioManager.Inst.PlayOneShot("SFX_UI_SelectCharacter");
    }

    public void OnHover()
    {
        AudioManager.Inst.PlayOneShot("SFX_UI_Hover");
    }

    public void OnOpenModal()
    {
        AudioManager.Inst.PlayOneShot("SFX_UI_OpenModal");
    }

    public void OnCloseModal()
    {
        AudioManager.Inst.PlayOneShot("SFX_UI_CloseModal");
    }

    public void OnOpenGameOverModal()
    {
        AudioManager.Inst.PlayOneShot("SFX_UI_OpenGameOverModal");
    }
}
