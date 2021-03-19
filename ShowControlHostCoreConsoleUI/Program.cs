using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks.Dataflow;

namespace ShowControlHostCoreConsoleUI
{
    class Program
    {
        private static AsyncNetworkLink link;

        private static AsyncUdpLink udpLink;

        static void Main(string[] args)
        {
            //SetupSCS();
            //SendSCSMultiTime(50);
            Console.WriteLine("Hello World!");
            //Console.WriteLine($"Getting a random number {ReturnRandom(1,99)}");
            SetupUDP();
        }

        public static void SetupUDP()
        {
            udpLink = new AsyncUdpLink("127.0.0.1", 5000, 660, true);
            string message = "hello from SetuUDP!0D";
            string message2 = message.Replace("!0D", "\r");
            Console.WriteLine(message);
            Console.WriteLine(message2);
            byte[] inputBytes = Encoding.ASCII.GetBytes(message2); // new byte array and feed it the input string
            udpLink.SendMessage(inputBytes);

            for (int i = 0; i < 50; i++)
            {
                //messageAdder++;
                message = $"hello{i}!0D";
                message2 = message.Replace("!0D", "\r");
                inputBytes = Encoding.ASCII.GetBytes(message2); // new byte array and feed it the input string
                udpLink.SendMessage(inputBytes); // send the byte array
                Thread.Sleep(5);
                Console.WriteLine($"Sent to SCS {message2}");
            }
        }
        
        public static void SetupSCS()
        {
            // this creates the TCP connection and event handler
            link = new AsyncNetworkLink("localhost", 8080, true);
            link.DataReceived += LinkOnDataReceived;
            Console.WriteLine("sleeping in Setup");
            Thread.Sleep(1000);
        }

        private static void LinkOnDataReceived(object sender, EventArgs e)
        {
            
        }

        static int ReturnRandom(int x, int y)
        { 
            Random rnd = new Random();
            int output = rnd.Next(x, y);
            return output;
        }

        static void SendSCSMultiTime(int numberOfMsgsToSend)
        {
            string message = string.Empty;
            //messageAdder = 1;
            
            Thread.Sleep(1000);
            //if (link.Enabled && link.IsConnected)
            if (1 == 1)
            {
                for (int i = 0; i < numberOfMsgsToSend; i++)
                {
                    //messageAdder++;
                    message = $"hello{i}\r";
                    byte[] inputBytes = Encoding.ASCII.GetBytes(message); // new byte array and feed it the input string
                    link.SendMessage(inputBytes); // send the byte array
                    Thread.Sleep(5);
                    Console.WriteLine($"Sent to SCS {message}");
                }
            }
            else
            {
                Console.WriteLine("SCS not connected");
            }

            // Console.WriteLine($"Sent to SCS {message}");
        }

        //static void SendSCSSingleTime()
        //{
        //    string message = string.Empty;
        //    message = "hello\r";
        //    Thread.Sleep(1000);
        //    if (link.Enabled && link.IsConnected)
        //    {
        //        byte[] inputBytes = Encoding.ASCII.GetBytes(message); // new byte array and feed it the input string
        //        link.SendMessage(inputBytes); // send the byte array
        //    }
        //    else
        //    {
        //        Console.WriteLine("SCS not connected");
        //    }

        //    Console.WriteLine($"Sent to SCS {message}");
        //}
    }
}
