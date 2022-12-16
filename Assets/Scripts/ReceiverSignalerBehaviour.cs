using System;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

internal class ReceiverSignalerBehaviour : WebSocketBehavior
{
    public event Action<string, WebSocket> OnClientConnected;
    public event Action<string, string> OnTextMessage;
    public event Action<string, CloseEventArgs> OnClientClosed;
    public event Action<string, ErrorEventArgs> OnClientError;

    protected override void OnOpen()
    {
        Debug.Log($"<ReceiverSignalerBehaviour> OnOpen > ipAddress: {Context.UserEndPoint.Address}");

        OnClientConnected?.Invoke(Context.UserEndPoint.Address.ToString(), Context.WebSocket);
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        Debug.Log($"<ReceiverSignalerBehaviour> OnMessage > data: {e.Data.Substring(0, 10)}");

        OnTextMessage?.Invoke(Context.UserEndPoint.Address.ToString(), e.Data);
    }

    protected override void OnClose(CloseEventArgs e)
    {
        Debug.Log($"<ReceiverSignalerBehaviour> OnClose");

        OnClientClosed?.Invoke(Context.UserEndPoint.Address.ToString(), e);
    }

    protected override void OnError(ErrorEventArgs e)
    {
        Debug.Log($"<ReceiverSignalerBehaviour> OnError > err: {e.Message}");

        OnClientError?.Invoke(Context.UserEndPoint.Address.ToString(), e);
    }
}