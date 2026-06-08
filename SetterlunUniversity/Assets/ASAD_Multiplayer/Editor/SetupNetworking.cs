using UnityEngine;
using UnityEditor;
// using Invector.vMelee;
// using Invector.vShooter;
using Invector.vCharacterController;
using Invector;
using Photon.Pun;
using System.Collections.Generic;
using ASAD_Multiplyer.Lobby;
using ASAD_Multiplyer.Network;
using ASAD_Multiplyer.PlayerController;

namespace ASAD_Multiplyer.CustomEditor
{
    public class SetupNetworking : EditorWindow
    {

        [MenuItem("Invector/Multiplayer/Create Network Manager")]
        private static void M_NetworkManager()
        {
            if (!FindObjectOfType<PUN_NetworkManager>())
            {
                GameObject _networkManager = new GameObject("NetworkManager");
                _networkManager.AddComponent<PUN_NetworkManager>();
                _networkManager.AddComponent<PUN_LobbyUI>();
                // _networkManager.AddComponent<PUN_NetworkManager>()._spawnPoint = _networkManager.transform;
            }
            else
            {
                PUN_NetworkManager _networkManager = FindObjectOfType<PUN_NetworkManager>();
                // _networkManager._spawnPoint = _networkManager.gameObject.transform;
            }
        }


        GameObject _player = null;
        GUISkin skin;
        Vector2 rect = new Vector2(400, 180);
        Vector2 max_rect = new Vector2(400, 500);
        Editor playerPreview;
        bool generated = false;

        [MenuItem("Invector/Multiplayer/Make Player Multiplayer Compatible")]
        private static void M_MakePlayerMultiplayer()
        {
            GetWindow<SetupNetworking>("Photon PUN - Make Player Multiplayer Compatible");
        }

        void PlayerPreview()
        {
            GUILayout.FlexibleSpace();

            if (_player != null)
            {
                playerPreview.OnInteractivePreviewGUI(GUILayoutUtility.GetRect(360, 300), "window");
            }
        }

        private void OnGUI()
        {
            if (!skin) skin = Resources.Load("skin") as GUISkin;
            GUI.skin = skin;

            this.minSize = rect;
            this.maxSize = max_rect;
            this.titleContent =
                new GUIContent("Photon PUN: Multiplayer", null, "Adds multiplayer support to your player.");
            GUILayout.BeginVertical("Add Multiplayer Compatiblity", "window");
            GUILayout.Space(35);

            GUILayout.BeginVertical("box");

            if (!_player)
                EditorGUILayout.HelpBox(
                    "Input your target player prefab to add multiplayer support. If you don't use a prefab the wrong object will be added to the network manager at the end.",
                    MessageType.Info);

            _player = EditorGUILayout.ObjectField("Player Prefab", _player, typeof(GameObject), true,
                GUILayout.ExpandWidth(true)) as GameObject;

            if (GUI.changed && _player != null)
            {
                playerPreview = Editor.CreateEditor(_player);
            }

            if (_player != null && _player.GetComponent<PUN_SyncPlayer>() != null && generated == false)
            {
                EditorGUILayout.HelpBox(
                    "This gameObject already contains the component \"PUN_SyncPlayer\". Adding support again will reset it's values to default.",
                    MessageType.Warning);
            }
            else if (_player != null && _player.GetComponent<PUN_SyncPlayer>() != null && generated == true)
            {
                EditorGUILayout.HelpBox(
                    "Last Step! \n\n Open the \"Photon View\" component and set the \"Observe option\" to \"Unreliable On Change\".",
                    MessageType.Info);
            }

            GUILayout.EndVertical();

            if (_player != null)
            {
                GUILayout.BeginHorizontal("box");
                PlayerPreview();
                GUILayout.EndHorizontal();
                if (GUILayout.Button("Add Multiplayer Support"))
                {
                    generated = true;
                    M_SetupMultiplayer();
                }
            }
        }

