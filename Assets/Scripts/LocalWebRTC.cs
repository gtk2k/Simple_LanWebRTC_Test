using System.Collections;
using Unity.WebRTC;
using UnityEngine;

public class LocalWebRTC : MonoBehaviour
{
    [SerializeField] private PeerType type;
    [SerializeField] private string remoteIPAddress;
    [SerializeField] private RenderTexture videoTexture;

    private SignalerBase signaler;
    private RTCPeerConnection peer;

    private enum DescSide
    {
        Local,
        Remote
    }

    public enum PeerType
    {
        Sender,
        Receiver
    }

    private void Start()
    {
        signaler = type == PeerType.Receiver ? new ReceiverSignaler() : new SenderSignaler(remoteIPAddress);
        signaler.OnConnected += Signaler_OnConnected;
        signaler.OnDesc += Signaler_OnDesc;
        signaler.Start();
    }

    private void Signaler_OnDesc(string ipAddress, RTCSessionDescription desc)
    {
        if(peer == null)
        {
            CreatePeer();
        }
        StartCoroutine(SetDesc(DescSide.Remote, desc));
    }

    private void Signaler_OnConnected(string obj)
    {
        if(type == PeerType.Sender)
        {
            CreatePeer();
        }
    }

    private void CreatePeer()
    {
        peer = new RTCPeerConnection();
        if (type == PeerType.Sender)
        {
            var videoTrack = new VideoStreamTrack(videoTexture);
            peer.AddTrack(videoTrack);
            StartCoroutine(CreateDesc(RTCSdpType.Offer));
        }
    }

    private IEnumerator CreateDesc(RTCSdpType sdpType)
    {
        var op = sdpType == RTCSdpType.Offer ? peer.CreateOffer() : peer.CreateAnswer();
        yield return op;
        if (op.IsError)
        {
            Debug.LogError($"CreateDesc {sdpType} Error; {op.Error.message}");
            yield break;
        }
        yield return StartCoroutine(SetDesc(DescSide.Local, op.Desc));
    }

    private IEnumerator SetDesc(DescSide side, RTCSessionDescription desc)
    {
        var op = side == DescSide.Local ? peer.SetLocalDescription(ref desc) : peer.SetRemoteDescription(ref desc);
        yield return op;
        if(op.IsError)
        {
            Debug.LogError($"Set {side} {desc.type} Error: {op.Error.message}");
            yield break;
        }
        if(side == DescSide.Local)
        {
            signaler.Send(remoteIPAddress, SignalingMessage.FromDesc(desc));
        }
        else if(desc.type == RTCSdpType.Offer)
        {
            yield return StartCoroutine(CreateDesc(RTCSdpType.Answer));
        }
    }
}
