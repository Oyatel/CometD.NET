CometD.NET is a project to provide an implementation of the Bayeux protocol in .NET. The client code is ported based on the source code from the java client found at http://cometd.org/. You can also visit that page if you are looking for a CometD server.


This is the CometD.NET project home.

cometd/bayeux           - The Bayeux Specification
cometd/client 		- The .NET client library
cometd/common		- Classes from the java cometd common directory

RUNNING COMETD CLIENT

The CometD.NET client won't work without a bayeux/cometd server to connect to. If you have installed the cometd server from cometd.org, you should be able to connect to it.

USAGE EXAMPLES

We have not made any usage examples yet, but it's basically the same as http://cometd.org/documentation/cometd-java/client . You can look at https://github.com/Oyatel/oyatel-api-examples/blob/master/csharp/Oyatel.Connect.Tutorial/StreamingEvents.cs to see how we use it to communicate with our own bayeux-server.

