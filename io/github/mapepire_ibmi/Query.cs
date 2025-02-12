using io.github.mapepire_ibmi.types;

namespace io.github.mapepire_ibmi {
public class Query {
    /**
     * A list of all global queries that are currently open.
     */
    private static List<Query> globalQueryList = [];

    /**
     * The SQL job that this query will be executed in.
     */
    private SqlJob? Job { get; set; }

    /**
     * The correlation ID associated with the query.
     */
    private String? CorrelationId;

    /**
     * The SQL statement to be executed.
     */
    private String? Sql;

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

    /**
     * Construct a new Query instance.
     * 
     * @param job   The SQL job that this query will be executed in.
     * @param query The SQL statement to be executed.
     * @param opts  The options for configuring a query.
     */
    public Query(SqlJob job, String query, QueryOptions opts) {
        this.Job = job;
        this.Sql = query;
        this.IsPrepared = opts.Parameters != null;
        this.Parameters = opts.Parameters;
        this.IsCLCommand = opts.IsClCommand;
        this.IsTerseResults = opts.IsTerseResults;

        Query.AddQuery(this);
    }

    public static void AddQuery(Query query) {
        lock(globalQueryList) {
          globalQueryList.Add(query);
        }
    }

        /**
         * Get a Query instance by its correlation ID.
         * 
         * @param id The correlation ID of the query.
         * @return The corresponding Query instance or null if not found.
         */
        public static Query? byId(String? id)
        {
            lock (globalQueryList)
            {
                if (id == null || id.Equals(""))
                {
                    return null;
                }
                else
                {
                    return Query.globalQueryList.Find(x => (id.Equals(x.CorrelationId)));

                }
            }
        }
        /**
         * Get a list of open correlation IDs.
         * 
         * @return A list of correlation IDs for open queries.
         */
        public static List<String?> getOpenIds() {
        return Query.getOpenIds(null);
    }

    /**
     * Get a list of open correlation IDs for the specified job.
     * 
     * @param forJob The job to filter the queries by.
     * @return A list of correlation IDs for open queries.
     */
    public static List<String?> getOpenIds(SqlJob? forJob) {
        lock(globalQueryList) {
                List<Query> matchingQueries = Query.globalQueryList.FindAll(
                                q => (forJob == null || (forJob.Equals(q.Job))) &&
                             (q.State == QueryState.NOT_YET_RUN
                                        || q.State == QueryState.RUN_MORE_DATA_AVAILABLE));
                List<String?> answer = matchingQueries.ConvertAll(q=>q.CorrelationId); 
                return answer;                         
        }
    }

    /**
     * Clean up queries that are done or are in error state from the global query
     * list.
     */
    public void cleanup()  {
         lock(globalQueryList) {
        /* TODO  .. Java code below */ 
        /* 
        List<CompletableFuture<Void>> futures = globalQueryList.stream()
                .filter(query -> query.getState() == QueryState.RUN_DONE || query.getState() == QueryState.ERROR)
                .map(query -> CompletableFuture.runAsync(() -> {
                    try {
                        query.close();
                    } catch (Exception e) {
                        e.printStackTrace();
                    }
                }))
                .collect(Collectors.toList());

        return CompletableFuture.allOf(futures.toArray(new CompletableFuture[0]))
                .thenRun(() -> {
                    globalQueryList = globalQueryList.stream()
                            .filter(q -> q.getState() != QueryState.RUN_DONE)
                            .collect(Collectors.toList());
                });
                */ 
         }
    }

    /**
     * Execute an SQL command and returns the result.
     * 
     * @param <T> The type of data to be returned.
     * @return A CompletableFuture that resolves to the query result.
     */
    public QueryResult<Object> Execute()  {
        return this.Execute(100);
    }

