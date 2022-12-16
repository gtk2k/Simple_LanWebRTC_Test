using MemoryPack;
using System.Collections;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.Video;

public class LocalWebRTC : MonoBehaviour
{
    [SerializeField] private PeerType type;
    [SerializeField] private string remoteIPAddress;
    [SerializeField] private GameObject videoPlayerGO;
    [SerializeField] private StreamingType streamingType;
    [SerializeField] private GameObject display;
    [SerializeField] private Vector2Int streamingSize;

    private SignalerBase signaler;
    private RTCPeerConnection peer;
    private RTCDataChannel dc;

    public RenderTexture streamingTexture, videoTexture;

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

    private enum StreamingType
    {
        Video,
        Screen
    }

    private enum DataType
    {
        None = 0,
        StreamingType = 1,
        ScreenSize = 2
    }

    private void Start()
    {
        Debug.Log($"<LocalWebRTC> Start");
        StartCoroutine(WebRTC.Update());

        if (type == PeerType.Sender)
        {
            streamingTexture = new RenderTexture(streamingSize.x, streamingSize.y, 0, RenderTextureFormat.BGRA32, 0);
            if (streamingType == StreamingType.Screen)
                videoTexture = new RenderTexture(streamingSize.x, streamingSize.y, 0, RenderTextureFormat.BGRA32, 0);
            else
                videoTexture = streamingTexture;
            var videoPlayer = videoPlayerGO.GetComponent<VideoPlayer>();
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayerGO.GetComponent<Renderer>().material.mainTexture = videoPlayer.targetTexture = videoTexture;
            videoPlayer.isLooping = true;
            videoPlayer.Play();
        }

        signaler = type == PeerType.Receiver ? new ReceiverSignaler() : new SenderSignaler(remoteIPAddress);
        signaler.OnConnected += Signaler_OnConnected;
        signaler.OnDesc += Signaler_OnDesc;
        signaler.OnCand += Signaler_OnCand;
        signaler.Start();
    }

    private void ScreenCap()
    {
        ScreenCapture.CaptureScreenshotIntoRenderTexture(streamingTexture);
    }

    private void Update()
    {
        if (streamingType == StreamingType.Screen)
        {
            ScreenCap();
            if(dc != null && dc.ReadyState == RTCDataChannelState.Open)
            {
                dc.Send(MemoryPackSerializer.Serialize(new Vector2(Screen.width, Screen.height)));
            }
        }
    }

    private void Signaler_OnCand(string ipAddress, RTCIceCandidate cand)
    {
        Debug.Log($"<LocalWebRTC> Signaler_OnCand > ipAddress: {ipAddress}, cand: {cand.Candidate}");
        peer.AddIceCandidate(cand);
    }

    private void OnApplicationQuit()
    {
        signaler.Stop();
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
            var videoTrack = new VideoStreamTrack(streamingTexture);
            peer.OnIceCandidate = cand =>
            {
                signaler.Send(remoteIPAddress, SignalingMessage.FromCand(cand));
            };
            peer.AddTrack(videoTrack);
            dc = peer.CreateDataChannel("test");
            dcEventHandler();
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
            peer.OnDataChannel = dataChannel =>
            {
                dc = dataChannel;
                dcEventHandler();
            };
        }
    }

    private void dcEventHandler()
    {
        dc.OnOpen = () =>
        {
            Debug.Log($"<DataChannel> Open");
        };
        dc.OnMessage = data =>
        {
            var screenSize = MemoryPackSerializer.Deserialize<Vector2>(data);
            Debug.Log(screenSize);
            display.transform.localScale = new Vector2(display.transform.localScale.x, display.transform.localScale.x * screenSize.y / screenSize.x);
        };
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
        if (op.IsError)
        {
            Debug.LogError($"Set {side} {desc.type} Error: {op.Error.message}");
            yield break;
        }
        if (side == DescSide.Local)
        {
            signaler.Send(remoteIPAddress, SignalingMessage.FromDesc(desc));
        }
        else if (desc.type == RTCSdpType.Offer)
        {
            yield return StartCoroutine(CreateDesc(RTCSdpType.Answer));
        }
    }
}
