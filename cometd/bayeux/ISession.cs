using System;
using System.Collections.Generic;


namespace Cometd.Bayeux
{
	public delegate void BatchDelegate();

	/// <summary> <p>A Bayeux session represents a connection between a bayeux client and a bayeux server.</p>
	/// <p>This interface is the common base interface for both the server side and the client side
	/// representations of a session:</p>
	/// <ul>
	/// <li>if the remote client is not a Java client, then only a {@link org.cometd.bayeux.server.ServerSession}
	/// instance will exist on the server and represents the remote client.</li>
	/// <li>if the remote client is a Java client, then a {@link org.cometd.bayeux.client.ClientSession}
	/// instance will exist on the client and a {@link org.cometd.bayeux.server.ServerSession}
	/// instance will exist on the server, linked by the same clientId.</li>
	/// <li>if the client is a Java client, but it is located in the server, then the
	/// {@link org.cometd.bayeux.client.ClientSession} instance will be an instance
	/// of {@link org.cometd.bayeux.server.LocalSession} and will be associated
	/// with a {@link org.cometd.bayeux.server.ServerSession} instance.</li>
	/// </ul>
	/// 
	/// </summary>
	/// <version>  $Revision: 1483 $ $Date: 2009-03-04 14:56:47 +0100 (Wed, 04 Mar 2009) $
	/// </version>
	public interface ISession
	{
		/// <summary> <p>The clientId of the session.</p>
		/// <p>This would more correctly be called a "sessionId", but for
		/// backwards compatibility with the Bayeux protocol, it is a field called "clientId"
		/// that identifies a session.
		/// </summary>
		/// <returns> the id of this session
		/// </returns>
		String Id { get; }

		/// <summary> <p>A connected session is a session where the link between the client and the server
		/// has been established.</p>
		/// </summary>
		/// <returns> whether the session is connected
		/// </returns>
		/// <seealso cref="disconnect()">
		/// </seealso>
		bool Connected { get; }

		/// <summary> <p>A handshook session is a session where the handshake has successfully completed></summary>
		/// <returns> whether the session is handshook
		/// </returns>
		bool Handshook { get; }

		/// <summary> Disconnects this session, ending the link between the client and the server peers.</summary>
		/// <seealso cref="isConnected()">
		/// </seealso>
		void disconnect();

		/// <summary> <p>Sets a named session attribute value.</p>
		/// <p>Session attributes are convenience data that allows arbitrary
		/// application data to be associated with a session.</p>
		/// </summary>
		/// <param name="name">the attribute name
		/// </param>
		/// <param name="value">the attribute value
		/// </param>
		void setAttribute(String name, Object val);

		/// <summary> <p>Retrieves the value of named session attribute.</p></summary>
		/// <param name="name">the name of the attribute
		/// </param>
		/// <returns> the attribute value or null if the attribute is not present
		/// </returns>
		Object getAttribute(String name);

		/// <returns> the list of session attribute names.
		/// </returns>
		ICollection<String> AttributeNames { get; }

		/// <summary> <p>Removes a named session attribute.</p></summary>
		/// <param name="name">the name of the attribute
		/// </param>
		/// <returns> the value of the attribute
		/// </returns>
		Object removeAttribute(String name);

		/// <summary> <p>Executes the given command in a batch so that any Bayeux message sent
		/// by the command (via the Bayeux API) is queued up until the end of the
		/// command and then all messages are sent at once.</p>
		/// </summary>
		/// <param name="batch">the Runnable to run as a batch
		/// </param>
		void batch(BatchDelegate batch);

		/// <summary> <p>Starts a batch, to be ended with {@link #endBatch()}.</p>
		/// <p>The {@link #batch(Runnable)} method should be preferred since it automatically
		/// starts and ends a batch without relying on a try/finally block.</p>
		/// <p>This method is to be used in the cases where the use of {@link #batch(Runnable)}
		/// is not possible or would make the code more complex.</p>
		/// </summary>
		/// <seealso cref="endBatch()">
		/// </seealso>
		/// <seealso cref="batch(IRunnable)">
		/// </seealso>
		void startBatch();

		/// <summary> <p>Ends a batch started with {@link #startBatch()}.</p></summary>
		/// <returns> true if the batch ended and there were messages to send.
		/// </returns>
		/// <seealso cref="startBatch()">
		/// </seealso>
		bool endBatch();
	}

}
