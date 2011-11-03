using System;
using System.Collections.Generic;
using System.Net;
using System.Timers;
using System.Threading;
using Cometd.Bayeux;
using Cometd.Bayeux.Client;
using Cometd.Client.Transport;
using Cometd.Common;


namespace Cometd.Client
{
    /// <summary> </summary>
    public class BayeuxClient : AbstractClientSession, IBayeux
    {
        public const String BACKOFF_INCREMENT_OPTION = "backoffIncrement";
        public const String MAX_BACKOFF_OPTION = "maxBackoff";
        public const String BAYEUX_VERSION = "1.0";

        //private Logger logger;
        private TransportRegistry transportRegistry = new TransportRegistry();
        private Dictionary<String, Object> options = new Dictionary<String, Object>();
        private BayeuxClientState bayeuxClientState;
        private Queue<IMutableMessage> messageQueue = new Queue<IMutableMessage>();
        private CookieCollection cookieCollection = new CookieCollection();
        private ITransportListener handshakeListener;
        private ITransportListener connectListener;
        private ITransportListener disconnectListener;
        private ITransportListener publishListener;
        private long backoffIncrement;
        private long maxBackoff;
        private static Mutex stateUpdateInProgressMutex = new Mutex();
        private int stateUpdateInProgress;
        private AutoResetEvent stateChanged = new AutoResetEvent(false);


        public BayeuxClient(String url, IList<ClientTransport> transports)
        {
            //logger = Log.getLogger(GetType().FullName + "@" + this.GetHashCode());
            //Console.WriteLine(GetType().FullName + "@" + this.GetHashCode());

            handshakeListener = new HandshakeTransportListener(this);
            connectListener = new ConnectTransportListener(this);
            disconnectListener = new DisconnectTransportListener(this);
            publishListener = new PublishTransportListener(this);

            if (transports == null || transports.Count == 0)
                throw new ArgumentException("Transport cannot be null");

            foreach (ClientTransport t in transports)
                transportRegistry.Add(t);

            foreach (String transportName in transportRegistry.KnownTransports)
            {
                ClientTransport clientTransport = transportRegistry.getTransport(transportName);
                if (clientTransport is HttpClientTransport)
                {
                    HttpClientTransport httpTransport = (HttpClientTransport)clientTransport;
                    httpTransport.setURL(url);
                    httpTransport.setCookieCollection(cookieCollection);
                }
            }

            bayeuxClientState = new DisconnectedState(this, null);
        }

        public long BackoffIncrement
        {
            get
            {
                return backoffIncrement;
            }
        }

        public long MaxBackoff
        {
            get
            {
                return maxBackoff;
            }
        }

        public String getCookie(String name)
        {
            Cookie cookie = cookieCollection[name];
            if (cookie != null)
                return cookie.Value;
            return null;
        }

        public void setCookie(String name, String val)
        {
            setCookie(name, val, -1);
        }

        public void setCookie(String name, String val, int maxAge)
        {
            Cookie cookie = new Cookie(name, val, null, null);
            if (maxAge > 0)
            {
                cookie.Expires = DateTime.Now;
                cookie.Expires.AddMilliseconds(maxAge);
            }
            cookieCollection.Add(cookie);
        }

        public override String Id
        {
            get
            {
                return bayeuxClientState.clientId;
            }
        }

        public override bool Connected
        {
            get
            {
                return isConnected(bayeuxClientState);
            }
        }

        private bool isConnected(BayeuxClientState bayeuxClientState)
        {
            return bayeuxClientState.type == State.CONNECTED;
        }

        public override bool Handshook
        {
            get
            {
                return isHandshook(bayeuxClientState);
            }
        }

        private bool isHandshook(BayeuxClientState bayeuxClientState)
        {
            return bayeuxClientState.type == State.CONNECTING || bayeuxClientState.type == State.CONNECTED || bayeuxClientState.type == State.UNCONNECTED;
        }

        private bool isHandshaking(BayeuxClientState bayeuxClientState)
        {
            return bayeuxClientState.type == State.HANDSHAKING || bayeuxClientState.type == State.REHANDSHAKING;
        }

        public bool Disconnected
        {
            get
            {
                return isDisconnected(bayeuxClientState);
            }
        }

