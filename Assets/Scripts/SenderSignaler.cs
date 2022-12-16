using UnityEngine;
using WebSocketSharp;

internal class SenderSignaler : SignalerBase
{
    private WebSocket _ws;
    private string _ipAddress;

    public SenderSignaler(string ipAddress, int port = 8989) : base()
    {
        Debug.Log($"<SenderSignaler> constructor > ipAddress: {ipAddress}, port: {port}");

        _ws = new WebSocket($"ws://{ipAddress}:{port}");
        _ws.OnOpen += (s, e) => OnOpen(_ipAddress);
        _ws.OnMessage += (s, e) => OnMessage(_ipAddress, e.Data);
        _ws.OnClose += (s, e) => OnClose(_ipAddress, e);
        _ws.OnError += (s, e) => OnError(_ipAddress, e);
        clients.Add(ipAddress, _ws);
    }

    public override void Start()
    {
        Debug.Log($"<SenderSignaler> Start");

        _ws.Connect();
    }

    public override void Stop()
    {
        Debug.Log($"<SenderSignaler> Stop");

        _ws.Close();
    }

    public override void Dispose()
    {
        Debug.Log($"<SenderSignaler> Dispose");

        if (_ws == null) return;
        if (_ws.ReadyState == WebSocketState.Open) _ws.Close();
        _ws = null;
    }
}
