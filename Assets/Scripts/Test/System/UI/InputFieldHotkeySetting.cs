using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InputFieldHotkeySetting : MonoBehaviour
{
    [SerializeField] private TMP_InputField[] inputFields;
    

    public void NextSelectInputField()
    {
        for(int i = 0; i < inputFields.Length; i++)
        {
            for(int j = i+1; j < inputFields.Length; j++)
            {
                if(inputFields[i].isFocused)
                {
                    AudioManager.Inst.PlayOneShot("SFX_UI_Hotkey");
                    inputFields[j].Select();
                    
                }
            }
        }

        if(inputFields[inputFields.Length-1].isFocused)
        {
            AudioManager.Inst.PlayOneShot("SFX_UI_Hotkey");
            inputFields[0].Select();
        }

    }
    
}
