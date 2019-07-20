namespace ChatService
{
    /// <summary>
    /// this class just declare common objects, but they must be initialized in the child class (can have different behaviours)
    /// </summary>
    public abstract class ChatNetworkObject
    {
        protected ChatTcpLayer tcpLayer;
        protected TaskScheduler scheduler;

        public CommandExecutor CommandExecutor { get; protected set; }
    }
}
