using System.Net;
using System.Net.Sockets;

namespace WorkerServiceHealthChecker;

public class WorkerServiceTcpListener : TcpListener
{
    public bool IsListening { get; private set; }

    public WorkerServiceTcpListener(IPAddress localaddr, int port)
        : base(localaddr, port)
    {
    }

    public new void Start()
    {
        base.Start();
        IsListening = true;
    }
    
    public new void Stop()
    {
        if (IsListening)
        {
            base.Stop();
        }
        
        IsListening = false;
    }
}