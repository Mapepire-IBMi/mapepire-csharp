using System.Text.Json;
using io.github.mapepire_ibmi.types;


namespace io.github.mapepire_ibmi
{
    /// <summary>Executes an SQL statement or CL command on a Mapepire job.</summary>
    public class Query
    {
        /**
         * A list of all global queries that are currently open.
         */
        private static List<Query> GlobalQueryList = [];

        /**
         * The SQL job that this query will be executed in.
         */
        private SqlJob Job { get; set; }

        /**
         * The correlation ID associated with the query.
         */
        private String? CorrelationId;

        /**
         * The SQL statement to be executed.
         */
        private String Sql;

        /**
         * Whether the query has been prepared.
         */
        private bool IsPrepared;

        /**
         * The parameters to be used with the SQL query.
         */
        private List<Object>? Parameters;

        /**
         * The number of rows to fetch in each execution.
         */
        private int RowsToFetch = 100;

        /**
         * Whether the query is a CL command.
         */
        private bool IsCLCommand;

        /**
         * The current state of the query execution.
         */
        private QueryState State = QueryState.NOT_YET_RUN;

        /**
         * Whether the results should be terse.
         */
        private bool IsTerseResults;

        // Explicitly closed queries must not be registered again by an in-flight reply.
        private bool _isClosed;

        /**
         * Construct a new Query instance.
         * 
         * @param job   The SQL job that this query will be executed in.
         * @param query The SQL statement to be executed.
         * @param opts  The options for configuring a query.
         */
        public Query(SqlJob job, String query, QueryOptions opts)
        {
            this.Job = job;
            this.Sql = query;
            this.IsPrepared = opts.Parameters != null;
            this.Parameters = opts.Parameters;
            this.IsCLCommand = opts.IsClCommand;
            this.IsTerseResults = opts.IsTerseResults;

            Query.AddQuery(this);
        }

        /// <summary>Registers the query by instance unless it was explicitly closed.</summary>
        /// <param name="query">The query to register.</param>
        public static void AddQuery(Query query)
        {
            lock (GlobalQueryList)
            {
                if (!query._isClosed && !GlobalQueryList.Any(entry => ReferenceEquals(entry, query)))
                {
                    GlobalQueryList.Add(query);
                }
            }
        }

        /// <summary>Removes this instance from the process-wide registry.</summary>
        internal void RemoveFromGlobalQueryList()
        {
            lock (GlobalQueryList)
            {
                GlobalQueryList.RemoveAll(entry => ReferenceEquals(entry, this));
            }
        }

        /**
         * Get a Query instance by its correlation ID.
         * 
         * @param id The correlation ID of the query.
         * @return The corresponding Query instance or null if not found.
         */
        public static Query? ById(String? id)
        {
            lock (GlobalQueryList)
            {
                if (id == null || id.Equals(""))
                {
                    return null;
                }
                else
                {
                    return Query.GlobalQueryList.Find(x => (id.Equals(x.CorrelationId)));

                }
            }
        }
        /**
         * Get a list of open correlation IDs.
         * 
         * @return A list of correlation IDs for open queries.
         */
        public static List<String?> GetOpenIds()
        {
            return Query.GetOpenIds(null);
        }

        /**
         * Get a list of open correlation IDs for the specified job.
         * 
         * @param forJob The job to filter the queries by.
         * @return A list of correlation IDs for open queries.
         */
        public static List<String?> GetOpenIds(SqlJob? forJob)
        {
            lock (GlobalQueryList)
            {
                List<Query> matchingQueries = Query.GlobalQueryList.FindAll(
                                q => (forJob == null || (forJob.Equals(q.Job))) &&
                             (q.State == QueryState.NOT_YET_RUN
                                        || q.State == QueryState.RUN_MORE_DATA_AVAILABLE));
                List<String?> answer = matchingQueries.ConvertAll(q => q.CorrelationId);
                return answer;
            }
        }

        /// <summary>Removes completed and failed queries from the process-wide registry.</summary>
        public void Cleanup()
        {
            lock (GlobalQueryList)
            {
                GlobalQueryList.RemoveAll(query => query.State == QueryState.RUN_DONE || query.State == QueryState.ERROR);
            }
        }

        /**
         * Execute an SQL command and returns the result.
         * 
         * @param <T> The type of data to be returned.
         * @return A CompletableFuture that resolves to the query result.
         */
        public QueryResult Execute()
        {
            return this.Execute(100);
        }

