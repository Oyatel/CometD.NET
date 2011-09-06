using System;
using System.Collections.Generic;
using Cometd.Bayeux;


namespace Cometd.Bayeux.Client
{
	/// <summary> <p>This interface represents the client side Bayeux session.</p>
	/// <p>In addition to the {@link Session common Bayeux session}, this
	/// interface provides method to configure extension, access channels
	/// and to initiate the communication with a Bayeux server(s).</p>
	/// 
	/// </summary>
	/// <version>  $Revision: 1483 $ $Date: 2009-03-04 14:56:47 +0100 (Wed, 04 Mar 2009) $
	/// </version>
	public interface IClientSession: ISession
	{
		/// <summary> Adds an extension to this session.</summary>
		/// <param name="extension">the extension to add
		/// </param>
		/// <seealso cref="removeExtension(IExtension)">
		/// </seealso>
		void addExtension(IExtension extension);

		/// <summary> Removes an extension from this session.</summary>
		/// <param name="extension">the extension to remove
		/// </param>
		/// <seealso cref="addExtension(IExtension)">
		/// </seealso>
		void removeExtension(IExtension extension);

		/// <summary> <p>Equivalent to {@link #handshake(Map) handshake(null)}.</p></summary>
		void handshake();

		/// <summary> <p>Initiates the bayeux protocol handshake with the server(s).</p>
		/// <p>The handshake initiated by this method is asynchronous and
		/// does not wait for the handshake response.</p>
		/// 
		/// </summary>
		/// <param name="template">additional fields to add to the handshake message.
		/// </param>
		void handshake(IDictionary<String, Object> template);

		/// <summary> <p>Returns a client side channel scoped by this session.</p>
		/// <p>The channel name may be for a specific channel (e.g. "/foo/bar")
		/// or for a wild channel (e.g. "/meta/**" or "/foo/*").</p>
		/// <p>This method will always return a channel, even if the
		/// the channel has not been created on the server side.  The server
		/// side channel is only involved once a publish or subscribe method
		/// is called on the channel returned by this method.</p>
		/// <p>Typical usage examples are:</p>
		/// <pre>
		/// clientSession.getChannel("/foo/bar").subscribe(mySubscriptionListener);
		/// clientSession.getChannel("/foo/bar").publish("Hello");
		/// clientSession.getChannel("/meta/*").addListener(myMetaChannelListener);
		/// </pre>
		/// </summary>
		/// <param name="channelName">specific or wild channel name.
		/// </param>
		/// <returns> a channel scoped by this session.
		/// </returns>
		IClientSessionChannel getChannel(String channelName);
	}


	/// <summary> <p>Extension API for client session.</p>
	/// <p>An extension allows user code to interact with the Bayeux protocol as late
	/// as messages are sent or as soon as messages are received.</p>
	/// <p>Messages may be modified, or state held, so that the extension adds a
	/// specific behavior simply by observing the flow of Bayeux messages.</p>
	/// 
	/// </summary>
	/// <seealso cref="IClientSession.addExtension(IExtension)">
	/// </seealso>
	public interface IExtension
	{
		/// <summary> Callback method invoked every time a normal message is received.</summary>
		/// <param name="session">the session object that is receiving the message
		/// </param>
		/// <param name="message">the message received
		/// </param>
		/// <returns> true if message processing should continue, false if it should stop
		/// </returns>
		bool rcv(IClientSession session, IMutableMessage message);

		/// <summary> Callback method invoked every time a meta message is received.</summary>
		/// <param name="session">the session object that is receiving the meta message
		/// </param>
		/// <param name="message">the meta message received
		/// </param>
		/// <returns> true if message processing should continue, false if it should stop
		/// </returns>
		bool rcvMeta(IClientSession session, IMutableMessage message);

		/// <summary> Callback method invoked every time a normal message is being sent.</summary>
		/// <param name="session">the session object that is sending the message
		/// </param>
		/// <param name="message">the message being sent
		/// </param>
		/// <returns> true if message processing should continue, false if it should stop
		/// </returns>
		bool send(IClientSession session, IMutableMessage message);

		/// <summary> Callback method invoked every time a meta message is being sent.</summary>
		/// <param name="session">the session object that is sending the message
		/// </param>
		/// <param name="message">the meta message being sent
		/// </param>
		/// <returns> true if message processing should continue, false if it should stop
		/// </returns>
		bool sendMeta(IClientSession session, IMutableMessage message);
	}
}
