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
using ASAD_Multiplyer.Chat;
using ASAD_Multiplyer.PlayerController;
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
        private const string PlayerPrefsUserId = "OnboardingUserId_Str";
        private const string PlayerPrefsUsername = "OnboardingUsername";
        private int targetSceneNumber;
        private string targetSceneName;
        Vector3 lastPosition=Vector3.zero;
        [Header("Reconnect")]
        [SerializeField] private bool autoReconnectOnUnexpectedDisconnect = true;
        [SerializeField] private int maxReconnectAttempts = 4;
        [SerializeField] private float reconnectRetryDelay = 2f;
        [SerializeField] private float reconnectAttemptTimeout = 10f;
        [SerializeField] private float restoreReadyDelay = 0.5f;

        private bool manualDisconnectRequested;
        private bool restoreAfterReconnect;
        private bool waitingForReconnectAndRejoin;
        private Coroutine reconnectRoutine;
        private Coroutine restoreRoutine;
        private float nextRestoreStateSaveTime;
        private readonly ReconnectRestoreState reconnectRestoreState = new ReconnectRestoreState();
        
        
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
        [Tooltip("Leave empty to let Photon use the app/dashboard region settings. Set only if your Photon app has that region enabled, for example us, eu, asia, in.")]
        public string fixedRegion = "";

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

        private class ReconnectRestoreState
        {
            public bool hasState;
            public string sceneName;
            public string roomName;
            public string nickname;
            public Vector3 position;
            public Quaternion rotation = Quaternion.identity;
        }

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

            PhotonNetwork.KeepAliveInBackground = 10;
            PhotonNetwork.SendRate = 30;
            PhotonNetwork.SerializationRate = 30;
            // PhotonNetwork.NetworkingClient.LoadBalancingPeer.DisconnectTimeout = 60000;
            PhotonNetwork.AutomaticallySyncScene =
                _syncScenes;
            // Debug.Log("user room id: "+URLImageRetriever.instance.myRoomId);

            // StartCoroutine(ConnetNow());

        }

        public void ConnetNow()
        {
            manualDisconnectRequested = false;
            restoreAfterReconnect = false;
            waitingForReconnectAndRejoin = false;
            // yield return new WaitForEndOfFrame();
            // yield return new WaitUntil(() => !string.IsNullOrEmpty(URLImageRetriever.instance.myRoomId));
            // SetPlayerName(URLImageRetriever.instance.userData.data.username +"$" + URLImageRetriever.instance.urlData.user_id +
            //               "$" +URLImageRetriever.instance.userData.data.profileImage.url+"$" +URLImageRetriever.instance.myRoomId);
            // StartCoroutine(LoadMiniMapIcon(URLImageRetriever.instance.userData.data.profileImage.url));
            SetPlayerName(BuildInitialPlayerName());
            PhotonNetwork.NetworkingClient.UseAlternativeUdpPorts = true;
            // PhotonNetwork.NetworkingClient.LoadBalancingPeer.DebugOut = ExitGames.Client.Photon.DebugLevel.ALL;
            // PhotonNetwork.NetworkingClient.AddCallbackTarget(new DebugLogger());
            Connect();
        }

        private void Update()
        {
            if (!autoReconnectOnUnexpectedDisconnect || restoreAfterReconnect || Time.unscaledTime < nextRestoreStateSaveTime)
            {
                return;
            }

            nextRestoreStateSaveTime = Time.unscaledTime + 2f;
            CaptureReconnectRestoreState();
        }

        string GenerateRandomName()
        {
            string adjective = adjectives[Random.Range(0, adjectives.Length)];
            string noun = nouns[Random.Range(0, nouns.Length)];
            int number = Random.Range(100, 999);

            return $"{adjective}{noun}{number}";
        }

        string BuildInitialPlayerName()
        {
            string userId = PlayerPrefs.GetString(PlayerPrefsUserId, "");
            string username = PlayerPrefs.GetString(PlayerPrefsUsername, "");

            if (!string.IsNullOrWhiteSpace(userId))
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    username = GenerateRandomName();
                }

                return ChatIdentityUtility.BuildPunNickname(username, userId);
            }

            return GenerateRandomName();
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
                PUN_SyncPlayer localPlayer = GetLocalPlayerSync();
                if (localPlayer != null)
                {
                    myPlayer = localPlayer.gameObject;
                    localPlayer.TeleportLocalPlayerTo(pos);
                }
                else if (myPlayer != null)
                {
                    myPlayer.GetComponent<vThirdPersonController>().MoveToPositionRotaion(pos);
                }
                // myPlayer.GetComponent<vThirdPersonController>().RotateToPosition(pos.eulerAngles);
                // myPlayer.transform.position = pos.position;
            }
        }

        private PUN_SyncPlayer GetLocalPlayerSync()
        {
            if (myPlayer != null)
            {
                PhotonView playerView = myPlayer.GetComponent<PhotonView>();
                PUN_SyncPlayer playerSync = myPlayer.GetComponent<PUN_SyncPlayer>();
                if (playerView != null && playerView.IsMine && playerSync != null)
                {
                    return playerSync;
                }
            }

            PUN_SyncPlayer[] players = FindObjectsOfType<PUN_SyncPlayer>();
            foreach (PUN_SyncPlayer player in players)
            {
                if (player == null)
                {
                    continue;
                }

                PhotonView playerView = player.view != null ? player.view : player.GetComponent<PhotonView>();
                if (playerView != null && playerView.IsMine)
                {
                    return player;
                }
            }

            return null;
        }

        private void CaptureReconnectRestoreState()
        {
            if (myPlayer == null)
            {
                return;
            }

            reconnectRestoreState.hasState = true;
            reconnectRestoreState.sceneName = SceneManager.GetActiveScene().name;
            if (PhotonNetwork.CurrentRoom != null)
            {
                reconnectRestoreState.roomName = PhotonNetwork.CurrentRoom.Name;
            }

            reconnectRestoreState.nickname = PhotonNetwork.NickName;
            reconnectRestoreState.position = myPlayer.transform.position;
            reconnectRestoreState.rotation = myPlayer.transform.rotation;
            lastPosition = reconnectRestoreState.position;
        }

        private string GetReconnectStateLog()
        {
            return $"room='{reconnectRestoreState.roomName}', scene='{reconnectRestoreState.sceneName}', nickname='{reconnectRestoreState.nickname}', position={reconnectRestoreState.position}, rotation={reconnectRestoreState.rotation.eulerAngles}";
        }

        private bool ShouldAutoReconnect(DisconnectCause cause)
        {
            if (!autoReconnectOnUnexpectedDisconnect || manualDisconnectRequested || restoreAfterReconnect)
            {
                return false;
            }

            if (!reconnectRestoreState.hasState)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(PlayerPrefs.GetString(PlayerPrefsUserId, "")))
            {
                return false;
            }

            return cause != DisconnectCause.DisconnectByClientLogic;
        }

        private void BeginAutoReconnect(DisconnectCause cause)
        {
            if (reconnectRoutine != null)
            {
                return;
            }

            restoreAfterReconnect = true;
            waitingForReconnectAndRejoin = false;
            _connecting = true;
            _connectStatus = "Connection lost. Reconnecting...";
            CustomDebug.Log($"[Reconnect] Starting auto reconnect after disconnect: {cause}");
            CustomDebug.Log($"[Reconnect] Saved restore state: {GetReconnectStateLog()}");

            if (ScreenManager.Instance != null)
            {
                ScreenManager.Instance.ShowLoadingScreen("Connection lost. Reconnecting...");
            }

            if (myPlayer != null)
            {
                Destroy(myPlayer);
                myPlayer = null;
            }

            reconnectRoutine = StartCoroutine(AutoReconnectRoutine());
        }

        private IEnumerator AutoReconnectRoutine()
        {
            yield return WaitUntilPhotonDisconnected();

            for (int attempt = 1; attempt <= Mathf.Max(1, maxReconnectAttempts); attempt++)
            {
                _connectStatus = $"Reconnecting... ({attempt}/{maxReconnectAttempts})";
                UpdateReconnectLoading(_connectStatus);
                CustomDebug.Log($"[Reconnect] Attempt {attempt}/{maxReconnectAttempts} started. {GetReconnectStateLog()}");

                if (!string.IsNullOrWhiteSpace(reconnectRestoreState.nickname))
                {
                    SetPlayerName(reconnectRestoreState.nickname);
                }
                else
                {
                    SetPlayerName(BuildInitialPlayerName());
                }

                waitingForReconnectAndRejoin = attempt == 1 && !string.IsNullOrWhiteSpace(reconnectRestoreState.roomName);
                bool started = waitingForReconnectAndRejoin && PhotonNetwork.ReconnectAndRejoin();
                if (waitingForReconnectAndRejoin)
                {
                    CustomDebug.Log($"[Reconnect] ReconnectAndRejoin requested for room '{reconnectRestoreState.roomName}'. Started={started}");
                }

                if (!started)
                {
                    waitingForReconnectAndRejoin = false;
                    PhotonNetwork.GameVersion = _gameVersion;
                    PhotonNetwork.NetworkingClient.LoadBalancingPeer.DisconnectTimeout = 120000;
                    started = PhotonNetwork.ConnectUsingSettings();
                    CustomDebug.Log($"[Reconnect] Falling back to ConnectUsingSettings. Started={started}");
                }

                if (!started)
                {
                    CustomDebug.Log($"[Reconnect] Attempt {attempt}/{maxReconnectAttempts} could not start. Retrying in {reconnectRetryDelay:0.##}s.");
                    yield return new WaitForSecondsRealtime(reconnectRetryDelay);
                    continue;
                }

                float timeoutAt = Time.realtimeSinceStartup + Mathf.Max(3f, reconnectAttemptTimeout);
                while (Time.realtimeSinceStartup < timeoutAt)
                {
                    if (PhotonNetwork.InRoom)
                    {
                        CustomDebug.Log($"[Reconnect] Attempt {attempt}/{maxReconnectAttempts} reached room '{PhotonNetwork.CurrentRoom?.Name}'. Waiting for OnJoinedRoom restore.");
                        reconnectRoutine = null;
                        yield break;
                    }

                    yield return null;
                }

                waitingForReconnectAndRejoin = false;
                CustomDebug.Log($"[Reconnect] Attempt {attempt}/{maxReconnectAttempts} timed out before joining a room.");

                if (PhotonNetwork.IsConnected)
                {
                    PhotonNetwork.Disconnect();
                    yield return WaitUntilPhotonDisconnected();
                }

                yield return new WaitForSecondsRealtime(reconnectRetryDelay);
            }

            reconnectRoutine = null;
            restoreAfterReconnect = false;
            waitingForReconnectAndRejoin = false;
            _connecting = false;
            _connectStatus = "Reconnect failed";
            UpdateReconnectLoading("Reconnect failed. Please refresh or try again.");
            Debug.LogWarning("[Reconnect] Auto reconnect failed after all attempts.");
        }

        private IEnumerator WaitUntilPhotonDisconnected()
        {
            float timeoutAt = Time.realtimeSinceStartup + 3f;
            while (PhotonNetwork.IsConnected && Time.realtimeSinceStartup < timeoutAt)
            {
                yield return null;
            }
        }

        private void UpdateReconnectLoading(string status)
        {
            if (ScreenManager.Instance == null)
            {
                return;
            }

            LoadingScreen loading = ScreenManager.Instance.GetLoadingScreen();
            if (loading != null)
            {
                loading.SetStatus(status);
            }
        }

        private void JoinRestoreRoomOrFallback()
        {
            if (!string.IsNullOrWhiteSpace(reconnectRestoreState.roomName))
            {
                _connectStatus = "Rejoining previous room...";
                CustomDebug.Log($"[Reconnect] Connected to master. Joining previous room '{reconnectRestoreState.roomName}'.");
                PhotonNetwork.JoinRoom(reconnectRestoreState.roomName);
            }
            else
            {
                _connectStatus = "Finding a room...";
                CustomDebug.Log("[Reconnect] Connected to master. No previous room saved, joining a random room.");
                PhotonNetwork.JoinRandomRoom();
            }
        }

        private IEnumerator CompleteReconnectRestoreRoutine()
        {
            UpdateReconnectLoading("Restoring session...");
            CustomDebug.Log($"[Reconnect] Restore routine started. {GetReconnectStateLog()}");

            string targetScene = reconnectRestoreState.sceneName;
            if (!string.IsNullOrWhiteSpace(targetScene) && SceneManager.GetActiveScene().name != targetScene)
            {
                CustomDebug.Log($"[Reconnect] Loading restore scene '{targetScene}' from current scene '{SceneManager.GetActiveScene().name}'.");
                AsyncOperation sceneLoad = SceneManager.LoadSceneAsync(targetScene);
                if (sceneLoad != null)
                {
                    while (!sceneLoad.isDone)
                    {
                        LoadingScreen loading = ScreenManager.Instance != null ? ScreenManager.Instance.GetLoadingScreen() : null;
                        if (loading != null)
                        {
                            loading.SetProgress(sceneLoad.progress);
                        }

                        yield return null;
                    }
                }

                CustomDebug.Log($"[Reconnect] Restore scene loaded: '{SceneManager.GetActiveScene().name}'.");
            }

            for (int i = 0; i < 2; i++)
            {
                yield return new WaitForEndOfFrame();
            }

            float timeoutAt = Time.realtimeSinceStartup + 5f;
            PUN_SyncPlayer localPlayer = GetLocalPlayerSync();
            while (localPlayer == null && Time.realtimeSinceStartup < timeoutAt)
            {
                localPlayer = GetLocalPlayerSync();
                yield return null;
            }

            if (localPlayer != null)
            {
                myPlayer = localPlayer.gameObject;
                PhotonView localView = myPlayer.GetComponent<PhotonView>();
                CustomDebug.Log($"[Reconnect] Local clone found for restore. name='{myPlayer.name}', viewId={(localView != null ? localView.ViewID : 0)}");
                localPlayer.TeleportLocalPlayerTo(reconnectRestoreState.position, reconnectRestoreState.rotation);
                CustomDebug.Log($"[Reconnect] Local clone restored to position={reconnectRestoreState.position}, rotation={reconnectRestoreState.rotation.eulerAngles}");
            }
            else if (myPlayer != null)
            {
                CustomDebug.Log($"[Reconnect] Local PUN_SyncPlayer was not found, restoring myPlayer transform directly. name='{myPlayer.name}'");
                myPlayer.transform.SetPositionAndRotation(reconnectRestoreState.position, reconnectRestoreState.rotation);
                Physics.SyncTransforms();
            }
            else
            {
                CustomDebug.LogError("[Reconnect] Restore failed: local clone was not found after rejoining.");
            }

            if (restoreReadyDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(restoreReadyDelay);
            }

            restoreAfterReconnect = false;
            waitingForReconnectAndRejoin = false;
            reconnectRoutine = null;
            restoreRoutine = null;
            _connecting = false;
            _connectStatus = "Reconnected";

            if (ScreenManager.Instance != null)
            {
                LoadingScreen loading = ScreenManager.Instance.GetLoadingScreen();
                if (loading != null)
                {
                    loading.Hide();
                }

                ScreenManager.Instance.ShowScreen(ScreenType.MainWorld);
            }

            CustomDebug.Log("[Reconnect] Restore complete.");
        }
        
        #region Callable Methods

        public void SetPlayerName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            PhotonNetwork.NickName = name;
            if (ChatIdentityUtility.TryParsePunNickname(name, out _, out string firebaseUid))
            {
                PhotonNetwork.AuthValues = new AuthenticationValues(firebaseUid);
            }
        }

        public void Connect()
        {
            _connecting = true;
            _connectStatus = "Finding a room...";
            CustomDebug.Log("Connecting... " +PhotonNetwork.IsConnected);
            if (string.IsNullOrWhiteSpace(fixedRegion))
            {
                PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = string.Empty;
                ServerSettings.ResetBestRegionCodeInPreferences();
            }
            else
            {
                PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = fixedRegion.Trim().ToLowerInvariant();
            }
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.JoinRandomRoom();
                // PhotonNetwork.JoinLobby();
            }
            else
            {
                PhotonNetwork.GameVersion = _gameVersion;
                PhotonNetwork.NetworkingClient.LoadBalancingPeer.DisconnectTimeout = 120000;
                PhotonNetwork.ConnectUsingSettings();
            }
        }

        public void LeaveRoom()
        {
            manualDisconnectRequested = true;
            PhotonNetwork.LeaveRoom();
        }

        public void ShutdownForLogout()
        {
            manualDisconnectRequested = true;
            restoreAfterReconnect = false;
            waitingForReconnectAndRejoin = false;
            if (reconnectRoutine != null)
            {
                StopCoroutine(reconnectRoutine);
                reconnectRoutine = null;
            }

            if (restoreRoutine != null)
            {
                StopCoroutine(restoreRoutine);
                restoreRoutine = null;
            }

            _connecting = false;
            _connectStatus = "Logging out";

            if (myPlayer != null)
            {
                PhotonView playerView = myPlayer.GetComponent<PhotonView>();
                try
                {
                    if (PhotonNetwork.InRoom && playerView != null && playerView.IsMine)
                    {
                        PhotonNetwork.Destroy(myPlayer);
                    }
                    else
                    {
                        Destroy(myPlayer);
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"[PUN_NetworkManager] Could not destroy network player cleanly during logout: {exception.Message}");
                    Destroy(myPlayer);
                }

                myPlayer = null;
            }

            PhotonNetwork.AuthValues = null;
            PhotonNetwork.NickName = string.Empty;

            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Disconnect();
            }
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
            if (restoreAfterReconnect)
            {
                CustomDebug.Log($"[Reconnect] OnConnectedToMaster received. waitingForReconnectAndRejoin={waitingForReconnectAndRejoin}");
                if (!waitingForReconnectAndRejoin)
                {
                    JoinRestoreRoomOrFallback();
                }

                base.OnConnectedToMaster();
                return;
            }

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
            if (restoreAfterReconnect)
            {
                CustomDebug.Log("[Reconnect] Joined lobby during restore. Joining random fallback room.");
                PhotonNetwork.JoinRandomRoom();
                return;
            }

            // JoinOrCreateVeeliveRoom();
            PhotonNetwork.JoinRandomRoom();
        }
        

        public override void OnDisconnected(DisconnectCause cause)
        {
            CaptureReconnectRestoreState();
            _connecting = false;
            _connectStatus = "Disconnected: " + cause;
            CustomDebug.Log($"[Reconnect] OnDisconnected cause={cause}, manual={manualDisconnectRequested}, restoreAfterReconnect={restoreAfterReconnect}, hasState={reconnectRestoreState.hasState}");
            if (myPlayer != null)
            {
                lastPosition = myPlayer.transform.position;
            }

            if (ShouldAutoReconnect(cause))
            {
                BeginAutoReconnect(cause);
            }

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
            if (restoreAfterReconnect)
            {
                CustomDebug.Log("[Reconnect] Random fallback room not found. Creating a new restore room.");
            }
            
            // PhotonNetwork.JoinRoom(RoomName);
            PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = maxPlayerPerRoom,PublishUserId=true });
            // PhotonNetwork.CreateRoom("Veelive", new RoomOptions { MaxPlayers = maxPlayerPerRoom,PublishUserId=true });
            base.OnJoinRandomFailed(returnCode, message);
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            if (restoreAfterReconnect)
            {
                waitingForReconnectAndRejoin = false;
                CustomDebug.Log($"[Reconnect] Previous room join failed. Finding fallback room. Error code: {returnCode}, message: {message}");
                PhotonNetwork.JoinRandomRoom();
                return;
            }

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
            _connecting = restoreAfterReconnect;
            _connectStatus = "Successfully joined a room";_onJoinedRoom.Invoke();
            base.OnJoinedRoom();
            CustomDebug.Log($"[Reconnect] OnJoinedRoom. restoreAfterReconnect={restoreAfterReconnect}, room='{PhotonNetwork.CurrentRoom?.Name}', actor={PhotonNetwork.LocalPlayer?.ActorNumber}, scene='{SceneManager.GetActiveScene().name}'");
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
                    Vector3 spawnPosition = restoreAfterReconnect && reconnectRestoreState.hasState
                        ? reconnectRestoreState.position
                        : spawnPoint != null ? spawnPoint.position : Vector3.zero;

                    Quaternion spawnRotation = restoreAfterReconnect && reconnectRestoreState.hasState
                        ? reconnectRestoreState.rotation
                        : spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

                    CustomDebug.Log($"[Reconnect] Instantiating local player clone. prefab='{playerPrefab[selectedCharacter].name}', selectedCharacter={selectedCharacter}, spawnPosition={spawnPosition}, spawnRotation={spawnRotation.eulerAngles}, restore={restoreAfterReconnect}");
                    myPlayer = PhotonNetwork.Instantiate(playerPrefab[selectedCharacter].name, spawnPosition,
                        spawnRotation, 0);
                    PhotonView spawnedView = myPlayer != null ? myPlayer.GetComponent<PhotonView>() : null;
                    
                    FindAnyObjectByType<vThirdPersonCamera>()?.SetTarget(myPlayer.transform);
                    CustomDebug.Log($"[Reconnect] Local player clone successfully instantiated. name='{(myPlayer != null ? myPlayer.name : "null")}', viewId={(spawnedView != null ? spawnedView.ViewID : 0)}, ownerActor={(spawnedView != null && spawnedView.Owner != null ? spawnedView.Owner.ActorNumber : -1)}, position={(myPlayer != null ? myPlayer.transform.position : Vector3.zero)}");
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
                if (!restoreAfterReconnect && loadingCanvas != null)
                    loadingCanvas.SetActive(false);
            }

            if (!restoreAfterReconnect)
            {
                CaptureReconnectRestoreState();
            }

            if (restoreAfterReconnect)
            {
                if (restoreRoutine != null)
                {
                    StopCoroutine(restoreRoutine);
                }

                restoreRoutine = StartCoroutine(CompleteReconnectRestoreRoutine());
            }
            
            
            // ChatCanvas.Instance.InitChat();
            // ChatCanvas.Instance.landUi.ShowLandUI();
            // RemoveOtherInstance();

        }

        
        public override void OnLeftRoom()
        {
            _onLeftRoom.Invoke();
            if (myPlayer != null)
            {
                Destroy(myPlayer);
                myPlayer = null;
            }
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

        private void OnDestroy()
        {
            if (nm == this)
            {
                nm = null;
            }
        }
    }
}
