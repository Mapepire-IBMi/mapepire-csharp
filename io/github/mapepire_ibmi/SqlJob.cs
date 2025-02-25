using io.github.mapepire_ibmi.types;
using io.github.mapepire_ibmi.types.jdbcOptions;
using System.Data;
using System.Net;
using System.Net.Security;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace io.github.mapepire_ibmi
{


    public class SqlJob
    {


        /**
         * A counter to generate unique IDs for each SQLJob instance.
         */
        private static int uniqueIdCounter = 0;
        private static Object lockObject = new Object();
        /**
         * The socket used to communicate with the Mapepire Server component.
         */
        private ClientWebSocket? socket;

        /**
         * The server trace data destination.
         */
        private String? traceDest;

        /**
         * Whether channel data is being traced.
         */
        private bool isTracingChannelData;

        /**
         * The unique job identifier for the connection.
         * TODO: This is not being used.
         */
        public String? Id { get; set; }



        /**
         * The JDBC options.
         */
        private JDBCOptions? Options;

        /**
          * TODO: Currently unused but we will inevitably need a unique ID assigned to
          * each instance since server job names can be reused in some circumstances.
          */
        private String UniqueId = SqlJob.GetNewUniqueId("sqljob");

        /**
         * Construct a new SqlJob instance.
         */
        public SqlJob()
        {
            this.Options = new JDBCOptions();
        }

        /**
          * Construct a new SqlJob instance.
          *
          * @param options The JDBC options.
          */
        public SqlJob(JDBCOptions options)
        {
            this.Options = options;
        }

        /**
         * Get a new unique ID with "id" as the prefix.
         *
         * @return The unique ID.
         */
        public static String GetNewUniqueId()
        {
            return SqlJob.GetNewUniqueId("id");
        }

        /**
         * Get a new unique ID with a custom prefix.
         *
         * @param prefix The custom prefix.
         * @return The unique ID.
         */
        public static String GetNewUniqueId(String prefix)
        {
            lock (lockObject)
            {
                return prefix + (++uniqueIdCounter);
            }
        }


        /**
        */

        public static bool AllowCertificateCallback(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
        /**
         * Get a WebSocketClient instance which can be used to connect to the specified
         * DB2 server.
         *
         * @param db2Server The server details for the connection.
         * @return A CompletableFuture that resolves to the WebSocketClient instance.
         */
        private ClientWebSocket GetChannel(DaemonServer db2Server)
        {

            Uri uri = new Uri("wss://" + db2Server.Host + ":" + db2Server.Port + "/db/");
            String auth = db2Server.User + ":" + db2Server.Password;
            byte[] plainTextBytes = System.Text.Encoding.UTF8.GetBytes(auth);
            String encodedAuth = System.Convert.ToBase64String(plainTextBytes);

            var ws = new ClientWebSocket();
            ws.Options.SetRequestHeader("Authorization", "Basic " + encodedAuth);
            if (db2Server.RejectUnauthorized == false)
            {

                ws.Options.RemoteCertificateValidationCallback = new RemoteCertificateValidationCallback(AllowCertificateCallback);
            }
            Task result = ws.ConnectAsync(uri, CancellationToken.None);
            result.Wait();


            return ws;
        }

        /**
         * Send a message to the connected database server.
         *
         * @param content The message content to send.
         * @return The server's response.
         */

        public String Send(String content)
        {
            if (this.isTracingChannelData)
            {
                Console.WriteLine("\n>> " + content);
            }

            if (this.socket == null) throw new Exception("NULL SOCKET");

            Task sendTask = this.socket.SendAsync(
                Encoding.UTF8.GetBytes(content + "\n"),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);

            /* this.status = JobStatus.Busy; */
            sendTask.Wait();
            var buffer = new byte[100000];
            Task<WebSocketReceiveResult> task = this.socket.ReceiveAsync(new ArraySegment<byte>(buffer),
            CancellationToken.None);
            task.Wait();
            WebSocketReceiveResult taskResult = task.Result;
            if (taskResult.EndOfMessage)
            {
                return Encoding.UTF8.GetString(buffer, 0, taskResult.Count);
            }
            else
            {
                List<byte[]> buffers = []; 
                List<Int32> counts = []; 
                int totalSize = 0; 
                int blockSize; 
                blockSize = taskResult.Count; 
                // Loop to get all. 
                while (!taskResult.EndOfMessage) { 
                    buffers.Add(buffer);
                    counts.Add(taskResult.Count); 
                    totalSize += taskResult.Count; 
                    buffer = new byte[blockSize] ;   
                    task = this.socket.ReceiveAsync(new ArraySegment<byte>(buffer),
            CancellationToken.None);
                    task.Wait();
                    taskResult = task.Result;
                    
                }
                // Put the rest into the buffer
                buffers.Add(buffer);
                counts.Add(taskResult.Count); 
                totalSize += taskResult.Count; 
                    
                // Put into 1 buffer; 
                buffer = new byte[totalSize];
                int offset = 0; 
                for (int i = 0; i < buffers.Count; i++) { 
                    System.Buffer.BlockCopy(buffers[i], 0, buffer, offset, counts[i]);
                    offset += counts[i]; 
                } 
                return Encoding.UTF8.GetString(buffer, 0, totalSize); 

            }
        }

        /**
         * Get the current status of the job.
         *
         * @return The current status of the job.
         */
        /*
       public JobStatus getStatus() {
           return this.status;
       }
   */
        /**
         * Get the count of ongoing requests for the job.
         *
         * @return The number of ongoing requests.
         */
        /*
       public int getRunningCount() {
           return this.responseMap.size();
       }
       */

        /**
         * Connect to the specified DB2 server and initializes the SQL job.
         *
         * @param db2Server The server details for the connection.
         * @return A CompletableFuture that resolves to the connection result.
         */
        public ConnectionResult? Connect(DaemonServer db2Server)
        {
            /* this.Status = JobStatus.Connecting;  */
            ClientWebSocket ws = this.GetChannel(db2Server);
            this.socket = ws;

            var connectOptions = new ConnectOptionsRequest(SqlJob.GetNewUniqueId(),
           "connect", "tcp", "C# client");

            String result;
            try
            {
                result = this.Send(JsonSerializer.Serialize(connectOptions));
            }
            catch (Exception)
            {
                // Todo:  Thow better exception
                throw;
            }
            ConnectionResult? connectResult = JsonSerializer.Deserialize<ConnectionResult>(result);

            if (connectResult != null && connectResult.Success)
            {
                /* this.status = JobStatus.Ready; */
            }
            else
            {
                this.Dispose();
                /* this.status = JobStatus.NotStarted; */

                String? error = "Unknown";
                if (connectResult != null)
                    error = connectResult.Error;
                if (error != null && connectResult != null)
                {
                    throw new Exception(error + " SQL STATE = " + connectResult.SqlState);
                }
                else
                {
                    throw new Exception("Failed to connect to server");
                }
            }
            if (connectResult != null)
                this.Id = connectResult.Job;
            this.isTracingChannelData = false;

            return connectResult;
        }


        /**
         * Create a Query object for the specified SQL statement.
         *
         * @param sql The SQL query.
         * @return A new Query instance.
         */

        public Query Query(String sql)
        {
            return this.Query(sql, new QueryOptions());
        }

        /**
         * Create a Query object for the specified SQL statement.
         *
         * @param sql  The SQL query.
         * @param opts The options for configuring the query.
         * @return A new Query instance.
         */

        public Query Query(String sql, QueryOptions opts)
        {
            return new Query(this, sql, opts);
        }

        /**
         * Execute an SQL command and returns the result.
         *
         * @param <T> The type of data to be returned.
         * @param sql The SQL command to execute.
         * @return A CompletableFuture that resolves to the query result.
         */


        public QueryResult Execute(String sql)
        {
            return this.Execute(sql, new QueryOptions());
        }

        /**
         * Execute an SQL command and returns the result.
         *
         * @param sql  The SQL command to execute.
         * @param opts The options for configuring the query.
         * @return     The query result.
         */

        public QueryResult Execute(String sql, QueryOptions opts)
        {
            Query query = Query(sql, opts);
            QueryResult queryResult = query.Execute();
            if (!queryResult.Success)
            {
                String? error = queryResult.Error;
                if (error != null)
                {
                    throw new Exception(error + " SQLSTATE=" + queryResult.SqlState);
                }
                else
                {
                    throw new Exception("Failed to execute");
                }
            }
            return queryResult;
        }

        /**
         * Get the version information from the database server.
         *
         * @return A version check result.
         */

        public VersionCheckResult GetVersion()
        {

            VersionRequest versionRequest = new VersionRequest(SqlJob.GetNewUniqueId(), "getversion");
            String result;
            VersionCheckResult? versionCheckResult;
            try
            {

                result = this.Send(JsonSerializer.Serialize(versionRequest));
                versionCheckResult = JsonSerializer.Deserialize<VersionCheckResult>(result);
                if (versionCheckResult == null) throw new Exception("null versionCheckResult");
            }
            catch (Exception)
            {
                // Todo:  Thow better exception
                throw;
            }


            if (!versionCheckResult.Success)
            {
                String? error = versionCheckResult.Error;
                if (error != null)
                {
                    throw new Exception(error + " SQLSTATE:" + versionCheckResult.SqlState);
                }
                else
                {
                    throw new Exception("Failed to get version");
                }
            }

            return versionCheckResult;

        }

        /**
         * Explains a SQL statement and returns the results.
         *
         * @param statement The SQL statement to explain.
         * @return The explain results.
         */
        
            public ExplainResults Explain(String statement)  {
                return this.Explain(statement, ExplainType.RUN);
            }
        

        /**
         * Explains a SQL statement and returns the results.
         *
         * @param statement The SQL statement to explain.
         * @param type      The type of explain to perform (default is ExplainType.Run).
         * @return          The explain results.
         */

        public ExplainResults Explain(String statement, ExplainType type)
        {
            ExplainRequest explainRequest = new(SqlJob.GetNewUniqueId(), "dove",
         statement, type == ExplainType.RUN);

            String stringResult = this.Send(JsonSerializer.Serialize(explainRequest));

            ExplainResults? results = JsonSerializer.Deserialize<ExplainResults>(stringResult);
            if (results != null)
            {

                if (!results.Success)
                {
                    String? error = results.Error;
                    if (error != null)
                    {
                        throw new Exception(error + " sqlstate=" + results.SqlState);
                    }
                    else
                    {
                        throw new Exception("Failed to explain");
                    }
                }

                return results;
            }
            else
            {
                throw new Exception("NULL EXPLAIN RESULTS");
            }

        }

        /**
         * Get the file path of the trace file, if available.
         */
        public String? GetTraceFilePath()
        {
            return this.traceDest;
        }

        /**
         * Get trace data from the backend.
         *
         * @return The trace data result.
         */

        public GetTraceDataResult GetTraceDataResult()
        {
            IdTypeRequest traceDataRequest = new(SqlJob.GetNewUniqueId(), "gettracedata");

            String stringResult = this.Send(JsonSerializer.Serialize(traceDataRequest));

            GetTraceDataResult? traceDataResult = JsonSerializer.Deserialize<GetTraceDataResult>(stringResult);

            if (traceDataResult == null)
            {
                throw new Exception("Null return from traceDatate");
            }

            if (!traceDataResult.Success)
            {
                String? error = traceDataResult.Error;
                if (error != null)
                {
                    throw new Exception(error + " SQLCODE=" + traceDataResult.SqlState);
                }
                else
                {
                    throw new Exception("Failed to get trace data");
                }
            }

            return traceDataResult;

        }

        /**
         * Set the server trace destination.
         * 
         * @param dest The server trace destination.
         * @return The set config result.
         */
        
          public SetConfigResult SetTraceDest(ServerTraceDest dest) {
              return SetTraceConfig(dest, null, null, null);
          }
      

        /**
         * Set the server trace level.
         * 
         * @param level The server trace level.
         * @return A CompletableFuture that resolves to the set config result.
         */
       
            public SetConfigResult setTraceLevel(ServerTraceLevel level)  {
                return SetTraceConfig(null, level, null, null);
            }
      

        /**
         * Set the JTOpen trace data destination.
         * 
         * @param jtOpenTraceDest The JTOpen trace data destination.
         * @return A CompletableFuture that resolves to the set config result.
         */
        
            public SetConfigResult SetJtOpenTraceDest(ServerTraceDest jtOpenTraceDest)  {
                return SetTraceConfig(null, null, jtOpenTraceDest, null);
            }
        

        /**
         * Set the JTOpen trace level.
         * 
         * @param jtOpenTraceLevel The JTOpen trace level.
         * @return A CompletableFuture that resolves to the set config result.
         */
        
            public SetConfigResult SetJtOpenTraceLevel(ServerTraceLevel jtOpenTraceLevel)  {
                return SetTraceConfig(null, null, null, jtOpenTraceLevel);
            }
        
        /**
         * Set the trace config on the backend.
         *
         * @param dest             The server trace destination.
         * @param level            The server trace level.
         * @param jtOpenTraceDest  The JTOpen trace data destination.
         * @param jtOpenTraceLevel The JTOpen trace level.
         * @return The set config result.
         */

        public SetConfigResult SetTraceConfig(ServerTraceDest? dest, ServerTraceLevel? level,
                ServerTraceDest? jtOpenTraceDest, ServerTraceLevel? jtOpenTraceLevel)
        {

            this.isTracingChannelData = true;

            SetTraceConfigRequest setTraceConfigRequest = new(SqlJob.GetNewUniqueId(), "setconfig");
            if (dest != null)
            {
                setTraceConfigRequest.Tracedest = dest.GetValue();
            }
            if (level != null)
            {
                setTraceConfigRequest.Tracelevel = level.GetValue();
            }
            if (jtOpenTraceDest != null)
            {
                setTraceConfigRequest.JTOpenTraceDest = jtOpenTraceDest.GetValue();
            }
            if (jtOpenTraceLevel != null)
            {
                setTraceConfigRequest.JTOpenTraceLevel = jtOpenTraceLevel.GetValue();
            }
            
            String stringResult = this.Send(JsonSerializer.Serialize(setTraceConfigRequest));
            SetConfigResult? setConfigResult = JsonSerializer.Deserialize<SetConfigResult>(stringResult) ?? throw new Exception("setConfigResult is null");
            if (!setConfigResult.Success)
            {
                String? error = setConfigResult.Error;
                if (error != null)
                {
                    throw new Exception(error + " SQLSTATE: " + setConfigResult.SqlState);
                }
                else
                {
                    throw new Exception("Failed to set trace config");
                }
            }
            this.traceDest = setConfigResult.TraceDest != null
                                     && setConfigResult.TraceDest[0] == '/'
                                             ? setConfigResult.TraceDest
                                             : null;
            return setConfigResult;

        }

        /**
         * Create a CL command query.
         *
         * @param cmd The CL command.
         * @return A new Query instance for the command.
         */

        public Query ClCommand(String cmd)
        {
            QueryOptions options = new() { IsClCommand = true };
            return new Query(this, cmd, options);
        }


        /**
         * Check if the job is under commitment control based on the transaction
         * isolation level.
         *
         * @return Whether the job is under commitment control.
         */
        public bool UnderCommitControl()
        {
            if (this.Options == null)
            {
                return false;
            }
            return this.Options.getOption(Option.TRANSACTION_ISOLATION) != null
                    && TransactionIsolation.NONE.Equals(
                        this.Options.getOption(Option.TRANSACTION_ISOLATION));
        }

        /**
         * Get the count of pending transactions.
         *
         * @return A CompletableFuture that resolves to the count of pending
         *         transactions.
         */
        /*
       public CompletableFuture<Integer> getPendingTransactions() throws Exception {
           ObjectMapper objectMapper = SingletonObjectMapper.getInstance();
           String transactionCountQuery = String.join("\n", Arrays.asList(
                   "select count(*) as thecount",
                   "  from qsys2.db_transaction_info",
                   "  where JOB_NAME = qsys2.job_name and",
                   "    (local_record_changes_pending = 'YES' or local_object_changes_pending = 'YES')"));

           return this.query(transactionCountQuery).execute(1)
                   .thenApply(queryResult -> {
                       if (queryResult.getSuccess() && queryResult.getData() != null
                               && queryResult.getData().size() == 1) {
                           String data;
                           try {
                               data = objectMapper.writeValueAsString(queryResult.getData().get(0));
                               Map<String, Object> req = objectMapper.readValue(data, Map.class);
                               Integer count = (Integer) req.get("THECOUNT");
                               return count;
                           } catch (Exception e) {
                               throw new CompletionException(e);
                           }
                       }

                       return 0;
                   });
       }
   */
        /**
         * Ends the current transaction by committing or rolling back.
         *
         * @param type The type of transaction ending (commit or rollback).
         * @return The result of the transaction
         *         operation.
         */

        public QueryResult EndTransaction(TransactionEndType type)
        {
            String query;

            switch (type)
            {
                case TransactionEndType.COMMIT:
                    query = "COMMIT";
                    break;
                case TransactionEndType.ROLLBACK:
                    query = "ROLLBACK";
                    break;
                default:
                    throw new Exception("TransactionEndType " + type + " not valid");
            }

            return this.Query(query).Execute();
        }


        /**
         * Get the unique ID assigned to this SqlJob instance.
         * TODO: Currently unused but we will inevitably need a unique ID assigned to
         * each instance since server job names can be reused in some circumstances
         *
         * @return The unique ID assigned to this SqlJob instance
         */
        public String GetUniqueId()
        {
            return this.UniqueId;
        }

        /**
         * Enable local tracing of channel data.
         */
        public void EnableLocalTrace()
        {
            this.isTracingChannelData = true;
        }

        /**
         * Close the job.
         */
        public void Close()
        {
            this.Dispose();
        }

        /**
         * Close the socket and set the status to be ended.
         */
        private void Dispose()
        {
            if (this.socket != null)
            {
                this.socket.Dispose();
            }
            /* this.status = JobStatus.Ended; */
        }




    }



}