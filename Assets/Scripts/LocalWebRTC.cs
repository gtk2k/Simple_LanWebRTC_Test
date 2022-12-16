using System.Collections;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.Video;

public class LocalWebRTC : MonoBehaviour
{
    [SerializeField] private PeerType type;
    [SerializeField] private string remoteIPAddress;
    [SerializeField] private VideoPlayer player;
    [SerializeField] private GameObject display;
    [SerializeField] private Vector2Int streamingSize;

    private SignalerBase signaler;
    private RTCPeerConnection peer;

    public RenderTexture videoTexture;

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
        Debug.Log($"<LocalWebRTC> Start");

        StartCoroutine(WebRTC.Update());

        if (type == PeerType.Sender)
        {
            videoTexture = new RenderTexture(streamingSize.x, streamingSize.y, 0, RenderTextureFormat.BGRA32, 0);
            player.renderMode = VideoRenderMode.RenderTexture;
            player.targetTexture = videoTexture;
            player.isLooping = true;
            player.Play();
        }

        signaler = type == PeerType.Receiver ? new ReceiverSignaler() : new SenderSignaler(remoteIPAddress);
        signaler.OnConnected += Signaler_OnConnected;
        signaler.OnDesc += Signaler_OnDesc;
        signaler.Start();
    }

    private void Signaler_OnDesc(string ipAddress, RTCSessionDescription desc)
    {
        Debug.Log($"<LocalWebRTC> Signaler_OnDesc > ipAddress: {ipAddress}, desc: {desc.type}");
        if (peer == null)
        {
            CreatePeer();
        }
        StartCoroutine(SetDesc(DescSide.Remote, desc));
    }

    private void Signaler_OnConnected(string ipAddress)
    {
        Debug.Log($"<LocalWebRTC> Signaler_OnConnected > ipAddress: {ipAddress}");
        if (type == PeerType.Sender)
        {
            CreatePeer();
        }
    }

    private void CreatePeer()
    {
        Debug.Log($"<LocalWebRTC> CreatePeer");
        peer = new RTCPeerConnection();
        if (type == PeerType.Sender)
        {
            var videoTrack = new VideoStreamTrack(videoTexture);
            peer.AddTrack(videoTrack);
            StartCoroutine(CreateDesc(RTCSdpType.Offer));
        }
        else
        {
            peer.OnTrack = e =>
            {
                Debug.Log($"<LocalWebRTC> OnTrack");
                if (e.Track is VideoStreamTrack videoTrack)
                {
                    Debug.Log($"<LocalWebRTC> OnTrack > VideoStreamTrack");
                    videoTrack.OnVideoReceived += tex =>
                    {
                        Debug.Log($"<LocalWebRTC> OnTrack > OnVideoReceived");
                        display.GetComponent<Renderer>().material.mainTexture = tex;
                    };
                }

                if (e.Track is AudioStreamTrack audioTrack)
                {
                }
            };
        }
    }

    private IEnumerator CreateDesc(RTCSdpType sdpType)
    {
        Debug.Log($"<LocalWebRTC> CreateDesc > sdpType: {sdpType}");
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
        Debug.Log($"<LocalWebRTC> SetDesc > side: {side},  desc: {desc.type}");
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
