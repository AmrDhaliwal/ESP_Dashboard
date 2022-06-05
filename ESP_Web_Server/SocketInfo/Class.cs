using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ESP_Web_Server.SocketInfo
{
    public class TCPSocketListener
    {
        // State object for reading client data asynchronously  
        public class StateObject
        {
            // Size of receive buffer.  
            public const int BufferSize = 1024;

            // Receive buffer.  
            public byte[] buffer = new byte[BufferSize];

            // Received data string.
            public StringBuilder sb = new ();

            // Client socket.
            public Socket? workSocket = null;
        }

        // Thread signal.  
        public static ManualResetEvent allDone = new(false);

        public static void StartListening()
        {

            // Establish the local endpoint for the socket.  
            // The DNS name of the computer  
            // running the listener is "host.contoso.com".  
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint localEndPoint = new(ipAddress, 11000);

            // Create a TCP/IP socket.  
            Socket listener = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections.  
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);

                while (true)
                {
                    // Set the event to nonsignaled state.  
                    allDone.Reset();

                    // Start an asynchronous socket to listen for connections.  
                    Console.WriteLine("Waiting for a connection...");
                    listener.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        listener);

                    // Wait until a connection is made before continuing.  
                    allDone.WaitOne();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();
        }

        public static void AcceptCallback(IAsyncResult ar)
        {
            // Get the socket that handles the client request.  
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Signal the main thread to continue.  
            allDone.Set();

            // Create the state object.  
            StateObject state = new StateObject();
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(AsynchronousSocketListener.ReadCallback), state);
        }

        public static void ReadCallback(IAsyncResult ar)
        {
            StateObject state = (StateObject)ar.AsyncState;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            Socket handler = state.workSocket;


            // Read data from the client socket.  
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            int read = handler.EndReceive(ar);
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            // Data was read from the client socket.  
            if (read > 0)
            {
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, read));
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);
            }
            else
            {
                if (state.sb.Length > 1)
                {
                    // All the data has been read from the client;  
                    // display it on the console.  
                    string content = state.sb.ToString();
                    Console.WriteLine($"Read {content.Length} bytes from socket.\n Data : {content}");
                }
                handler.Close();
            }
        }
    }
}
