using System;
using System.Collections.Generic;

namespace Cometd.Bayeux
{

	/// <summary> <p>The {@link Bayeux} interface is the common API for both client-side and
	/// server-side configuration and usage of the Bayeux object.</p>
	/// <p>The {@link Bayeux} object handles configuration options and a set of
	/// transports that is negotiated with the server.</p>
	/// </summary>
	/// <seealso cref="ITransport">
	/// </seealso>
	public interface IBayeux
	{
		/// <returns> the set of known transport names of this {@link Bayeux} object.
		/// </returns>
		/// <seealso cref="getAllowedTransports()">
		/// </seealso>
		ICollection<String> KnownTransportNames { get; }

		/// <param name="transport">the transport name
		/// </param>
		/// <returns> the transport with the given name or null
		/// if no such transport exist
		/// </returns>
		ITransport getTransport(String transport);

		/// <returns> the ordered list of transport names that will be used in the
		/// negotiation of transports with the other peer.
		/// </returns>
		/// <seealso cref="getKnownTransportNames()">
		/// </seealso>
		IList<String> AllowedTransports { get; }

		/// <param name="qualifiedName">the configuration option name
		/// </param>
		/// <returns> the configuration option with the given {@code qualifiedName}
		/// </returns>
		/// <seealso cref="setOption(String, Object)">
		/// </seealso>
		/// <seealso cref="getOptionNames()">
		/// </seealso>
		Object getOption(String qualifiedName);

		/// <param name="qualifiedName">the configuration option name
		/// </param>
		/// <param name="value">the configuration option value
		/// </param>
		/// <seealso cref="getOption(String)">
		/// </seealso>
		void setOption(String qualifiedName, Object value);

		/// <returns> the set of configuration options
		/// </returns>
		/// <seealso cref="getOption(String)">
		/// </seealso>
		ICollection<String> OptionNames { get; }
	}

	/// <summary> <p>The common base interface for Bayeux listeners.</p>
	/// <p>Specific sub-interfaces define what kind of events listeners will be notified.</p>
	/// </summary>
	public interface IBayeuxListener
	{
	}
}