    /**
     * Execute an SQL command and returns the result.
     * 
     * @param <T>         The type of data to be returned.
     * @param rowsToFetch The number of rows to fetch.
     * @return A CompletableFuture that resolves to the query result.
     */
    public QueryResult<Object> Execute(int rowsToFetch) {
        if (rowsToFetch <= 0) {
            throw new Exception("Rows to fetch must be greater than 0");
        }

        switch (this.State) {
            case QueryState.RUN_MORE_DATA_AVAILABLE:
                throw new Exception("Statement has already been run");
            case QueryState.RUN_DONE:
                throw new Exception("Statement has already been fully run");
            
        }

/* TODO:  Fix this code.  */ 
/* 
        ObjectMapper objectMapper = SingletonObjectMapper.getInstance();
        ObjectNode executeRequest = objectMapper.createObjectNode();
        if (this.isCLCommand) {
            executeRequest.put("id", SqlJob.getNewUniqueId("clcommand"));
            executeRequest.put("type", "cl");
            executeRequest.put("terse", this.isTerseResults);
            executeRequest.put("cmd", this.sql);
        } else {
            executeRequest.put("id", SqlJob.getNewUniqueId("query"));
            executeRequest.put("type", this.isPrepared ? "prepare_sql_execute" : "sql");
            executeRequest.put("sql", this.sql);
            executeRequest.put("terse", this.isTerseResults);
            executeRequest.put("rows", rowsToFetch);
            if (this.parameters != null) {
                JsonNode parameters = objectMapper.valueToTree(this.parameters);
                executeRequest.set("parameters", parameters);
            }
        }
*/
/*
        this.rowsToFetch = rowsToFetch;
*/

        return null; 
        
        /* job.send(objectMapper.writeValueAsString(executeRequest)) 
        
                .thenApply(result -> {
                    QueryResult<T> queryResult;
                    try {
                        queryResult = objectMapper.readValue(result, QueryResult.class);
                    } catch (Exception e) {
                        throw new CompletionException(e);
                    }

                    this.state = queryResult.getIsDone() ? QueryState.RUN_DONE
                            : QueryState.RUN_MORE_DATA_AVAILABLE;

                    if (!queryResult.getSuccess() && !this.isCLCommand) {
                        this.state = QueryState.ERROR;

                        List<String> errorList = new ArrayList<>();
                        String error = queryResult.getError();
                        if (error != null) {
                            errorList.add(error);
                        }
                        String sqlState = queryResult.getSqlState();
                        if (sqlState != null) {
                            errorList.add(sqlState);
                        }
                        String sqlRc = String.valueOf(queryResult.getSqlRc());
                        if (sqlRc != null) {
                            errorList.add(sqlRc);
                        }
                        if (errorList.isEmpty()) {
                            errorList.add("Failed to run query");
                        }

                        throw new CompletionException(
                                new SQLException(String.join(", ", errorList), queryResult.getSqlState()));
                    }

                    this.correlationId = queryResult.getId();
                    return queryResult;
                });
               */
               return null; 
    }

    /**
     * Fetch more rows from the currently running query.
     * 
     * @return A CompletableFuture that resolves to the query result.
     */
    public QueryResult<Object> FetchMore()  {
        return this.FetchMore(this.RowsToFetch);
    }

    /**
     * Fetch more rows from the currently running query.
     * 
     * @param rowsToFetch The number of additional rows to fetch.
     * @return A CompletableFuture that resolves to the query result.
     */
    public QueryResult<Object> FetchMore(int rowsToFetch)  {
        if (rowsToFetch <= 0) {
            throw new Exception("Rows to fetch must be greater than 0");
        }

        switch (this.State) {
            case QueryState.NOT_YET_RUN:
                throw new Exception("Statement has not yet been run");
            case QueryState.RUN_DONE:
                throw new Exception("Statement has already been fully run");
        }
/*
        ObjectMapper objectMapper = SingletonObjectMapper.getInstance();
        ObjectNode fetchMoreRequest = objectMapper.createObjectNode();
        fetchMoreRequest.put("id", SqlJob.getNewUniqueId("fetchMore"));
        fetchMoreRequest.put("cont_id", this.correlationId);
        fetchMoreRequest.put("type", "sqlmore");
        fetchMoreRequest.put("sql", this.sql);
        fetchMoreRequest.put("rows", rowsToFetch);

        this.rowsToFetch = rowsToFetch;

        return job.send(objectMapper.writeValueAsString(fetchMoreRequest))
                .thenApply(result -> {
                    QueryResult<T> queryResult;
                    try {
                        queryResult = objectMapper.readValue(result, QueryResult.class);
                    } catch (Exception e) {
                        throw new CompletionException(e);
                    }

                    this.state = queryResult.getIsDone() ? QueryState.RUN_DONE
                            : QueryState.RUN_MORE_DATA_AVAILABLE;

                    if (!queryResult.getSuccess()) {
                        this.state = QueryState.ERROR;

                        String error = queryResult.getError();
                        if (error != null) {
                            throw new CompletionException(
                                    new SQLException(error.toString(), queryResult.getSqlState()));
                        } else {
                            throw new CompletionException(new UnknownServerException("Failed to fetch more"));
                        }
                    }

                    return queryResult;
                });
                */ 

                return null; 
    }

    /**
     * Close the query.
     * 
     * @return A CompletableFuture that resolves when the query is closed.
     */
    public String close()  {
        if (CorrelationId != null && State != QueryState.RUN_DONE) {
            State = QueryState.RUN_DONE;
/*
            ObjectMapper objectMapper = SingletonObjectMapper.getInstance();
            ObjectNode closeRequest = objectMapper.createObjectNode();
            closeRequest.put("id", SqlJob.getNewUniqueId("sqlclose"));
            closeRequest.put("cont_id", correlationId);
            closeRequest.put("type", "sqlclose");

            return job.send(objectMapper.writeValueAsString(closeRequest));
            */
        } else if (CorrelationId == null) {
            State = QueryState.RUN_DONE;
            return null;
        } else {
            return null;
        }
        return null; 
    }

}
}
