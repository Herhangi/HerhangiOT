using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HerhangiOT.ScriptLibrary;
using HerhangiOT.ServerLibrary;

namespace HerhangiOT.GameServer
{
    public abstract class Vocation
    {
        public static Dictionary<ushort, Vocation> Vocations = new Dictionary<ushort, Vocation>();

        public Dictionary<uint, ulong> ManaReqCache { get; private set; }
        public Dictionary<uint, uint>[] SkillReqCache { get; private set; }

		public string name;
		protected string description;

        protected float[] skillMultipliers;//[SKILL_LAST + 1];
		protected float manaMultiplier;

		protected uint gainHealthTicks;
		protected uint gainHealthAmount;
		protected uint gainManaTicks;
		protected uint gainManaAmount;
		protected uint gainCap;
		protected uint gainMana;
		protected uint gainHP;
		protected uint fromVocation;
		protected uint attackSpeed;
		protected uint baseSpeed;
		protected ushort id;
        
		protected ushort gainSoulTicks;
		protected ushort soulMax;
        
		protected byte clientId;

        protected static uint[] skillBase;//[SKILL_LAST + 1];

        private void AddToList()
        {
            Vocations.Add(id, this);

            ManaReqCache = new Dictionary<uint, ulong>();
            SkillReqCache = new Dictionary<uint, uint>[6];
        }

        public static bool Load()
        {
            Logger.LogOperationStart("Loading vocations");
            VocationNone none = new VocationNone();
            none.AddToList();

            Assembly vocationsAssembly;
            List<string> externalAssemblies = new List<string>();
            externalAssemblies.Add(Assembly.GetExecutingAssembly().Location);
            bool redFromCache;

            if(!ScriptManager.CompileCsScripts("Data/Vocations", "Vocations.*.dll", externalAssemblies, out vocationsAssembly, out redFromCache))
                return false;

            try
            {
                foreach (Type vocation in vocationsAssembly.GetTypes())
                {
                    if (vocation.BaseType == typeof(Vocation))
                    {
                        Vocation voc = (Vocation)Activator.CreateInstance(vocation);
                        voc.AddToList();
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogOperationFailed(e.ToString());
                return false;
            }

            if (redFromCache)
                Logger.LogOperationCached();
            else
                Logger.LogOperationDone();
            return true;
        }
    }
}
