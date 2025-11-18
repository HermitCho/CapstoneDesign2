using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class AdrenalinEffect : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private ParticleSystem effect;

    void Awake()
    {
        if(effect == null) effect = GetComponent<ParticleSystem>();
    }
}
