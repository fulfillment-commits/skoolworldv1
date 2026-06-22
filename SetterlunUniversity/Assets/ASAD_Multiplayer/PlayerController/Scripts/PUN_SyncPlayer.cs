using System.Linq;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System.Collections.Generic;
using Invector.vCharacterController;
using Invector.vCharacterController.PointClick;
using Invector.vCharacterController.vActions;
using UnityEngine.SceneManagement;

namespace ASAD_Multiplyer.PlayerController
{
    [RequireComponent(typeof(PhotonView))]
    [RequireComponent(typeof(PhotonAnimatorView))]
    public class PUN_SyncPlayer : MonoBehaviourPunCallbacks, IPunObservable
    {
        private const string CurrentSceneProperty = "currentScene";

        #region Reference Components

        private string waveAinamtion="Wave";
        private string sitAnimation="StandToSit";
        private string standAnimation="SitToStand";
        public string currentSceneName;
        public string currentRoomID;
        
        public GameObject charModel;
        
        [Header("Other References")]
        
        public bool playingAnimation = false;
        public string noneLocalTag;
        private Transform local_head, local_neck, local_spine, local_chest = null;
        private Quaternion server_head, server_neck, server_spine, server_chest = Quaternion.identity;
        private Quaternion potential_head, potential_neck, potential_spine, potential_chest = Quaternion.identity;
        public PhotonView view;
        public Animator animator;

        // private vThirdPersonInput _input;
        private vPointAndClickInput _input;

        public LoadCharcterData characterOutfit;
        // public PlayerNameDisplay NameDisplay;

        #endregion

        #region Modifiables

        [SerializeField]
        private bool _syncBones = true;

        [SerializeField]
        private float _boneLerpRate = 12.0f;

        [SerializeField]
        private float _networkInterpolationBackTime = 0.15f;

        [SerializeField]
        private float _networkExtrapolateLimit = 0.05f;

        [SerializeField]
        private float _networkTeleportDistance = 5f;

        [SerializeField]
        private float _networkMaxInterpolationSpeed = 16f;

        [SerializeField]
        private float _boneSendDeadZoneDegrees = 0.25f;

        [SerializeField]
        private float _boneReceiveDeadZoneDegrees = 0.35f;

        private readonly List<NetworkFrame> _networkFrames = new List<NetworkFrame>(8);
        private Rigidbody _body;
        private vThirdPersonController _controller;
        private bool _hasNetworkTransform;
        private bool _hasStableBoneRotations;
        private double _ignoreFramesBeforeTime;
        private Quaternion _stableHead;
        private Quaternion _stableNeck;
        private Quaternion _stableSpine;
        private Quaternion _stableChest;


        private struct NetworkFrame
        {
            public double Time;
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Velocity;
            public Quaternion Head;
            public Quaternion Neck;
            public Quaternion Spine;
            public Quaternion Chest;
        }

        #endregion

        #region Initializations

        void Awake()
        {
            view = GetComponent<PhotonView>();
            _body = GetComponent<Rigidbody>();
            _controller = GetComponent<vThirdPersonController>();
            ConfigureNetworkSync();
        }

        void ConfigureNetworkSync()
        {
            PhotonTransformView transformView = GetComponent<PhotonTransformView>();
            if (transformView != null)
            {
                transformView.enabled = false;
            }

            PhotonRigidbodyView rigidbodyView = GetComponent<PhotonRigidbodyView>();
            if (rigidbodyView != null)
            {
                rigidbodyView.enabled = false;
            }

            if (_body == null)
            {
                _body = GetComponent<Rigidbody>();
            }

            if (_body != null)
            {
                _body.interpolation = RigidbodyInterpolation.Interpolate;
            }

            if (view == null)
            {
                return;
            }

            if (view.ObservedComponents == null)
            {
                view.ObservedComponents = new List<Component>();
            }

            view.ObservedComponents.Clear();
            view.ObservedComponents.Add(this);

            PhotonAnimatorView animatorView = GetComponent<PhotonAnimatorView>();
            if (animatorView != null)
            {
                view.ObservedComponents.Add(animatorView);
            }
        }

