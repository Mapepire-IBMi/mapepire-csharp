using System;
using System.Net;
using System.Net.WebSockets;
using System.Net.Security;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Reflection;
using System.Security.Claims;
using System.Net.Http;
using System.ServiceModel;
using System.Security.AccessControl;


namespace MobiMapepireClient
{

    /// <summary>
    /// This class is a dotnet WebSocket client class for 
    /// the IBM i Mapepire WebSocket Data Server component.
    //  Created as an initial prototype by Richard Schoen - MobiGoGo LLC.
    /// Learn more here: 
    /// https://github.com/Mapepire-IBMi
    /// </summary>
    internal class Client
    {
        private ClientWebSocket _clientWebSocket = null;
        private bool _connected = false;
        private string _connurl = "";
        private string _lastError = "";
        private string _connectResults = "";
        private string _queryResults = "";
        private bool _querySuccess = false;
        private string _clCmdResults = "";
        private bool _clCmdSuccess = false;
        private bool _pingAlive = false;
        private bool _pingDbAlive = false;
        private string _pingResults = "";
        private bool _pingSuccess = false;
        private string _notconnectedmsg = "Not connected to mapepire server.";
        private string _connectedmsg = "Already connected to mapepire server.";

        /// <summary>
        /// Get connection URL for display.
        /// </summary>
        /// <returns>Full connection url after connect.</returns>
        public string GetConnUrl()
        {
            return _connurl;
        }

        /// <summary>
        /// Get last error message
        /// </summary>
        /// <returns>String from last error message.</returns>
        public string GetLastError()
        {
            return _lastError;
        }

        /// <summary>
        /// Get last connection results
        /// </summary>
        /// <returns>JSON query results</returns>
        public string GetConnectionResults()
        {
            return _connectResults;
        }

        /// <summary>
        /// Get last query results
        /// </summary>
        /// <returns>JSON query results</returns>
        public string GetQueryResults()
        {
            return _queryResults;
        }

        /// <summary>
        /// Get last query success or failure
        /// </summary>
        /// <returns>False=Query failed, True=Success</returns>
        public bool GetQuerySuccess()
        {
            return _querySuccess;
        }
        /// <summary>
        /// Get last CL command results
        /// </summary>
        /// <returns>JSON query results</returns>
        public string GetClResults()
        {
            return _clCmdResults;
        }

        /// <summary>
        /// Get last CL command success or failure
        /// </summary>
        /// <returns>False=CL command failed, True=Success</returns>
        public bool GetClSuccess()
        {
            return _clCmdSuccess;
        }

        /// <summary>
        /// Get last ping command results
        /// </summary>
        /// <returns>JSON query results</returns>
        public string GetPingResults()
        {
            return _pingResults;
        }

        /// <summary>
        /// Get last ping command success or failure
        /// </summary>
        /// <returns>False=CL command failed, True=Success</returns>
        public bool GetPingSuccess()
        {
            return _pingSuccess;
        }

        /// <summary>
        /// Get last ping alive status
        /// </summary>
        /// <returns>False=Not alive, True=Alive</returns>
        public bool GetPingAlive()
        {
            return _pingAlive;
        }

        /// <summary>
        /// Get last ping db_alive status
        /// </summary>
        /// <returns>False=Not alive, True=Alive</returns>
        public bool GetPingDbAlive()
        {
            return _pingDbAlive;
        }

        /// <summary>
        /// IsConnected
        /// </summary>
        /// <returns>Is web socket connection connected ?</returns>
        public bool IsConnected()
        {
            return _connected;
        }

