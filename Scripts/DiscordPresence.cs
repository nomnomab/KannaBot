//using System;
//using System.Diagnostics;
//using System.Timers;
//
//namespace KannaBot.Scripts
//{
//    public delegate void DynamicEvent<in T>(T t = default(T));
//
////    [System.Serializable]
////    public class DiscordJoinEvent : UnityEngine.Events.UnityEvent<string> { }
////
////    [System.Serializable]
////    public class DiscordSpectateEvent : UnityEngine.Events.UnityEvent<string> { }
////
////    [System.Serializable]
////    public class DiscordJoinRequestEvent : UnityEngine.Events.UnityEvent<DiscordRpc.JoinRequest> { }
//
//    public class DiscordPresence
//    {
//        private readonly DiscordRpc.RichPresence _presence = new DiscordRpc.RichPresence();
//        private readonly string _applicationId;
//        private readonly string _optionalSteamId;
//        private int _callbackCalls;
//        private int _clickCounter;
//        private DiscordRpc.JoinRequest _joinRequest;
//        public event DynamicEvent<object> OnConnect;
//        public event DynamicEvent<object> OnDisconnect;
//        public event DynamicEvent<object> HasResponded;
//        public event DynamicEvent<string> OnJoin;
//        public event DynamicEvent<string> OnSpectate;
//        public event DynamicEvent<DiscordRpc.JoinRequest> OnJoinRequest;
//        private DiscordRpc.EventHandlers _handlers;
//        private Timer _timer;
//
//        public DiscordPresence(string id, string optionalId = "")
//        {
//            _applicationId = id;
//            _optionalSteamId = optionalId;
//            Init();
//        }
//        
//        public void OnClick()
//        {
//            Program.Log("Discord: on click!");
//            _clickCounter++;
//
//            _presence.details = $"Button clicked {_clickCounter} times";
//
//            DiscordRpc.UpdatePresence(_presence);
//        }
//
//        public void RequestRespondYes()
//        {
//            Program.Log("Discord: responding yes to Ask to Join request");
//            DiscordRpc.Respond(_joinRequest.userId, DiscordRpc.Reply.Yes);
//            HasResponded?.Invoke();
//        }
//
//        public void RequestRespondNo()
//        {
//            Program.Log("Discord: responding no to Ask to Join request");
//            DiscordRpc.Respond(_joinRequest.userId, DiscordRpc.Reply.No);
//            HasResponded?.Invoke();
//        }
//
//        private void ReadyCallback()
//        {
//            ++_callbackCalls;
//            Program.Log("Discord: ready");
//            OnConnect?.Invoke();
//        }
//
//        private void DisconnectedCallback(int errorCode, string message)
//        {
//            ++_callbackCalls;
//            Program.Log($"Discord: disconnect {errorCode}: {message}");
//            OnDisconnect?.Invoke();
//        }
//
//        private void ErrorCallback(int errorCode, string message)
//        {
//            ++_callbackCalls;
//            Program.Log($"Discord: error {errorCode}: {message}");
//        }
//
//        private void JoinCallback(string secret)
//        {
//            ++_callbackCalls;
//            Program.Log($"Discord: join ({secret})");
//            OnJoin?.Invoke(secret);
//        }
//
//        private void SpectateCallback(string secret)
//        {
//            ++_callbackCalls;
//            Program.Log($"Discord: spectate ({secret})");
//            OnSpectate?.Invoke(secret);
//        }
//
//        private void RequestCallback(ref DiscordRpc.JoinRequest request)
//        {
//            ++_callbackCalls;
//            Program.Log($"Discord: join request {request.username}#{request.discriminator}: {request.userId}");
//            _joinRequest = request;
//            OnJoinRequest?.Invoke(request);
//        }
//
//        private void Init()
//        {
//            Program.Log("Discord: init");
//            _callbackCalls = 0;
//
//            _handlers = new DiscordRpc.EventHandlers();
//            _handlers.readyCallback = ReadyCallback;
//            _handlers.disconnectedCallback += DisconnectedCallback;
//            _handlers.errorCallback += ErrorCallback;
//            _handlers.joinCallback += JoinCallback;
//            _handlers.spectateCallback += SpectateCallback;
//            _handlers.requestCallback += RequestCallback;
//            DiscordRpc.Initialize(_applicationId, ref _handlers, true, _optionalSteamId);
//            _timer = new Timer(1000);
//            _timer.Elapsed += TimerOnElapsed;
//            _timer.Start();
//        }
//
//        private void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
//        {
//            DiscordRpc.RunCallbacks();
//        }
//
//        private void Disable()
//        {
//            Program.Log("Discord: shutdown");
//            DiscordRpc.Shutdown();
//        }
//    }
//}