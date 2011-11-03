using System;
using System.Collections.Generic;
using Cometd.Bayeux;
using Cometd.Bayeux.Client;
using Cometd.Common;

namespace Cometd.Client.Ext
{
    /// <summary> AckExtension
    /// 
    /// This client-side extension enables the client to acknowledge to the server
    /// the messages that the client has received.
    /// For the acknowledgement to work, the server must be configured with the
    /// correspondent server-side ack extension. If both client and server support
    /// the ack extension, then the ack functionality will take place automatically.
    /// By enabling this extension, all messages arriving from the server will arrive
    /// via the long poll, so the comet communication will be slightly chattier.
    /// The fact that all messages will return via long poll means also that the
    /// messages will arrive with total order, which is not guaranteed if messages
    /// can arrive via both long poll and normal response.
    /// Messages are not acknowledged one by one, but instead a group of messages is
    /// acknowledged when long poll returns.
    /// 
    /// </summary>
    /// <author>  dyu
    /// </author>

    public class AckExtension : IExtension
    {
        public const String EXT_FIELD = "ack";

        private volatile bool _serverSupportsAcks = false;
        private volatile int _ackId = -1;

        public bool rcv(IClientSession session, IMutableMessage message)
        {
            return true;
        }

        public bool rcvMeta(IClientSession session, IMutableMessage message)
        {
            if (Channel_Fields.META_HANDSHAKE.Equals(message.Channel))
            {
                Dictionary<String, Object> ext = (Dictionary<String, Object>)message.getExt(false);
                _serverSupportsAcks = ext != null && true.Equals(ext[EXT_FIELD]);
            }
            else if (_serverSupportsAcks && true.Equals(message[Message_Fields.SUCCESSFUL_FIELD]) && Channel_Fields.META_CONNECT.Equals(message.Channel))
            {
                Dictionary<String, Object> ext = (Dictionary<String, Object>)message.getExt(false);
                if (ext != null)
                {
                    Object ack;
                    ext.TryGetValue(EXT_FIELD, out ack);
                    _ackId = ObjectConverter.ToInt32(ack, _ackId);
                }
            }

            return true;
        }

        public bool send(IClientSession session, IMutableMessage message)
        {
            return true;
        }

        public bool sendMeta(IClientSession session, IMutableMessage message)
        {
            if (Channel_Fields.META_HANDSHAKE.Equals(message.Channel))
            {
                message.getExt(true)[EXT_FIELD] = true;
                _ackId = -1;
            }
            else if (_serverSupportsAcks && Channel_Fields.META_CONNECT.Equals(message.Channel))
            {
                message.getExt(true)[EXT_FIELD] = _ackId;
            }

            return true;
        }
    }
}
