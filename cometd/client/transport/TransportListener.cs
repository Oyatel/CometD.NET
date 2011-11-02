using System;
using System.Collections.Generic;
using Cometd.Bayeux;

namespace Cometd.Client.Transport
{
    /// <version>  $Revision: 902 $ $Date: 2010-10-01 22:45:07 +0200 (Fri, 01 Oct 2010) $
    /// </version>
    public interface ITransportListener
    {
        void onSending(IList<IMessage> messages);

        void onMessages(IList<IMutableMessage> messages);

        void onConnectException(Exception x, IList<IMessage> messages);

        void onException(Exception x, IList<IMessage> messages);

        void onExpire(IList<IMessage> messages);

        void onProtocolError(String info, IList<IMessage> messages);
    }
}