        private bool isDisconnected(BayeuxClientState bayeuxClientState)
        {
            return bayeuxClientState.type == State.DISCONNECTING || bayeuxClientState.type == State.DISCONNECTED;
        }

        protected State CurrentState
        {
            get
            {
                return bayeuxClientState.type;
            }
        }

        public override void handshake()
        {
            handshake(null);
        }

        public override void handshake(IDictionary<String, Object> handshakeFields)
        {
            initialize();

            IList<String> allowedTransports = AllowedTransports;
            // Pick the first transport for the handshake, it will renegotiate if not right
            ClientTransport initialTransport = transportRegistry.getTransport(allowedTransports[0]);
            initialTransport.init();
            //Console.WriteLine("Using initial transport {0} from {1}", initialTransport.Name, Print.List(allowedTransports));

            updateBayeuxClientState(
                    delegate(BayeuxClientState oldState)
                    {
                        return new HandshakingState(this, handshakeFields, initialTransport);
                    });
        }

        public State handshake(int waitMs)
        {
            return handshake(null, waitMs);
        }

        public State handshake(IDictionary<String, Object> template, int waitMs)
        {
            handshake(template);
            ICollection<State> states = new List<State>();
            states.Add(State.CONNECTING);
            states.Add(State.DISCONNECTED);
            return waitFor(waitMs, states);
        }

        protected bool sendHandshake()
        {
            BayeuxClientState bayeuxClientState = this.bayeuxClientState;

            if (isHandshaking(bayeuxClientState))
            {
                IMutableMessage message = newMessage();
                if (bayeuxClientState.handshakeFields != null)
                    foreach (KeyValuePair<String, Object> kvp in bayeuxClientState.handshakeFields)
                        message.Add(kvp.Key, kvp.Value);

                message.Channel = Channel_Fields.META_HANDSHAKE;
                message[Message_Fields.SUPPORTED_CONNECTION_TYPES_FIELD] = AllowedTransports;
                message[Message_Fields.VERSION_FIELD] = BayeuxClient.BAYEUX_VERSION;
                if (message.Id == null)
                    message.Id = newMessageId();

                //Console.WriteLine("Handshaking with extra fields {0}, transport {1}", Print.Dictionary(bayeuxClientState.handshakeFields), Print.Dictionary(bayeuxClientState.transport as IDictionary<String, Object>));
                bayeuxClientState.send(handshakeListener, message);
                return true;
            }
            return false;
        }

        public State waitFor(int waitMs, ICollection<State> states)
        {
            DateTime stop = DateTime.Now.AddMilliseconds(waitMs);
            int duration = waitMs;

            State s = CurrentState;
            if(states.Contains(s))
                return s;

            while (stateChanged.WaitOne(duration))
            {
                if (stateUpdateInProgress == 0)
                {
                    s = CurrentState;
                    if (states.Contains(s))
                        return s;
                }

                duration = (int)(stop - DateTime.Now).TotalMilliseconds;
                if (duration <= 0) break;
            }

            s = CurrentState;
            if (states.Contains(s))
                return s;

            return State.INVALID;
        }

        protected bool sendConnect()
        {
            BayeuxClientState bayeuxClientState = this.bayeuxClientState;
            if (isHandshook(bayeuxClientState))
            {
                IMutableMessage message = newMessage();
                message.Channel = Channel_Fields.META_CONNECT;
                message[Message_Fields.CONNECTION_TYPE_FIELD] = bayeuxClientState.transport.Name;
                if (bayeuxClientState.type == State.CONNECTING || bayeuxClientState.type == State.UNCONNECTED)
                {
                    // First connect after handshake or after failure, add advice
                    message.getAdvice(true)["timeout"] = 0;
                }
                bayeuxClientState.send(connectListener, message);
                return true;
            }
            return false;
        }

        protected override ChannelId newChannelId(String channelId)
        {
            // Save some parsing by checking if there is already one
            AbstractSessionChannel channel;
            Channels.TryGetValue(channelId, out channel);
            return channel == null ? new ChannelId(channelId) : channel.ChannelId;
        }

        protected override AbstractSessionChannel newChannel(ChannelId channelId)
        {
            return new BayeuxClientChannel(this, channelId);
        }

