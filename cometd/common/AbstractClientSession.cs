using System;
using System.Collections;
using System.Collections.Generic;

using Cometd.Bayeux;
using Cometd.Bayeux.Client;

namespace Cometd.Common
{
    /// <summary> <p>Partial implementation of {@link ClientSession}.</p>
    /// <p>It handles extensions and batching, and provides utility methods to be used by subclasses.</p>
    /// </summary>
    public abstract class AbstractClientSession : IClientSession
    {
        // @@ax: WARNING Should implement thread safety, as in http://msdn.microsoft.com/en-us/library/3azh197k.aspx
        private List<IExtension> _extensions = new List<IExtension>();
        private Dictionary<String, Object> _attributes = new Dictionary<String, Object>();
        private Dictionary<String, AbstractSessionChannel> _channels = new Dictionary<String, AbstractSessionChannel>();
        private int _batch;
        private int _idGen = 0;

        protected AbstractClientSession()
        {
        }

        protected String newMessageId()
        {
            return Convert.ToString(_idGen++);
        }

        public void addExtension(IExtension extension)
        {
            _extensions.Add(extension);
        }

        public void removeExtension(IExtension extension)
        {
            _extensions.Remove(extension);
        }

        protected bool extendSend(IMutableMessage message)
        {
            if (message.Meta)
            {
                foreach (IExtension extension in _extensions)
                    if (!extension.sendMeta(this, message))
                        return false;
            }
            else
            {
                foreach (IExtension extension in _extensions)
                    if (!extension.send(this, message))
                        return false;
            }
            return true;
        }

        protected bool extendRcv(IMutableMessage message)
        {
            if (message.Meta)
            {
                foreach (IExtension extension in _extensions)
                    if (!extension.rcvMeta(this, message))
                        return false;
            }
            else
            {
                foreach (IExtension extension in _extensions)
                    if (!extension.rcv(this, message))
                        return false;
            }
            return true;
        }

        /* ------------------------------------------------------------ */
        protected abstract ChannelId newChannelId(String channelId);

        /* ------------------------------------------------------------ */
        protected abstract AbstractSessionChannel newChannel(ChannelId channelId);

        /* ------------------------------------------------------------ */
        public IClientSessionChannel getChannel(String channelId)
        {
            AbstractSessionChannel channel;
            _channels.TryGetValue(channelId, out channel);

            if (channel == null)
            {
                ChannelId id = newChannelId(channelId);
                AbstractSessionChannel new_channel = newChannel(id);

                if (_channels.ContainsKey(channelId))
                    channel = _channels[channelId];
                else
                    _channels[channelId] = new_channel;

                if (channel == null)
                    channel = new_channel;
            }
            return channel;
        }

        protected Dictionary<String, AbstractSessionChannel> Channels
        {
            get
            {
                return _channels;
            }
        }

        /* ------------------------------------------------------------ */
        public void startBatch()
        {
            _batch++;
        }

        /* ------------------------------------------------------------ */
        protected abstract void sendBatch();

        /* ------------------------------------------------------------ */
        public bool endBatch()
        {
            if (--_batch == 0)
            {
                sendBatch();
                return true;
            }
            return false;
        }

        /* ------------------------------------------------------------ */
        public void batch(BatchDelegate batch)
        {
            startBatch();
            try
            {
                batch();
            }
            finally
            {
                endBatch();
            }
        }

        protected bool Batching
        {
            get
            {
                return _batch > 0;
            }

        }
        /* ------------------------------------------------------------ */
        public Object getAttribute(String name)
        {
            Object obj;
            _attributes.TryGetValue(name, out obj);
            return obj;
        }

        /* ------------------------------------------------------------ */
        public ICollection<String> AttributeNames
        {
            get
            {
                return _attributes.Keys;
            }
        }

