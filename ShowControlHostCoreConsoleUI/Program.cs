using System;
using System.Text;
using System.Threading;

namespace ShowControlHostCoreConsoleUI
{
    class Program
    {
        private static AsyncNetworkLink link;

        static void Main(string[] args)
        {
            SetupSCS();
            SendSCSMultiTime(50);
            Console.WriteLine("Hello World!");
            Console.WriteLine($"Getting a random number {ReturnRandom(1,99)}");
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
            throw new NotImplementedException();
        }

        static int ReturnRandom(int x, int y)
        {
            int output = 0;
            Random rnd = new Random();
            output = rnd.Next(x, y);
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
