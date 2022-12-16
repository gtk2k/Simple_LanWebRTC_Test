using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

internal class ReceiverSignaler : SignalerBase
{
    private WebSocketServer _wss;

    public ReceiverSignaler(int port = 8989) : base()
    {
        Debug.Log($"<ReceiverSignaler> constructor > port: {port}");

        _wss = new WebSocketServer(port);
        _wss.AddWebSocketService("/", () =>
        {
            var behaviour = new ReceiverSignalerBehaviour();
            behaviour.OnClientConnected += (clientIPAddress, ws) =>
            {
                clients.Add(clientIPAddress, ws);
                OnOpen(clientIPAddress);
            };
            behaviour.OnTextMessage += (ws, data) => OnMessage(null, data);
            behaviour.OnClientClosed += (ws, e) => OnClose(null, e);
            behaviour.OnClientError += (ws, e) => OnError(null, e);
            return behaviour;
        });
    }

    public override void Start()
    {
        Debug.Log($"<ReceiverSignaler> Start()");

        _wss.Start();
    }

    public override void Stop()
    {
        Debug.Log($"<ReceiverSignaler> Stop()");

        _wss.Stop();
    }

    public override void Dispose()
    {
        Debug.Log($"<ReceiverSignaler> Dispose()");

        if (_wss == null) return;
        _wss.Stop();
        _wss = null;
    }
}