        protected override void sendBatch()
        {
            BayeuxClientState bayeuxClientState = this.bayeuxClientState;
            if (isHandshaking(bayeuxClientState))
                return;

            IList<IMutableMessage> messages = takeMessages();
            if (messages.Count > 0)
                sendMessages(messages);
        }

        protected bool sendMessages(IList<IMutableMessage> messages)
        {
            BayeuxClientState bayeuxClientState = this.bayeuxClientState;
            if (bayeuxClientState.type == State.CONNECTING || isConnected(bayeuxClientState))
            {
                bayeuxClientState.send(publishListener, messages);
                return true;
            }
            else
            {
                failMessages(null, ObjectConverter.ToListOfIMessage(messages));
                return false;
            }
        }

        /// <summary>
        /// Wait for send queue to be emptied
        /// </summary>
        /// <param name="timeoutMS"></param>
        /// <returns>true if queue is empty, false if timed out</returns>
        public bool waitForEmptySendQueue(int timeoutMS)
        {
            if (messageQueue.Count == 0)
                return true;

            DateTime start = DateTime.Now;

            while ((DateTime.Now - start).TotalMilliseconds < timeoutMS)
            {
                if (messageQueue.Count == 0)
                    return true;

                System.Threading.Thread.Sleep(100);
            }

            return false;
        }

        private IList<IMutableMessage> takeMessages()
        {
            IList<IMutableMessage> queue = new List<IMutableMessage>(messageQueue);
            messageQueue.Clear();
            return queue;
        }

        public override void disconnect()
        {
            updateBayeuxClientState(
                    delegate(BayeuxClientState oldState)
                    {
                        if (isConnected(oldState))
                            return new DisconnectingState(this, oldState.transport, oldState.clientId);
                        else
                            return new DisconnectedState(this, oldState.transport);
                    });
        }

        public void abort()
        {
            updateBayeuxClientState(
                    delegate(BayeuxClientState oldState)
                    {
                        return new AbortedState(this, oldState.transport);
                    });
        }

        protected void processHandshake(IMutableMessage handshake)
        {
            if (handshake.Successful)
            {
                // @@ax: I think this should be able to return a list of objects?
                Object serverTransportObject;
                handshake.TryGetValue(Message_Fields.SUPPORTED_CONNECTION_TYPES_FIELD, out serverTransportObject);
                IList<Object> serverTransports = serverTransportObject as IList<Object>;
                //Console.WriteLine("Supported transport: {0}", serverTransport);
                //IList<Object> serverTransports = new List<Object>();
                //serverTransports.Add(serverTransport);
                IList<ClientTransport> negotiatedTransports = transportRegistry.Negotiate(serverTransports, BAYEUX_VERSION);
                ClientTransport newTransport = negotiatedTransports.Count == 0 ? null : negotiatedTransports[0];
                if (newTransport == null)
                {
                    updateBayeuxClientState(
                            delegate(BayeuxClientState oldState)
                            {
                                return new DisconnectedState(this, oldState.transport);
                            },
                            delegate()
                            {
                                receive(handshake);
                            });

                    // Signal the failure
                    String error = "405:c" + transportRegistry.AllowedTransports + ",s" + serverTransports.ToString() + ":no transport";

                    handshake.Successful = false;
                    handshake[Message_Fields.ERROR_FIELD] = error;
                    // TODO: also update the advice with reconnect=none for listeners ?
                }
                else
                {
                    updateBayeuxClientState(
                            delegate(BayeuxClientState oldState)
                            {
                                if (newTransport != oldState.transport)
                                {
                                    oldState.transport.reset();
                                    newTransport.init();
                                }

                                String action = getAdviceAction(handshake.Advice, Message_Fields.RECONNECT_RETRY_VALUE);
                                if (Message_Fields.RECONNECT_RETRY_VALUE.Equals(action))
                                    return new ConnectingState(this, oldState.handshakeFields, handshake.Advice, newTransport, handshake.ClientId);
                                else if (Message_Fields.RECONNECT_NONE_VALUE.Equals(action))
                                    return new DisconnectedState(this, oldState.transport);

                                return null;
                            },
                            delegate()
                            {
                                receive(handshake);
                            });
                }
            }
            else
            {
                updateBayeuxClientState(
                        delegate(BayeuxClientState oldState)
                        {
                            String action = getAdviceAction(handshake.Advice, Message_Fields.RECONNECT_HANDSHAKE_VALUE);
                            if (Message_Fields.RECONNECT_HANDSHAKE_VALUE.Equals(action) || Message_Fields.RECONNECT_RETRY_VALUE.Equals(action))
                                return new RehandshakingState(this, oldState.handshakeFields, oldState.transport, oldState.nextBackoff());
                            else if (Message_Fields.RECONNECT_NONE_VALUE.Equals(action))
                                return new DisconnectedState(this, oldState.transport);
                            return null;
                        },
                        delegate()
                        {
                            receive(handshake);
                        });
            }
        }

