using System;
using System.Collections;
using System.Collections.Generic;
using Bozo.ModularCharacters;
using UnityEngine;

public class LoadCharcterData : MonoBehaviour
{
    [SerializeField] private CharacterData characterData;
    [SerializeField] private OutfitSystem outfitSystem;
    private const string PLAYERPREFS_AVATAR_INDEX = "OnboardingAvatarIndex";
    private int outfitIndex
    {
        get { return PlayerPrefs.GetInt(PLAYERPREFS_AVATAR_INDEX, 0); }
    }
    public int LoadGetOutfit()
    {
        LoadOutfit(outfitIndex);
        return outfitIndex;
    }

    public void LoadOutfit(int index)
    {
        if (outfitSystem != null)
        {
            outfitSystem.characterData=characterData.characterObjects[index];
            outfitSystem.LoadFromObject();
        }
    }
}