        /// <summary>
        /// Connect to Mapepire WebSocket and authorize
        /// </summary>
        /// <param name="host">Server host</param>
        /// <param name="user">Server user</param>
        /// <param name="password">Server password</param>
        /// <param name="port">Server port. Default=8076</param>
        /// <param name="secure">
        /// Use secure socket-wss://, True=wss://, False=ws://
        /// Default=true;
        /// </param>
        /// <param name="allowinvalidcert">Allow invalid certificates. 
        /// True=Allow any cert to pass through, essentially ignore checks.
        /// False=Certificates must be valid. Default=False
        /// </param>
        /// <returns>True=Connected, False=Error. Use GetLastError to check message.</returns>
        public async Task<bool> Connect(string host, string user, string password, int port = 8076, bool secure = true,bool allowinvalidcert=false)
        {

            try
            {

                _lastError = "";
                _connectResults = "";
                _connected = false;

                // Check if connected. Use existing connection if so
                if (IsConnected())
                {
                    _lastError = _connectedmsg + " Existing connection will be used unless you Disconnect first.";
                    return true;
                }

                // Create new web socket
                _clientWebSocket = new ClientWebSocket();

                // Build Secure/Unsecure URL for WebSocket connection
                if (secure)
                {
                    _connurl = $"wss://{host}:{port}/db";
                }
                else
                {
                    _connurl = $"ws://{host}:{port}/db";

                }

                // Encode the authorization to base64 for auth header
                string userauth = ($"{user}:{password}");
                string encodedauth = (Convert.ToBase64String(Encoding.Default.GetBytes(userauth)));
                _clientWebSocket.Options.SetRequestHeader("Authorization", "Basic " + encodedauth);

                // Set SSL/TLS certificate validation. If we want to allow all certs,
                // including invalid ones,simply return true from the validation callback.
                if (allowinvalidcert)
                {
                    // Simpler way to write as a one-liner.
                    //_clientWebSocket.Options.RemoteCertificateValidationCallback = (sender, X509Certificate, X509Chain, SslPolicyErrors) => true;
                    _clientWebSocket.Options.RemoteCertificateValidationCallback = (message, cert, chain, sslPolicyErrors) =>
                     {
                         return true;
                     };
                }

                // Set the URI
                Uri ibmiUri = new Uri(_connurl);

                // Attempt to connect to WebSocket
                await _clientWebSocket.ConnectAsync(ibmiUri, CancellationToken.None);

                //Console.WriteLine("Connection state: " + _clientWebSocket.State);
                _connectResults = "Connection state: " + _clientWebSocket.State + "\n";

                // Send connect request to WebSocket server now that socket connected
                ArraySegment<byte> bytesToSend = new ArraySegment<byte>(Encoding.UTF8.GetBytes("{\"id\":\"connecting\", \"type\":\"connect\", \"technique\":\"tcp\"}\n"));
                await _clientWebSocket.SendAsync(bytesToSend, WebSocketMessageType.Text, true, CancellationToken.None);
                byte[] receiveBuffer = new byte[1024 * 512];

                // Get response from WebSocket request
                WebSocketReceiveResult result = await _clientWebSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
                String msg = Encoding.UTF8.GetString(receiveBuffer, 0, result.Count);
                //Console.WriteLine(msg);
                _connectResults = _connectResults + msg + "\n";

                // Set connected status
                _connected = true;

                return true;

            }
            catch (Exception ex)
            {
                {
                    _lastError = ex.Message;
                    _connected = false;
                    return false;
                }

            }

        }

        /// <summary>
        /// Disconnect from Mapepire WebSocket server
        /// </summary>
        public async Task<bool> Disconnect()
        {

            try
            {

                _lastError = "";

                if (_clientWebSocket == null)
                {
                    throw new Exception("WebSocket client is not instantiated. You must Connect first.");
                }

                // Close websocket connection
                await _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None);

                // Reset connected status
                _connected = false;
                _connectResults = "";
                _clCmdResults = "";
                _clCmdSuccess = false;
                _queryResults = "";
                _querySuccess = false;
                _pingResults = "";
                _pingSuccess = false;
                _pingAlive = false;
                _pingDbAlive = false;

                // Dispose the web socket 
                _clientWebSocket.Dispose();
                _clientWebSocket = null;

                return true;

            }
            catch (Exception ex)
            {
                {
                    _lastError = ex.Message;
                    return false;
                }

            }

        }

        /// <summary>
        /// Run SQL Query and return JSON results
        /// </summary>
        /// <param name="sql">SQL query statement</param>
        /// <param name="id">Query ID. Default=q1</param>
        /// <returns>True=Query success. False=Query command error.</returns>

        public async Task<bool> ExecSqlQuery(string sql, string id = "Q1")
        {

            try
            {

                // Reset settings
                _lastError = "";
                _queryResults = "";
                _querySuccess = false;

                // Check if connected
                if (!IsConnected())
                {
                    throw new Exception(_notconnectedmsg);
                }

                // Init work variablea
                ArraySegment<byte> bytesToSend = null;
                byte[] receiveBuffer = new byte[1024 * 512];
                WebSocketReceiveResult result = null;
                String msg = "";

                // Compose the JSON request
                string queryjson = "{\"id\":\"@@ID\", \"type\":\"sql\", \"sql\":\"@@SQLQUERY\"}\n";
                queryjson = queryjson.Replace("@@ID", id);
                queryjson = queryjson.Replace("@@SQLQUERY", sql);

                bytesToSend = new ArraySegment<byte>(Encoding.UTF8.GetBytes(queryjson));
                await _clientWebSocket.SendAsync(bytesToSend, WebSocketMessageType.Text, true, CancellationToken.None);

                result = await _clientWebSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
                msg = Encoding.UTF8.GetString(receiveBuffer, 0, result.Count);

                // Save query results from return message
                _queryResults = msg;

                // Scan JSON for success
                if (msg.Contains("\"success\":true"))
                {
                    _querySuccess = true;
                }
                else
                {
                    _querySuccess = false;
                }

                return _querySuccess;

            }
            catch (Exception ex)
            {
                {
                    _lastError = ex.Message;

                    // No query results
                    _queryResults = "";
                    _querySuccess = false;

                    return _querySuccess;
                }

            }

        }

