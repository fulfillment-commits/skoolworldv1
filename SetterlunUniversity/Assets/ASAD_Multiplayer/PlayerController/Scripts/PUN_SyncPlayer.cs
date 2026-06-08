using System.Linq;
using UnityEngine;
using Photon.Pun;
using Invector.vCharacterController;       
using Invector.vCharacterController.vActions;
using UnityEngine.SceneManagement;

namespace ASAD_Multiplyer.PlayerController
{
    [RequireComponent(typeof(PhotonView))]
    [RequireComponent(typeof(PhotonTransformView))]
    [RequireComponent(typeof(PhotonAnimatorView))]
    [RequireComponent(typeof(PhotonRigidbodyView))]
    public class PUN_SyncPlayer : MonoBehaviour, IPunObservable
    {
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

        private vThirdPersonInput _input;

        public LoadCharcterData characterOutfit;
        // public PlayerNameDisplay NameDisplay;

        #endregion

        #region Modifiables

        [SerializeField]
        private bool _syncBones = true;

       [SerializeField]
        private float _boneLerpRate = 90.0f;

        #endregion

        #region Initializations

        void Start()
        {
            
            charModel = transform.GetChild(0).gameObject;
            
            animator = GetComponent<Animator>();
            // NameDisplay = GetComponentInChildren<PlayerNameDisplay>();
            view = GetComponent<PhotonView>();
            vGenericAnimation[] vGenericAnimations = GetComponents<vGenericAnimation>();
            if (view.IsMine && PhotonNetwork.IsConnected)
            {
                SceneManager.sceneLoaded += OnSceneLoaded;
                currentSceneName = SceneManager.GetActiveScene().name;
                view.RPC("SceneSwitch",RpcTarget.OthersBuffered,currentSceneName);
                // outfit setup
                
                if (GetComponent<vHeadTrack>()) GetComponent<vHeadTrack>().enabled = true;
                if (GetComponent<CursorToggle>()) GetComponent<CursorToggle>().enabled = true;
                if (GetComponent<vGenericAction>()) GetComponent<vGenericAction>().enabled = true;
                if (GetComponent<vLadderAction>()) GetComponent<vLadderAction>().enabled = true;


                if (GetComponent<vThirdPersonInput>())
                {
                    _input=GetComponent<vThirdPersonInput>();
                    _input.enabled = true;
                }
                // ChatCanvas.Instance.LoadAllPlayer();
                // NameDisplay.gameObject.SetActive(false);
            }
            else
            {
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
            }

            currentSceneName = SceneManager.GetActiveScene().name;
            if (_syncBones == true)
            {
                SetBones();
            }
            
            LoadOutfit();
            Invoke(nameof(DisableControl),0.5f);
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
            if (_syncBones == true)
            {
                if (stream.IsWriting) //Authoritative player sending data to server
                {
                    stream.SendNext(local_head.localRotation);
                    stream.SendNext(local_neck.localRotation);
                    stream.SendNext(local_spine.localRotation);
                    stream.SendNext(local_chest.localRotation);
                    // stream.SendNext(animator.GetFloat("Speed")); // Replace "Speed" with your parameter names
                    // stream.SendNext(animator.GetBool("isJumping")); // Example: Add all relevant parameters
                }
                else if (stream.IsReading) //Network player copies receiving data from server
                {
                    this.potential_head = (Quaternion)stream.ReceiveNext();
                    this.potential_neck = (Quaternion)stream.ReceiveNext();
                    this.potential_spine = (Quaternion)stream.ReceiveNext();
                    this.potential_chest = (Quaternion)stream.ReceiveNext();

                    server_head = (notNan(potential_head) && potential_head != Quaternion.identity)
                        ? potential_head
                        : server_head;
                    server_neck = (notNan(potential_neck) && potential_neck != Quaternion.identity)
                        ? potential_neck
                        : server_neck;
                    server_spine = (notNan(potential_spine) && potential_spine != Quaternion.identity)
                        ? potential_spine
                        : server_spine;
                    server_chest = (notNan(potential_chest) && potential_chest != Quaternion.identity)
                        ? potential_chest
                        : server_chest;
                }
            }
        }

        #endregion

        public bool sit;

        public Pun_Sitting sitting;
        
      

        void OnDisable()
        {
            if (view.IsMine)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }
        }
        
        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // if (string.Equals(scene.name, "OfficeBuilding_Small"))
            // {
            //     view.RPC("SceneSwitch",RpcTarget.AllBuffered,scene.name+URLImageRetriever.instance.officeScene);
            // }
            // else if (string.Equals(scene.name, "Sci_Fi Gallery"))
            // {
            //     view.RPC("SceneSwitch",RpcTarget.AllBuffered,scene.name+URLImageRetriever.instance.otherRoomId);
            // }
            // else
            {
                view.RPC("SceneSwitch",RpcTarget.AllBuffered,scene.name);
            }
        }

        public void SetControl(bool set)
        {
            _input.enabled = set;
            playingAnimation = !set;
            gameObject.GetComponent<Rigidbody>().useGravity = set;
            gameObject.GetComponent<Rigidbody>().isKinematic = !set;
            gameObject.GetComponent<Collider>().enabled = set;
            SetInputState(!set);
            if (!set)
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
            currentSceneName = sceneName;
            CustomDebug.Log("rpc scene change "+sceneName);
            PUN_SyncPlayer[] allPlayers = FindObjectsOfType<PUN_SyncPlayer>();
            allPlayers.First(a=>a.view.IsMine)?.OtherPlayerChangeScene();
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
            PUN_SyncPlayer[] allPlayers = FindObjectsOfType<PUN_SyncPlayer>();
            foreach (var player in allPlayers)
            {
                player.charModel.SetActive(string.Equals(player.currentSceneName, currentSceneName) );
            }
        }
        
        #region Local Actions Based on Server Changes

        void LateUpdate()
        {
            if (GetComponent<PhotonView>().IsMine == false)
            {
                SyncBoneRotation();
            }
        }

        void SyncBoneRotation()
        {
            local_head.localRotation =
                Quaternion.Lerp(local_head.localRotation, server_head, Time.deltaTime * _boneLerpRate);
            local_neck.localRotation =
                Quaternion.Lerp(local_neck.localRotation, server_neck, Time.deltaTime * _boneLerpRate);
            local_spine.localRotation =
                Quaternion.Lerp(local_spine.localRotation, server_spine, Time.deltaTime * _boneLerpRate);
            local_chest.localRotation =
                Quaternion.Lerp(local_chest.localRotation, server_chest, Time.deltaTime * _boneLerpRate);
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

            if (view.IsMine)
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
