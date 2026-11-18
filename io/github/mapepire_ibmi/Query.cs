using System.Data.SqlTypes;
using System.Text.Json;
using io.github.mapepire_ibmi.types;


namespace io.github.mapepire_ibmi
{
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
        private bool IsClosed;

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

        public static void AddQuery(Query query)
        {
            lock (GlobalQueryList)
            {
                if (!query.IsClosed && !GlobalQueryList.Any(entry => ReferenceEquals(entry, query)))
                {
                    GlobalQueryList.Add(query);
                }
            }
        }

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

        /**
         * Clean up queries that are done or are in error state from the global query
         * list.
         */
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

        /**
         * Execute an SQL command and returns the result.
         * 
         * @param <T>         The type of data to be returned.
         * @param rowsToFetch The number of rows to fetch.
         * @return A CompletableFuture that resolves to the query result.
         */
        public QueryResult Execute(int rowsToFetch)
        {
            if (rowsToFetch <= 0)
            {
                throw new Exception("Rows to fetch must be greater than 0");
            }

            switch (this.State)
            {
                case QueryState.RUN_MORE_DATA_AVAILABLE:
                    throw new Exception("Statement has already been run");
                case QueryState.RUN_DONE:
                    throw new Exception("Statement has already been fully run");

            }

            object requestObject;
            if (this.IsCLCommand)
            {
                ClExecuteRequest clExecuteRequest = new ClExecuteRequest(SqlJob.GetNewUniqueId("clcommand"), "cl", this.IsTerseResults, this.Sql);
                requestObject = clExecuteRequest;
            }
            else
            {
                if (Parameters == null)
                {
                    SqlExecuteRequest request = new SqlExecuteRequest(SqlJob.GetNewUniqueId("query"),
                    this.IsPrepared ? "prepare_sql_execute" : "sql",
                    this.Sql, this.IsTerseResults, rowsToFetch);
                    requestObject = request;
                }
                else
                {
                    SqlExecuteWithParametersRequest request = new SqlExecuteWithParametersRequest(SqlJob.GetNewUniqueId("query"),
                               this.IsPrepared ? "prepare_sql_execute" : "sql",
                               this.Sql, this.IsTerseResults, rowsToFetch, this.Parameters);
                    requestObject = request;
                }
            }


            this.RowsToFetch = rowsToFetch;
            QueryResult queryResult = SendQueryRequest(requestObject, this.IsTerseResults);

            if (!queryResult.Success && !this.IsCLCommand)
            {
                this.State = QueryState.ERROR;

                List<String> errorList = [];
                String? error = queryResult.Error;
                if (error == null)
                {
                    error = "Failed to run query";
                }
                String? sqlState = queryResult.SqlState;
                int sqlCode = queryResult.SqlRc;


                throw new SqlException(error, sqlCode, sqlState);
            }

            this.CorrelationId = queryResult.Id;
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

        /**
         * Fetch more rows from the currently running query.
         * 
         * @param rowsToFetch The number of additional rows to fetch.
         * @return A query result.
         */
        public QueryResult FetchMore(int rowsToFetch)
        {
            if (rowsToFetch <= 0)
            {
                throw new Exception("Rows to fetch must be greater than 0");
            }

            switch (this.State)
            {
                case QueryState.NOT_YET_RUN:
                    throw new Exception("Statement has not yet been run");
                case QueryState.RUN_DONE:
                    throw new Exception("Statement has already been fully run");
            }

            FetchMoreRequest fetchMoreRequest = new(SqlJob.GetNewUniqueId("fetchMore"),
            this.CorrelationId, "sqlmore", this.Sql, rowsToFetch);
            this.RowsToFetch = rowsToFetch;

            QueryResult queryResult = SendQueryRequest(fetchMoreRequest, false);

            if (!queryResult.Success)
            {
                this.State = QueryState.ERROR;

                String? error = queryResult.Error;
                if (error != null)
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

        /**
         * Close the query.
         * 
         * @return The results of closing the query. 
         */
        public String Close()
        {
            lock (GlobalQueryList)
            {
                this.IsClosed = true;
                RemoveFromGlobalQueryList();
            }
            if (CorrelationId != null && State != QueryState.RUN_DONE)
            {
                State = QueryState.RUN_DONE;
                CloseRequest closeRequest = new(SqlJob.GetNewUniqueId("sqlclose"),
        this.CorrelationId, "sqlclose");

                string requestString = JsonSerializer.Serialize(closeRequest);

                if (Job == null) throw new Exception("Job is null");
                string result = Job.Send(requestString);
                return result;


            }
            else if (CorrelationId == null)
            {
                State = QueryState.RUN_DONE;
                return null;
            }
            else
            {
                return null;
            }
        }

        private QueryResult SendQueryRequest(object request, bool terseResults)
        {
            try
            {
                if (Job == null) throw new Exception("Job is null");
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

                // CL replies contain all messages; newer servers omit is_done for them.
                if (this.IsCLCommand)
                {
                    queryResult.IsDone = true;
                }

                this.State = !queryResult.Success ? QueryState.ERROR
                    : queryResult.IsDone ? QueryState.RUN_DONE : QueryState.RUN_MORE_DATA_AVAILABLE;
                if (this.State == QueryState.RUN_MORE_DATA_AVAILABLE)
                {
                    // A caller may retry a failed query; keep its new cursor discoverable.
                    Query.AddQuery(this);
                }

                return queryResult;
            }
            catch
            {
                this.State = QueryState.ERROR;
                throw;
            }
            finally
            {
                if (this.State == QueryState.RUN_DONE || this.State == QueryState.ERROR)
                {
                    RemoveFromGlobalQueryList();
                }
            }
        }

    }
}
