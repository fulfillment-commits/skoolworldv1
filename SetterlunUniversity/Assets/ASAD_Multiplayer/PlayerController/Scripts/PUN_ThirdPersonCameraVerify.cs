using UnityEngine;
using Invector.vCamera;
using Photon.Pun;

namespace ASAD_Multiplyer.PlayerController
{
    public class PUN_ThirdPersonCameraVerify : MonoBehaviour
    {
        private void Start()
        {
            PhotonView view = GetComponent<PhotonView>();
            if (!PhotonNetwork.IsConnected || view.IsMine)
            {
                vThirdPersonCamera camera = FindObjectOfType<vThirdPersonCamera>();
                if (camera != null)
                {
                    camera.mainTarget = transform;
                }
            }
        }
    }
}
