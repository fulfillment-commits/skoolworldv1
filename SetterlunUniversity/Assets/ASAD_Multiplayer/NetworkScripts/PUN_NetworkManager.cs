using System;
using System.Collections;
using UnityEngine;
using Photon.Pun;               //to acces Photon features
using Photon.Realtime;          //to access Photon callbacks
using UnityEngine.Events;       //to call actions on various states
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using Invector.vCamera;
using Invector.vCharacterController;
using Unity.VisualScripting;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Random = UnityEngine.Random;

namespace ASAD_Multiplyer.Network
{
    public class PUN_NetworkManager : MonoBehaviourPunCallbacks
    {
        public Transform cam;
        // public CompassPro compass;
        // public bl_MiniMap miniMap;
        private const string RoomName = "Veelive";
        private const string PlayerIdKey = "PlayerIds";
        private int targetSceneNumber;
        private string targetSceneName;
        Vector3 lastPosition=Vector3.zero;
        
        
        private readonly string[] adjectives =
        {
            "Swift", "Shadow", "Crazy", "Epic", "Silent",
            "Dark", "Golden", "Lucky", "Wild", "Fearless"
        };

        private readonly string[] nouns =
        {
            "Tiger", "Wolf", "Eagle", "Dragon", "Hunter",
            "Knight", "Warrior", "Sniper", "Falcon", "Ghost"
        };
        public int selectedCharacter
        {
            get
            {
                // return PlayerPrefs.GetInt("selectedCharacter", 0);
                return 0;
            }
        }

        public GameObject[] environments;
        public string _gameVersion = "1.0";

        [SerializeField]
        private byte maxPlayerPerRoom = 4;

        public GameObject[] playerPrefab = null;

        // public Transform[] spawnPoint = null;
        public Transform spawnPoint ;
        public GameObject myPlayer;

        public string _connectStatus = "";

        public bool _syncScenes = true;

        public UnityEvent _onJoinedRoom;
        public UnityEvent _onLeftRoom;
        public UnityEvent _onPlayerEnteredRoom; 
        public UnityEvent _onPlayerLeftRoom;
        [HideInInspector] public bool _connecting = false;

        public PhotonView view;
        Sprite userProfileSprite;

        #region Internal Use Variables

        public static PUN_NetworkManager nm = null;

        public GameObject loadingCanvas;

        LoadingScreen loadingScreen;

        #endregion

        private void Awake()
        {
            if (nm == null)
            {
                nm = this;
                DontDestroyOnLoad(this.gameObject);
                this.gameObject.name = gameObject.name + " Instance";
                view=gameObject.AddComponent<PhotonView>();
                view.ViewID = 3000;
            }
            else
            {
                Destroy(this.gameObject);
                return;
            }

            PhotonNetwork.KeepAliveInBackground = 180;
            // PhotonNetwork.NetworkingClient.LoadBalancingPeer.DisconnectTimeout = 60000;
            PhotonNetwork.AutomaticallySyncScene =
                _syncScenes;
            // Debug.Log("user room id: "+URLImageRetriever.instance.myRoomId);

            StartCoroutine(ConnetNow());

        }

        IEnumerator ConnetNow()
        {
            yield return new WaitForEndOfFrame();
            // yield return new WaitUntil(() => !string.IsNullOrEmpty(URLImageRetriever.instance.myRoomId));
            // SetPlayerName(URLImageRetriever.instance.userData.data.username +"$" + URLImageRetriever.instance.urlData.user_id +
            //               "$" +URLImageRetriever.instance.userData.data.profileImage.url+"$" +URLImageRetriever.instance.myRoomId);
            // StartCoroutine(LoadMiniMapIcon(URLImageRetriever.instance.userData.data.profileImage.url));
            SetPlayerName(GenerateRandomName());
            PhotonNetwork.NetworkingClient.UseAlternativeUdpPorts = true;
            // PhotonNetwork.NetworkingClient.LoadBalancingPeer.DebugOut = ExitGames.Client.Photon.DebugLevel.ALL;
            // PhotonNetwork.NetworkingClient.AddCallbackTarget(new DebugLogger());
            Connect();
        }

