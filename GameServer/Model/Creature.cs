using System;
using System.Collections.Generic;
using HerhangiOT.GameServer.Enums;
using HerhangiOT.ServerLibrary.Threading;
using HerhangiOT.ServerLibrary.Utility;

namespace HerhangiOT.GameServer.Model
{
    public abstract class Creature : Thing
    {
        public const double SpeedA = 857.36;
        public const double SpeedB = 261.29;
        public const double SpeedC = -4795.01;
        public const int MapWalkWidth = Map.MaxViewportX*2 + 1;
        public const int MapWalkHeight = Map.MaxViewportY*2 + 1;
        public const int MaxWalkCacheWidth = (MapWalkWidth - 1)/2;
        public const int MaxWalkCacheHeight = (MapWalkHeight - 1)/2;

        public uint Id { get; protected set; }
        public Tile Parent { get; set; }
        public Position Position { get; set; }

        public virtual ushort Health { get; protected set; }
        public virtual ushort HealthMax { get; protected set; }
        public Directions Direction { get; set; }

        public Outfit CurrentOutfit { get; protected set; }
        public Outfit DefaultOutfit { get; protected set; }
        public LightInfo InternalLight { get; protected set; }

        public int BaseSpeed { get; protected set; }
        public int VarSpeed { get; protected set; }
        public int Speed { get { return BaseSpeed + VarSpeed; } }
        public virtual int StepSpeed { get { return Speed; } }

        public Creature AttackedCreature { get; protected set; }
        public Creature Master { get; protected set; }
        public Creature FollowCreature { get; protected set; }
        public List<Creature> Summons { get; protected set; }

        public bool IsHealthHidden { get; protected set; }
        public bool IsInternalRemoved { get; protected set; }
        public bool IsMapLoaded { get; protected set; }
        public bool ForceUpdateFollowPath { get; protected set; }
        public bool IsUpdatingPath { get; protected set; }
        public bool HasFollowPath { get; protected set; }
		public bool[][] LocalMapCache { get; protected set; }

        public long LastStep { get; protected set; }
        public uint LastStepCost { get; protected set; }
        public uint EventWalk { get; protected set; }
        public bool CancelNextWalk { get; protected set; }
        public Queue<Directions> WalkDirections { get; protected set; } 

        public Creature()
        {
            InternalLight = new LightInfo {Color = 0, Level = 0};
            LocalMapCache = new bool[MapWalkHeight][];
            for(int i = 0; i < MapWalkHeight; i++)
                LocalMapCache[i] = new bool[MapWalkWidth];
            Summons = new List<Creature>();
        }

        public virtual bool CanSeeInvisibility()
        {
            return false;
        }

        public virtual bool IsInGhostMode()
        {
            return false;
        }

        public bool IsInvisible()
        {
            return false; //TODO: FILL METHOD
        }
        
		public virtual SpeechBubbles GetSpeechBubble()
        {
			return SpeechBubbles.None;
		}

        public abstract CreatureTypes GetCreatureType();
        public abstract string GetName();
        public abstract void AddList();
        public abstract void SetID();

        public virtual bool HasExtraSwing()
        {
            return false;
        }

		public virtual void DoAttacking(uint interval) { }
        public virtual void OnWalkComplete() { }
        public virtual void OnWalkAborted() { }
        public virtual void OnWalk(ref Directions dir)
        {
            //if (hasCondition(CONDITION_DRUNK)) { //TODO: Drunk
            //    uint32_t r = uniform_random(0, 20);
            //    if (r <= DIRECTION_DIAGONAL_MASK) {
            //        if (r < DIRECTION_DIAGONAL_MASK) {
            //            dir = static_cast<Direction>(r);
            //        }
            //        g_game.internalCreatureSay(this, TALKTYPE_MONSTER_SAY, "Hicks!", false);
            //    }
            //}
        }

        public void StartAutoWalk(Directions direction)
        {
            WalkDirections = new Queue<Directions>();
            WalkDirections.Enqueue(direction);

            AddEventWalk(true);
        }

        public void StartAutoWalk(Queue<Directions> directions)
        {
	        WalkDirections = directions;

	        AddEventWalk(directions.Count == 1);
        }

