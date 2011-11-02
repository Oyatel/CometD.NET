using System;
using System.Collections.Generic;

namespace Cometd.Client.Transport
{
    /// <version>  $Revision$ $Date: 2010-10-01 11:38:19 +0200 (Fri, 01 Oct 2010) $
    /// </version>
    public class TransportRegistry
    {
        private IDictionary<String, ClientTransport> _transports = new Dictionary<String, ClientTransport>();
        private List<String> _allowed = new List<String>();

        public void Add(ClientTransport transport)
        {
            if (transport != null)
            {
                _transports[transport.Name] = transport;
                _allowed.Add(transport.Name);
            }
        }

        public IList<String> KnownTransports
        {
            get
            {
                List<String> newList = new List<String>(_transports.Keys.Count);
                foreach (String key in _transports.Keys)
                {
                    newList.Add(key);
                }
                return newList.AsReadOnly();
            }
        }

        public IList<String> AllowedTransports
        {
            get
            {
                return _allowed.AsReadOnly();
            }
        }

        public IList<ClientTransport> Negotiate(IList<Object> requestedTransports, String bayeuxVersion)
        {
            List<ClientTransport> list = new List<ClientTransport>();

            foreach (String transportName in _allowed)
            {
                foreach (Object requestedTransportName in requestedTransports)
                {
                    if (requestedTransportName.Equals(transportName))
                    {
                        ClientTransport transport = getTransport(transportName);
                        if (transport.accept(bayeuxVersion))
                        {
                            list.Add(transport);
                        }
                    }
                }
            }
            return list;
        }

        public ClientTransport getTransport(String transport)
        {
            ClientTransport obj;
            _transports.TryGetValue(transport, out obj);
            return obj;
        }
    }
}
