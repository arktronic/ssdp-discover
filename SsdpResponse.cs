using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;

namespace SSDP_Discover
{
    public class SsdpResponse
    {
        private SsdpResponse() { }

        private List<KeyValuePair<string, string>> headers = new List<KeyValuePair<string, string>>();

        public string HttpVersion { get; private set; }
        public int StatusCode { get; private set; }
        public string ReasonPhrase { get; private set; }
        public ReadOnlyCollection<KeyValuePair<string, string>> Headers { get { return headers.AsReadOnly(); } }
        public IPEndPoint RemoteEndPoint { get; private set; }

        public override string ToString()
        {
            string statusType = headers.FirstOrDefault(h => h.Key.ToLowerInvariant() == "st").Value ?? "Unknown service type";
            return RemoteEndPoint.Address.ToString() + " -> " + statusType;
        }

        public static SsdpResponse Parse(string data, EndPoint remote)
        {
            SsdpResponse response = new SsdpResponse();

            try
            {
                var parts = new List<string>(data.Split(new string[] { "\r\n" }, StringSplitOptions.None));
                var statusLine = parts[0].Split(' ');
                response.HttpVersion = statusLine[0];
                response.StatusCode = int.Parse(statusLine[1]);
                response.ReasonPhrase = statusLine[2];
                for (var i = 1; i < parts.Count; i++)
                {
                    if (parts[i].Length > 0)
                    {
                        var headerParts = parts[i].Split(new char[] { ':' }, 2);
                        response.headers.Add(new KeyValuePair<string, string>(headerParts[0].Trim(), headerParts[1].Trim()));
                    }
                    else
                    {
                        break;
                    }
                }

                response.RemoteEndPoint = (IPEndPoint)remote;
            }
            catch (Exception)
            {
                Console.WriteLine("Error parsing a response; skipping...");
                return null;
            }

            return response;
        }
    }
}