        public virtual void OnWalk()
        {
	        if (GetWalkDelay() <= 0)
            {
		        Directions dir = Directions.None;
                CylinderFlags flags = CylinderFlags.IgnoreFieldDamage;

		        if (GetNextStep(ref dir, ref flags))
                {
			        ReturnTypes ret = Game.Instance.InternalMoveCreature(this, dir, flags);
			        if (ret != ReturnTypes.NoError)
			        {
			            Player player = this as Player;
				        if (player != null)
                        {
					        player.SendCancelMessage(ret);
					        player.SendCancelWalk();
				        }

				        ForceUpdateFollowPath = true;
			        }
		        }
                else
                {
			        if (WalkDirections.Count == 0)
				        OnWalkComplete();

			        StopEventWalk();
		        }
	        }

	        if (CancelNextWalk)
            {
		        WalkDirections.Clear();
		        OnWalkAborted();
		        CancelNextWalk = false;
	        }

	        if (EventWalk != 0)
            {
		        EventWalk = 0;
		        AddEventWalk();
	        }
        }
        public void OnAttacking(uint interval)
        {
	        if (AttackedCreature == null)
		        return;

	        OnAttacked();
	        AttackedCreature.OnAttacked();

	        if (Map.Instance.IsSightClear(GetPosition(), AttackedCreature.GetPosition(), true))
            {
		        DoAttacking(interval);
	        }
        }

        public void OnAddTileItem(Tile tile, Position pos)
        {
	        if (IsMapLoaded && pos.Z == GetPosition().Z) {
		        UpdateTileCache(tile, pos);
	        }
        }
        public virtual void OnUpdateTileItem(Tile tile, Position pos, Item oldItem, ItemTemplate oldType, Item newItem, ItemTemplate newType)
        {
            if (!IsMapLoaded)
                return;

            if (oldType.DoesBlockSolid || oldType.DoesBlockPathFinding || newType.DoesBlockPathFinding || newType.DoesBlockSolid)
            {
                if (pos.Z == GetPosition().Z)
                    UpdateTileCache(tile, pos);
            }
        }
        public virtual void OnRemoveTileItem(Tile tile, Position pos, ItemTemplate iType, Item item)
        {
	        if (!IsMapLoaded)
		        return;

	        if (iType.DoesBlockSolid || iType.DoesBlockPathFinding || iType.Group == ItemGroups.Ground)
            {
		        if (pos.Z == Position.Z)
                {
			        UpdateTileCache(tile, pos);
		        }
	        }
        }

