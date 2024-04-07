using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class UContact : UdonSharpBehaviour
{
    [SerializeField]
    private HumanBodyBones trackingPoint;

    private VRCPlayerApi _playerLocal;

    private void Start()
    {
        _playerLocal = Networking.LocalPlayer;
    }

    public override void PostLateUpdate()
    {
        if (_playerLocal == null)
            return;

        transform.SetPositionAndRotation(
            _playerLocal.GetBonePosition(trackingPoint),
            _playerLocal.GetBoneRotation(trackingPoint)
        );
    }
}
