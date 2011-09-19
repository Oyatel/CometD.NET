CometD.NET is a project to provide an implementation of the Bayeux protocol in .NET. The client code is ported based on the source code from the java client found at http://cometd.org/. You can also visit that page if you are looking for a CometD server.


This is the CometD.NET project home.

cometd/bayeux           - The Bayeux Specification
cometd/client 		- The .NET client library
cometd/common		- Classes from the java cometd common directory

RUNNING COMETD CLIENT

The CometD.NET client won't work without a bayeux/cometd server to connect to. If you have installed the cometd server from cometd.org, you should be able to connect to it.

DEPENDENCIES

The CometD.NET client library is set to compile with .NET Framework 3.5, and does not support .NET Framework Client Profile. If you get an error about a dependency on "System.Web.Extensions", you should go to your project properties and change to a framework without Client Profile.

USAGE

Usage is basically the same as in the java-client (http://cometd.org/documentation/cometd-java/client). HakanL have made a simple command-line tool which utilizes the basic functionality (http://github.com/HakanL/Auto-Deployment). You can also look at https://github.com/Oyatel/oyatel-api-examples/blob/master/csharp/Oyatel.Connect.Tutorial/StreamingEvents.cs to see how we use it to communicate with our own bayeux-server.

BASIC USAGE EXAMPLE

using Cometd.Client;
using Cometd.Client.Transport;
using Cometd.Bayeux;
using Cometd.Bayeux.Client;
using Cometd.Common;

...
        class Listener : IMessageListener
        {
                public void onMessage(IClientSessionChannel channel, IMessage message)
                {
                    // Handle the message
		    Console.WriteLine(message);
                }
        }

        static void Main(string[] args)
        {
            // Handshake
            String url = "http://localhost:8080/cometd ";
            BayeuxClient client = new BayeuxClient(url, new List<ClientTransport>() { new LongPollingTransport(null) });
            
	    client.handshake();
	    client.waitFor(1000, new List<BayeuxClient.State>() { BayeuxClient.State.CONNECTED });

            // Subscription to channels
            IClientSessionChannel channel = client.getChannel("/service/echo");
            channel.subscribe( new Listener() );

            // Publishing to channels
            Dictionary<String, Object> data = new Dictionary<String, Object>();
            data.Add("bar", "baz");
            channel.publish(data);

            // Disconnecting
            client.disconnect();
	    client.waitFor(1000, new List<BayeuxClient.State>() { BayeuxClient.State.DISCONNECTED });
        }

