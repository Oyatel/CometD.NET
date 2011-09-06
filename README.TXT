CometD.NET is a project to provide an implementation of the Bayeux protocol in .NET. The client code is ported based on the source code from the java client found at http://cometd.org/. You can also visit that page if you are looking for a CometD server.


This is the CometD.NET project home.

cometd/bayeux           - The Bayeux Specification
cometd/client 		- The .NET client library
cometd/common		- Classes from the java cometd common directory

RUNNING COMETD CLIENT

The CometD.NET client won't work without a bayeux/cometd server to connect to. If you have installed the cometd server from cometd.org, you should be able to connect to it.

USAGE EXAMPLES

Usage is basically the same as http://cometd.org/documentation/cometd-java/client . You can also look at https://github.com/Oyatel/oyatel-api-examples/blob/master/csharp/Oyatel.Connect.Tutorial/StreamingEvents.cs to see how we use it to communicate with our own bayeux-server.

Typical usage:

        class Listener : IMessageListener
        {
                public void onMessage(IClientSessionChannel channel, IMessage message)
                {
                    // Handle the message
                }
        }

        static void Main(string[] args)
        {
            // Handshake
            String url = "http://localhost:8080/cometd";
            BayeuxClient client = new BayeuxClient(url, new List<ClientTransport>() { new LongPollingTransport(null) });
            
	    client.handshake();
	    client.waitFor(1000, new List<BayeuxClient.State>() { BayeuxClient.State.CONNECTED });

            // Subscription to channels
            IClientSessionChannel channel = client.getChannel("/foo");
            channel.subscribe( new Listener() );

            // Publishing to channels
            Dictionary<String, Object> data = new Dictionary<String, Object>();
            data.Add("bar", "baz");
            channel.publish(data);

            // Disconnecting
            client.disconnect();
	    client.waitFor(1000, new List<BayeuxClient.State>() { BayeuxClient.State.DISCONNECTED });
        }

