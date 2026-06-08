using UnityEngine;
using Invector.vCamera;
using Photon.Pun;

namespace ASAD_Multiplyer.PlayerController
{
    public class PUN_ThirdPersonCameraVerify : MonoBehaviour
    {
        private void Start()
        {
            if (GetComponent<PhotonView>().IsMine == false && PhotonNetwork.IsConnected == true)
            {
                FindObjectOfType<vThirdPersonCamera>().mainTarget = transform;
            }
        }
    }
}
