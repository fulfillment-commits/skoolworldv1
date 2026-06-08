using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ASAD_Multiplyer.Network;
using ASAD_Multiplyer.PlayerController;
using Photon.Pun;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneReference : MonoBehaviour
{
    public static SceneReference instance;

    public GameObject[] questSpawnPoints;
    // public bool cityScene;
    // public bool officeScene;
    public GameObject loadingCanvas;
    public Transform spawnPos;
    public GameObject env;
    // public CutSceneManager cutSceneManager;
    // public OfficeCutSceneManager officeCutSceneManager;

    public bool mainOfficeParent;

    public bool inOffice;
    private void Awake()
    {
        if (instance == null)
        { instance = this; }

        
        env?.SetActive(true);
    }

    private void Start()
    {
        // if (inOffice)
        // {
        //     ChatCanvas.Instance.sceneButtons.backToCityButton.gameObject.transform.GetChild(0)
        //         .GetComponent<TextMeshProUGUI>().text = "Exit From Office";
        //     URLImageRetriever.instance.backFromOffice = true;
        // }
        // else
        // {
        //     if (!mainOfficeParent)
        //     {
        //         URLImageRetriever.instance.backFromOffice = false;
        //     }
        //     
        //     // URLImageRetriever.instance.backFromOffice = false;
        //     ChatCanvas.Instance.sceneButtons.backToCityButton.gameObject.transform.GetChild(0)
        //         .GetComponent<TextMeshProUGUI>().text = "Back To City";
        // }
        // if (!cityScene)
        // CustomDebug.Log("conned "+PhotonNetwork.IsConnected);
        
        // if (cityScene)
        // {
        //     loadingCanvas.GetComponent<DeactivateObject>().enabled = PhotonNetwork.InRoom;
        // }
        // else if(!mainOfficeParent)
        // {
        //     URLImageRetriever.instance.officeScene = SceneManager.GetActiveScene().name;
        // }        
        // ChatCanvas.Instance?.sceneButtons.SetButtons(cityScene);
        
        // if (PhotonNetwork.InRoom)
        // {
        //     if (string.Equals("Sci_Fi Gallery", URLImageRetriever.instance.officeScene))
        //     {
        //         PUN_NetworkManager.nm.NewScene(loadingCanvas, spawnPos);
        //     }
        //     else
        //     {
        //         PUN_NetworkManager.nm.NewScene(loadingCanvas, spawnPos,false );
        //     }
        // }
        // else
        // {
        //     
        // }

        if (PhotonNetwork.InRoom && spawnPos == null)
        {
            string questNo = SceneTransitionManager.Instance.GetTargetSpawnPoint();
            spawnPos=questSpawnPoints.ToList().FirstOrDefault(a => a.name == questNo)?.transform;
            if (spawnPos == null) spawnPos = questSpawnPoints[0].transform;
        }
        PUN_NetworkManager.nm.NewScene(loadingCanvas, spawnPos,PhotonNetwork.InRoom);

        // if (cutSceneManager)
        // {
        //     cutSceneManager.StartDoorInAnimation();
        // }
    }

    public void BackToCityScene()
    {
        // if (cutSceneManager)
        // {
        //     cutSceneManager.StartDoorOutAnimation();
        //     cutSceneManager.AddOutAnimationEndEvent(BackToCity);
        // }
        // else if (officeCutSceneManager)
        // {
        //     officeCutSceneManager.mainGateCutScene.StartDoorOutAnimation();
        //     officeCutSceneManager.mainGateCutScene.AddOutAnimationEndEvent(BackToCity);
        // }
        // else
        // {
        //     BackToCity();
        // }
    }
    
    public void BackToCity()
    {
        PUN_NetworkManager.nm.loadingCanvas?.SetActive(true);
        // if (!inOffice)
        // {
        //     PhotonNetwork.LoadLevel((ChatCanvas.Instance.sceneButtons.isNightMode) ? "City_Night" : "City");
        // }
        // else
        // {
        //     // PhotonNetwork.LoadLevel("OfficeBuilding_Small");
        //     PhotonNetwork.LoadLevel(URLImageRetriever.instance.mainOfficeName);
        // }
    }
    
}