        protected void processConnect(IMutableMessage connect)
        {
            updateBayeuxClientState(
                    delegate(BayeuxClientState oldState)
                    {
                        IDictionary<String, Object> advice = connect.Advice;
                        if (advice == null)
                            advice = oldState.advice;

                        String action = getAdviceAction(advice, Message_Fields.RECONNECT_RETRY_VALUE);
                        if (connect.Successful)
                        {
                            if (Message_Fields.RECONNECT_RETRY_VALUE.Equals(action))
                                return new ConnectedState(this, oldState.handshakeFields, advice, oldState.transport, oldState.clientId);
                            else if (Message_Fields.RECONNECT_NONE_VALUE.Equals(action))
                                // This case happens when the connect reply arrives after a disconnect
                                // We do not go into a disconnected state to allow normal processing of the disconnect reply
                                return new DisconnectingState(this, oldState.transport, oldState.clientId);
                        }
                        else
                        {
                            if (Message_Fields.RECONNECT_HANDSHAKE_VALUE.Equals(action))
                                return new RehandshakingState(this, oldState.handshakeFields, oldState.transport, 0);
                            else if (Message_Fields.RECONNECT_RETRY_VALUE.Equals(action))
                                return new UnconnectedState(this, oldState.handshakeFields, advice, oldState.transport, oldState.clientId, oldState.nextBackoff());
                            else if (Message_Fields.RECONNECT_NONE_VALUE.Equals(action))
                                return new DisconnectedState(this, oldState.transport);
                        }

                        return null;
                    },
                delegate()
                {
                    receive(connect);
                });
        }

        protected void processDisconnect(IMutableMessage disconnect)
        {
            updateBayeuxClientState(
                    delegate(BayeuxClientState oldState)
                    {
                        return new DisconnectedState(this, oldState.transport);
                    },
                    delegate()
                    {
                        receive(disconnect);
                    });
        }

        protected void processMessage(IMutableMessage message)
        {
            // logger.debug("Processing message {}", message);
            receive(message);
        }

        private String getAdviceAction(IDictionary<String, Object> advice, String defaultResult)
        {
            String action = defaultResult;
            if (advice != null && advice.ContainsKey(Message_Fields.RECONNECT_FIELD))
                action = ((String)advice[Message_Fields.RECONNECT_FIELD]);
            return action;
        }

        protected bool scheduleHandshake(long interval, long backoff)
        {
            return scheduleAction(
                    delegate(object sender, ElapsedEventArgs e)
                    {
                        sendHandshake();
                    }
                    , interval, backoff);
        }

        protected bool scheduleConnect(long interval, long backoff)
        {
            return scheduleAction(
                    delegate(object sender, ElapsedEventArgs e)
                    {
                        sendConnect();
                    }
                    , interval, backoff);
        }

        private bool scheduleAction(ElapsedEventHandler action, long interval, long backoff)
        {
            System.Timers.Timer timer = new System.Timers.Timer(); // @@ax: What about support for multiple timers?
            timer.Elapsed += action;
            long wait = interval + backoff;
            if (wait <= 0) wait = 1;
            timer.Interval = wait;
            timer.AutoReset = false;
            timer.Enabled = true;
            return true;
        }

        public IList<String> AllowedTransports
        {
            get
            {
                return transportRegistry.AllowedTransports;
            }
        }

        public ICollection<String> KnownTransportNames
        {
            get
            {
                return transportRegistry.KnownTransports;
            }
        }

        public ITransport getTransport(String transport)
        {
            return transportRegistry.getTransport(transport);
        }

        public void setDebugEnabled(bool debug)
        {
            // ... todo
        }

