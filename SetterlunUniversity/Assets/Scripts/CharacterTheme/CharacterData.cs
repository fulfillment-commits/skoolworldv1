using System.Collections;
using System.Collections.Generic;
using Bozo.ModularCharacters;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterTheme", menuName = "CharacterData")]
public class CharacterData : ScriptableObject
{
    public CharacterObject[]  characterObjects;
}
