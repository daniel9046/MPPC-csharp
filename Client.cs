using System;
using System.Collections.Generic;
using System.Linq;
using WebSocketSharp;

public class MppClient : EventEmitter
{
    private string uri = "wss://mppclone.com:8443";
    private WebSocket ws;
    public int serverTimeOffset { get; set; }
    public object user { get; set; }
    public int participantId { get; set; }
    public object channel { get; set; }
    public Dictionary<string, object> ppl { get; set; }
    public int connectionTime { get; set; }
    public int connectionAttempts { get; set; }
    public int desiredChannelId { get; set; }
    public object desiredChannelSettings { get; set; }
    public int pingInterval { get; set; }
    public bool canConnect { get; set; }
    public List<object> noteBuffer { get; set; }
    public int noteBufferTime { get; set; }
    public int noteFlushInterval { get; set; }
    public Dictionary<string, object> permissions { get; set; }
    public int cat { get; set; }
    public object loginInfo { get; set; }
    public string token { get; set; }

    public MppClient()
    {
        uri = "wss://mppclone.com:8443";
        ws = null;
        serverTimeOffset = 0;
        user = null;
        participantId = 0;
        channel = null;
        ppl = new Dictionary<string, object>();
        connectionTime = 0;
        connectionAttempts = 0;
        desiredChannelId = 0;
        desiredChannelSettings = null;
        pingInterval = 0;
        canConnect = false;
        noteBuffer = new List<object>();
        noteBufferTime = 0;
        noteFlushInterval = 0;
        permissions = new Dictionary<string, object>();
        cat = 0;
        loginInfo = null;
        token = "0f1115e5db841f84ecd397c8.81758c47-b00b-4fc0-bea5-aef92ba115ab";

        BindEventListeners();

        Emit("status", "(Offline mode)");
    }

    public bool IsSupported()
    {
        return typeof(WebSocket) == typeof(WebSocket);
    }

    public bool IsConnected()
    {
        return IsSupported() && ws != null && ws.ReadyState == WebSocketState.Open;
    }

    public bool IsConnecting()
    {
        return IsSupported() && ws != null && ws.ReadyState == WebSocketState.Connecting;
    }

    public void Start()
    {
        canConnect = true;
        if (connectionTime == 0)
        {
            Connect();
        }
    }

    public void Stop()
    {
        canConnect = false;
        ws.Close();
    }

public void Connect() {
if (!CanConnect || !IsSupported() || IsConnected() || IsConnecting()) {
return;
}
Emit("status", "Connecting...");
ws = new WebSocket(Uri);
ws.OnClose += (sender, e) => {
    User = null;
    participantId = null;
    Channel = null;
    SetParticipants(new List<Participant>());
    ws.Close();
    Emit("disconnect", e);
    Emit("status", "Offline mode");

    // reconnect!
    if (connectionTime != null) {
        connectionTime = null;
        connectionAttempts = 0;
    } else {
        connectionAttempts++;
    }
    int[] ms_lut = { 50, 2500, 10000 };
    int idx = connectionAttempts;
    if (idx >= ms_lut.Length) idx = ms_lut.Length - 1;
    int ms = ms_lut[idx];
    Task.Delay(ms).ContinueWith(t => Connect());
};
ws.OnError += (sender, e) => {
    Emit("wserror", e);
    ws.Close();
};
ws.OnOpen += (sender, e) => {
    Emit("connect");
    Emit("status", "Joining channel...");
};
ws.OnMessage += async (sender, e) => {
    List<Message> transmission = JsonConvert.DeserializeObject<List<Message>>(e.Data);
    foreach (var msg in transmission) {
        Emit(msg.m, msg);
    }
};
}

private void BindEventListeners() {
Emit("status", "(Offline mode)");
}

public bool IsSupported() {
return true;
}

public bool IsConnected() {
return IsSupported() && ws != null && ws.ReadyState == WebSocketState.Open;
}

public bool IsConnecting() {
return IsSupported() && ws != null && ws.ReadyState == WebSocketState.Connecting;
}

public void Start() {
CanConnect = true;
if (connectionTime == null) {
Connect();
}
}

public void Stop() {
CanConnect = false;
ws.Close();
}

public void Connect() {
if (!CanConnect || !IsSupported() || IsConnected() || IsConnecting()) {
return;
}
Emit("status", "Connecting...");
ws = new WebSocket(Uri);
ws.OnClose += (sender, e) => {
    User = null;
    participantId = null;
    Channel = null;
    SetParticipants(new List<Participant>());
    ws.Close();
    Emit("disconnect", e);
    Emit("status", "Offline mode");

    // reconnect!
    if (connectionTime != null) {
        connectionTime = null;
        connectionAttempts = 0;
    } else {
        connectionAttempts++;
    }
    int[] ms_lut = { 50, 2500, 10000 };
    int idx = connectionAttempts;
    if (idx >= ms_lut.Length) idx = ms_lut.Length - 1;
    int ms = ms_lut[idx];
    Task.Delay(ms).ContinueWith(t => Connect());
};
ws.OnError += (sender, e) => {
    Emit("wserror", e);
    ws.Close();
};
ws.OnOpen += (sender, e) => {
    Emit("connect");
    Emit("status", "Joining channel...");
};
ws.OnMessage += async (sender, e) => {
    List<Message> transmission = JsonConvert.DeserializeObject<List<Message>>(e.Data);
    foreach (var msg in transmission) {
        Emit(msg.m, msg);
    }
};
}

private void BindEventListeners()
{
var self = this;
this.On("hi", (msg) => {
self.connectionTime = DateTime.Now.Ticks;
self.user = msg.u;
self.ReceiveServerTime(msg.t, msg.e ?? default(object));
if(self.desiredChannelId != null)
{
self.SetChannel();
}
if (msg.permissions != null)
{
self.permissions = msg.permissions;
}
else
{
self.permissions = new Dictionary<string, object>();
}
if (msg.accountInfo != null)
{
self.accountInfo = msg.accountInfo;
}
else
{
self.accountInfo = null;
}
});
this.On("t", (msg) => {
self.ReceiveServerTime(msg.t, msg.e ?? default(object));
});
this.On("ch", (msg) => {
self.desiredChannelId = msg.ch._id;
self.desiredChannelSettings = msg.ch.settings;
self.channel = msg.ch;
if(msg.p != null)
self.participantId = msg.p;
self.SetParticipants(msg.ppl);
});
this.On("p", (msg) => {
self.ParticipantUpdate(msg);
self.Emit("participant update", self.FindParticipantById(msg.id));
});
this.On("m", (msg) => {
if(self.ppl.ContainsKey(msg.id))
{
self.ParticipantMoveMouse(msg);
}
});
this.On("bye", (msg) => {
self.RemoveParticipant(msg.p);
});
this.On("b", (msg) => {
var hiMsg = new Dictionary<string, object>
{
{"m", "hi"}
};
hiMsg["🐈"] = self["🐈"]++ ?? default(object);
if (this.loginInfo != null)
hiMsg.Add("login", this.loginInfo);
this.loginInfo = null;
try
{
if (msg.code.StartsWith("~"))
{
hiMsg.Add("code", msg.code.Substring(1));
}
else
{
hiMsg.Add("code", msg.code);
}
}
catch (Exception err)
{
hiMsg.Add("code", "broken");
}
hiMsg.Add("token", this.token);
self.SendArray(new List<Dictionary<string, object>> { hiMsg });
});
}

}