        public virtual void OnCreatureMove(Creature creature, Tile newTile, Position newPos, Tile oldTile, Position oldPos, bool teleport)
        {
	        if (creature == this)
            {
		        LastStep = Tools.GetSystemMilliseconds();
		        LastStepCost = 1;

		        if (!teleport)
                {
			        if (oldPos.Z != newPos.Z)
                    {
				        //floor change extra cost
				        LastStepCost = 2;
			        }
                    else if (Position.GetDistanceX(newPos, oldPos) >= 1 && Position.GetDistanceY(newPos, oldPos) >= 1)
                    {
				        //diagonal extra cost
				        LastStepCost = 3;
			        }
		        }
                else
                {
			        StopEventWalk();
		        }

                //if (!summons.empty()) //TODO: SUMMONS
                //{
                //    //check if any of our summons is out of range (+/- 2 floors or 30 tiles away)
                //    std::forward_list<Creature*> despawnList;
                //    for (Creature* summon : summons) {
                //        const Position pos = summon->getPosition();
                //        if (Position::getDistanceZ(newPos, pos) > 2 || (std::max<int32_t>(Position::getDistanceX(newPos, pos), Position::getDistanceY(newPos, pos)) > 30)) {
                //            despawnList.push_front(summon);
                //        }
                //    }

                //    for (Creature* despawnCreature : despawnList) {
                //        g_game.removeCreature(despawnCreature, true);
                //    }
                //}

		        if (newTile.GetZone() != oldTile.GetZone())
			        OnChangeZone(GetZone());

		        //update map cache
		        if (IsMapLoaded)
                {
			        if (teleport || oldPos.Z != newPos.Z)
                    {
				        UpdateMapCache();
			        }
                    else
                    {
				        Tile tile;
				        Position myPos = GetPosition();
				        Position pos;

				        if (oldPos.Y > newPos.Y)
                        { //north
					        //shift y south
					        for (int y = MapWalkHeight - 1; --y >= 0;)
                            {
                                LocalMapCache[y+1].MemCpy(0, LocalMapCache[y], LocalMapCache[y].Length);
					        }

					        //update 0
					        for (int x = -MaxWalkCacheWidth; x <= MaxWalkCacheWidth; ++x)
                            {
						        tile = Map.Instance.GetTile((ushort)(myPos.X + x), (ushort)(myPos.Y - MaxWalkCacheHeight), myPos.Z);
						        UpdateTileCache(tile, x, -MaxWalkCacheHeight);
					        }
				        }
                        else if (oldPos.Y < newPos.Y)
                        { // south
					        //shift y north
					        for (int y = 0; y <= MapWalkHeight - 2; ++y)
                            {
                                LocalMapCache[y].MemCpy(0, LocalMapCache[y+1], LocalMapCache[y].Length);
					        }

					        //update mapWalkHeight - 1
					        for (int x = -MaxWalkCacheWidth; x <= MaxWalkCacheWidth; ++x)
                            {
						        tile = Map.Instance.GetTile((ushort)(myPos.X + x), (ushort)(myPos.Y + MaxWalkCacheHeight), myPos.Z);
						        UpdateTileCache(tile, x, MaxWalkCacheHeight);
					        }
				        }

				        if (oldPos.X < newPos.X)
                        { // east
					        //shift y west
					        int starty = 0;
					        int endy = MapWalkHeight - 1;
					        int dy = Position.GetDistanceY(oldPos, newPos);

					        if (dy < 0)
						        endy += dy;
                            else if (dy > 0)
						        starty = dy;

					        for (int y = starty; y <= endy; ++y)
                            {
						        for (int x = 0; x <= MapWalkWidth - 2; ++x)
                                {
							        LocalMapCache[y][x] = LocalMapCache[y][x + 1];
						        }
					        }

					        //update mapWalkWidth - 1
					        for (int y = -MaxWalkCacheHeight; y <= MaxWalkCacheHeight; ++y)
                            {
						        tile = Map.Instance.GetTile((ushort)(myPos.X + MaxWalkCacheWidth), (ushort)(myPos.Y + y), myPos.Z);
						        UpdateTileCache(tile, MaxWalkCacheWidth, y);
					        }
				        }
                        else if (oldPos.X > newPos.X)
                        { // west
					        //shift y east
					        int starty = 0;
					        int endy = MapWalkHeight - 1;
					        int dy = Position.GetDistanceY(oldPos, newPos);

					        if (dy < 0)
						        endy += dy;
					        else if (dy > 0)
						        starty = dy;

					        for (int y = starty; y <= endy; ++y)
                            {
						        for (int x = MapWalkWidth - 1; --x >= 0;)
                                {
							        LocalMapCache[y][x + 1] = LocalMapCache[y][x];
						        }
					        }

					        //update 0
					        for (int y = -MaxWalkCacheHeight; y <= MaxWalkCacheHeight; ++y)
                            {
						        tile = Map.Instance.GetTile((ushort)(myPos.X - MaxWalkCacheWidth), (ushort)(myPos.Y + y), myPos.Z);
						        UpdateTileCache(tile, -MaxWalkCacheWidth, y);
					        }
				        }

				        UpdateTileCache(oldTile, oldPos);
			        }
		        }
	        }
            else
            {
		        if (IsMapLoaded)
                {
			        Position myPos = GetPosition();

			        if (newPos.Z == myPos.Z)
                    {
				        UpdateTileCache(newTile, newPos);
			        }

			        if (oldPos.Z == myPos.Z)
                    {
				        UpdateTileCache(oldTile, oldPos);
			        }
		        }
	        }

	        if (creature == FollowCreature || (creature == this && FollowCreature != null))
            {
		        if (HasFollowPath)
			        IsUpdatingPath = true;

		        if (newPos.Z != oldPos.Z || !CanSee(FollowCreature.GetPosition()))
			        OnCreatureDisappear(FollowCreature, false);
	        }

	        if (creature == AttackedCreature || (creature == this && AttackedCreature != null))
            {
		        if (newPos.Z != oldPos.Z || !CanSee(AttackedCreature.GetPosition()))
			        OnCreatureDisappear(AttackedCreature, false);
		        else
                {
			        if (HasExtraSwing())
                    {
				        //our target is moving lets see if we can get in hit
                        DispatcherManager.GameDispatcher.AddTask(new Task(() => Game.Instance.CheckCreatureAttack(Id)));
			        }

			        if (newTile.GetZone() != oldTile.GetZone())
                    {
				        OnAttackedCreatureChangeZone(AttackedCreature.GetZone());
			        }
		        }
	        }
        }