        string GenerateRandomName()
        {
            string adjective = adjectives[Random.Range(0, adjectives.Length)];
            string noun = nouns[Random.Range(0, nouns.Length)];
            int number = Random.Range(100, 999);

            return $"{adjective}{noun}{number}";
        }
        
        
        IEnumerator LoadMiniMapIcon(string url)
        {
            using (WWW www = new WWW(url))
            {
                yield return www;

                if (www.error == null)
                {
                    Texture2D texture = www.texture;
                    Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
                    // compass.miniMapPlayerIconSprite = sprite;
                    // miniMap.MiniMapUI.playerIcon.SetIcon(sprite,true);
                    // CustomDebug.Log("load");
                }
                else
                {
                    CustomDebug.Log("Failed to load mini map image: " + www.error +" URL: "+url);
                }
            }
        }
        
        public void InitScene(GameObject loading, Transform pos)
        {
            
            loadingCanvas = loading;
            spawnPoint = pos;
        }
        public void NewScene(GameObject loading, Transform pos , bool setPlayerPos=true)
        {
            if(loading!=null) loadingCanvas = loading;
            
            
            
            if (setPlayerPos)
            {
                
                myPlayer.GetComponent<vThirdPersonController>().MoveToPositionRotaion(pos);
                // myPlayer.GetComponent<vThirdPersonController>().RotateToPosition(pos.eulerAngles);
                // myPlayer.transform.position = pos.position;
            }
        }
        
        #region Callable Methods

        public void SetPlayerName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            PhotonNetwork.NickName = name;
        }

