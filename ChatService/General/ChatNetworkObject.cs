using ChatService.Log;
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

        public ChatNetworkObject()
        {
            CommandExecutor = new CommandExecutor();

            //get all available commands
            CommandExecutor.Add("help", new string[] { }, "Shows you possible options", (args) =>
            {

                ChatLog.Info("Available commands:");

                if(args.Length == 0)
                {
                    foreach (var c in CommandExecutor.GetAllAvailableCommands())
                        ChatLog.Info("-->" + c);
                }
                else
                {
                    ChatLog.Info("Wrong argument. Only accepted verbose");
                }
            });
        }
    }
}
