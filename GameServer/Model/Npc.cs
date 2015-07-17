using System;
using System.Collections.Generic;
using HerhangiOT.GameServer.Enums;

namespace HerhangiOT.GameServer.Model
{
    public class Npc : Creature
    {
        public static uint NpcAutoID = 0x80000000;
        
        protected string Name { get; set; }

        protected uint WalkTicks { get; set; }
        protected uint FocusCreature { get; set; }
        protected int MasterRadius { get; set; }
        protected Position MasterPosition { get; set; }

        protected SpeechBubbles SpeechBubble { get; set; }
        protected List<Player> ShopPlayerSet { get; set; }

        public event Action<Player> OnPlayerCloseChannel;
        public event Action<Player, int, ushort, byte, byte, bool, bool> OnPlayerTrade;
        public event Action<Player, int, int> OnPlayerEndTrade;
        #region Method Overrides
        public sealed override void SetID()
        {
            if (Id == 0)
                Id = NpcAutoID++;
        }
        public sealed override bool IsPushable()
        {
            return WalkTicks > 0;
        }

        public sealed override void AddList()
        {
            Game.AddNpc(this);
        }
        public sealed override void RemoveList()
        {
            Game.RemoveNpc(this);
        }

        public sealed override bool CanSee(Position pos)
        {
            if (pos.Z != GetPosition().Z)
                return false;
            return CanSee(GetPosition(), pos, 3, 3);
        }

        public sealed override string GetName()
        {
            return Name;
        }
        public sealed override string GetNameDescription()
        {
            return Name;
        }

        public sealed override CreatureTypes GetCreatureType()
        {
            return CreatureTypes.Npc;
        }
        public sealed override SpeechBubbles GetSpeechBubble()
        {
            return SpeechBubble;
        }
        public override string GetDescription(int lookDistance)
        {
            throw new NotImplementedException();
        }

        #endregion

        public static Npc CreateNpc(string name)
        {
            return null;//TODO: Program Here
        }
        public static bool Load()
        {
            return false; //TODO : Program Here
        }
        public static void Reload()
        {
            //TODO : Program Here
        }

        public void DoSay(string text)
        {
            Game.InternalCreatureSay(this, SpeakTypes.Say, text, false);
        }
        public void DoSayToPlayer(Player player, string text)
        {
            if (player != null)
            {
                player.SendCreatureSay(this, SpeakTypes.PrivateNp, text);
                player.OnCreatureSay(this, SpeakTypes.PrivateNp, text);
            }
        }

        public void DoMoveTo(Position target)
        {
	        Queue<Directions> listDir = new Queue<Directions>();

	        if (GetPathTo(target, ref listDir, 1, 1, true, true))
		        StartAutoWalk(listDir);
        }

        public void SetMasterPosition(Position pos, int radius = 1)
        {
            MasterPosition = pos;
            
            if (MasterRadius == -1)
                MasterRadius = radius;
        }

        public void FireOnPlayerCloseChannel(Player player)
        {
            if (OnPlayerCloseChannel != null)
                OnPlayerCloseChannel(player);
        }
        public void FireOnPlayerTrade(Player player, int callback, ushort itemId, byte count, byte amount, bool ignore = false, bool inBackpacks = false)
        {
            if (OnPlayerTrade != null)
                OnPlayerTrade(player, callback, itemId, count, amount, ignore, inBackpacks);
            
            player.SendSaleItemList();
        }
        public void FireOnPlayerEndTrade(Player player, int buyCallback, int sellCallback)
        {
            RemoveShopPlayer(player);

            if (OnPlayerEndTrade != null)
                OnPlayerEndTrade(player, buyCallback, sellCallback);
        }

        public void TurnToCreature(Creature creature)
        {
	        Position creaturePos = creature.GetPosition();
	        Position myPos = GetPosition();
	        int dx = Position.GetOffsetX(myPos, creaturePos);
	        int dy = Position.GetOffsetY(myPos, creaturePos);

	        float tan;
	        if (dx != 0)
		        tan = ((float)dy) / dx;
            else
		        tan = 10;

	        Directions dir;
	        if (Math.Abs(tan) < 1)
            {
		        if (dx > 0)
			        dir = Directions.West;
		        else
			        dir = Directions.East;
	        }
            else
            {
		        if (dy > 0)
			        dir = Directions.North;
		        else
			        dir = Directions.South;
	        }

	        Game.InternalCreatureTurn(this, dir);
        }
        public void SetCreatureFocus(Creature creature)
        {
            if (creature != null)
            {
                FocusCreature = creature.Id;
                TurnToCreature(creature);
            }
            else
            {
                FocusCreature = 0;
            }
        }

        protected void AddShopPlayer(Player player)
        {
	        ShopPlayerSet.Add(player);
        }
        protected void RemoveShopPlayer(Player player)
        {
	        ShopPlayerSet.Remove(player);
        }
        protected void CloseAllShopWindows()
        {
            while (ShopPlayerSet.Count > 0)
            {
                Player player = ShopPlayerSet[0];
                if (!player.CloseShopWindow())
                {
                    RemoveShopPlayer(player);
                }
            }
        }

        public override void OnCreatureAppear(Creature creature, bool isLogin)
        {
            base.OnCreatureAppear(creature, isLogin);

	        if (creature == this)
            {
		        if (WalkTicks > 0)
			        AddEventWalk();

                //if (m_npcEventHandler) { TODO: Scripting
                //    m_npcEventHandler->onCreatureAppear(creature);
                //}
	        }
            else if (creature is Player)
            {
                //if (m_npcEventHandler) { TODO: Scripting
                //    m_npcEventHandler->onCreatureAppear(creature);
                //}
	        }
        }
    }
}
