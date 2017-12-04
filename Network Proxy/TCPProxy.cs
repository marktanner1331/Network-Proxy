using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;

namespace Network_Proxy
{
    class TCPProxy
    {
        private Thread myThread;
        private bool shouldAbort;

        private int localPort;
        private string remoteHost;
        private int remotePort;

        private HashSet<TCPStreamProxy> streamProxies;

        public TCPProxy()
        {
            streamProxies = new HashSet<TCPStreamProxy>();
        }

        public void start(int localPort, string remoteHost, int remotePort)
        {
            string message = string.Format("Starting TCP Proxy: \r\nLocal Port: {0} \r\nRemote Host: {1} \r\nRemote Port: {2}\r\n\r\n", localPort, remoteHost, remotePort);
            MainWindow.logger.Add(message);

            this.localPort = localPort;
            this.remoteHost = remoteHost;
            this.remotePort = remotePort;

            if(myThread != null)
            {
                stop();
                myThread.Join();
                myThread = null;
            }

            shouldAbort = false;
            myThread = new Thread(run);
            myThread.Start();
        }

        public void stop()
        {
            if (myThread != null)
            {
                shouldAbort = true;

                try
                {
                    //the run method is blocking on tcpListener.AcceptTcpClient()
                    //so we set shouldAbort to true, and start a new connection to stop it blocking
                    TcpClient RemoteSocket = new TcpClient("127.0.0.1", localPort);
                }
                catch
                { }
            }

            foreach(TCPStreamProxy streamProxy in streamProxies)
            {
                streamProxy.stop();
            }
        }

        private void run()
        {
            TcpListener tcpListener = new TcpListener(IPAddress.Any, localPort);
            tcpListener.Start();

            while (true)
            {
                TcpClient LocalSocket = tcpListener.AcceptTcpClient();
                if(shouldAbort)
                {
                    tcpListener.Stop();
                    return;
                }

                MainWindow.logger.Add("Connection Established from: " + LocalSocket.Client.RemoteEndPoint + "\r\n");

                NetworkStream NetworkStreamLocal = LocalSocket.GetStream();

                TcpClient RemoteSocket = new TcpClient(remoteHost, remotePort);
                NetworkStream NetworkStreamRemote = RemoteSocket.GetStream();

                addStreamProxy(new TCPStreamProxy(LocalSocket.Client.RemoteEndPoint, RemoteSocket.Client.RemoteEndPoint)
                {
                    outputStream = NetworkStreamLocal,
                    inputStream = NetworkStreamRemote,
                    socket = RemoteSocket
                });

                addStreamProxy(new TCPStreamProxy(RemoteSocket.Client.RemoteEndPoint, LocalSocket.Client.RemoteEndPoint)
                {
                    outputStream = NetworkStreamRemote,
                    inputStream = NetworkStreamLocal,
                    socket = LocalSocket
                });
            }
        }

        private void addStreamProxy(TCPStreamProxy streamProxy)
        {
            streamProxies.Add(streamProxy);
            streamProxy.didFinish += StreamProxy_didFinish;
            streamProxy.start();
        }

        private void StreamProxy_didFinish(TCPStreamProxy streamProxy)
        {
            streamProxy.didFinish -= StreamProxy_didFinish;
            streamProxies.Remove(streamProxy);
        }
    }
}
