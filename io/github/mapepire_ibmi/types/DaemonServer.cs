namespace io.github.mapepire_ibmi.types {

public class DaemonServer {
  public String? Host { get; set;}
  public int Port  { get; set;} = 8076;

  public String? User {get; set;} = null ;

  public String? Password {get; set;}= null ; 

  public bool RejectUnauthorized {get; set;} = true ;

   public String? Ca  {get; set;} = null;


    public DaemonServer() {

    }
    public DaemonServer(String host, int port, String user, String password) {
        this.Host = host;
        this.Port = port;
        this.User = user;
        this.Password = password;
    }

   public DaemonServer(String host, int port, String user, String password, bool rejectUnauthorized) {
        this.Host = host;
        this.Port = port;
        this.User = user;
        this.Password = password;
        this.RejectUnauthorized = rejectUnauthorized;
    }

    public DaemonServer(string host, int port, string user, string password, bool rejectUnauthorized,
            string? ca) {
        this.Host = host;
        this.Port = port;
        this.User = user;
        this.Password = password;
        this.RejectUnauthorized = rejectUnauthorized;
        this.Ca = ca;
    }



}






}