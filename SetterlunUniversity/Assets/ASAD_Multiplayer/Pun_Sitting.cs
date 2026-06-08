using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ASAD_Multiplyer.PlayerController;
using Invector.vCharacterController;
using Invector.vCharacterController.vActions;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

[System.Serializable]
public class SittinObj
{
    public int player=-1;
    public bool fill;
    public Transform sittingPos;
    public Transform endingPos;
}

public class Pun_Sitting : MonoBehaviourPunCallbacks
{
    public PhotonView view;
    public int viewId;
    public List<SittinObj> sittings = new List<SittinObj>();
    public Collider myCollider;
    private void OnEnable()
    {
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient)
        {
            // Send RPC to Master Client requesting data initialization
            view.RPC("RequestSittingDataFromMaster", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }
    private void Awake()
    {
        view = GetComponent<PhotonView>();
        if (view != null)
        {
            Destroy(view);
        }
        view=gameObject.AddComponent<PhotonView>();
        view.ViewID=viewId;
        // Debug.Log("Object Name: "+gameObject.name+" v id: "+view.ViewID +" view ID: "+viewId);
        // if (view == null)
        // {
        //     view = gameObject.AddComponent<PhotonView>();
        //     // PhotonNetwork.AllocateViewID(view);
        //     // CustomDebug.Log("view id "+view.ViewID +" Deug "+PhotonNetwork.IsMasterClient);
        // }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        // CustomDebug.Log(otherPlayer.ActorNumber);
        bool playerIsSit=sittings.Any(x => x.player == otherPlayer.ActorNumber);
        if (playerIsSit)
        {
            int index = sittings.FindIndex(x => x.player == otherPlayer.ActorNumber);
            sittings[index].fill = false;
            sittings[index].player = -1;
        }
        DetectFill();
    }


    private void OnTriggerEnter(Collider other)
    {
        
        if (other.gameObject.CompareTag("Player"))
        {
            bool hasEmptySeat = sittings.Any(sitting => !sitting.fill);
            PUN_SyncPlayer player = other.gameObject.GetComponent<PUN_SyncPlayer>();
            if (player.view.IsMine && !player.playingAnimation) 
            {
                player.sitting = this;
                if (hasEmptySeat)
                {
                    // CustomDebug.Log("player enter in sofa trigger");
                    // ChatCanvas.Instance.txtSit.text = "Press T to Sit";
                    // ChatCanvas.Instance.txtSit.transform.parent.gameObject.SetActive(true);
                    SetSittingAnimation(true, player.gameObject);
                }
                else
                {
                    // ChatCanvas.Instance.txtSit.text = "No seat available";
                }
                // ChatCanvas.Instance.txtSit.transform.parent.gameObject.SetActive(true);
            }
        }
    }

    
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            PUN_SyncPlayer player = other.gameObject.GetComponent<PUN_SyncPlayer>();
            if (player.view.IsMine && !player.playingAnimation)
            {
                player.sitting = null;
                // CustomDebug.Log("player enter in sofa trigger");
                // ChatCanvas.Instance.txtSit.transform.parent.gameObject.SetActive(false);
                SetSittingAnimation(false,player.gameObject);
            }
        }
    }

    public bool AddPlayer(PUN_SyncPlayer newPlayer)
    {
        for (int i = 0; i < sittings.Count; i++)
        {
            if (!sittings[i].fill)
            {
                newPlayer.GetComponent<vThirdPersonController>().strafeSpeed.rotateWithCamera = false;
                myCollider.enabled = false;
                
                newPlayer.transform.position = sittings[i].sittingPos.position;
                newPlayer.transform.rotation = sittings[i].sittingPos.rotation;
                sittings[i].player = PhotonNetwork.LocalPlayer.ActorNumber;
                sittings[i].fill = true;

                CustomDebug.Log($"Player {newPlayer.name} has been seated.");

                view.RPC("RPC_SetSeatFill", RpcTarget.AllBuffered, i, true,sittings[i].player);

                return true;
            }
        }

        CustomDebug.Log("No empty seats available!");
        return false;
    }
    
    public bool RemovePlayer(PUN_SyncPlayer playerToRemove)
    {
        for (int i = 0; i < sittings.Count; i++)
        {
            if (sittings[i].fill && sittings[i].player == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                if (sittings[i].endingPos != null)
                {
                    playerToRemove.transform.position = sittings[i].endingPos.position;
                }
                myCollider.enabled = true;
                sittings[i].player = -1;
                sittings[i].fill = false;
                

                CustomDebug.Log($"Player {playerToRemove.name} has left the seat.");
                view.RPC("RPC_SetSeatFill", RpcTarget.AllBuffered, i, false,-1);

                return true; 
            }
        }

        CustomDebug.Log("Player not found in any seat!");
        return false;
    }

    void SetSittingAnimation(bool set, GameObject player)
    {
        
        vGenericAnimation[] anims = player.GetComponents<vGenericAnimation>();
        foreach (var ani in anims)
        {
            if (string.Equals(ani.animationClip, "StandToSit"))
            {
                ani.enabled = set;
            }
        }
     }
    
    [PunRPC]
    void RPC_SetSeatFill(int seatIndex, bool isFilled,int actorNo)
    {
        // CustomDebug.Log("calllllllll");
        // myCollider.enabled = !isFilled;
        if (seatIndex >= 0 && seatIndex < sittings.Count)
        {
            sittings[seatIndex].player = actorNo;
            sittings[seatIndex].fill = isFilled;
        }
        DetectFill();
    }


    void DetectFill()
    {
        bool isEmptySeat = sittings.Any(sitting => !sitting.fill);
        if (sittings.Any(a => a.player == PhotonNetwork.LocalPlayer.ActorNumber))
        {
            return;
        }
        CustomDebug.Log("is empty "+ isEmptySeat);
        // if (!isEmptySeat)
        {
            PUN_SyncPlayer[] players = FindObjectsOfType<PUN_SyncPlayer>();
            foreach (var player in players)
            {
                if (player.view.IsMine && !player.playingAnimation && player.sitting == this)
                {
                    // ChatCanvas.Instance.txtSit.text = isEmptySeat?"Press T to Sit":"No seat available";
                    SetSittingAnimation(isEmptySeat,player.gameObject);
                }
            }
        }
        // else
        // {
        //     
        // }
    }
    
    
    
    [PunRPC]
    void RequestSittingDataFromMaster(int requestingPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            List<int> playerIds = sittings.Select(s => s.player).ToList();
            List<bool> seatFills = sittings.Select(s => s.fill).ToList();

            view.RPC("ReceiveSittingData", RpcTarget.OthersBuffered, playerIds.ToArray(), seatFills.ToArray());
        }
    }

    [PunRPC]
    void ReceiveSittingData(int[] playerIds, bool[] seatFills)
    {
        for (int i = 0; i < sittings.Count && i < playerIds.Length; i++)
        {
            sittings[i].player = playerIds[i];
            sittings[i].fill = seatFills[i];
        }

        DetectFill(); // Update UI and logic
    }
    
    
}