        /// <summary>Executes this query and returns its first result. CL commands return their messages in one reply.</summary>
        /// <param name="rowsToFetch">The maximum number of SQL rows to request in the first page.</param>
        /// <returns>The first SQL result or the CL command result, including a failed CL result.</returns>
        /// <exception cref="SqlException">The server rejected the SQL statement.</exception>
        public QueryResult Execute(int rowsToFetch)
        {
            if (rowsToFetch <= 0)
            {
                throw new Exception("Rows to fetch must be greater than 0");
            }

            switch (State)
            {
                case QueryState.RUN_MORE_DATA_AVAILABLE:
                    throw new Exception("Statement has already been run");
                case QueryState.RUN_DONE:
                    throw new Exception("Statement has already been fully run");
            }

            object requestObject;
            if (IsCLCommand)
            {
                ClExecuteRequest clExecuteRequest = new ClExecuteRequest(SqlJob.GetNewUniqueId("clcommand"), "cl", IsTerseResults, Sql);
                requestObject = clExecuteRequest;
            }
            else
            {
                if (Parameters is null)
                {
                    SqlExecuteRequest request = new SqlExecuteRequest(SqlJob.GetNewUniqueId("query"),
                        IsPrepared ? "prepare_sql_execute" : "sql",
                        Sql, IsTerseResults, rowsToFetch);
                    requestObject = request;
                }
                else
                {
                    SqlExecuteWithParametersRequest request = new SqlExecuteWithParametersRequest(SqlJob.GetNewUniqueId("query"),
                        IsPrepared ? "prepare_sql_execute" : "sql",
                        Sql, IsTerseResults, rowsToFetch, Parameters);
                    requestObject = request;
                }
            }

            RowsToFetch = rowsToFetch;
            QueryResult queryResult = SendQueryRequest(requestObject, IsTerseResults);

            if (!queryResult.Success && !IsCLCommand)
            {
                String? error = queryResult.Error;
                if (error is null)
                {
                    error = "Failed to run query";
                }

                String? sqlState = queryResult.SqlState;
                int sqlCode = queryResult.SqlRc;

                throw new SqlException(error, sqlCode, sqlState);
            }

            CorrelationId = queryResult.Id;
            return queryResult;
        }

        /**
         * Fetch more rows from the currently running query.
         * 
         * @return A query result.
         */
        public QueryResult FetchMore()
        {
            return this.FetchMore(this.RowsToFetch);
        }

        /// <summary>Fetches the next page using the original SQL cursor ID.</summary>
        /// <param name="rowsToFetch">The maximum number of rows to request.</param>
        /// <returns>The next result page.</returns>
        /// <exception cref="SqlException">The server returned an SQL error while fetching rows.</exception>
        public QueryResult FetchMore(int rowsToFetch)
        {
            if (rowsToFetch <= 0)
            {
                throw new Exception("Rows to fetch must be greater than 0");
            }

            switch (State)
            {
                case QueryState.NOT_YET_RUN:
                    throw new Exception("Statement has not yet been run");
                case QueryState.RUN_DONE:
                    throw new Exception("Statement has already been fully run");
            }

            FetchMoreRequest fetchMoreRequest = new(SqlJob.GetNewUniqueId("fetchMore"),
                CorrelationId, "sqlmore", Sql, rowsToFetch);
            RowsToFetch = rowsToFetch;

            QueryResult queryResult = SendQueryRequest(fetchMoreRequest, false);

            if (!queryResult.Success)
            {
                String? error = queryResult.Error;
                if (error is not null)
                {
                    throw new SqlException(error, queryResult.SqlRc, queryResult.SqlState);
                }
                else
                {
                    throw new Exception("Failed to fetch more");
                }
            }

            return queryResult;
        }

        /// <summary>Unregisters the query and sends a close request for an unfinished query with a cursor ID.</summary>
        /// <returns>The close response, or <see langword="null"/> when no close request is sent.</returns>
        public String? Close()
        {
            lock (GlobalQueryList)
            {
                _isClosed = true;
                RemoveFromGlobalQueryList();
            }
            if (CorrelationId is not null && State != QueryState.RUN_DONE)
            {
                State = QueryState.RUN_DONE;
                CloseRequest closeRequest = new(SqlJob.GetNewUniqueId("sqlclose"),
                    CorrelationId, "sqlclose");

                string requestString = JsonSerializer.Serialize(closeRequest);

                if (Job is null)
                {
                    throw new Exception("Job is null");
                }
                string result = Job.Send(requestString);
                return result;

            }
            else if (CorrelationId is null)
            {
                State = QueryState.RUN_DONE;
                return null;
            }
            else
            {
                return null;
            }
        }

        /// <summary>Sends a query request and updates registry membership based on the outcome.</summary>
        /// <param name="request">The typed SQL, fetch, or CL request.</param>
        /// <param name="terseResults">Whether the reply contains positional rows.</param>
        /// <returns>The decoded query result.</returns>
        /// <exception cref="JsonException">The request or response could not be serialized or deserialized.</exception>
        private QueryResult SendQueryRequest(object request, bool terseResults)
        {
            try
            {
                if (Job is null)
                {
                    throw new Exception("Job is null");
                }
                string result = Job.Send(JsonSerializer.Serialize(request));
                QueryResult queryResult;
                if (terseResults)
                {
                    TerseQueryResult terseResult = JsonSerializer.Deserialize<TerseQueryResult>(result)
                        ?? throw new Exception("Null terse query result");
                    queryResult = terseResult.ConvertToQueryResult();
                }
                else
                {
                    queryResult = JsonSerializer.Deserialize<QueryResult>(result)
                        ?? throw new Exception("Null query result");
                }

                // CL replies contain all messages, including errors, and never allocate an SQL cursor.
                if (IsCLCommand)
                {
                    queryResult.IsDone = true;
                    State = QueryState.RUN_DONE;
                }
                else if (!queryResult.Success)
                {
                    State = QueryState.ERROR;
                }
                else if (queryResult.IsDone)
                {
                    State = QueryState.RUN_DONE;
                }
                else
                {
                    State = QueryState.RUN_MORE_DATA_AVAILABLE;
                    // A caller may retry a failed query. Keep its new cursor discoverable.
                    AddQuery(this);
                }

                return queryResult;
            }
            catch
            {
                State = QueryState.ERROR;
                throw;
            }
            finally
            {
                if (State == QueryState.RUN_DONE || State == QueryState.ERROR)
                {
                    RemoveFromGlobalQueryList();
                }
            }
        }

    }
}