        public virtual void OnAttackedCreatureChangeZone(ZoneTypes zone)
        {
            if (zone == ZoneTypes.Protection)
            {
                OnCreatureDisappear(AttackedCreature, false);
            }
        }

        private void AddEventWalk(bool firstStep = false)
        {
	        CancelNextWalk = false;

	        if (StepSpeed <= 0 || EventWalk != 0)
		        return;

	        long ticks = GetEventStepTicks(firstStep);
	        if (ticks <= 0)
		        return;

	        // Take first step right away, but still queue the next
	        if (ticks == 1)
	        {
	            Game.Instance.CheckCreatureWalk(Id);
	        }

	        EventWalk = DispatcherManager.Scheduler.AddEvent(SchedulerTask.CreateSchedulerTask((uint)ticks, () => Game.Instance.CheckCreatureWalk(Id)));
        }
        
        private void StopEventWalk()
        {
	        if (EventWalk != 0) {
		        DispatcherManager.Scheduler.StopEvent(EventWalk);
		        EventWalk = 0;
	        }
        }

        protected void UpdateMapCache()
        {
	        Tile tile;
	        Position myPos = GetPosition();
	        Position pos = new Position(0, 0, myPos.Z);

	        for (int y = -MaxWalkCacheHeight; y <= MaxWalkCacheHeight; ++y)
            {
		        for (int x = -MaxWalkCacheWidth; x <= MaxWalkCacheWidth; ++x)
                {
			        pos.X = (ushort)(myPos.X + x);
                    pos.Y = (ushort)(myPos.Y + y);
			        tile = Map.Instance.GetTile(pos.X, pos.Y, pos.Z);
			        UpdateTileCache(tile, pos);
		        }
	        }
        }

        protected void OnCreatureDisappear(Creature creature, bool isLogout)
        {
	        if (AttackedCreature == creature)
            {
		        SetAttackedCreature(null);
		        OnAttackedCreatureDisappear(isLogout);
	        }

	        if (FollowCreature == creature)
            {
		        SetFollowCreature(null);
		        OnFollowCreatureDisappear(isLogout);
	        }
        }

        protected virtual bool SetAttackedCreature(Creature creature)
        {
	        if (creature != null)
            {
		        Position creaturePos = creature.Position;
		        if (creaturePos.Z != Position.Z || !CanSee(creaturePos)) {
			        AttackedCreature = null;
			        return false;
		        }

		        AttackedCreature = creature;
		        OnAttackedCreature(AttackedCreature);
		        AttackedCreature.OnAttacked();
	        }
            else
            {
		        AttackedCreature = null;
	        }

	        foreach (Creature summon in Summons)
            {
		        summon.SetAttackedCreature(creature);
	        }
	        return true;
        }
        protected virtual bool SetFollowCreature(Creature creature)
        {
	        if (creature != null)
            {
		        if (FollowCreature == creature)
			        return true;

		        Position creaturePos = creature.Position;
		        if (creaturePos.Z != Position.Z || !CanSee(creaturePos)) {
			        FollowCreature = null;
			        return false;
		        }

		        if (WalkDirections.Count != 0)
                {
                    WalkDirections.Clear();
			        OnWalkAborted();
		        }

		        HasFollowPath = false;
		        ForceUpdateFollowPath = false;
		        FollowCreature = creature;
		        IsUpdatingPath = true;
	        }
            else
            {
		        IsUpdatingPath = false;
		        FollowCreature = null;
	        }

	        OnFollowCreature(creature);
	        return true;
        }

        protected virtual void OnAttackedCreature(Creature creature) {}
        protected virtual void OnAttacked() {}
		protected virtual void OnAttackedCreatureDisappear(bool isLogout) {}
        protected virtual void OnFollowCreatureDisappear(bool isLogout) { }
		protected virtual void OnFollowCreature(Creature creature) {}
		protected virtual void OnFollowCreatureComplete(Creature creature) {}

        protected virtual void OnChangeZone(ZoneTypes zone)
        {
            if (AttackedCreature != null && zone == ZoneTypes.Protection)
            {
                OnCreatureDisappear(AttackedCreature, false);
            }
        }

        protected virtual bool GetNextStep(ref Directions dir, ref CylinderFlags flags)
        {
	        if (WalkDirections.Count == 0)
		        return false;

            dir = WalkDirections.Dequeue();
	        OnWalk(ref dir);
	        return true;
        }