        public bool isDebugEnabled()
        {
            return false;
        }

        protected void initialize()
        {
            Int64 backoffIncrement = ObjectConverter.ToInt64(getOption(BACKOFF_INCREMENT_OPTION), 1000L);
            this.backoffIncrement = backoffIncrement;

            Int64 maxBackoff = ObjectConverter.ToInt64(getOption(MAX_BACKOFF_OPTION), 30000L);
            this.maxBackoff = maxBackoff;
        }

        protected void terminate()
        {
            IList<IMutableMessage> messages = takeMessages();
            failMessages(null, ObjectConverter.ToListOfIMessage(messages));
        }

        public Object getOption(String qualifiedName)
        {
            Object obj;
            options.TryGetValue(qualifiedName, out obj);
            return obj;
        }

        public void setOption(String qualifiedName, Object val)
        {
            options[qualifiedName] = val;
        }

        public ICollection<String> OptionNames
        {
            get
            {
                return options.Keys;
            }
        }

        public IDictionary<String, Object> Options
        {
            // @@ax: Should return a copy?
            get
            {
                return options;
            }
        }

        protected IMutableMessage newMessage()
        {
            return new DictionaryMessage();
        }

        protected void enqueueSend(IMutableMessage message)
        {
            if (canSend())
            {
                IList<IMutableMessage> messages = new List<IMutableMessage>();
                messages.Add(message);
                bool sent = sendMessages(messages);
                //Console.WriteLine("{0} message {1}", sent?"Sent":"Failed", message);
            }
            else
            {
                messageQueue.Enqueue(message);
                //Console.WriteLine("Enqueued message {0} (batching: {1})", message, this.Batching);
            }
        }

        private bool canSend()
        {
            return !isDisconnected(this.bayeuxClientState) && !this.Batching && !isHandshaking(this.bayeuxClientState);
        }

        protected void failMessages(Exception x, IList<IMessage> messages)
        {
            foreach (IMessage message in messages)
            {
                IMutableMessage failed = newMessage();
                failed.Id = message.Id;
                failed.Successful = false;
                failed.Channel = message.Channel;
                failed["message"] = messages;
                if (x != null)
                    failed["exception"] = x;
                receive(failed);
            }
        }

        public void onSending(IList<IMessage> messages)
        {
        }

        public void onMessages(IList<IMutableMessage> messages)
        {
        }

        public virtual void onFailure(Exception x, IList<IMessage> messages)
        {
            Console.WriteLine("{0}", x.ToString());
        }

        private void updateBayeuxClientState(BayeuxClientStateUpdater_createDelegate create)
        {
            updateBayeuxClientState(create, null);
        }

        private void updateBayeuxClientState(BayeuxClientStateUpdater_createDelegate create, BayeuxClientStateUpdater_postCreateDelegate postCreate)
        {
            stateUpdateInProgressMutex.WaitOne();
            ++stateUpdateInProgress;
            stateUpdateInProgressMutex.ReleaseMutex();

            BayeuxClientState newState = null;
            BayeuxClientState oldState = bayeuxClientState;

            newState = create(oldState);
            if (newState == null)
                throw new SystemException();

            if (!oldState.isUpdateableTo(newState))
            {
                //Console.WriteLine("State not updateable : {0} -> {1}", oldState, newState);
                return;
            }

            bayeuxClientState = newState;

            if (postCreate != null) postCreate();

            if (oldState.Type != newState.Type)
                newState.enter(oldState.Type);

            newState.execute();

            // Notify threads waiting in waitFor()
            stateUpdateInProgressMutex.WaitOne();
            --stateUpdateInProgress;

            if (stateUpdateInProgress == 0)
                stateChanged.Set();
            stateUpdateInProgressMutex.ReleaseMutex();
        }

        public String dump()
        {
            return "";
        }

        public enum State
        {
            INVALID, UNCONNECTED, HANDSHAKING, REHANDSHAKING, CONNECTING, CONNECTED, DISCONNECTING, DISCONNECTED
        }

        private class PublishTransportListener : ITransportListener
        {
            protected BayeuxClient bayeuxClient;

            public PublishTransportListener(BayeuxClient bayeuxClient)
            {
                this.bayeuxClient = bayeuxClient;
            }

            public void onSending(IList<IMessage> messages)
            {
                bayeuxClient.onSending(messages);
            }

