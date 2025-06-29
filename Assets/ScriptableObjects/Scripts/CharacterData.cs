using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterData", menuName = "ScriptableObjects/Character/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("캐릭터 기본 정보")]
    public string characterName = "Default Character";
    public float startingHealth;
    public float startingShield;
    
    [Header("이동 속도")]
    public float moveSpeed;
    public float sprintSpeed;
    public float jumpForce;
}