        void M_SetupMultiplayer()
        {
            foreach (MonoBehaviour script in _player.GetComponents(typeof(MonoBehaviour)))
            {
                script.enabled = false;
            }

            if (_player.GetComponent<PUN_SyncPlayer>() == null)
            {
                _player.AddComponent<PUN_SyncPlayer>();
            }
            if (_player.GetComponent<PUN_ThirdPersonCameraVerify>() == null)
            {
                _player.AddComponent<PUN_ThirdPersonCameraVerify>();
            }

            //Sync Rigidbody
            _player.GetComponent<PhotonRigidbodyView>().m_SynchronizeVelocity = true;
            _player.GetComponent<PhotonRigidbodyView>().m_SynchronizeAngularVelocity = true;
            _player.GetComponent<PhotonRigidbodyView>().m_TeleportEnabled = false;

            //Sync Transform
            _player.GetComponent<PhotonTransformView>().m_SynchronizePosition = true;
            _player.GetComponent<PhotonTransformView>().m_SynchronizeRotation = true;
            _player.GetComponent<PhotonTransformView>().m_SynchronizeScale = false;

            //Sync All params
            foreach (var param in _player.GetComponent<PhotonAnimatorView>().GetSynchronizedParameters())
            {
                _player.GetComponent<PhotonAnimatorView>().SetParameterSynchronized(param.Name, param.Type,
                    PhotonAnimatorView.SynchronizeType.Discrete);
            }

            //Sync All Layers
            for (int index = 0;
                 index < _player.GetComponent<PhotonAnimatorView>().GetSynchronizedLayers().Count;
                 index++)
            {
                _player.GetComponent<PhotonAnimatorView>()
                    .SetLayerSynchronized(index, PhotonAnimatorView.SynchronizeType.Discrete);
            }

            //Add Photon Components To Photon View To Sync Them over network
            _player.GetComponent<PhotonView>().ObservedComponents = null;
            List<Component> observables = new List<Component>();
            observables.Add(_player.GetComponent<PhotonTransformView>());
            observables.Add(_player.GetComponent<PhotonRigidbodyView>());
            observables.Add(_player.GetComponent<PhotonAnimatorView>());
            observables.Add(_player.GetComponent<PUN_SyncPlayer>());
            _player.GetComponent<PhotonView>().ObservedComponents = observables;

            
            //Enable multiplayer compatiable components
            
            // _player.GetComponent<vRagdoll>().enabled = true;
            _player.GetComponent<vFootStep>().enabled = true;
            _player.GetComponent<vThirdPersonController>().enabled = true;
            _player.GetComponent<PUN_ThirdPersonCameraVerify>().enabled = true;
            _player.GetComponent<PUN_SyncPlayer>().enabled = true;
            _player.GetComponent<PhotonRigidbodyView>().enabled = true;
            _player.GetComponent<PhotonTransformView>().enabled = true;
            _player.GetComponent<PhotonAnimatorView>().enabled = true;
            if(_player.GetComponent<vRagdoll>()!=null) _player.GetComponent<vRagdoll>().enabled = true;
            // if(_player.GetComponent<vFootStep>()!=null) _player.GetComponent<vFootStep>().enabled = true;
            // if(_player.GetComponent<vThirdPersonController>()!=null) _player.GetComponent<vThirdPersonController>().enabled = true;
            // if(_player.GetComponent<PUN_ThirdPersonCameraVerify>()!=null) _player.GetComponent<PUN_ThirdPersonCameraVerify>().enabled = true;
            // if(_player.GetComponent<PUN_SyncPlayer>()!=null) _player.GetComponent<PUN_SyncPlayer>().enabled = true;
            // if(_player.GetComponent<PhotonRigidbodyView>()!=null) _player.GetComponent<PhotonRigidbodyView>().enabled = true;
            // if(_player.GetComponent<PhotonTransformView>()!=null) _player.GetComponent<PhotonTransformView>().enabled = true;
            // if(_player.GetComponent<PhotonAnimatorView>()!=null) _player.GetComponent<PhotonAnimatorView>().enabled = true;
        }

    }
}

