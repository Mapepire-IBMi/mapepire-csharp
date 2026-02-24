using System;
using io.github.mapepire_ibmi.types;

namespace io.github.mapepire_ibmi { 

public class InteractiveClient {

	private static DaemonServer? daemonServer;
    private static SqlJob? job; 
	public static void Main(string[] args) {

		Console.WriteLine("Running interactive MapepireClient");
		while (ProcessLine());
	}

	private static bool ProcessLine() {
		string? line = null;
		try {
		    line = Console.ReadLine();
		    if (line == null) return false;

		    string[] lineElements = splitLine(line);
		    if (lineElements.Length > 0) {
                string command = lineElements[0].ToLowerInvariant(); 
			    if ("connect".Equals(command)) {
				    if (lineElements.Length == 2) {
					    connect(lineElements[1]);
				    } else {
					    connect(lineElements);
				    }
			    } else if ("runquery".Equals(command)) {
				    RunQuery(line);
			    } else if ("help".Equals(command)) {
				    ProcessHelp(); 
			    } else if ("exit".Equals(command)) {
				    return false;
			    } else {
				    Console.WriteLine("Did not recognize command: " + line);
				    ProcessHelp();
			    }
		    }
		} catch (Exception e) { 
			Console.WriteLine("Error processing "+line); 
            Console.WriteLine(e.ToString());
			Console.WriteLine(e.StackTrace); 
		}
		return true;
	}

	private static void RunQuery(String Line)  {
        bool showTypes = false; 

        string? envVar = Environment.GetEnvironmentVariable("MAPEPIRE_SHOW_TYPES");
		if (envVar != null) showTypes=true; 

		int runQueryIndex = Line.ToUpper().IndexOf("RUNQUERY"); 

		Line = Line.Substring(runQueryIndex+8);
		// Initialize and execute query
        
        if (job == null ) { 
            Console.WriteLine("Job is null"); return; 
        }
		
		Query query = job.Query(Line);
		QueryResult result = query.Execute();

		String? errorString = result.Error;
		if (errorString != null) { 
			Console.WriteLine("Query returned errorString:"+errorString); 
		} else { 
			QueryMetadata? metadata = result.Metadata ?? throw new Exception("NULL metadata");
                
            List<Dictionary<String,Object>>? data = result.Data;
			if (data == null) throw new Exception("NULL data"); 
			Console.WriteLine(" rows returned = "+data.Count+" isDone="+result.IsDone);
            List<ColumnMetadata>? columns = metadata.Columns ?? throw new Exception("NULL column");
                
			columns.ForEach(col=>Console.Write(col.Name+" ")); 
			Console.WriteLine(); 
			

			List<Dictionary<String,Object>>.Enumerator enumerator = data.GetEnumerator(); 
			while (enumerator.MoveNext()) { 
				Dictionary<String, Object> hashmap = (Dictionary <String,Object>) enumerator.Current; 
				columns.ForEach(col=>Console.Write(hashmap.GetValueOrDefault(col.Name ?? throw new Exception("Null column"))+" "));
                Console.WriteLine(); 
				if (showTypes) {
				   List<ColumnMetadata>.Enumerator columnEnumerator = columns.GetEnumerator();
					while (columnEnumerator.MoveNext()) {
						Object? value = hashmap.GetValueOrDefault(columnEnumerator.Current.Name);
						if (value == null) { 
                           Console.Write("null "); 
						} else {
							Console.Write(value.GetType().ToString()+" "); 
						}
					}
                    Console.WriteLine(); 
				}
			}
			
		}
		// Close query and job
		query.Close();

		
		
	}

	private static void ProcessHelp() {
		Console.WriteLine("Possible commands:");
		Console.WriteLine("connect configFile"); 
		Console.WriteLine("connect host port user password validateCA CA");
		Console.WriteLine("runQuery QUERY");
		Console.WriteLine("help"); 
		Console.WriteLine("exit"); 
		
	}

	private static void connect(string configFilePath) {
		string host = "";
		int port = 8076;
		string user = "";
		string password = "";
		bool rejectUnauthorized = true;
		string ca = "";
		try {
			string[] lines = File.ReadAllLines(configFilePath);
			
			// Get the position of the = sign within each line
			var pairs = lines.Select(l => new { Line = l, Pos = l.IndexOf("=") });

			// Build a dictionary of key/value pairs by splitting the string at the = sign
			Dictionary<string, string> dictionary = pairs.ToDictionary(
				p => p.Line.Substring(0, p.Pos).Trim(), 
				p => p.Line.Substring(p.Pos + 1).Trim());

			foreach (KeyValuePair<string, string> entry in dictionary) {
				switch (entry.Key.ToLower()) {
					case "host":
						host = entry.Value;
						break;
					case "port":
						port = Int32.Parse(entry.Value);
						break;
					case "user":
						user = entry.Value;
						break;
					case "password":
						password = entry.Value;
						break;
					case "rejectunauthorized":
						rejectUnauthorized = Boolean.Parse(entry.Value);
						break;
					case "ca":
						ca = entry.Value;
						break;
					default:
					    Console.WriteLine("Invalid parameter in configuration file: " + entry.Key);
						break;
				}
			}

		}
		catch (Exception ex) {
			if (ex is FileNotFoundException || ex is DirectoryNotFoundException) {
				Console.WriteLine("Configuration file is not found: " + configFilePath);
			}
			else {
				Console.WriteLine("Exception thrown when reading configuration file");
				Console.WriteLine("Exception message: " + ex.Message);
			}
			return;
		}

		connect(host, port, user, password, rejectUnauthorized, ca);

	}

	private static void connect(String host, int port, String user, String password, 
		bool rejectUnauthorized, String ca) {

		if (host == "" || user == "" || password == "") {
			if (host == "") {
				Console.WriteLine("Missing parameter: host");
			}
			else if (user == "") {
				Console.WriteLine("Missing parameter: user");
			}
			else if (password == "") {
				Console.WriteLine("Missing parameter: password");
			}
			return;
		}

		DaemonServer newDaemonServer = new DaemonServer(host, port, user, password, rejectUnauthorized,
			ca != "" ? ca : null);
		string successString  = "connection created using (" + host + "," + port + "," + user + ",*******,"
					+ rejectUnauthorized + "," + ca + ")";
	    daemonServer = newDaemonServer;
		SqlJob newJob = new SqlJob(); 
		ConnectionResult? cr = newJob.Connect(daemonServer);
		Console.WriteLine(successString);
        Console.WriteLine("JOB ="+newJob.Id);

		if (job != null) { 
			job.Close(); 
		}
		job = newJob; 

	}

	private static void connect(string[] lineElements) {

		int elementCount = lineElements.Length;
		string host  = "";
		int port = 8076;
		string user = "";
		string password = "";
		bool rejectUnauthorized = true;
		string ca = "";

		if (elementCount > 1)
			host = lineElements[1];
		if (elementCount > 2)
			port = Int32.Parse(lineElements[2]);
		if (elementCount > 3)
			user = lineElements[3];
		if (elementCount > 4)
			password = lineElements[4];
		if (elementCount > 5)
			rejectUnauthorized = Boolean.Parse(lineElements[5]);
		if (elementCount > 6)
			ca = lineElements[6];
		
		connect(host, port, user, password, rejectUnauthorized, ca);
	}

	private static string[] splitLine(String line) {
		return line.Split(" ");
	}

}
}
