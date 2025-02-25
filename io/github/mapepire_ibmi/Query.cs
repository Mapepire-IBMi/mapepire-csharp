using System.Text.Json;
using io.github.mapepire_ibmi.types;
using Microsoft.AspNetCore.Mvc.Filters;

namespace io.github.mapepire_ibmi {
public class Query {
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
        lock(GlobalQueryList) {
          GlobalQueryList.Add(query);
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
        public static List<String?> GetOpenIds() {
        return Query.GetOpenIds(null);
    }

    /**
     * Get a list of open correlation IDs for the specified job.
     * 
     * @param forJob The job to filter the queries by.
     * @return A list of correlation IDs for open queries.
     */
    public static List<String?> GetOpenIds(SqlJob? forJob) {
        lock(GlobalQueryList) {
                List<Query> matchingQueries = Query.GlobalQueryList.FindAll(
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
    public void Cleanup()  {
         lock(GlobalQueryList) {
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
    public QueryResult Execute()  {
        return this.Execute(100);
    }

    /**
     * Execute an SQL command and returns the result.
     * 
     * @param <T>         The type of data to be returned.
     * @param rowsToFetch The number of rows to fetch.
     * @return A CompletableFuture that resolves to the query result.
     */
    public QueryResult Execute(int rowsToFetch) {
        if (rowsToFetch <= 0) {
            throw new Exception("Rows to fetch must be greater than 0");
        }

        switch (this.State) {
            case QueryState.RUN_MORE_DATA_AVAILABLE:
                throw new Exception("Statement has already been run");
            case QueryState.RUN_DONE:
                throw new Exception("Statement has already been fully run");
            
        }

        string requestString; 
        if (this.IsCLCommand) {
            ClExecuteRequest clExecuteRequest = new ClExecuteRequest(SqlJob.GetNewUniqueId("clcommand"),"cl",this.IsTerseResults,this.Sql);
            requestString = JsonSerializer.Serialize(clExecuteRequest); 
         } else {
            if (Parameters == null) { 
               SqlExecuteRequest request = new SqlExecuteRequest(SqlJob.GetNewUniqueId("query"),
               this.IsPrepared ? "prepare_sql_execute" : "sql", 
               this.Sql, this.IsTerseResults,rowsToFetch); 
               requestString = JsonSerializer.Serialize(request); 
            } else {
    SqlExecuteWithParametersRequest request = new SqlExecuteWithParametersRequest(SqlJob.GetNewUniqueId("query"),
               this.IsPrepared ? "prepare_sql_execute" : "sql", 
               this.Sql, this.IsTerseResults,rowsToFetch, this.Parameters); 
    requestString = JsonSerializer.Serialize(request); 
            }
        }


        this.RowsToFetch = rowsToFetch;
        if (Job == null) throw new Exception("Job is null"); 
        string result = Job.Send(requestString);
        QueryResult? queryResult = JsonSerializer.Deserialize<QueryResult>(result);
        if (queryResult == null) throw new Exception("Null query result"); 

        this.State = queryResult.IsDone ? QueryState.RUN_DONE
                                : QueryState.RUN_MORE_DATA_AVAILABLE;

        if (!queryResult.Success && !this.IsCLCommand) {
                            this.State = QueryState.ERROR;

                            List<String> errorList = [];
                            String? error = queryResult.Error;
                            if (error != null) {
                                errorList.Add(error);
                            }
                            String? sqlState = queryResult.SqlState;
                            if (sqlState != null) {
                                errorList.Add(sqlState);
                            }
                            String sqlRc = ""+queryResult.SqlRc;
                                errorList.Add(sqlRc);
                            
                            if (errorList.Count == 0) {
                                errorList.Add("Failed to run query");
                            }

                            throw new Exception(String.Join(",", errorList)+ queryResult.SqlState);
                        }

                        this.CorrelationId = queryResult.Id;
                        return queryResult;
    
                   
    }

    /**
     * Fetch more rows from the currently running query.
     * 
     * @return A query result.
     */
    public QueryResult FetchMore()  {
        return this.FetchMore(this.RowsToFetch);
    }

    /**
     * Fetch more rows from the currently running query.
     * 
     * @param rowsToFetch The number of additional rows to fetch.
     * @return A query result.
     */
    public QueryResult FetchMore(int rowsToFetch)  {
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
     * @return The results of closing the query. 
     */
    public String Close()  {
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