        void Start()
        {
            
            charModel = transform.GetChild(0).gameObject;
            
            animator = GetComponent<Animator>();
            // NameDisplay = GetComponentInChildren<PlayerNameDisplay>();
            if (view == null)
            {
                view = GetComponent<PhotonView>();
            }
            ConfigureNetworkSync();
            _input = GetComponent<vPointAndClickInput>();
            if (_controller == null)
            {
                _controller = GetComponent<vThirdPersonController>();
            }
            vGenericAnimation[] vGenericAnimations = GetComponents<vGenericAnimation>();
            if (view.IsMine && PhotonNetwork.IsConnected)
            {
                SceneManager.sceneLoaded += OnSceneLoaded;
                currentSceneName = SceneManager.GetActiveScene().name;
                SetCurrentSceneProperty(currentSceneName);
                // outfit setup
                
                if (GetComponent<vHeadTrack>()) GetComponent<vHeadTrack>().enabled = true;
                if (GetComponent<vPointAndClickInput>()) GetComponent<vPointAndClickInput>().enabled = true;
                if (GetComponent<vGenericAction>()) GetComponent<vGenericAction>().enabled = true;
                if(transform.GetChild(1).GetComponent<vPointClickCursor>()) transform.GetChild(1).gameObject.SetActive(true);
                if (GetComponent<CursorToggle>()) GetComponent<CursorToggle>().enabled = true;
                if (GetComponent<vGenericAction>()) GetComponent<vGenericAction>().enabled = true;
                if (GetComponent<vLadderAction>()) GetComponent<vLadderAction>().enabled = true;
                if (GetComponent<PlayerTeleport>()) GetComponent<PlayerTeleport>().enabled = true;
                // if (GetComponent<KeyboardDirectInput>()) GetComponent<KeyboardDirectInput>().enabled = true;


                if (_input != null)
                {
                    // _input.enabled = true;
                }
                // ChatCanvas.Instance.LoadAllPlayer();
                // NameDisplay.gameObject.SetActive(false);
            }
            else
            {
                if (_controller != null)
                {
                    _controller.enabled = false;
                }

                vHeadTrack headTrack = GetComponent<vHeadTrack>();
                if (headTrack != null)
                {
                    headTrack.enabled = false;
                }

                foreach (var vAni in vGenericAnimations)
                {
                    vAni.enabled = false;
                }
                // NameDisplay.playerNameText.text = URLImageRetriever.instance.GetPartFromName(view.Owner.NickName, 0);
                if (!string.IsNullOrEmpty(noneLocalTag))
                {
                    this.tag = noneLocalTag;
                }

                gameObject.GetComponent<Collider>().enabled = false;
                gameObject.GetComponent<Rigidbody>().isKinematic = true;
                if (_input != null)
                {
                    _input.enabled = false;
                }
            }

            currentSceneName = SceneManager.GetActiveScene().name;
            RefreshPlayersForCurrentScene();
            if (_syncBones == true)
            {
                SetBones();
            }
            
            LoadOutfit();
            if (view.IsMine || !PhotonNetwork.IsConnected)
            {
                Invoke(nameof(DisableControl),0.5f);
            }
            DontDestroyOnLoad(gameObject);
        }

        public void LoadOutfit()
        {
            if (view.IsMine && PhotonNetwork.IsConnected)
            {
                int outfitId = characterOutfit.LoadGetOutfit();
                view.RPC("LoadMyOutfit",RpcTarget.OthersBuffered,outfitId);
            }
        }
        
        void DisableControl()
        {
            SetControl(false);
        }
        void SetBones()
        {
            if (local_head == null)
            {
                try
                {
                    local_head = animator.GetBoneTransform(HumanBodyBones.Head).transform;
                    server_head = local_head.localRotation;
                }
                catch (System.Exception e)
                {
                    Debug.LogError(e);
                }
            }

            if (local_neck == null)
            {
                try
                {
                    local_neck = animator.GetBoneTransform(HumanBodyBones.Neck).transform;
                    server_neck = local_neck.localRotation;
                }
                catch (System.Exception e)
                {
                    Debug.LogError(e);
                }
            }

            if (local_spine == null)
            {
                try
                {
                    local_spine = animator.GetBoneTransform(HumanBodyBones.Spine).transform;
                    server_spine = local_spine.localRotation;
                }
                catch (System.Exception e)
                {
                    Debug.LogError(e);
                }
            }

            if (local_chest == null)
            {
                try
                {
                    local_chest = animator.GetBoneTransform(HumanBodyBones.Chest).transform;
                    server_chest = local_chest.localRotation;
                }
                catch (System.Exception e)
                { 
                    Debug.LogError(e);
                }
            }
        }