        public void Connect()
        {
            _connecting = true;
            _connectStatus = "Finding a room...";
            CustomDebug.Log("Connecting... " +PhotonNetwork.IsConnected);
            PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "us";
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.JoinRandomRoom();
                // PhotonNetwork.JoinLobby();
            }
            else
            {
                PhotonNetwork.GameVersion = _gameVersion;
                PhotonNetwork.NetworkingClient.LoadBalancingPeer.DisconnectTimeout = 10000;
                PhotonNetwork.ConnectUsingSettings();
            }
        }

        public void LeaveRoom()
        {
            PhotonNetwork.LeaveRoom();
        }

        public int GetPlayerCount()
        {
            return PhotonNetwork.CurrentRoom.PlayerCount;
        }

        public void NetworkLoadLevel(int level)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.LoadLevel(level);
            }
        }

        #endregion

        #region Photon Callback Methods

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            _onPlayerEnteredRoom.Invoke();
            base.OnPlayerEnteredRoom(newPlayer);
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            
            CustomDebug.Log("Player left with name "+otherPlayer.NickName);
            _onPlayerLeftRoom.Invoke();
            base.OnPlayerLeftRoom(otherPlayer);
        }

        
        public override void OnConnectedToMaster()
        {
            CustomDebug.Log("connect to master");
            if (_connecting == true)
            {
                _connectStatus = "Connected to master server, attemping to find a room...";
                PhotonNetwork.JoinLobby();
            }

            base.OnConnectedToMaster();
        }


        public override void OnJoinedLobby()
        {
            CustomDebug.Log("Successfully joined the lobby. Attempting to join a random room...");
            // JoinOrCreateVeeliveRoom();
            PhotonNetwork.JoinRandomRoom();
        }
        

        public override void OnDisconnected(DisconnectCause cause)
        {
            _connecting = false;
            _connectStatus = "Disconnected: " + cause;
            lastPosition = myPlayer.transform.position;
            PhotonNetwork.DestroyPlayerObjects(myPlayer.GetComponent<PhotonView>().ViewID, myPlayer);
            // if ( cause == DisconnectCause.ClientTimeout ||
            //     cause == DisconnectCause.ServerTimeout || cause == DisconnectCause.DisconnectByServerLogic)
            // {
            //     StartReconnection();
            // }
            base.OnDisconnected(cause);
        }

        private void StartReconnection()
        {
            // if (!isReconnecting)
            {
                // isReconnecting = true;
                CustomDebug.Log("Attempting to reconnect...");
                PhotonNetwork.Reconnect();
            }
        }
        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            _connectStatus = "Failed to find a room, creating one...";
            
            CustomDebug.Log($"Failed to find a room. Error code: {returnCode}, message: {message}");
            
            // PhotonNetwork.JoinRoom(RoomName);
            PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = maxPlayerPerRoom,PublishUserId=true });
            // PhotonNetwork.CreateRoom("Veelive", new RoomOptions { MaxPlayers = maxPlayerPerRoom,PublishUserId=true });
            base.OnJoinRandomFailed(returnCode, message);
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            // CustomDebug.Log($"JoinRoom failed: {message}. Creating a new room...");
            // If joining the room fails (perhaps because it doesn't exist), create it.
            // PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = maxPlayerPerRoom, PublishUserId = true });
        }
        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            CustomDebug.LogError($"Room creation failed. Error code: {returnCode}, message: {message}");
            // Here you can handle the failure, such as informing the user or trying another action.
        }
        public void RestPosition()
        {
            myPlayer.transform.position = spawnPoint.position;
            myPlayer.transform.rotation = spawnPoint.rotation;
            PhotonNetwork.LoadLevel(SceneManager.GetActiveScene().name);
        }
        public override void OnJoinedRoom()
        {
            _connecting = false;
            _connectStatus = "Successfully joined a room";_onJoinedRoom.Invoke();
            base.OnJoinedRoom();
            int rand = 0;
            if (playerPrefab != null)
            {
                // if (lastPosition == Vector3.zero)
                // {
                    // Transform spPos;
                    // Transform clonePose = FindObjectOfType<CityOfficeCutSceneManager>().GiveBuildingPoint();
                    // if (clonePose == null)
                    // {
                    //     spPos = spawnPoint;
                    //     
                    //    }
                    // else
                    // {
                    //     spPos = clonePose;
                    // }

                    // Debug.Log("name of spawn pos: "+spPos.name);
                    // myPlayer = PhotonNetwork.Instantiate(playerPrefab[selectedCharacter].name, spPos.position,
                    //     spPos.rotation, 0);
                // }
                // else
                {
                    myPlayer = PhotonNetwork.Instantiate(playerPrefab[selectedCharacter].name, spawnPoint.position,
                        spawnPoint.rotation, 0);
                }

                // miniMap.Target = myPlayer.transform;
                // compass.follow = myPlayer.transform;
                // compass.miniMapPlayerIconSprite.
            }
            CustomDebug.Log("RoomName: " + PhotonNetwork.CurrentRoom.Name);

            

            CustomDebug.Log($"Successfully joined room: {PhotonNetwork.CurrentRoom.Name}");

            // if (!string.IsNullOrEmpty(URLImageRetriever.instance.urlData.scene_name))
            // {
            //     CustomDebug.Log("redirect");
            //     if (string.Equals(URLImageRetriever.instance.urlData.scene_name, "Sci_Fi Gallery"))
            //     {
            //         URLImageRetriever.instance.EnterCityUserRoom(
            //             URLImageRetriever.instance.GetPartFromName(PhotonNetwork.LocalPlayer.NickName, 0)
            //             ,URLImageRetriever.instance.urlData.room_id);
            //     }
            //     else if (string.Equals("BigShoppingMall_F2", URLImageRetriever.instance.urlData.scene_name))
            //     {
            //         URLImageRetriever.instance.RedirectToShoppingMall(URLImageRetriever.instance.urlData.extras);
            //     }
            //     else
            //     {
            //         PhotonNetwork.LoadLevel(URLImageRetriever.instance.urlData.scene_name);
            //     }
            //     URLImageRetriever.instance.urlData.scene_name = null;
            // }
            // else
            {
                if (loadingCanvas != null)
                    loadingCanvas.SetActive(false);
            }
            
            
            // ChatCanvas.Instance.InitChat();
            // ChatCanvas.Instance.landUi.ShowLandUI();
            // RemoveOtherInstance();

        }

        
        public override void OnLeftRoom()
        {
            _onLeftRoom.Invoke();
            PhotonNetwork.DestroyPlayerObjects(myPlayer.GetComponent<PhotonView>().ViewID, myPlayer);
            base.OnLeftRoom();
        }

        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            CustomDebug.Log($"Room list updated. Total rooms: {roomList.Count} {PhotonNetwork.InRoom}");
        }

        
        
        #endregion

        private void RemoveOtherInstance()
        {
            foreach (var player in PhotonNetwork.PlayerList)
            {
                // if (URLImageRetriever.instance.GetPartFromName(player.NickName,1) == URLImageRetriever.instance.urlData.user_id &&
                //     player.ActorNumber != PhotonNetwork.LocalPlayer.ActorNumber)
                // {
                //     photonView.RPC("KickPlayer", RpcTarget.All, player.ActorNumber);
                // }
            }
        }

        #region PUN_RPC
        [PunRPC]
        public void KickPlayer(int actorNumber)
        {
            if (PhotonNetwork.LocalPlayer.ActorNumber == actorNumber)
            {
                
                CustomDebug.Log($"Player {PhotonNetwork.LocalPlayer.NickName} has been kicked from the room.");
                LeaveRoom();
                // Destroy(FindObjectOfType<ScreenTouchDetector>().gameObject);
            }
        }
        
        #endregion
    }
}
