using System;
using System.Collections.Generic;

namespace Cometd.Bayeux
{
	/// <summary> <p>The Bayeux protocol exchange information by means of messages.</p>
	/// <p>This interface represents the API of a Bayeux message, and consists
	/// mainly of convenience methods to access the known fields of the message map.</p>
	/// <p>This interface comes in both an immutable and {@link Mutable mutable} versions.<br/>
	/// Mutability may be deeply enforced by an implementation, so that it is not correct
	/// to cast a passed Message, to a Message.Mutable, even if the implementation
	/// allows this.</p>
	/// 
	/// </summary>
	/// <version>  $Revision: 1483 $ $Date: 2009-03-04 14:56:47 +0100 (Wed, 04 Mar 2009) $
	/// </version>
	public class Message_Fields
	{
		public const String CLIENT_ID_FIELD = "clientId";
		public const String DATA_FIELD = "data";
		public const String CHANNEL_FIELD = "channel";
		public const String ID_FIELD = "id";
		public const String ERROR_FIELD = "error";
		public const String TIMESTAMP_FIELD = "timestamp";
		public const String TRANSPORT_FIELD = "transport";
		public const String ADVICE_FIELD = "advice";
		public const String SUCCESSFUL_FIELD = "successful";
		public const String SUBSCRIPTION_FIELD = "subscription";
		public const String EXT_FIELD = "ext";
		public const String CONNECTION_TYPE_FIELD = "connectionType";
		public const String VERSION_FIELD = "version";
		public const String MIN_VERSION_FIELD = "minimumVersion";
		public const String SUPPORTED_CONNECTION_TYPES_FIELD = "supportedConnectionTypes";
		public const String RECONNECT_FIELD = "reconnect";
		public const String INTERVAL_FIELD = "interval";
		public const String RECONNECT_RETRY_VALUE = "retry";
		public const String RECONNECT_HANDSHAKE_VALUE = "handshake";
		public const String RECONNECT_NONE_VALUE = "none";
	}

	public interface IMessage : IDictionary<String, Object>
	{
		/// <summary> Convenience method to retrieve the {@link #ADVICE_FIELD}</summary>
		/// <returns> the advice of the message
		/// </returns>
		IDictionary<String, Object> Advice { get; }

		/// <summary> Convenience method to retrieve the {@link #CHANNEL_FIELD}.
		/// Bayeux message always have a non null channel.
		/// </summary>
		/// <returns> the channel of the message
		/// </returns>
		String Channel { get; }

		/// <summary> Convenience method to retrieve the {@link #CHANNEL_FIELD}.
		/// Bayeux message always have a non null channel.
		/// </summary>
		/// <returns> the channel of the message
		/// </returns>
		ChannelId ChannelId { get; }

		/// <summary> Convenience method to retrieve the {@link #CLIENT_ID_FIELD}</summary>
		/// <returns> the client id of the message
		/// </returns>
		String ClientId { get; }

		/// <summary> Convenience method to retrieve the {@link #DATA_FIELD}</summary>
		/// <returns> the data of the message
		/// </returns>
		/// <seealso cref="getDataAsMap()">
		/// </seealso>
		Object Data { get; }

		/// <summary> A messages that has a meta channel is dubbed a "meta message".</summary>
		/// <returns> whether the channel's message is a meta channel
		/// </returns>
		bool Meta { get; }

		/// <summary> Convenience method to retrieve the {@link #SUCCESSFUL_FIELD}</summary>
		/// <returns> whether the message is successful
		/// </returns>
		bool Successful { get; }

		/// <returns> the data of the message as a map
		/// </returns>
		/// <seealso cref="getData()">
		/// </seealso>
		IDictionary<String, Object> DataAsDictionary { get; }

		/// <summary> Convenience method to retrieve the {@link #EXT_FIELD}</summary>
		/// <returns> the ext of the message
		/// </returns>
		IDictionary<String, Object> Ext { get; }

		/// <summary> Convenience method to retrieve the {@link #ID_FIELD}</summary>
		/// <returns> the id of the message
		/// </returns>
		String Id { get; }

		/// <returns> this message as a JSON string
		/// </returns>
		String JSON { get; }
	}


	/// <summary> The mutable version of a {@link Message}</summary>
	public interface IMutableMessage : IMessage
	{
		/// <summary> Convenience method to retrieve the {@link #ADVICE_FIELD} and create it if it does not exist</summary>
		/// <param name="create">whether to create the advice field if it does not exist
		/// </param>
		/// <returns> the advice of the message
		/// </returns>
		IDictionary<String, Object> getAdvice(bool create);

		/// <summary> Convenience method to retrieve the {@link #DATA_FIELD} and create it if it does not exist</summary>
		/// <param name="create">whether to create the data field if it does not exist
		/// </param>
		/// <returns> the data of the message
		/// </returns>
		IDictionary<String, Object> getDataAsDictionary(bool create);

		/// <summary> Convenience method to retrieve the {@link #EXT_FIELD} and create it if it does not exist</summary>
		/// <param name="create">whether to create the ext field if it does not exist
		/// </param>
		/// <returns> the ext of the message
		/// </returns>
		IDictionary<String, Object> getExt(bool create);

		/// <param name="channel">the channel of this message
		/// </param>
		new String Channel { get; set; }

		/// <param name="clientId">the client id of this message
		/// </param>
		new String ClientId { get; set; }

		/// <param name="data">the data of this message
		/// </param>
		new Object Data { get; set; }

		/// <param name="id">the id of this message
		/// </param>
		new String Id { get; set; }

		/// <param name="successful">the successfulness of this message
		/// </param>
		new bool Successful { get; set; }
	}
}
