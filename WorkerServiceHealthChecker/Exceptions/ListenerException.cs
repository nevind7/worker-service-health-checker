namespace WorkerServiceHealthChecker.Exceptions;

[Serializable]
public class  ListenerException : Exception
{
    public  ListenerException() { }
    public  ListenerException(string message) : base(message) { }
    public ListenerException(string message, Exception inner) : base(message, inner) { }
}