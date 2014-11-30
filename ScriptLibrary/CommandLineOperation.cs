using HerhangiOT.ServerLibrary;

namespace HerhangiOT.ScriptLibrary
{
    public abstract class CommandLineOperation
    {
        protected string Command;
        protected bool IsUsableInGameServer;
        protected bool IsUsableInLoginServer;

        public void InviteToGameServerCommandList(Server server)
        {
            if (!IsUsableInGameServer) return;

            if(server.CommandLineOperations.ContainsKey(Command))
                Logger.Log(LogLevels.Warning, "GameServer already contains command: "+Command+"!");
            else
                server.CommandLineOperations.Add(Command, Operation);
        }

        public void InviteToLoginServerCommandList(Server server)
        {
            if (!IsUsableInLoginServer) return;

            if (server.CommandLineOperations.ContainsKey(Command))
                Logger.Log(LogLevels.Warning, "LoginServer already contains command: " + Command + "!");
            else
                server.CommandLineOperations.Add(Command, Operation);
        }

        public abstract void Setup();
        public abstract void Operation();
    }
}