            public void onMessages(IList<IMutableMessage> messages)
            {
                bayeuxClient.onMessages(messages);
                foreach (IMutableMessage message in messages)
                    processMessage(message);
            }

            public void onConnectException(Exception x, IList<IMessage> messages)
            {
                onFailure(x, messages);
            }

            public void onException(Exception x, IList<IMessage> messages)
            {
                onFailure(x, messages);
            }

            public void onExpire(IList<IMessage> messages)
            {
                onFailure(new TimeoutException("expired"), messages);
            }

            public void onProtocolError(String info, IList<IMessage> messages)
            {
                onFailure(new ProtocolViolationException(info), messages);
            }

            protected virtual void processMessage(IMutableMessage message)
            {
                bayeuxClient.processMessage(message);
            }

            protected virtual void onFailure(Exception x, IList<IMessage> messages)
            {
                bayeuxClient.onFailure(x, messages);
                bayeuxClient.failMessages(x, messages);
            }
        }

        private class HandshakeTransportListener : PublishTransportListener
        {
            public HandshakeTransportListener(BayeuxClient bayeuxClient)
                : base(bayeuxClient)
            {
            }

            protected override void onFailure(Exception x, IList<IMessage> messages)
            {
                bayeuxClient.updateBayeuxClientState(
                        delegate(BayeuxClientState oldState)
                        {
                            return new RehandshakingState(bayeuxClient, oldState.handshakeFields, oldState.transport, oldState.nextBackoff());
                        });
                base.onFailure(x, messages);
            }

            protected override void processMessage(IMutableMessage message)
            {
                if (Channel_Fields.META_HANDSHAKE.Equals(message.Channel))
                    bayeuxClient.processHandshake(message);
                else
                    base.processMessage(message);
            }
        }

        private class ConnectTransportListener : PublishTransportListener
        {
            public ConnectTransportListener(BayeuxClient bayeuxClient)
                : base(bayeuxClient)
            {
            }

            protected override void onFailure(Exception x, IList<IMessage> messages)
            {
                bayeuxClient.updateBayeuxClientState(
                        delegate(BayeuxClientState oldState)
                        {
                            return new UnconnectedState(bayeuxClient, oldState.handshakeFields, oldState.advice, oldState.transport, oldState.clientId, oldState.nextBackoff());
                        });
                base.onFailure(x, messages);
            }

            protected override void processMessage(IMutableMessage message)
            {
                if (Channel_Fields.META_CONNECT.Equals(message.Channel))
                    bayeuxClient.processConnect(message);
                else
                    base.processMessage(message);
            }
        }

        private class DisconnectTransportListener : PublishTransportListener
        {
            public DisconnectTransportListener(BayeuxClient bayeuxClient)
                : base(bayeuxClient)
            {
            }

            protected override void onFailure(Exception x, IList<IMessage> messages)
            {
                bayeuxClient.updateBayeuxClientState(
                        delegate(BayeuxClientState oldState)
                        {
                            return new DisconnectedState(bayeuxClient, oldState.transport);
                        });
                base.onFailure(x, messages);
            }

            protected override void processMessage(IMutableMessage message)
            {
                if (Channel_Fields.META_DISCONNECT.Equals(message.Channel))
                    bayeuxClient.processDisconnect(message);
                else
                    base.processMessage(message);
            }
        }

        public class BayeuxClientChannel : AbstractSessionChannel
        {
            protected BayeuxClient bayeuxClient;

            public BayeuxClientChannel(BayeuxClient bayeuxClient, ChannelId channelId)
                : base(channelId)
            {
                this.bayeuxClient = bayeuxClient;
            }

            public override IClientSession Session
            {
                get
                {
                    return this as IClientSession;
                }
            }


            protected override void sendSubscribe()
            {
                IMutableMessage message = bayeuxClient.newMessage();
                message.Channel = Channel_Fields.META_SUBSCRIBE;
                message[Message_Fields.SUBSCRIPTION_FIELD] = Id;
                bayeuxClient.enqueueSend(message);
            }

            protected override void sendUnSubscribe()
            {
                IMutableMessage message = bayeuxClient.newMessage();
                message.Channel = Channel_Fields.META_UNSUBSCRIBE;
                message[Message_Fields.SUBSCRIPTION_FIELD] = Id;
                bayeuxClient.enqueueSend(message);
            }