        #endregion

        #region Server Sync Logic

        public void
            OnPhotonSerializeView(PhotonStream stream,
                PhotonMessageInfo info) //this function called by Photon View component
        {
            NetworkFrame receivedFrame = new NetworkFrame();
            if (stream.IsWriting)
            {
                stream.SendNext(transform.position);
                stream.SendNext(transform.rotation);
                stream.SendNext(_body != null ? _body.velocity : Vector3.zero);
            }
            else if (stream.IsReading)
            {
                receivedFrame.Time = info.SentServerTime;
                receivedFrame.Position = (Vector3)stream.ReceiveNext();
                receivedFrame.Rotation = (Quaternion)stream.ReceiveNext();
                receivedFrame.Velocity = (Vector3)stream.ReceiveNext();
                receivedFrame.Head = server_head;
                receivedFrame.Neck = server_neck;
                receivedFrame.Spine = server_spine;
                receivedFrame.Chest = server_chest;
            }

            if (_syncBones == true)
            {
                if (stream.IsWriting) //Authoritative player sending data to server
                {
                    if (local_head == null || local_neck == null || local_spine == null || local_chest == null)
                    {
                        SetBones();
                    }

                    stream.SendNext(GetStableBoneRotation(local_head, ref _stableHead));
                    stream.SendNext(GetStableBoneRotation(local_neck, ref _stableNeck));
                    stream.SendNext(GetStableBoneRotation(local_spine, ref _stableSpine));
                    stream.SendNext(GetStableBoneRotation(local_chest, ref _stableChest));
                    _hasStableBoneRotations = true;
                    // stream.SendNext(animator.GetFloat("Speed")); // Replace "Speed" with your parameter names
                    // stream.SendNext(animator.GetBool("isJumping")); // Example: Add all relevant parameters
                }
                else if (stream.IsReading) //Network player copies receiving data from server
                {
                    potential_head = (Quaternion)stream.ReceiveNext();
                    potential_neck = (Quaternion)stream.ReceiveNext();
                    potential_spine = (Quaternion)stream.ReceiveNext();
                    potential_chest = (Quaternion)stream.ReceiveNext();

                    receivedFrame.Head = GetFilteredBoneTarget(potential_head, server_head);
                    receivedFrame.Neck = GetFilteredBoneTarget(potential_neck, server_neck);
                    receivedFrame.Spine = GetFilteredBoneTarget(potential_spine, server_spine);
                    receivedFrame.Chest = GetFilteredBoneTarget(potential_chest, server_chest);
                }
            }

            if (stream.IsReading)
            {
                AddNetworkFrame(receivedFrame);
            }
        }

        #endregion

        public bool sit;

        public Pun_Sitting sitting;
        
      

