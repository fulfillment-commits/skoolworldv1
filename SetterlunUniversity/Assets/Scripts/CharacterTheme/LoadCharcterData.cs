using System;
using System.Collections;
using System.Collections.Generic;
using Bozo.ModularCharacters;
using UnityEngine;

public class LoadCharcterData : MonoBehaviour
{
    [SerializeField] private CharacterData characterData;
    private OutfitSystem outfitSystem;
    private const string PLAYERPREFS_AVATAR_INDEX = "OnboardingAvatarIndex";
    private int outfitIndex
    {
        get { return PlayerPrefs.GetInt(PLAYERPREFS_AVATAR_INDEX, 0); }
    }
    private void Start()
    {
        outfitSystem = GetComponent<OutfitSystem>();
        if (outfitSystem != null)
        {
            outfitSystem.characterData=characterData.characterObjects[outfitIndex];
            outfitSystem.LoadFromObject();
        }
    }
}
