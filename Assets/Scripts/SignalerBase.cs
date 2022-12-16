using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using Unity.WebRTC;
using UnityEngine;
using WebSocketSharp;

internal class SignalerBase
{
    public event Action<string> OnConnected;
    public event Action<string, RTCIceCandidate> OnCand;
    public event Action<string, RTCSessionDescription> OnDesc;

    private SynchronizationContext _context;
    protected string _peerName;

    protected JsonSerializerSettings jsonSettings = new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Ignore
    };

    protected Dictionary<string, WebSocket> clients = new Dictionary<string, WebSocket>();

    protected SignalerBase()
    {
        Debug.Log($"<SignalerBase> constructor");

        _context = SynchronizationContext.Current;
    }

    public virtual void Start() { }
    public virtual void Stop() { }
    public virtual void Dispose() { }

    protected void OnOpen(string ipAddress)
    {
        Debug.Log($"<SignalerBase> OnOpen > ipAddress: {ipAddress}");

        _context.Post(_ =>
        {
            OnConnected?.Invoke(ipAddress);
        }, null);
    }

    protected void OnMessage(string ipAddress, string data)
    {
        Debug.Log($"<SignalerBase> OnMessage > ipAddress: {ipAddress}, data: {data.Substring(0, 10)}");

        var msg = JsonConvert.DeserializeObject<SignalingMessage>(data, jsonSettings);
        switch (msg.type)
        {
            //case "id":
            //    _id = msg.id;
            //    clients.Add(_id, ws);
            //    break;
            case "offer":
            case "answer":
                _context.Post(_ =>
                {
                    var desc = msg.ToDesc();
                    OnDesc?.Invoke(ipAddress, desc);
                }, null);
                break;

            case "candidate":
                _context.Post(_ =>
                {
                    var cand = msg.ToCand();
                    OnCand?.Invoke(ipAddress, cand);
                }, null);
                break;
        }
    }

    protected void OnClose(string ipAddress, CloseEventArgs e)
    {
        Debug.Log($"<SignalerBase> OnClose > ipAddress: {ipAddress}, code: {e.Code}, reason:{e.Reason}");
    }

    protected void OnError(string ipAddress, ErrorEventArgs e)
    {
        Debug.Log($"<SignalerBase> OnClose > ipAddress: {ipAddress}, err: {e.Message}");
    }

    public void Send(string ipAddress, SignalingMessage msg)
    {
        Debug.Log($"<SignalerBase> Send > ipAddress: {ipAddress}, err: {msg}");

        var sendData = JsonConvert.SerializeObject(msg, jsonSettings);
        clients[ipAddress].Send(sendData);
    }
}