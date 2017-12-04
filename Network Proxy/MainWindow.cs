using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Network_Proxy
{
    public partial class MainWindow : Form
    {
        public static readonly BlockingCollection<string> logger = new BlockingCollection<string>();
        private const string ABORT = "ABORT";

        private Thread loggingThread;
        TCPProxy proxy;

        public MainWindow()
        {
            InitializeComponent();

            stopButton.Enabled = false;
            
            loggingThread = new Thread(watchLogger);
            loggingThread.Start();

            proxy = new TCPProxy();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            proxy.stop();
            logger.Add(ABORT);
        }

        private void watchLogger()
        {
            while (true)
            {
                string message = logger.Take();

                if(message == ABORT)
                {
                    return;
                }

                outputMessage(message);
            }
        }

        private void outputMessage(string message)
        {
            MethodInvoker methodInvokerDelegate = delegate ()
            {
                outputBox.AppendText(message);
            };

            outputBox.Invoke(methodInvokerDelegate);
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            try
            {
                proxy.start(int.Parse(localPort.Text), remoteHost.Text, int.Parse(remotePort.Text));
                stopButton.Enabled = true;
            }
            catch
            {
                logger.Add("Error Starting Proxy");
            }
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            proxy.stop();
            stopButton.Enabled = false;
        }
    }
}
