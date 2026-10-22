using System;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConstructionAddIn.Services.EnterpriseConnection
{
    /// <summary>
    /// Cheap "is the SQL Server reachable?" check used by the background monitor.
    ///
    /// It only opens a TCP socket to the SQL Server port, so it runs on a thread-pool
    /// thread and never blocks ArcGIS Pro's main CIM thread (a real geodatabase
    /// connection attempt can freeze Pro for many seconds while the server is down).
    /// The full geodatabase test is done only when the user reconnects.
    /// </summary>
    public static class SqlServerProbe
    {
        private const int DefaultPort = 1433;
        private const int BrowserPort = 1434;
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(4);

        public static async Task<bool> IsReachableAsync(string instance)
        {
            if (!TryParse(instance, out var host, out var port, out var instanceName))
                return false;

            try
            {
                // Named instance without an explicit port: ask SQL Server Browser for it.
                if (port == null && instanceName != null)
                    port = await GetNamedInstancePortAsync(host, instanceName);

                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(host, port ?? DefaultPort);
                var finished = await Task.WhenAny(connectTask, Task.Delay(Timeout));

                return finished == connectTask && !connectTask.IsFaulted && client.Connected;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Splits a SQL Server instance into host / port / instance name.
        /// Accepts "host", "host,port", "host\name", "host\name,port", "tcp:host,port".
        /// </summary>
        private static bool TryParse(string instance, out string host, out int? port, out string instanceName)
        {
            host = null;
            port = null;
            instanceName = null;

            if (string.IsNullOrWhiteSpace(instance))
                return false;

            string value = instance.Trim();
            if (value.StartsWith("tcp:", StringComparison.OrdinalIgnoreCase))
                value = value.Substring(4);

            int comma = value.IndexOf(',');
            if (comma >= 0)
            {
                if (int.TryParse(value.Substring(comma + 1).Trim(), out int parsedPort))
                    port = parsedPort;

                value = value.Substring(0, comma);
            }

            int slash = value.IndexOf('\\');
            if (slash >= 0)
            {
                instanceName = value.Substring(slash + 1).Trim();
                value = value.Substring(0, slash);
            }

            host = value.Trim();
            if (host == "." || host.Equals("(local)", StringComparison.OrdinalIgnoreCase))
                host = "localhost";

            return host.Length > 0;
        }

        /// <summary>
        /// SQL Server Browser protocol (CLNT_UCAST_INST): returns the TCP port of a named instance,
        /// or null if the browser service does not answer.
        /// </summary>
        private static async Task<int?> GetNamedInstancePortAsync(string host, string instanceName)
        {
            try
            {
                using var udp = new UdpClient();
                udp.Connect(host, BrowserPort);

                var request = new byte[instanceName.Length + 2];
                request[0] = 0x04;
                Encoding.ASCII.GetBytes(instanceName, 0, instanceName.Length, request, 1);
                await udp.SendAsync(request, request.Length);

                var receiveTask = udp.ReceiveAsync();
                if (await Task.WhenAny(receiveTask, Task.Delay(Timeout)) != receiveTask)
                    return null;

                string response = Encoding.ASCII.GetString(receiveTask.Result.Buffer);
                var match = Regex.Match(response, @"tcp;(\d+)", RegexOptions.IgnoreCase);

                return match.Success ? int.Parse(match.Groups[1].Value) : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
