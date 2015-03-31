namespace HerhangiOT.ScriptLibrary
{
    public abstract class CommandLineOperation
    {
        public string Command { get; protected set; }

        public abstract void Setup();
        public virtual void Destroy() { }
        public abstract void Operation(string[] args);
    }
}
