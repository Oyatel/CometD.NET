using System;
using System.Collections.Generic;
using Cometd.Bayeux;
using Cometd.Common;

namespace Cometd.Client.Transport
{
	/// <version>  $Revision: 902 $ $Date: 2010-10-19 12:35:37 +0200 (Tue, 19 Oct 2010) $
	/// </version>
	public abstract class ClientTransport: AbstractTransport
	{
		public const String TIMEOUT_OPTION = "timeout";
		public const String INTERVAL_OPTION = "interval";
		public const String MAX_NETWORK_DELAY_OPTION = "maxNetworkDelay";

		protected internal long _timeout = -1;
		protected internal long _interval = -1;
		protected internal long _maxNetworkDelay = 10000;

		/* ------------------------------------------------------------ */
        public ClientTransport(String name, IDictionary<String, Object> options)
            : base(name, options)
		{
			setOption(TIMEOUT_OPTION, _timeout);
			setOption(INTERVAL_OPTION, _interval);
			setOption(MAX_NETWORK_DELAY_OPTION, _maxNetworkDelay);
		}
				
		
		public virtual void init()
		{
			_timeout = getOption(TIMEOUT_OPTION, _timeout);
			_interval = getOption(INTERVAL_OPTION, _interval);
			_maxNetworkDelay = getOption(MAX_NETWORK_DELAY_OPTION, _maxNetworkDelay);
		}
		
		public abstract void abort();
		
		/* ------------------------------------------------------------ */
		public abstract void reset();
		
		/* ------------------------------------------------------------ */
		public abstract bool accept(String version);
		
		/* ------------------------------------------------------------ */
        public abstract void send(ITransportListener listener, IList<IMutableMessage> messages);
	}
}