        public static bool CanSee(Position myPos, Position pos, int viewRangeX, int viewRangeY)
        {
	        if (myPos.Z <= 7)
            {
		        //we are on ground level or above (7 -> 0)
		        //view is from 7 -> 0
		        if (pos.Z > 7) {
			        return false;
		        }
	        }
            else if (myPos.Z >= 8)
            {
		        //we are underground (8 -> 15)
		        //view is +/- 2 from the floor we stand on
		        if (Position.GetDistanceZ(myPos, pos) > 2) {
			        return false;
		        }
	        }

	        int offsetz = myPos.Z - pos.Z;
	        return (pos.X >= myPos.X - viewRangeX + offsetz) && (pos.X <= myPos.X + viewRangeX + offsetz)
		        && (pos.Y >= myPos.Y - viewRangeY + offsetz) && (pos.Y <= myPos.Y + viewRangeY + offsetz);
        }

        public virtual bool CanSee(Position pos)
        {
	        return CanSee(Position, pos, Map.MaxViewportX, Map.MaxViewportY);
        }

        public virtual bool CanSeeCreature(Creature creature)
        {
            if (!CanSeeInvisibility() && creature.IsInvisible())
                return false;
            return true;
        }

        private long GetEventStepTicks(bool onlyDelay)
        {
	        long ret = GetWalkDelay();
	        if (ret <= 0) {
		        long stepDuration = GetStepDuration();
		        if (onlyDelay && stepDuration > 0) {
			        ret = 1;
		        } else {
			        ret = stepDuration * LastStepCost;
		        }
	        }
	        return ret;
        }

        private int GetWalkDelay()
        {
	        //Used for auto-walking
	        if (LastStep == 0)
		        return 0;

            long ct = Tools.GetSystemMilliseconds();
	        long stepDuration = GetStepDuration() * LastStepCost;
	        return (int)(stepDuration - (ct - LastStep));
        }

        protected long GetStepDuration(Directions dir)
        {
	        long stepDuration = GetStepDuration();
	        if ((dir & Directions.DiagonalMask) != 0)
		        stepDuration *= 3;
	        return stepDuration;
        }

        protected long GetStepDuration()
        {
	        if (IsInternalRemoved)
		        return 0;

	        uint calculatedStepSpeed;
	        uint groundSpeed;

	        int stepSpeed = StepSpeed;
	        if (stepSpeed > -SpeedB)
            {
		        calculatedStepSpeed = (uint)Math.Floor((SpeedA * Math.Log((stepSpeed / 2f) + SpeedB) + SpeedC) + 0.5);
		        if (calculatedStepSpeed <= 0)
			        calculatedStepSpeed = 1;
	        }
            else
            {
		        calculatedStepSpeed = 1;
	        }

	        Tile tile = Parent;
	        if (tile != null && tile.Ground != null)
            {
		        ushort groundId = tile.Ground.Id;
                groundSpeed = ItemManager.Templates[groundId].Speed;
		        if (groundSpeed == 0)
			        groundSpeed = 150;
	        }
            else
            {
		        groundSpeed = 150;
	        }

	        double duration = Math.Floor(1000D * groundSpeed / calculatedStepSpeed);
	        long stepDuration = (long)(Math.Ceiling(duration / 50) * 50);

	        Monster monster = this as Monster;
	        if (monster != null && monster.IsTargetNearby && !monster.IsFleeing && monster.Master == null)
            {
		        stepDuration *= 2;
	        }

	        return stepDuration;
        }

        private void UpdateTileCache(Tile tile, Position pos)
        {
            Position myPos = Position;
	        if (pos.Z == myPos.Z) {
		        int dx = Position.GetOffsetX(pos, myPos);
		        int dy = Position.GetOffsetY(pos, myPos);
		        UpdateTileCache(tile, dx, dy);
	        }
        }
        
        private void UpdateTileCache(Tile tile, int dx, int dy)
        {
	        if (Math.Abs(dx) <= MaxWalkCacheWidth && Math.Abs(dy) <= MaxWalkCacheHeight)
            {
		        LocalMapCache[MaxWalkCacheHeight + dy][MaxWalkCacheWidth + dx] = tile != null && tile.QueryAdd(0, this, 1, CylinderFlags.Pathfinding | CylinderFlags.IgnoreFieldDamage) == ReturnTypes.NoError;
	        }
        }

        public override Position GetPosition()
        {
            return Position;
        }

        private ZoneTypes GetZone()
        {
			return Parent.GetZone();
		}
    }
}
