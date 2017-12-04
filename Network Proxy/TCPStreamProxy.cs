using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading;
using System.Net;

namespace Network_Proxy
{
    class TCPStreamProxy
    {
        public TcpClient socket;
        public NetworkStream outputStream;
        public NetworkStream inputStream;

        private Thread myThread;
        public event Action<TCPStreamProxy> didFinish;
        private bool shouldAbort;

        private EndPoint localEndPoint;
        private EndPoint remoteEndPoint;

        public TCPStreamProxy(EndPoint localEndPoint, EndPoint remoteEndPoint)
        {
            this.localEndPoint = localEndPoint;
            this.remoteEndPoint = remoteEndPoint;
        }

        public void start()
        {
            shouldAbort = false;
            myThread = new Thread(new ThreadStart(run));
            myThread.Start();
        }

        public void stop()
        {
            shouldAbort = true;
        }

        private void run()
        {
            byte[] data = new byte[1 << 16];

            while (shouldAbort == false && socket.Connected)
            {
                if (socket.Available > 0)
                {
                    int bytesReaded = inputStream.Read(data, 0, socket.Available);
                    outputStream.Write(data, 0, bytesReaded);

                    MainWindow.logger.Add(String.Format("Sending {0} bytes from {1} to {2}\r\n", bytesReaded, localEndPoint, remoteEndPoint));
                }

                Thread.Sleep(10);
            }

            MainWindow.logger.Add(String.Format("Connection from {0} closed\r\n", remoteEndPoint));
            didFinish?.Invoke(this);
        }
    }
}
