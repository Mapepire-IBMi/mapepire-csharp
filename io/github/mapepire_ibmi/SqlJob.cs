using io.github.mapepire_ibmi.types;
using io.github.mapepire_ibmi.types.jdbcOptions;
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
        private JDBCOptions? options;

        /**
          * TODO: Currently unused but we will inevitably need a unique ID assigned to
          * each instance since server job names can be reused in some circumstances.
          */
        private String uniqueId = SqlJob.getNewUniqueId("sqljob");

        /**
         * Construct a new SqlJob instance.
         */
        public SqlJob()
        {
            this.options = new JDBCOptions();
        }

        /**
          * Construct a new SqlJob instance.
          *
          * @param options The JDBC options.
          */
        public SqlJob(JDBCOptions options)
        {
            this.options = options;
        }

        /**
         * Get a new unique ID with "id" as the prefix.
         *
         * @return The unique ID.
         */
        public static String getNewUniqueId()
        {
            return SqlJob.getNewUniqueId("id");
        }

        /**
         * Get a new unique ID with a custom prefix.
         *
         * @param prefix The custom prefix.
         * @return The unique ID.
         */
        public static String getNewUniqueId(String prefix)
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
        private ClientWebSocket getChannel(DaemonServer db2Server)
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

        public String send(String content)
        {
            if (this.isTracingChannelData)
            {
                Console.WriteLine("\n>> " + content);
            }

            /* Get the id from the input JSON 
                    ObjectMapper objectMapper = SingletonObjectMapper.getInstance();
                    Map<String, Object> req = objectMapper.readValue(content, Map.class);
                    String id = (String) req.get("id");

                    CompletableFuture<String> future = new CompletableFuture<>();
                    responseMap.put(id, future);
            */
            if (this.socket == null) throw new Exception("NULL SOCKET");

            Task sendTask = this.socket.SendAsync(
                Encoding.UTF8.GetBytes(content + "\n"),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);

            /* this.status = JobStatus.Busy; */
            sendTask.Wait();
            var buffer = new byte[65535];
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
                // TODO:  Loop to get all of message. 
                throw new Exception("Partial buffer");
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
        public ConnectionResult? connect(DaemonServer db2Server)
        {
            /* this.Status = JobStatus.Connecting;  */
            ClientWebSocket ws = this.getChannel(db2Server);
            this.socket = ws;

            var connectOptions = new ConnectOptionsRequest(SqlJob.getNewUniqueId(),
           "connect", "tcp", "C# client");

            String result;
            try
            {
                result = this.send(JsonSerializer.Serialize(connectOptions));
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
                this.dispose();
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

        /*
            public QueryResult<T> Execute<T>(String sql)  {
                return this.Execute(sql, new QueryOptions());
            }
        */
        /**
         * Execute an SQL command and returns the result.
         *
         * @param <T>  The type of data to be returned.
         * @param sql  The SQL command to execute.
         * @param opts The options for configuring the query.
         * @return A CompletableFuture that resolves to the query result.
         */
        /*
       public <T> CompletableFuture<QueryResult<T>> execute(String sql, QueryOptions opts) {
           Query query = query(sql, opts);
           return query.<T>execute()
                   .thenCompose(queryResult -> {
                       try {
                           return query.close().thenApply(v -> queryResult);
                       } catch (Exception e) {
                           throw new CompletionException(e);
                       }
                   }).thenApply(queryResult -> {
                       if (!queryResult.getSuccess()) {
                           String error = queryResult.getError();
                           if (error != null) {
                               throw new CompletionException(new SQLException(error, queryResult.getSqlState()));
                           } else {
                               throw new CompletionException(new UnknownServerException("Failed to execute"));
                           }
                       }

                       return queryResult;
                   });
       }
   */
        /**
         * Get the version information from the database server.
         *
         * @return A CompletableFuture that resolves to the version check result.
         */
        /*
       public CompletableFuture<VersionCheckResult> getVersion()   {
           ObjectMapper objectMapper = SingletonObjectMapper.getInstance();
           ObjectNode versionRequest = objectMapper.createObjectNode();
           versionRequest.put("id", SqlJob.getNewUniqueId());
           versionRequest.put("type", "getversion");

           return this.send(objectMapper.writeValueAsString(versionRequest))
                   .thenApply(result -> {
                       VersionCheckResult versionCheckResult;
                       try {
                           // versionCheckResult = objectMapper.readValue(result, VersionCheckResult.class);
                       } catch (Exception e) {
                           throw new CompletionException(e);
                       }

                       if (!versionCheckResult.getSuccess()) {
                           String error = versionCheckResult.getError();
                           if (error != null) {
                               throw new CompletionException(new SQLException(error, versionCheckResult.getSqlState()));
                           } else {
                               throw new CompletionException(new UnknownServerException("Failed to get version"));
                           }
                       }

                       return versionCheckResult;
                   });
       }
   */
        /**
         * Explains a SQL statement and returns the results.
         *
         * @param statement The SQL statement to explain.
         * @return A CompletableFuture that resolves to the explain results.
         */
        /*
            public CompletableFuture<ExplainResults<?>> explain(String statement)  {
                return this.explain(statement, ExplainType.RUN);
            }
        */

        /**
         * Explains a SQL statement and returns the results.
         *
         * @param statement The SQL statement to explain.
         * @param type      The type of explain to perform (default is ExplainType.Run).
         * @return A CompletableFuture that resolves to the explain results.
         */
        /*
            public CompletableFuture<ExplainResults<?>> explain(String statement, ExplainType type) throws Exception {
                ObjectMapper objectMapper = SingletonObjectMapper.getInstance();
                ObjectNode explainRequest = objectMapper.createObjectNode();
                explainRequest.put("id", SqlJob.getNewUniqueId());
                explainRequest.put("type", "dove");
                explainRequest.put("sql", statement);
                explainRequest.put("run", type == ExplainType.RUN);

                return this.send(objectMapper.writeValueAsString(explainRequest))
                        .thenApply(result -> {
                            ExplainResults<?> explainResult;
                            try {
                                explainResult = objectMapper.readValue(result, ExplainResults.class);
                            } catch (Exception e) {
                                throw new CompletionException(e);
                            }

                            if (!explainResult.getSuccess()) {
                                String error = explainResult.getError();
                                if (error != null) {
                                    throw new CompletionException(new SQLException(error, explainResult.getSqlState()));
                                } else {
                                    throw new CompletionException(new UnknownServerException("Failed to explain"));
                                }
                            }

                            return explainResult;
                        });
            }
        */
        /**
         * Get the file path of the trace file, if available.
         */
        public String? getTraceFilePath()
        {
            return this.traceDest;
        }

        /**
         * Get trace data from the backend.
         *
         * @return A CompletableFuture that resolves to the trace data result.
         */
        /*
       public CompletableFuture<GetTraceDataResult> getTraceData() throws Exception {
           ObjectMapper objectMapper = SingletonObjectMapper.getInstance();
           ObjectNode traceDataRequest = objectMapper.createObjectNode();
           traceDataRequest.put("id", SqlJob.getNewUniqueId());
           traceDataRequest.put("type", "gettracedata");

           return this.send(objectMapper.writeValueAsString(traceDataRequest))
                   .thenApply(result -> {
                       GetTraceDataResult traceDataResult;
                       try {
                           traceDataResult = objectMapper.readValue(result, GetTraceDataResult.class);
                       } catch (Exception e) {
                           throw new CompletionException(e);
                       }

                       if (!traceDataResult.getSuccess()) {
                           String error = traceDataResult.getError();
                           if (error != null) {
                               throw new CompletionException(new SQLException(error, traceDataResult.getSqlState()));
                           } else {
                               throw new CompletionException(new UnknownServerException("Failed to get trace data"));
                           }
                       }

                       return traceDataResult;
                   });
       }
   */
        /**
         * Set the server trace destination.
         * 
         * @param dest The server trace destination.
         * @return A CompletableFuture that resolves to the set config result.
         */
        /*
          public CompletableFuture<SetConfigResult> setTraceDest(ServerTraceDest dest) throws Exception {
              return setTraceConfig(dest, null, null, null);
          }
      */

        /**
         * Set the server trace level.
         * 
         * @param level The server trace level.
         * @return A CompletableFuture that resolves to the set config result.
         */
        /* 
            public CompletableFuture<SetConfigResult> setTraceLevel(ServerTraceLevel level) throws Exception {
                return setTraceConfig(null, level, null, null);
            }
        */

        /**
         * Set the JTOpen trace data destination.
         * 
         * @param jtOpenTraceDest The JTOpen trace data destination.
         * @return A CompletableFuture that resolves to the set config result.
         */
        /*
            public CompletableFuture<SetConfigResult> setJtOpenTraceDest(ServerTraceDest jtOpenTraceDest) throws Exception {
                return setTraceConfig(null, null, jtOpenTraceDest, null);
            }
        */

        /**
         * Set the JTOpen trace level.
         * 
         * @param jtOpenTraceLevel The JTOpen trace level.
         * @return A CompletableFuture that resolves to the set config result.
         */
        /*
            public CompletableFuture<SetConfigResult> setJtOpenTraceLevel(ServerTraceLevel jtOpenTraceLevel) throws Exception {
                return setTraceConfig(null, null, null, jtOpenTraceLevel);
            }
        */
        /**
         * Set the trace config on the backend.
         *
         * @param dest             The server trace destination.
         * @param level            The server trace level.
         * @param jtOpenTraceDest  The JTOpen trace data destination.
         * @param jtOpenTraceLevel The JTOpen trace level.
         * @return A CompletableFuture that resolves to the set config result.
         */
        /*
       public CompletableFuture<SetConfigResult> setTraceConfig(ServerTraceDest dest, ServerTraceLevel level,
               ServerTraceDest jtOpenTraceDest, ServerTraceLevel jtOpenTraceLevel) throws Exception {
           ObjectMapper objectMapper = SingletonObjectMapper.getInstance();
           ObjectNode setTraceConfigRequest = objectMapper.createObjectNode();
           setTraceConfigRequest.put("id", SqlJob.getNewUniqueId());
           setTraceConfigRequest.put("type", "setconfig");

           if (dest != null) {
               setTraceConfigRequest.put("tracedest", dest.getValue());
           }
           if (level != null) {
               setTraceConfigRequest.put("tracelevel", level.getValue());
           }
           if (jtOpenTraceDest != null) {
               setTraceConfigRequest.put("jtopentracedest", jtOpenTraceDest.getValue());
           }
           if (jtOpenTraceLevel != null) {
               setTraceConfigRequest.put("jtopentracelevel", jtOpenTraceLevel.getValue());
           }

           this.isTracingChannelData = true;

           return this.send(objectMapper.writeValueAsString(setTraceConfigRequest))
                   .thenApply(result -> {
                       SetConfigResult setConfigResult;
                       try {
                           setConfigResult = objectMapper.readValue(result, SetConfigResult.class);
                       } catch (Exception e) {
                           throw new CompletionException(e);
                       }

                       if (!setConfigResult.getSuccess()) {
                           String error = setConfigResult.getError();
                           if (error != null) {
                               throw new CompletionException(new SQLException(error, setConfigResult.getSqlState()));
                           } else {
                               throw new CompletionException(new UnknownServerException("Failed to set trace config"));
                           }
                       }

                       this.traceDest = setConfigResult.getTraceDest() != null
                               && setConfigResult.getTraceDest().charAt(0) == '/'
                                       ? setConfigResult.getTraceDest()
                                       : null;
                       return setConfigResult;
                   });
       }
   */
        /**
         * Create a CL command query.
         *
         * @param cmd The CL command.
         * @return A new Query instance for the command.
         */
        /*
          public Query clCommand(String cmd) {
              QueryOptions options = new QueryOptions();
              options.setIsClCommand(true);
              return new Query(this, cmd, options);
          }
      */

        /**
         * Check if the job is under commitment control based on the transaction
         * isolation level.
         *
         * @return Whether the job is under commitment control.
         */
        public bool underCommitControl()
        {
            if (this.options == null)
            {
                return false;
            }
            return this.options.getOption(Option.TRANSACTION_ISOLATION) != null
                    && TransactionIsolation.NONE.Equals(
                        this.options.getOption(Option.TRANSACTION_ISOLATION));
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
         * @return A CompletableFuture that resolves to the result of the transaction
         *         operation.
         */
        /*
          public CompletableFuture<QueryResult<JobLogEntry>> endTransaction(TransactionEndType type) throws Exception {
              String query;

              switch (type) {
                  case COMMIT:
                      query = "COMMIT";
                      break;
                  case ROLLBACK:
                      query = "ROLLBACK";
                      break;
                  default:
                      throw new IllegalArgumentException("TransactionEndType " + type + " not valid");
              }

              return this.query(query).execute();
          }
      */

        /**
         * Get the unique ID assigned to this SqlJob instance.
         * TODO: Currently unused but we will inevitably need a unique ID assigned to
         * each instance since server job names can be reused in some circumstances
         *
         * @return The unique ID assigned to this SqlJob instance
         */
        public String getUniqueId()
        {
            return this.uniqueId;
        }

        /**
         * Enable local tracing of channel data.
         */
        public void enableLocalTrace()
        {
            this.isTracingChannelData = true;
        }

        /**
         * Close the job.
         */
        public void close()
        {
            this.dispose();
        }

        /**
         * Close the socket and set the status to be ended.
         */
        private void dispose()
        {
            if (this.socket != null)
            {
                this.socket.Dispose();
            }
            /* this.status = JobStatus.Ended; */
        }




    }



}