        void OnDisable()
        {
            if (view != null && view.IsMine)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }
        }
        
        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            currentSceneName = scene.name;
            SetCurrentSceneProperty(scene.name);
            RefreshPlayersForCurrentScene();
        }

        private void SetCurrentSceneProperty(string sceneName)
        {
            if (!PhotonNetwork.IsConnected ||
                !PhotonNetwork.InRoom ||
                PhotonNetwork.NetworkClientState != ClientState.Joined ||
                PhotonNetwork.LocalPlayer == null ||
                view == null ||
                !view.IsMine)
            {
                return;
            }

            PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable
            {
                { CurrentSceneProperty, sceneName }
            });

            CustomDebug.Log($"[ScenePresence] Local player scene set to '{sceneName}'.");
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            if (changedProps != null && changedProps.ContainsKey(CurrentSceneProperty))
            {
                RefreshPlayersForCurrentScene();
            }
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            RefreshPlayersForCurrentScene();
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            RefreshPlayersForCurrentScene();
        }

        private void RefreshPlayersForCurrentScene()
        {
            string localSceneName = SceneManager.GetActiveScene().name;
            PUN_SyncPlayer[] allPlayers = FindObjectsOfType<PUN_SyncPlayer>();

            foreach (PUN_SyncPlayer player in allPlayers)
            {
                if (player == null)
                {
                    continue;
                }

                PhotonView playerView = player.view != null ? player.view : player.GetComponent<PhotonView>();
                if (playerView == null)
                {
                    continue;
                }

                string playerSceneName = playerView.IsMine ? localSceneName : GetPlayerScene(playerView.Owner);
                player.currentSceneName = playerSceneName;

                if (player.charModel != null)
                {
                    player.charModel.SetActive(playerView.IsMine || string.Equals(playerSceneName, localSceneName));
                }
            }
        }

        private static string GetPlayerScene(Player player)
        {
            if (player != null &&
                player.CustomProperties != null &&
                player.CustomProperties.TryGetValue(CurrentSceneProperty, out object sceneValue))
            {
                return sceneValue as string;
            }

            return string.Empty;
        }

        public void SetControl(bool set)
        {
            if (view != null && PhotonNetwork.IsConnected && !view.IsMine)
            {
                return;
            }

            if (_input != null)
            {
                _input.enabled = set;
            }

            playingAnimation = !set;
            Rigidbody body = gameObject.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.useGravity = set;
                body.isKinematic = !set;
            }

            Collider playerCollider = gameObject.GetComponent<Collider>();
            if (playerCollider != null)
            {
                playerCollider.enabled = set;
            }

            SetInputState(!set);
            if (!set && animator != null)
            {
                animator.SetFloat("InputVertical", 0f);
                animator.SetFloat("InputMagnitude", 0.0f);
                animator.SetFloat("InputDirection", 0f);
                animator.SetFloat("InputHorizontal", 0f);
                animator.SetBool("IsGrounded", true);
            }
            // CustomDebug.Log(gameObject.GetComponent<Rigidbody>().useGravity );
        }

        [PunRPC]
        void SceneSwitch(string sceneName)
        {
            CustomDebug.Log($"[SceneSwitch] Ignoring legacy buffered scene RPC for '{sceneName}'. Scene visibility now uses player custom properties.");
            RefreshPlayersForCurrentScene();
        }

        [PunRPC]
        void LoadMyOutfit(int index)
        {
            characterOutfit.LoadOutfit(index);
        }

        [PunRPC]
        void RoomID(string roomID)
        {
            
        }

        
        public void OtherPlayerChangeScene()
        {
            RefreshPlayersForCurrentScene();
        }

        public void TeleportLocalPlayerTo(Transform targetPosition)
        {
            if (targetPosition == null)
            {
                return;
            }

            if (PhotonNetwork.IsConnected && view != null && !view.IsMine)
            {
                CustomDebug.Log("[NetworkTeleport] Ignored teleport request on a non-owned player.");
                return;
            }

            if (_controller == null)
            {
                _controller = GetComponent<vThirdPersonController>();
            }

            if (_body == null)
            {
                _body = GetComponent<Rigidbody>();
            }

            ClearNetworkMovementState(PhotonNetwork.Time);
            ResetLocalMotion();

            if (_controller != null)
            {
                _controller.StopCharacter();
                _controller.MoveToPositionRotaion(targetPosition);
            }
            else
            {
                transform.position = targetPosition.position;
                transform.rotation = targetPosition.rotation;
            }

            ResetLocalMotion();
            Physics.SyncTransforms();
            SetNetworkFrameFromCurrentTransform(PhotonNetwork.Time);

            if (PhotonNetwork.IsConnected && view != null && view.IsMine)
            {
                view.RPC(nameof(SnapRemotePlayerTo), RpcTarget.Others, transform.position, transform.rotation);
                PhotonNetwork.SendAllOutgoingCommands();
            }
        }

        private void ResetLocalMotion()
        {
            if (_body == null)
            {
                return;
            }

            _body.velocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
            _body.position = transform.position;
            _body.rotation = transform.rotation;
            _body.WakeUp();
        }

        private void ClearNetworkMovementState(double time)
        {
            _networkFrames.Clear();
            _ignoreFramesBeforeTime = time;
            _hasNetworkTransform = false;
        }

        private void SetNetworkFrameFromCurrentTransform(double time)
        {
            NetworkFrame frame = new NetworkFrame
            {
                Time = time,
                Position = transform.position,
                Rotation = transform.rotation,
                Velocity = Vector3.zero,
                Head = server_head,
                Neck = server_neck,
                Spine = server_spine,
                Chest = server_chest
            };

            _networkFrames.Clear();
            _networkFrames.Add(frame);
            _hasNetworkTransform = true;
        }

        [PunRPC]
        private void SnapRemotePlayerTo(Vector3 position, Quaternion rotation, PhotonMessageInfo info)
        {
            if (view != null && view.IsMine)
            {
                return;
            }

            transform.SetPositionAndRotation(position, rotation);

            if (_body == null)
            {
                _body = GetComponent<Rigidbody>();
            }

            if (_body != null)
            {
                _body.velocity = Vector3.zero;
                _body.angularVelocity = Vector3.zero;
                _body.position = position;
                _body.rotation = rotation;
            }

            Physics.SyncTransforms();
            ClearNetworkMovementState(info.SentServerTime);
            SetNetworkFrameFromCurrentTransform(info.SentServerTime);
        }
        
        #region Local Actions Based on Server Changes

        void LateUpdate()
        {
            if (view != null && view.IsMine == false)
            {
                SmoothNetworkTransform();
                SyncBoneRotation();
            }
        }

        void SmoothNetworkTransform()
        {
            if (!_hasNetworkTransform || _networkFrames.Count == 0)
            {
                return;
            }

            NetworkFrame targetFrame = GetRenderFrame();
            float distance = Vector3.Distance(transform.position, targetFrame.Position);
            if (distance > _networkTeleportDistance)
            {
                _networkFrames.Clear();
                _networkFrames.Add(targetFrame);
            }

            transform.position = targetFrame.Position;
            transform.rotation = targetFrame.Rotation;
            server_head = targetFrame.Head;
            server_neck = targetFrame.Neck;
            server_spine = targetFrame.Spine;
            server_chest = targetFrame.Chest;
        }

        private void AddNetworkFrame(NetworkFrame frame)
        {
            if (frame.Time < _ignoreFramesBeforeTime)
            {
                return;
            }

            if (!notNan(frame.Rotation))
            {
                frame.Rotation = transform.rotation;
            }

            _networkFrames.Add(frame);
            _networkFrames.Sort((a, b) => a.Time.CompareTo(b.Time));

            while (_networkFrames.Count > 8)
            {
                _networkFrames.RemoveAt(0);
            }

            if (!_hasNetworkTransform)
            {
                transform.position = frame.Position;
                transform.rotation = frame.Rotation;
                server_head = frame.Head;
                server_neck = frame.Neck;
                server_spine = frame.Spine;
                server_chest = frame.Chest;
                _hasNetworkTransform = true;
            }
        }

        private NetworkFrame GetRenderFrame()
        {
            double renderTime = PhotonNetwork.Time - _networkInterpolationBackTime;

            while (_networkFrames.Count >= 2 && _networkFrames[1].Time <= renderTime)
            {
                _networkFrames.RemoveAt(0);
            }

            if (_networkFrames.Count == 1)
            {
                return ExtrapolateFrame(_networkFrames[0], renderTime);
            }

            NetworkFrame from = _networkFrames[0];
            NetworkFrame to = _networkFrames[1];
            if (renderTime <= from.Time)
            {
                return from;
            }

            float duration = Mathf.Max((float)(to.Time - from.Time), 0.0001f);
            float t = Mathf.Clamp01((float)((renderTime - from.Time) / duration));
            return LerpFrame(from, to, t);
        }

        private NetworkFrame ExtrapolateFrame(NetworkFrame frame, double renderTime)
        {
            float extrapolateTime = Mathf.Clamp((float)(renderTime - frame.Time), 0f, _networkExtrapolateLimit);
            frame.Position += frame.Velocity * extrapolateTime;
            return frame;
        }

        private NetworkFrame LerpFrame(NetworkFrame from, NetworkFrame to, float t)
        {
            float snapshotDuration = Mathf.Max((float)(to.Time - from.Time), 0.0001f);
            Vector3 fromVelocity = Vector3.ClampMagnitude(from.Velocity, _networkMaxInterpolationSpeed);
            Vector3 toVelocity = Vector3.ClampMagnitude(to.Velocity, _networkMaxInterpolationSpeed);

            return new NetworkFrame
            {
                Time = Mathf.Lerp((float)from.Time, (float)to.Time, t),
                Position = HermitePosition(from.Position, fromVelocity * snapshotDuration, to.Position, toVelocity * snapshotDuration, t),
                Rotation = Quaternion.Slerp(from.Rotation, to.Rotation, t),
                Velocity = Vector3.Lerp(from.Velocity, to.Velocity, t),
                Head = Quaternion.Slerp(from.Head, to.Head, t),
                Neck = Quaternion.Slerp(from.Neck, to.Neck, t),
                Spine = Quaternion.Slerp(from.Spine, to.Spine, t),
                Chest = Quaternion.Slerp(from.Chest, to.Chest, t)
            };
        }

        private Vector3 HermitePosition(Vector3 startPosition, Vector3 startTangent, Vector3 endPosition, Vector3 endTangent, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            Vector3 hermite = (2f * t3 - 3f * t2 + 1f) * startPosition
                              + (t3 - 2f * t2 + t) * startTangent
                              + (-2f * t3 + 3f * t2) * endPosition
                              + (t3 - t2) * endTangent;

            Vector3 linear = Vector3.Lerp(startPosition, endPosition, t);
            return Vector3.Distance(hermite, linear) > 0.75f ? linear : hermite;
        }

        private Quaternion GetValidRotation(Quaternion value, Quaternion fallback)
        {
            return notNan(value) && value != Quaternion.identity ? value : fallback;
        }

        private Quaternion GetFilteredBoneTarget(Quaternion value, Quaternion fallback)
        {
            Quaternion validRotation = GetValidRotation(value, fallback);
            return Quaternion.Angle(validRotation, fallback) < _boneReceiveDeadZoneDegrees ? fallback : validRotation;
        }

        private Quaternion GetStableBoneRotation(Transform bone, ref Quaternion stableRotation)
        {
            Quaternion currentRotation = bone != null ? bone.localRotation : stableRotation;
            if (!_hasStableBoneRotations || stableRotation == Quaternion.identity)
            {
                stableRotation = currentRotation;
                return stableRotation;
            }

            if (Quaternion.Angle(stableRotation, currentRotation) >= _boneSendDeadZoneDegrees)
            {
                stableRotation = currentRotation;
            }

            return stableRotation;
        }

        void SyncBoneRotation()
        {
            if (local_head == null || local_neck == null || local_spine == null || local_chest == null)
            {
                SetBones();
            }

            if (local_head == null || local_neck == null || local_spine == null || local_chest == null)
            {
                return;
            }

            float t = 1f - Mathf.Exp(-_boneLerpRate * Time.deltaTime);
            local_head.localRotation = Quaternion.Slerp(local_head.localRotation, server_head, t);
            local_neck.localRotation = Quaternion.Slerp(local_neck.localRotation, server_neck, t);
            local_spine.localRotation = Quaternion.Slerp(local_spine.localRotation, server_spine, t);
            local_chest.localRotation = Quaternion.Slerp(local_chest.localRotation, server_chest, t);
        }

        bool notNan(Quaternion value)
        {
            if (!float.IsNaN(value.x) && !float.IsNaN(value.y) && !float.IsNaN(value.z) && !float.IsNaN(value.w))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region Custom Animation Sync

        public void SetInputState(bool set)
        {

            if (view.IsMine && _input != null && _input.cc != null)
            {
                _input.cc.StopCharacter();
                _input.SetLockAllInput(set);
                // _input.SetLockCameraInput(set);
            }
                        
        }
        
        public void PlayWaveAnimation()
        {
            if (view.IsMine)
            {
                
                view.RPC("PlayAnimation", RpcTarget.Others, waveAinamtion);
            }
        }

        public void PlayStandToSit()
        {
            CustomDebug.Log("player is sitting");
            if (view.IsMine)
            {
                playingAnimation = true;
                sitting.AddPlayer(this);
                sit = true;
                // ChatCanvas.Instance.txtSit.text = "Press T to Stand";
                view.RPC("PlayAnimation", RpcTarget.Others, sitAnimation);
            }
        }

        public void PlaySitToStand()
        {
            
            CustomDebug.Log("player is standing");
            Invoke(nameof(DelyOn),2f);
            if (view.IsMine)
            {
                view.RPC("PlayAnimation", RpcTarget.Others, standAnimation);
            }
        }


        public void RotateWithCamera(bool set)
        {
            
            gameObject.GetComponent<vThirdPersonController>().strafeSpeed.rotateWithCamera = set;
        }
        
        void DelyOn()
        {
            sit = false;
            // ChatCanvas.Instance.txtSit.text = "Press T to Sit";
            playingAnimation = false;
            sitting.RemovePlayer(this);
            gameObject.GetComponent<vThirdPersonController>().strafeSpeed.rotateWithCamera = true;
        }
        
        [PunRPC]
        public void PlayAnimation(string animationName)
        {
            CustomDebug.Log("rcp called recived aimation "+ animationName);
            animator.CrossFadeInFixedTime(animationName, 0.1f);
        }

        #endregion
    }
}
