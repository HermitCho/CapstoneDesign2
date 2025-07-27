using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundController : Singleton<SoundController>
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
}
