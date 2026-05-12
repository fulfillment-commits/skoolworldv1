using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.CrossPlatformInput;

public class GamePlayUIManager : MonoBehaviour
{
    public Button AuthenticateButton;
    public Slider AuthenticateSlider;
    public Button EnterVideoButton;

    public Button ExitVideoButton;

    public GameObject VideoControls;

    public TMP_Text MessageText;

    public MoveCamera moveCamera;

    [Header("Teleport UI")]
    public GameObject Door;

    public static GamePlayUIManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }





}