        /// <summary>
        /// Run CL command
        /// </summary>
        /// <param name="cmd">CL command</param>
        /// <param name="id">Command ID. Default=cmd1</param>
        /// <returns>True=Command success. False=CL command error.</returns>
        public async Task<bool> ExecClCommand(string cmd, string id = "cmd1")
        {

            try
            {
                // Reset settings
                _lastError = "";
                _clCmdResults = "";
                _clCmdSuccess = false;

                // Check if connected
                if (!IsConnected())
                {
                    throw new Exception(_notconnectedmsg);
                }

                // Init work variablea
                ArraySegment<byte> bytesToSend = null;
                byte[] receiveBuffer = new byte[1024 * 512];
                WebSocketReceiveResult result = null;
                String msg = "";

                // Compose the JSON request
                string queryjson = "{\"id\":\"@@ID\", \"type\":\"cl\", \"cmd\":\"@@CLCMD\"}\n";
                queryjson = queryjson.Replace("@@ID", id);
                queryjson = queryjson.Replace("@@CLCMD", cmd);

                // Send the WebSocket request
                bytesToSend = new ArraySegment<byte>(Encoding.UTF8.GetBytes(queryjson));
                await _clientWebSocket.SendAsync(bytesToSend, WebSocketMessageType.Text, true, CancellationToken.None);

                // Receive the WebSocket respose
                result = await _clientWebSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
                msg = Encoding.UTF8.GetString(receiveBuffer, 0, result.Count);

                // Save CL results from return message
                _clCmdResults = msg;

                // Scan JSON for success
                if (msg.Contains("\"success\":true"))
                {
                    _clCmdSuccess = true;
                }
                else
                {
                    _clCmdSuccess = false;
                }

                return _clCmdSuccess;

            }
            catch (Exception ex)
            {
                {
                    _lastError = ex.Message;

                    // No query results
                    _clCmdResults = "";
                    _clCmdSuccess = false;

                    return _clCmdSuccess;
                }

            }

        }

        /// <summary>
        /// Run connection ping
        /// </summary>
        /// <returns>True=Command success. False=CL command error.</returns>
        public async Task<bool> Ping(string id = "ping1")
        {

            try
            {
                // Reset settings
                _lastError = "";
                _pingResults = "";
                _pingSuccess = false;
                _pingAlive = false;
                _pingDbAlive = false;

                // Init work variablea
                ArraySegment<byte> bytesToSend = null;
                byte[] receiveBuffer = new byte[1024 * 512];
                WebSocketReceiveResult result = null;
                String msg = "";

                // Compose the JSON request
                string queryjson = "{\"id\":\"@@ID\", \"type\":\"ping\"}\n";
                queryjson = queryjson.Replace("@@ID", id);

                // Send the WebSocket request
                bytesToSend = new ArraySegment<byte>(Encoding.UTF8.GetBytes(queryjson));
                await _clientWebSocket.SendAsync(bytesToSend, WebSocketMessageType.Text, true, CancellationToken.None);

                // Receive the WebSocket respose
                result = await _clientWebSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
                msg = Encoding.UTF8.GetString(receiveBuffer, 0, result.Count);

                // Save CL results from return message
                _pingResults = msg;

                // Scan JSON for success
                if (msg.Contains("\"success\":true"))
                {
                    _pingSuccess = true;
                }
                else
                {
                    _pingSuccess = false;
                }

                // Scan JSON for alive
                if (msg.Contains("\"alive\":true"))
                {
                    _pingAlive = true;
                }
                else
                {
                    _pingAlive = false;
                }

                // Scan JSON for db_alive
                if (msg.Contains("\"db_alive\":true"))
                {
                    _pingDbAlive = true;
                }
                else
                {
                    _pingDbAlive = false;
                }

                return _pingSuccess;

            }
            catch (Exception ex)
            {
                {
                    _lastError = ex.Message;

                    // No query results
                    _pingResults = "";
                    _pingSuccess = false;

                    return _pingSuccess;
                }

            }

        }

    }

}