            public override void publish(Object data)
            {
                publish(data, null);
            }

            public override void publish(Object data, String messageId)
            {
                IMutableMessage message = bayeuxClient.newMessage();
                message.Channel = Id;
                message.Data = data;
                if (messageId != null)
                    message.Id = messageId;
                bayeuxClient.enqueueSend(message);
            }
        }

        private delegate BayeuxClientState BayeuxClientStateUpdater_createDelegate(BayeuxClientState oldState);
        private delegate void BayeuxClientStateUpdater_postCreateDelegate();

        abstract public class BayeuxClientState
        {
            public State type;
            public IDictionary<String, Object> handshakeFields;
            public IDictionary<String, Object> advice;
            public ClientTransport transport;
            public String clientId;
            public long backoff;
            protected BayeuxClient bayeuxClient;

            public BayeuxClientState(BayeuxClient bayeuxClient, State type, IDictionary<String, Object> handshakeFields,
                    IDictionary<String, Object> advice, ClientTransport transport, String clientId, long backoff)
            {
                this.bayeuxClient = bayeuxClient;
                this.type = type;
                this.handshakeFields = handshakeFields;
                this.advice = advice;
                this.transport = transport;
                this.clientId = clientId;
                this.backoff = backoff;
            }

            public long Interval
            {
                get
                {
                    long result = 0;
                    if (advice != null && advice.ContainsKey(Message_Fields.INTERVAL_FIELD))
                        result = ObjectConverter.ToInt64(advice[Message_Fields.INTERVAL_FIELD], result);

                    return result;
                }
            }

            public void send(ITransportListener listener, IMutableMessage message)
            {
                IList<IMutableMessage> messages = new List<IMutableMessage>();
                messages.Add(message);
                send(listener, messages);
            }

            public void send(ITransportListener listener, IList<IMutableMessage> messages)
            {
                foreach (IMutableMessage message in messages)
                {
                    if (message.Id == null)
                        message.Id = bayeuxClient.newMessageId();
                    if (clientId != null)
                        message.ClientId = clientId;

                    if (!bayeuxClient.extendSend(message))
                        messages.Remove(message);
                }
                if (messages.Count > 0)
                {
                    transport.send(listener, messages);
                }
            }

            public long nextBackoff()
            {
                return Math.Min(backoff + bayeuxClient.BackoffIncrement, bayeuxClient.MaxBackoff);
            }

            public abstract bool isUpdateableTo(BayeuxClientState newState);

            public virtual void enter(State oldState)
            {
            }

            public abstract void execute();

            public State Type
            {
                get
                {
                    return type;
                }
            }

            public override String ToString()
            {
                return type.ToString();
            }
        }

        private class DisconnectedState : BayeuxClientState
        {
            public DisconnectedState(BayeuxClient bayeuxClient, ClientTransport transport)
                : base(bayeuxClient, State.DISCONNECTED, null, null, transport, null, 0)
            {
            }

            public override bool isUpdateableTo(BayeuxClientState newState)
            {
                return newState.type == State.HANDSHAKING;
            }

            public override void execute()
            {
                transport.reset();
                bayeuxClient.terminate();
            }
        }

        private class AbortedState : DisconnectedState
        {
            public AbortedState(BayeuxClient bayeuxClient, ClientTransport transport)
                : base(bayeuxClient, transport)
            {
            }

            public override void execute()
            {
                transport.abort();
                base.execute();
            }
        }

        private class HandshakingState : BayeuxClientState
        {
            public HandshakingState(BayeuxClient bayeuxClient, IDictionary<String, Object> handshakeFields, ClientTransport transport)
                : base(bayeuxClient, State.HANDSHAKING, handshakeFields, null, transport, null, 0)
            {
            }

            public override bool isUpdateableTo(BayeuxClientState newState)
            {
                return newState.type == State.REHANDSHAKING ||
                    newState.type == State.CONNECTING ||
                    newState.type == State.DISCONNECTED;
            }

            public override void enter(State oldState)
            {
                // Always reset the subscriptions when a handshake has been requested.
                bayeuxClient.resetSubscriptions();
            }