        /* ------------------------------------------------------------ */
        public Object removeAttribute(String name)
        {
            try
            {
                Object old = _attributes[name];
                _attributes.Remove(name);
                return old;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /* ------------------------------------------------------------ */
        public void setAttribute(String name, Object val)
        {
            _attributes[name] = val;
        }

        /* ------------------------------------------------------------ */
        public void resetSubscriptions()
        {
            foreach (KeyValuePair<String, AbstractSessionChannel> channel in _channels)
            {
                channel.Value.resetSubscriptions();
            }
        }

        /* ------------------------------------------------------------ */
        /// <summary> <p>Receives a message (from the server) and process it.</p>
        /// <p>Processing the message involves calling the receive {@link ClientSession.Extension extensions}
        /// and the channel {@link ClientSessionChannel.ClientSessionChannelListener listeners}.</p>
        /// </summary>
        /// <param name="message">the message received.
        /// </param>
        /// <param name="mutable">the mutable version of the message received
        /// </param>
        public void receive(IMutableMessage message)
        {
            String id = message.Channel;
            if (id == null)
            {
                throw new ArgumentException("Bayeux messages must have a channel, " + message);
            }

            if (!extendRcv(message))
                return;

            AbstractSessionChannel channel = (AbstractSessionChannel)getChannel(id);
            ChannelId channelId = channel.ChannelId;

            channel.notifyMessageListeners(message);

            foreach (String channelPattern in channelId.Wilds)
            {
                ChannelId channelIdPattern = newChannelId(channelPattern);
                if (channelIdPattern.matches(channelId))
                {
                    AbstractSessionChannel wildChannel = (AbstractSessionChannel)getChannel(channelPattern);
                    wildChannel.notifyMessageListeners(message);
                }
            }
        }

        public abstract void handshake(IDictionary<String, Object> template);
        public abstract void handshake();
        public abstract void disconnect();
        public abstract bool Handshook { get; }
        public abstract String Id { get; }
        public abstract bool Connected { get; }

        /// <summary> <p>A channel scoped to a {@link ClientSession}.</p></summary>
        public abstract class AbstractSessionChannel : IClientSessionChannel
        {
            //protected Logger logger = Log.getLogger(GetType().FullName);
            private ChannelId _id;
            private Dictionary<String, Object> _attributes = new Dictionary<String, Object>();
            private List<IMessageListener> _subscriptions = new List<IMessageListener>();
            private int _subscriptionCount = 0;
            private List<IClientSessionChannelListener> _listeners = new List<IClientSessionChannelListener>();

            /* ------------------------------------------------------------ */
            public AbstractSessionChannel(ChannelId id)
            {
                _id = id;
            }

            /* ------------------------------------------------------------ */
            public ChannelId ChannelId
            {
                get
                {
                    return _id;
                }
            }

            /* ------------------------------------------------------------ */
            public void addListener(IClientSessionChannelListener listener)
            {
                _listeners.Add(listener);
            }

            /* ------------------------------------------------------------ */
            public void removeListener(IClientSessionChannelListener listener)
            {
                _listeners.Remove(listener);
            }

            /* ------------------------------------------------------------ */
            protected abstract void sendSubscribe();

            /* ------------------------------------------------------------ */
            protected abstract void sendUnSubscribe();

            /* ------------------------------------------------------------ */
            public void subscribe(IMessageListener listener)
            {
                _subscriptions.Add(listener);

                _subscriptionCount++;
                int count = _subscriptionCount;
                if (count == 1)
                    sendSubscribe();
            }

            /* ------------------------------------------------------------ */
            public void unsubscribe(IMessageListener listener)
            {
                _subscriptions.Remove(listener);

                _subscriptionCount--;
                if (_subscriptionCount < 0) _subscriptionCount = 0;
                int count = _subscriptionCount;
                if (count == 0)
                    sendUnSubscribe();
            }

            /* ------------------------------------------------------------ */
            public void unsubscribe()
            {
                foreach (IMessageListener listener in new List<IMessageListener>(_subscriptions))
                    unsubscribe(listener);
            }

            /* ------------------------------------------------------------ */
            public void resetSubscriptions()
            {
                foreach (IMessageListener listener in new List<IMessageListener>(_subscriptions))
                {
                    _subscriptions.Remove(listener);
                    _subscriptionCount--;
                }
            }

            /* ------------------------------------------------------------ */
            public String Id
            {
                get
                {
                    return _id.ToString();
                }
            }

            /* ------------------------------------------------------------ */
            public bool DeepWild
            {
                get
                {
                    return _id.DeepWild;
                }
            }

            /* ------------------------------------------------------------ */
            public bool Meta
            {
                get
                {
                    return _id.isMeta();
                }
            }

            /* ------------------------------------------------------------ */
            public bool Service
            {
                get
                {
                    return _id.isService();
                }
            }

            /* ------------------------------------------------------------ */
            public bool Wild
            {
                get
                {
                    return _id.Wild;
                }
            }

            public void notifyMessageListeners(IMessage message)
            {
                foreach (IClientSessionChannelListener listener in _listeners)
                {
                    if (listener is IMessageListener)
                    {
                        try
                        {
                            ((IMessageListener)listener).onMessage(this, message);
                        }
                        catch (Exception x)
                        {
                            Console.WriteLine("{0}", x);
                            //logger.info(x);
                        }
                    }
                }

                var list = new List<IMessageListener>(_subscriptions);
                foreach (IClientSessionChannelListener listener in list)
                {
                    if (listener is IMessageListener)
                    {
                        if (message.Data != null)
                        {
                            try
                            {
                                ((IMessageListener)listener).onMessage(this, message);
                            }
                            catch (System.Exception x)
                            {
                                Console.WriteLine("{0}", x);
                                //logger.info(x);
                            }
                        }
                    }
                }
            }

            public void setAttribute(String name, Object val)
            {
                _attributes[name] = val;
            }

            public Object getAttribute(String name)
            {
                Object obj;
                _attributes.TryGetValue(name, out obj);
                return obj;
            }

            public ICollection<String> AttributeNames
            {
                get
                {
                    return _attributes.Keys;
                }
            }

            public Object removeAttribute(String name)
            {
                Object old = getAttribute(name);
                _attributes.Remove(name);
                return old;
            }

            public abstract IClientSession Session { get; }

            /* ------------------------------------------------------------ */
            public override String ToString()
            {
                return _id.ToString();
            }

            public abstract void publish(Object param1);
            public abstract void publish(Object param1, String param2);
        }
    }
}
