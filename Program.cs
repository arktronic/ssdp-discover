using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace SSDP_Discover
{
    public class Program
    {
        private static List<SsdpResponse> Responses = new List<SsdpResponse>();

        public static void Main(string[] args)
        {
            Scan();
            ListResponses();
            while (true)
            {
                ShowOptions();
                ProcessEntry();
            }
        }

        private static void ProcessEntry()
        {
            int responseNum;
            var entry = Console.ReadLine().Trim();
            if (entry == "q")
            {
                Environment.Exit(0);
            }
            else if (entry == "r")
            {
                ListResponses();
            }
            else if (entry == "s")
            {
                Scan();
                ListResponses();
            }
            else if (int.TryParse(entry, out responseNum))
            {
                responseNum--;
                if (responseNum < 0 || responseNum > Responses.Count - 1)
                {
                    Console.WriteLine("That number is not valid.");
                }
                else
                {
                    var response = Responses[responseNum];
                    Console.WriteLine($"Response #{responseNum + 1}:");
                    Console.WriteLine($"Source: {response.RemoteEndPoint.Address}:{response.RemoteEndPoint.Port}");
                    Console.WriteLine($"Status: {response.HttpVersion} {response.StatusCode} ({response.ReasonPhrase})");
                    foreach(var header in response.Headers)
                    {
                        Console.WriteLine($"  {header.Key}: {header.Value}");
                    }
                }
            }
            else
            {
                Console.WriteLine("That is not a valid choice.");
            }
        }

        private static void ShowOptions()
        {
            Console.WriteLine();
            Console.WriteLine("Please choose from the following:");
            if (Responses.Count == 1)
            {
                Console.Write("[1] View entire response ");
            }
            else if (Responses.Count > 1)
            {
                Console.Write($"[1-{Responses.Count}] View entire response ");
            }
            Console.WriteLine("[s] Search again [r] View all responses [q] Quit");
        }

        private static void ListResponses()
        {
            Console.WriteLine($"Got {Responses.Count} responses");
            for (var i = 0; i < Responses.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {Responses[i]}");
            }
        }

        private static void Scan()
        {
            Console.WriteLine("Searching...");
            var responses = new List<SsdpResponse>();

            try
            {
                var localEndPoint = new IPEndPoint(IPAddress.Any, 0);
                var ssdpEndPoint = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900);
                using (var udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    udpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    udpSocket.Bind(localEndPoint);
                    udpSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(ssdpEndPoint.Address, IPAddress.Any));

                    const string SearchString = "M-SEARCH * HTTP/1.1\r\nHost: 239.255.255.250:1900\r\nMan: \"ssdp:discover\"\r\nST: ssdp:all\r\nMX: 3\r\n\r\n";
                    udpSocket.SendTo(System.Text.Encoding.ASCII.GetBytes(SearchString), ssdpEndPoint);

                    var buffer = new byte[4096];
                    var timeout = DateTime.UtcNow + TimeSpan.FromSeconds(3);
                    while (timeout > DateTime.UtcNow)
                    {
                        if (udpSocket.Available > 0)
                        {
                            EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                            var size = udpSocket.ReceiveFrom(buffer, ref remote);
                            var response = SsdpResponse.Parse(System.Text.Encoding.ASCII.GetString(buffer, 0, size), remote);
                            if (response != null) responses.Add(response);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Search error: " + ex);
            }

            Responses = responses;
        }
    }
}