            public override void execute()
            {
                // The state could change between now and when sendHandshake() runs;
                // in this case the handshake message will not be sent and will not
                // be failed, because most probably the client has been disconnected.
                bayeuxClient.sendHandshake();
            }
        }

        private class RehandshakingState : BayeuxClientState
        {
            public RehandshakingState(BayeuxClient bayeuxClient, IDictionary<String, Object> handshakeFields, ClientTransport transport, long backoff)
                : base(bayeuxClient, State.REHANDSHAKING, handshakeFields, null, transport, null, backoff)
            {
            }

            public override bool isUpdateableTo(BayeuxClientState newState)
            {
                return newState.type == State.CONNECTING ||
                    newState.type == State.REHANDSHAKING ||
                    newState.type == State.DISCONNECTED;
            }

            public override void enter(State oldState)
            {
                // Reset the subscriptions if this is not a failure from a requested handshake.
                // Subscriptions may be queued after requested handshakes.
                if (oldState != State.HANDSHAKING)
                {
                    // Reset subscriptions if not queued after initial handshake
                    bayeuxClient.resetSubscriptions();
                }
            }

            public override void execute()
            {
                bayeuxClient.scheduleHandshake(Interval, backoff);
            }
        }

        private class ConnectingState : BayeuxClientState
        {
            public ConnectingState(BayeuxClient bayeuxClient, IDictionary<String, Object> handshakeFields, IDictionary<String, Object> advice, ClientTransport transport, String clientId)
                : base(bayeuxClient, State.CONNECTING, handshakeFields, advice, transport, clientId, 0)
            {
            }

            public override bool isUpdateableTo(BayeuxClientState newState)
            {
                return newState.type == State.CONNECTED ||
                    newState.type == State.UNCONNECTED ||
                    newState.type == State.REHANDSHAKING ||
                    newState.type == State.DISCONNECTING ||
                    newState.type == State.DISCONNECTED;
            }

            public override void execute()
            {
                // Send the messages that may have queued up before the handshake completed
                bayeuxClient.sendBatch();
                bayeuxClient.scheduleConnect(Interval, backoff);
            }
        }

        private class ConnectedState : BayeuxClientState
        {
            public ConnectedState(BayeuxClient bayeuxClient, IDictionary<String, Object> handshakeFields, IDictionary<String, Object> advice, ClientTransport transport, String clientId)
                : base(bayeuxClient, State.CONNECTED, handshakeFields, advice, transport, clientId, 0)
            {
            }

            public override bool isUpdateableTo(BayeuxClientState newState)
            {
                return newState.type == State.CONNECTED ||
                    newState.type == State.UNCONNECTED ||
                    newState.type == State.REHANDSHAKING ||
                    newState.type == State.DISCONNECTING ||
                    newState.type == State.DISCONNECTED;
            }

            public override void execute()
            {
                bayeuxClient.scheduleConnect(Interval, backoff);
            }
        }

        private class UnconnectedState : BayeuxClientState
        {
            public UnconnectedState(BayeuxClient bayeuxClient, IDictionary<String, Object> handshakeFields, IDictionary<String, Object> advice, ClientTransport transport, String clientId, long backoff)
                : base(bayeuxClient, State.UNCONNECTED, handshakeFields, advice, transport, clientId, backoff)
            {
            }

            public override bool isUpdateableTo(BayeuxClientState newState)
            {
                return newState.type == State.CONNECTED ||
                    newState.type == State.UNCONNECTED ||
                    newState.type == State.REHANDSHAKING ||
                    newState.type == State.DISCONNECTED;
            }

            public override void execute()
            {
                bayeuxClient.scheduleConnect(Interval, backoff);
            }
        }

        private class DisconnectingState : BayeuxClientState
        {
            public DisconnectingState(BayeuxClient bayeuxClient, ClientTransport transport, String clientId)
                : base(bayeuxClient, State.DISCONNECTING, null, null, transport, clientId, 0)
            {
            }

            public override bool isUpdateableTo(BayeuxClientState newState)
            {
                return newState.type == State.DISCONNECTED;
            }

            public override void execute()
            {
                IMutableMessage message = bayeuxClient.newMessage();
                message.Channel = Channel_Fields.META_DISCONNECT;
                send(bayeuxClient.disconnectListener, message);
            }
        }
    }
}
