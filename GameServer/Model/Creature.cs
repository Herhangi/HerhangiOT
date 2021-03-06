﻿using System;
using System.Collections.Generic;
using System.Linq;
using HerhangiOT.GameServer.Enums;
using HerhangiOT.GameServer.Model.Items;
using HerhangiOT.ServerLibrary;
using HerhangiOT.ServerLibrary.Threading;
using HerhangiOT.ServerLibrary.Utility;

namespace HerhangiOT.GameServer.Model
{
    public abstract class Creature : Thing
    {
        #region Constants
        public const double SpeedA = 857.36;
        public const double SpeedB = 261.29;
        public const double SpeedC = -4795.01;

        public const int MapWalkWidth = Map.MaxViewportX * 2 + 1;
        public const int MapWalkHeight = Map.MaxViewportY * 2 + 1;
        public const int MaxWalkCacheWidth = (MapWalkWidth - 1) / 2;
        public const int MaxWalkCacheHeight = (MapWalkHeight - 1) / 2;
        #endregion
        #region Properties
        public uint Id { get; protected set; }
        private Tile _parent;
        public Tile Parent
        {
            get { return _parent; }
            set
            {
                _parent = value;
                Position = _parent.Position;
            }
        }
        public Position Position { get; set; }
        public Directions Direction { get; set; }

        public Outfit CurrentOutfit { get; set; }
        public Outfit DefaultOutfit { get; protected set; }

        public bool LootDrop { get; protected set; }
        public bool SkillLoss { get; set; }
        public bool IsHealthHidden { get; set; }
        public bool IsInternalRemoved { get; private set; }
        public bool IsMapLoaded { get; protected set; }
        public bool CreatureCheck { get; set; }
        public bool InCheckCreaturesVector { get; set; }
        public bool ForceUpdateFollowPath { get; protected set; }
        public bool IsUpdatingPath { get; protected set; }
        public bool HasFollowPath { get; protected set; }
        public bool[][] LocalMapCache { get; protected set; }

        public uint BlockTicks { get; protected set; }
        public uint WalkUpdateTicks { get; protected set; }
        public uint LastHitCreature { get; protected set; }
        public uint BlockCount { get; protected set; }
        public long LastStep { get; protected set; }
        public uint LastStepCost { get; protected set; }
        public uint EventWalk { get; protected set; }
        public bool CancelNextWalk { get; set; }
        public Queue<Directions> WalkDirections { get; protected set; }

        public virtual RaceTypes Race { get { return RaceTypes.None; } }
        #endregion

        public Creature()
        {
            InternalLight = new LightInfo { Color = 0, Level = 0 };
            LocalMapCache = new bool[MapWalkHeight][];
            for (int i = 0; i < MapWalkHeight; i++)
                LocalMapCache[i] = new bool[MapWalkWidth];
            Summons = new List<Creature>();
            Conditions = new List<Condition>();

            _skull = SkullTypes.None;
        }

        #region Abstract Methods
        public abstract string GetName();
        public abstract string GetNameDescription();
        public abstract CreatureTypes GetCreatureType();
        public abstract void SetID();

        public abstract void AddList();
        public abstract void RemoveList();
        #endregion
        #region Virtual Methods
        protected virtual ulong GetGainedExperience(Creature attacker)
        {
            return (ulong)Math.Floor(GetDamageRatio(attacker) * GetLostExperience());
        }
        protected virtual void DropLoot(Container corpse, Creature lastHitCreature) { }
		protected virtual ushort GetLookCorpse() { return 0; }
        protected virtual void GetPathSearchParams(Creature creature, PathfindingParameters fpp)
        {
	        fpp.FullPathSearch = !HasFollowPath;
	        fpp.ClearSight = true;
	        fpp.MaxSearchDist = 12;
	        fpp.MinTargetDist = 1;
	        fpp.MaxTargetDist = 1;
        }
        protected virtual void Death(Creature lastHitCreature) { }
        protected virtual bool DropCorpse(Creature lastHitCreature, Creature mostDamageCreature, bool lastHitUnjustified, bool mostDamageUnjustified)
        {
            //TODO: Program Here
            return false;
        }
        protected virtual Item GetCorpse(Creature lastHitCreature, Creature mostDamageCreature)
        {
	        return Item.CreateItem(GetLookCorpse());
        }
        #endregion

        #region Speed Operations
        public int BaseSpeed { get; protected set; }
        public int VarSpeed { get; protected set; }
        public int Speed { get { return BaseSpeed + VarSpeed; } }
        public virtual int StepSpeed { get { return Speed; } }

        public void SetSpeed(int varSpeedDelta)
        {
            int oldSpeed = Speed;
            VarSpeed = varSpeedDelta;

            if (Speed <= 0)
            {
                StopEventWalk();
                CancelNextWalk = true;
            }
            else if (oldSpeed <= 0 && WalkDirections.Count != 0)
            {
                AddEventWalk();
            }
        }
        #endregion
        #region Skull Operations
        private SkullTypes _skull;
        public virtual SkullTypes Skull
        { 
            get { return _skull; } 
            set 
            { 
                _skull = value;
                Game.UpdateCreatureSkull(this);
            }
        }
        public virtual SkullTypes GetSkullClient(Creature creature) { return creature.Skull; }
        #endregion

        #region Abstract Overrides
        public sealed override int GetThrowRange()
        {
            return 1;
        }
        public override bool IsPushable()
        {
            return GetWalkDelay() <= 0;
        }
        #endregion

        #region Invisibility Methods
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
            return Conditions.Any(condition => condition.ConditionType == ConditionFlags.Invisible);
        }
        #endregion

        #region Walk Operations
        public void StartAutoWalk(Directions direction)
        {
            WalkDirections = new Queue<Directions>();
            WalkDirections.Enqueue(direction);

            AddEventWalk(true);
        }
        public void StartAutoWalk(Queue<Directions> directions)
        {
            WalkDirections = directions;

            AddEventWalk(directions.Count >= 1);
        }
        protected void AddEventWalk(bool firstStep = false)
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
                Game.CheckCreatureWalk(Id);
            }

            EventWalk = DispatcherManager.Scheduler.AddEvent(SchedulerTask.CreateSchedulerTask((uint)ticks, () => Game.CheckCreatureWalk(Id)));
        }
        private void StopEventWalk()
        {
            if (EventWalk != 0)
            {
                DispatcherManager.Scheduler.StopEvent(EventWalk);
                EventWalk = 0;
            }
        }
        protected virtual void GoToFollowCreature()
        {
            if (FollowCreature != null)
            {
                //TODO: Pathfinding
                //FindPathParams fpp;
                //getPathSearchParams(followCreature, fpp);

                //Monster* monster = getMonster();
                //if (monster && !monster->getMaster() && (monster->isFleeing() || fpp.maxTargetDist > 1)) {
                //    Direction dir = DIRECTION_NONE;

                //    if (monster->isFleeing()) {
                //        monster->getDistanceStep(followCreature->getPosition(), dir, true);
                //    } else { //maxTargetDist > 1
                //        if (!monster->getDistanceStep(followCreature->getPosition(), dir)) {
                //             if we can't get anything then let the A* calculate
                //            listWalkDir.clear();
                //            if (getPathTo(followCreature->getPosition(), listWalkDir, fpp)) {
                //                hasFollowPath = true;
                //                startAutoWalk(listWalkDir);
                //            } else {
                //                hasFollowPath = false;
                //            }

                //            return;
                //        }
                //    }

                //    if (dir != DIRECTION_NONE) {
                //        listWalkDir.clear();
                //        listWalkDir.push_front(dir);

                //        hasFollowPath = true;
                //        startAutoWalk(listWalkDir);
                //    }
                //} else {
                //    listWalkDir.clear();
                //    if (getPathTo(followCreature->getPosition(), listWalkDir, fpp)) {
                //        hasFollowPath = true;
                //        startAutoWalk(listWalkDir);
                //    } else {
                //        hasFollowPath = false;
                //    }
                //}
            }

            OnFollowCreatureComplete(FollowCreature);
        }
        protected virtual bool GetNextStep(ref Directions dir, ref CylinderFlags flags)
        {
            if (WalkDirections.Count == 0)
                return false;

            dir = WalkDirections.Dequeue();
            OnWalk(ref dir);
            return true;
        }
        
        public virtual void OnWalk(ref Directions dir)
        {
            if (HasCondition(ConditionFlags.Drunk))
            {
                Directions r = (Directions)Game.RNG.Next(0, 20);
                if (r <= Directions.DiagonalMask)
                {
                    if (r < Directions.DiagonalMask)
                    {
                        dir = r;
                    }
                    Game.InternalCreatureSay(this, SpeakTypes.MonsterSay, "Hicks!", false);
                }
            }
        }
        public virtual void OnWalkAborted() { }
        public virtual void OnWalkComplete() { }
        #endregion
        #region Follow Operations
        public Creature FollowCreature { get; private set; }
        protected virtual bool SetFollowCreature(Creature creature)
        {
            if (creature != null)
            {
                if (FollowCreature == creature)
                    return true;

                Position creaturePos = creature.Position;
                if (creaturePos.Z != Position.Z || !CanSee(creaturePos))
                {
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

        protected virtual void OnFollowCreature(Creature creature) { }
        protected virtual void OnFollowCreatureDisappear(bool isLogout) { }
        protected virtual void OnFollowCreatureComplete(Creature creature) { }
        #endregion
        #region Combat Operations
        public Creature AttackedCreature { get; private set; }

        protected virtual bool SetAttackedCreature(Creature creature)
        {
            if (creature != null)
            {
                Position creaturePos = creature.Position;
                if (creaturePos.Z != Position.Z || !CanSee(creaturePos))
                {
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
        public virtual BlockTypes BlockHit(Creature attacker, CombatTypeFlags combatType, ref int damage, bool checkDefense = false, bool checkArmor = false, bool field = false)
        {
            BlockTypes blockType = BlockTypes.None;

            if (IsImmune(combatType))
            {
                damage = 0;
                blockType = BlockTypes.Immunity;
            }
            else if (checkDefense || checkArmor)
            {
                bool hasDefense = false;

                if (BlockCount > 0)
                {
                    --BlockCount;
                    hasDefense = true;
                }

                if (checkDefense && hasDefense)
                {
                    int defense = GetDefense();
                    damage -= Game.RNG.Next(defense / 2, defense);

                    if (damage <= 0)
                    {
                        damage = 0;
                        blockType = BlockTypes.Defense;
                        checkArmor = false;
                    }
                }

                if (checkArmor)
                {
                    int armorValue = GetArmor();
                    if (armorValue > 1)
                    {
                        double armorFormula = armorValue * 0.475;
                        int armorReduction = (int)Math.Ceiling(armorFormula);
                        damage -= Game.RNG.Next(armorReduction, armorReduction + (int)Math.Floor(armorFormula));
                    }
                    else if (armorValue == 1)
                    {
                        --damage;
                    }

                    if (damage <= 0)
                    {
                        damage = 0;
                        blockType = BlockTypes.Armor;
                    }
                }

                if (hasDefense && blockType != BlockTypes.None)
                {
                    OnBlockHit();
                }
            }

            if (attacker != null)
            {
                attacker.OnAttackedCreature(this);
                attacker.OnAttackedCreatureBlockHit(blockType);
            }

            OnAttacked();
            return blockType;
        }
		protected virtual bool ChallengeCreature(Creature creature)
        {
			return false;
		}
        protected virtual bool ConvinceCreature(Creature creature)
        {
			return false;
		}
        protected virtual void DoAttacking(uint interval) { }
        public virtual bool HasExtraSwing()
        {
            return false;
        }
        public virtual bool GetCombatValues(ref int min, ref int max)
        {
            return false;
        }

        protected virtual void OnAttackedCreature(Creature creature) { }
        protected virtual void OnAttacked() { }
        protected virtual void OnAttackedCreatureDrainHealth(Creature target, int amount)
        {
            target.AddDamagePoints(this, amount);
        }
        protected virtual void OnTargetCreatureGainHealth(Creature target, int amount) { }
        protected virtual void OnAttackedCreatureDisappear(bool isLogout) { }
        #endregion
        #region Summon Operations
        public Creature Master { get; set; }
        public List<Creature> Summons { get; protected set; }
        public bool IsSummon { get { return Master != null; } }
        public int SummonCount { get { return Summons.Count; } }

        public void AddSummon(Creature creature)
        {
            creature.LootDrop = false;
            creature.SkillLoss = false;
            creature.Master = this;
            creature.IncrementReferenceCounter();
            Summons.Add(creature);
        }
        public void RemoveSummon(Creature creature)
        {
            for (int i = 0; i < Summons.Count; i++)
            {
                Creature summon = Summons[i];
                if (summon != creature) continue;

                summon.LootDrop = false;
                summon.SkillLoss = true;
                summon.Master = null;
                summon.DecrementReferenceCounter();
                Summons.RemoveAt(i);
                break;
            }
        }
        #endregion
        #region Condition Operations
        public List<Condition> Conditions { get; protected set; } 

        public bool AddCondition(Condition condition, bool force = false)
        {
            if (condition == null)
                return false;

            if (!force && condition.ConditionType == ConditionFlags.Haste && HasCondition(ConditionFlags.Paralyze))
            {
                int walkDelay = GetWalkDelay();
                if (walkDelay > 0)
                {
                    DispatcherManager.Scheduler.AddEvent(SchedulerTask.CreateSchedulerTask((uint)walkDelay, () => Game.ForceAddCondition(Id, condition)));
                    return false;
                }
            }

            Condition prevCond = GetCondition(condition.ConditionType, condition.Id, condition.SubId);
            if (prevCond != null)
            {
                prevCond.AddCondition(this, condition);
                return true;
            }

            if (condition.StartCondition(this))
            {
                Conditions.Add(condition);
                OnAddCondition(condition.ConditionType);
                return true;
            }

            return false;
        }
        public bool AddCombatCondition(Condition condition)
        {
            ConditionFlags type = condition.ConditionType;

            if (!AddCondition(condition))
                return false;

            OnAddCombatCondition(type);
            return true;
        }
        public void RemoveCondition(ConditionFlags type, ConditionIds id, bool force = false)
        {
            for (int i = 0; i < Conditions.Count; ++i)
            {
                Condition condition = Conditions[i];
                if (condition.ConditionType != type || condition.Id != id)
                    continue;

                if (!force && type == ConditionFlags.Paralyze)
                {
                    int walkDelay = GetWalkDelay();
                    if (walkDelay > 0)
                    {
                        DispatcherManager.Scheduler.AddEvent(SchedulerTask.CreateSchedulerTask((uint)walkDelay, () => Game.ForceRemoveCondition(Id, type)));
                        return;
                    }
                }

                Conditions.RemoveAt(i);
                condition.EndCondition(this);

                OnEndCondition(type);
            }
        }
        public void RemoveCondition(ConditionFlags type, bool force = false)
        {
            for (int i = 0; i < Conditions.Count; ++i)
            {
                Condition condition = Conditions[i];
                if (condition.ConditionType != type)
                    continue;

                if (!force && type == ConditionFlags.Paralyze)
                {
                    int walkDelay = GetWalkDelay();
                    if (walkDelay > 0)
                    {
                        DispatcherManager.Scheduler.AddEvent(SchedulerTask.CreateSchedulerTask((uint)walkDelay, () => Game.ForceRemoveCondition(Id, type)));
                        return;
                    }
                }

                Conditions.RemoveAt(i);
                condition.EndCondition(this);

                OnEndCondition(type);
            }
        }
        public void RemoveCondition(Condition condition, bool force = false)
        {
            int position = Conditions.IndexOf(condition);
            if (position == -1) return;

            if (!force && condition.ConditionType == ConditionFlags.Paralyze)
            {
                int walkDelay = GetWalkDelay();
                if (walkDelay > 0)
                {
                    DispatcherManager.Scheduler.AddEvent(SchedulerTask.CreateSchedulerTask((uint)walkDelay, () => Game.ForceRemoveCondition(Id, condition.ConditionType)));
                    return;
                }
            }

            Conditions.RemoveAt(position);
            condition.EndCondition(this);

            OnEndCondition(condition.ConditionType);
        }
        public void RemoveCombatCondition(ConditionFlags type)
        {
            List<Condition> conditionsToRemove = Conditions.Where(condition => condition.ConditionType == type).ToList();

            foreach (Condition condition in conditionsToRemove)
            {
                OnCombatRemoveCondition(condition);
            }
        }
        public Condition GetCondition(ConditionFlags type)
        {
            return Conditions.FirstOrDefault(c => c.ConditionType == type);
        }
        public Condition GetCondition(ConditionFlags type, ConditionIds id, uint subId = 0)
        {
            return Conditions.FirstOrDefault(c => c.ConditionType == type && c.Id == id && c.SubId == subId);
        }
        public void ExecuteConditions(uint interval)
        {
            int iInterval = (int)interval;

            for (int i = Conditions.Count - 1; i > -1; i--)
            {
                Condition condition = Conditions[i];

                if (!condition.ExecuteCondition(this, iInterval))
                {
                    Conditions.RemoveAt(i);

                    condition.EndCondition(this);
                    OnEndCondition(condition.ConditionType);
                }
            }
        }
        public bool HasCondition(ConditionFlags type, uint subId = 0)
        {
            if (IsSuppressed(type))
                return false;

            long timeNow = Tools.GetSystemMilliseconds();
            foreach (Condition condition in Conditions)
            {
                if (condition.ConditionType != type || condition.SubId != subId)
                    continue;

                if (condition.EndTime >= timeNow)
                    return true;
            }
            return false;
        }

        public virtual bool IsAttackable()
        {
            return true;
        }
        public virtual bool IsImmune(CombatTypeFlags damageType)
        {
            return GetDamageImmunities().HasFlag(damageType);
        }
        public virtual bool IsImmune(ConditionFlags condition)
        {
            return GetConditionImmunities().HasFlag(condition);
        }
        public virtual bool IsSuppressed(ConditionFlags condition)
        {
            return GetConditionSuppressions().HasFlag(condition);
        }
        public virtual CombatTypeFlags GetDamageImmunities()
        {
            return CombatTypeFlags.None;
        }
        public virtual ConditionFlags GetConditionImmunities()
        {
            return ConditionFlags.None;
        }
        public virtual ConditionFlags GetConditionSuppressions()
        {
            return ConditionFlags.None;
        }

        protected virtual void OnAddCondition(ConditionFlags type)
        {
            if (type == ConditionFlags.Paralyze && HasCondition(ConditionFlags.Haste))
            {
                RemoveCondition(ConditionFlags.Haste);
            }
            else if (type == ConditionFlags.Haste && HasCondition(ConditionFlags.Paralyze))
            {
                RemoveCondition(ConditionFlags.Paralyze);
            }
        }
        protected virtual void OnAddCombatCondition(ConditionFlags type) { }
        protected virtual void OnEndCondition(ConditionFlags type) { }
        public void OnTickCondition(ConditionFlags type, ref bool bRemove)
        {
            MagicField field = Parent.GetMagicFieldItem();
            if (field == null)
                return;

            switch (type)
            {
                case ConditionFlags.Fire:
                    bRemove = (field.GetCombatType() != CombatTypeFlags.FireDamage);
                    break;
                case ConditionFlags.Energy:
                    bRemove = (field.GetCombatType() != CombatTypeFlags.EnergyDamage);
                    break;
                case ConditionFlags.Poison:
                    bRemove = (field.GetCombatType() != CombatTypeFlags.EarthDamage);
                    break;
                case ConditionFlags.Freezing:
                    bRemove = (field.GetCombatType() != CombatTypeFlags.IceDamage);
                    break;
                case ConditionFlags.Dazzled:
                    bRemove = (field.GetCombatType() != CombatTypeFlags.HolyDamage);
                    break;
                case ConditionFlags.Cursed:
                    bRemove = (field.GetCombatType() != CombatTypeFlags.DeathDamage);
                    break;
                case ConditionFlags.Drown:
                    bRemove = (field.GetCombatType() != CombatTypeFlags.DrownDamage);
                    break;
                case ConditionFlags.Bleeding:
                    bRemove = (field.GetCombatType() != CombatTypeFlags.PhysicalDamage);
                    break;
            }
        }
        protected virtual void OnCombatRemoveCondition(Condition condition)
        {
            RemoveCondition(condition);
        }
        #endregion
        #region Health/Mana Operations
        public virtual ushort Health { get; protected set; }
        public virtual ushort HealthMax { get; protected set; }
        public virtual ushort Mana { get; protected set; }
        public virtual ushort ManaMax { get; protected set; }

        public void ChangeHealth(int healthChange, bool sendHealthChange = true)
        {
            int oldHealth = Health;

            if (healthChange > 0)
                Health += (ushort)Math.Min(healthChange, HealthMax - Health);
            else
                Health = (ushort)Math.Max(0, Health + healthChange);

            if (sendHealthChange && oldHealth != Health)
                Game.AddCreatureHealth(this);
        }
        public void ChangeMana(int manaChange)
        {
            if (manaChange > 0)
            {
                Mana += (ushort)Math.Min(manaChange, ManaMax - Mana);
            }
            else
            {
                Mana = (ushort)Math.Max(0, Mana + manaChange);
            }
        }
        public void GainHealth(Creature healer, int healthGain)
        {
            ChangeHealth(healthGain);

            if (healer != null)
                healer.OnTargetCreatureGainHealth(this, healthGain);
        }
        public virtual void DrainHealth(Creature attacker, int damage)
        {
            ChangeHealth(-damage, false);

            if (attacker != null)
                attacker.OnAttackedCreatureDrainHealth(this, damage);
        }
        public virtual void DrainMana(Creature attacker, int manaLoss)
        {
            OnAttacked();
            ChangeMana(-manaLoss);

            if (attacker != null)
                AddDamagePoints(attacker, manaLoss);
        }
        #endregion
        #region Damage Operations
        public class DamageBlock
        {
            public int Total { get; set; }
            public long Ticks { get; set; }
        }

        protected Dictionary<uint, DamageBlock> DamageMap;
        private double GetDamageRatio(Creature attacker)
        {
	        int totalDamage = 0;
	        int attackerDamage = 0;

	        foreach (KeyValuePair<uint, DamageBlock> damage in DamageMap)
            {
		        totalDamage += damage.Value.Total;

		        if (damage.Key == attacker.Id)
                {
			        attackerDamage = damage.Value.Total;
		        }
	        }

	        if (totalDamage == 0)
		        return 0;

            return ((double)attackerDamage) / totalDamage;
        }
        protected void AddDamagePoints(Creature attacker, int damagePoints)
        {
            if (damagePoints <= 0)
                return;

            uint attackerId = attacker.Id;

            DamageBlock damage;
            if(!DamageMap.TryGetValue(attackerId, out damage))
                damage = new DamageBlock();

            damage.Ticks = Tools.GetSystemMilliseconds();
            damage.Total += damage.Total + damagePoints;

            DamageMap[attackerId] = damage;

            LastHitCreature = attackerId;
        }
        protected bool HasBeenAttacked(uint attackerId)
        {
            DamageBlock damage;
            if (!DamageMap.TryGetValue(attackerId, out damage))
                return false;

            return Tools.GetSystemMilliseconds() - damage.Ticks <= ConfigManager.Instance[ConfigInt.PzLocked];
        }
        #endregion
        #region Light Operations
        public LightInfo InternalLight { get; set; }

        public virtual LightInfo GetCreatureLight()
        {
            return InternalLight;
        }
        public virtual void SetNormalCreatureLight()
        {
            InternalLight.Level = 0;
            InternalLight.Color = 0;
        }
        #endregion

        #region METHODS TO BE CONVERTED TO VIRTUAL PROPERTY
        protected virtual int GetDefense()
        {
            return 0;
        }
        protected virtual int GetArmor()
        {
            return 0;
        }
        protected virtual float GetAttackFactor()
        {
            return 1.0f;
        }
        protected virtual float GetDefenseFactor()
        {
            return 1.0f;
        }
        public virtual SpeechBubbles GetSpeechBubble()
        {
            return SpeechBubbles.None;
        }
		public virtual ulong GetLostExperience()
        {
			return 0;
		}
        public override Position GetPosition()
        {
            return Position;
        }
        public override void SetParent(Thing parent)
        {
            Parent = (Tile)parent;
        }
        public override bool IsRemoved()
        {
            return IsInternalRemoved;
        }
        public void SetRemoved()
        {
            IsInternalRemoved = true;
        }
        public ZoneTypes GetZone()
        {
            return Parent.GetZone();
        }
        #endregion

        #region Operation Handlers
        public void OnDeath()
        {
            //TODO: Fill this method
        }
        protected virtual bool OnKilledCreature(Creature target, bool lastHit = true)
        {
            if (Master != null)
                Master.OnKilledCreature(target);

            //TODO: Scripting
            //const CreatureEventList& killEvents = getCreatureEvents(CREATURE_EVENT_KILL);
            //for (CreatureEvent* killEvent : killEvents) {
            //    killEvent->executeOnKill(this, target);
            //}
            return false;
        }
        protected virtual void OnGainExperience(uint gainExp, Creature target)
        {
            if (gainExp == 0 || Master == null)
            {
                return;
            }

            gainExp /= 2;
            Master.OnGainExperience(gainExp, target);

            HashSet<Creature> spectators = new HashSet<Creature>();
            Map.GetSpectators(ref spectators, Position, false, true);
            if (spectators.Count == 0)
                return;

            TextMessage message = new TextMessage()
            {
                Type = MessageTypes.ExperienceOthers,
                Text = string.Format("{0} gained {1} experience points.", GetNameDescription(), gainExp),
                Position = Position,
                PrimaryColor = TextColors.WhiteExp,
                PrimaryValue = gainExp
            };

            foreach (Player spectator in spectators)
            {
                spectator.SendTextMessage(message);
            }
        }
        protected virtual void OnAttackedCreatureBlockHit(BlockTypes blockType) { }
        protected virtual void OnBlockHit() { }
        protected virtual void OnChangeZone(ZoneTypes zone)
        {
            if (AttackedCreature != null && zone == ZoneTypes.Protection)
            {
                OnCreatureDisappear(AttackedCreature, false);
            }
        }
        public virtual void OnAttackedCreatureChangeZone(ZoneTypes zone)
        {
            if (zone == ZoneTypes.Protection)
            {
                OnCreatureDisappear(AttackedCreature, false);
            }
        }
        protected virtual void OnIdleStatus()
        {
            if (Health > 0)
            {
                DamageMap.Clear();
                LastHitCreature = 0;
            }
        }

        public virtual void OnThink(uint interval)
        {
            if (!IsMapLoaded && UseCacheMap())
            {
                IsMapLoaded = true;
                UpdateMapCache();
            }

            if (FollowCreature != null && Master != FollowCreature && !CanSeeCreature(FollowCreature))
                OnCreatureDisappear(FollowCreature, false);

            if (AttackedCreature != null && Master != AttackedCreature && !CanSeeCreature(AttackedCreature))
                OnCreatureDisappear(AttackedCreature, false);

            BlockTicks += interval;
            if (BlockTicks >= 1000)
            {
                BlockCount = Math.Min(BlockCount + 1, 2);
                BlockTicks = 0;
            }

            if (FollowCreature != null)
            {
                WalkUpdateTicks += interval;
                if (ForceUpdateFollowPath || WalkUpdateTicks >= 2000)
                {
                    WalkUpdateTicks = 0;
                    ForceUpdateFollowPath = false;
                    IsUpdatingPath = true;
                }
            }

            if (IsUpdatingPath)
            {
                IsUpdatingPath = false;
                GoToFollowCreature();
            }

            ////scripting event - onThink TODO: Scripting
            //const CreatureEventList& thinkEvents = getCreatureEvents(CREATURE_EVENT_THINK);
            //for (CreatureEvent* thinkEvent : thinkEvents) {
            //    thinkEvent->executeOnThink(this, interval);
            //}
        }
        public void OnAttacking(uint interval)
        {
            if (AttackedCreature == null)
                return;

            OnAttacked();
            AttackedCreature.OnAttacked();

            if (Map.IsSightClear(GetPosition(), AttackedCreature.GetPosition(), true))
            {
                DoAttacking(interval);
            }
        }
        public virtual void OnWalk()
        {
            if (GetWalkDelay() <= 0)
            {
                Directions dir = Directions.None;
                CylinderFlags flags = CylinderFlags.IgnoreFieldDamage;

                if (GetNextStep(ref dir, ref flags))
                {
                    ReturnTypes ret = Game.InternalMoveCreature(this, dir, flags);
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

        public void OnAddTileItem(Tile tile, Position pos)
        {
            if (IsMapLoaded && pos.Z == GetPosition().Z)
            {
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

        public virtual void OnCreatureAppear(Creature creature, bool isLogin)
        {
            if (creature == this)
            {
                if (UseCacheMap())
                {
                    IsMapLoaded = true;
                    UpdateMapCache();
                }
            }
            else if (IsMapLoaded)
            {
                if (creature.GetPosition().Z == GetPosition().Z)
                {
                    UpdateTileCache(creature.Parent, creature.GetPosition());
                }
            }
        }
        public virtual void OnRemoveCreature(Creature creature, bool isLogout)
        {
            OnCreatureDisappear(creature, true);
            if (creature == this)
            {
                if (Master != null && !Master.IsRemoved())
                {
                    Master.RemoveSummon(this);
                }
            }
            else if (IsMapLoaded)
            {
                if (creature.GetPosition().Z == GetPosition().Z)
                {
                    UpdateTileCache(creature.Parent, creature.GetPosition());
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

                if (Summons.Count > 0)
                {
                    //check if any of our summons is out of range (+/- 2 floors or 30 tiles away)
                    List<Creature> despawnList = new List<Creature>();
                    foreach (Creature summon in Summons)
                    {
                        Position pos = summon.GetPosition();
                        if (Position.GetDistanceZ(newPos, pos) > 2 || (Math.Max(Position.GetDistanceX(newPos, pos), Position.GetDistanceY(newPos, pos)) > 30))
                        {
                            despawnList.Add(summon);
                        }
                    }

                    foreach (Creature despawnCreature in despawnList)
                    {
                        Game.RemoveCreature(despawnCreature, true);
                    }
                }

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

                        if (oldPos.Y > newPos.Y)
                        { //north
                            //shift y south
                            for (int y = MapWalkHeight - 1; --y >= 0; )
                            {
                                LocalMapCache[y + 1].MemCpy(0, LocalMapCache[y], LocalMapCache[y].Length);
                            }

                            //update 0
                            for (int x = -MaxWalkCacheWidth; x <= MaxWalkCacheWidth; ++x)
                            {
                                tile = Map.GetTile((ushort)(myPos.X + x), (ushort)(myPos.Y - MaxWalkCacheHeight), myPos.Z);
                                UpdateTileCache(tile, x, -MaxWalkCacheHeight);
                            }
                        }
                        else if (oldPos.Y < newPos.Y)
                        { // south
                            //shift y north
                            for (int y = 0; y <= MapWalkHeight - 2; ++y)
                            {
                                LocalMapCache[y].MemCpy(0, LocalMapCache[y + 1], LocalMapCache[y].Length);
                            }

                            //update mapWalkHeight - 1
                            for (int x = -MaxWalkCacheWidth; x <= MaxWalkCacheWidth; ++x)
                            {
                                tile = Map.GetTile((ushort)(myPos.X + x), (ushort)(myPos.Y + MaxWalkCacheHeight), myPos.Z);
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
                                tile = Map.GetTile((ushort)(myPos.X + MaxWalkCacheWidth), (ushort)(myPos.Y + y), myPos.Z);
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
                                for (int x = MapWalkWidth - 1; --x >= 0; )
                                {
                                    LocalMapCache[y][x + 1] = LocalMapCache[y][x];
                                }
                            }

                            //update 0
                            for (int y = -MaxWalkCacheHeight; y <= MaxWalkCacheHeight; ++y)
                            {
                                tile = Map.GetTile((ushort)(myPos.X - MaxWalkCacheWidth), (ushort)(myPos.Y + y), myPos.Z);
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
                        DispatcherManager.GameDispatcher.AddTask(() => Game.CheckCreatureAttack(Id));
                    }

                    if (newTile.GetZone() != oldTile.GetZone())
                    {
                        OnAttackedCreatureChangeZone(AttackedCreature.GetZone());
                    }
                }
            }
        }

        public virtual void OnCreatureSay(Creature creature, SpeakTypes type, string text) { }
        public virtual void OnCreatureConvinced(Creature convincer, Creature creature) { }
        public virtual void OnPlacedCreature() { }
        #endregion
        #region Scripting
        public bool RegisterCreatureEvent(string name)
        {
            return false; //TODO: Scripting
        }
        public bool UnregisterCreatureEvent(string name)
        {
            return false; //TODO: Scripting
        }
        #endregion

        #region Can See
        public static bool CanSee(Position myPos, Position pos, int viewRangeX, int viewRangeY)
        {
            if (myPos.Z <= 7)
            {
                //we are on ground level or above (7 -> 0)
                //view is from 7 -> 0
                if (pos.Z > 7)
                {
                    return false;
                }
            }
            else if (myPos.Z >= 8)
            {
                //we are underground (8 -> 15)
                //view is +/- 2 from the floor we stand on
                if (Position.GetDistanceZ(myPos, pos) > 2)
                {
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
        #endregion
        #region Time Calculations
        private int GetWalkDelay()
        {
            //Used for auto-walking
            if (LastStep == 0)
                return 0;

            long ct = Tools.GetSystemMilliseconds();
            long stepDuration = GetStepDuration() * LastStepCost;
            return (int)(stepDuration - (ct - LastStep));
        }
        private int GetWalkDelay(Directions dir)
        {
            if (LastStep == 0)
                return 0;

            long ct = Tools.GetSystemMilliseconds();
            long stepDuration = GetStepDuration(dir);
            return (int)(stepDuration - (ct - LastStep));
        }
        private long GetTimeSinceLastMove()
        {
	        if (LastStep != 0)
                return Tools.GetSystemMilliseconds() - LastStep;
            return long.MaxValue;
        }

        private long GetEventStepTicks(bool onlyDelay)
        {
            long ret = GetWalkDelay();
            if (ret <= 0)
            {
                long stepDuration = GetStepDuration();
                if (onlyDelay && stepDuration > 0)
                {
                    ret = 1;
                }
                else
                {
                    ret = stepDuration * LastStepCost;
                }
            }
            return ret;
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

            Item ground = Parent.Ground;
            if (ground != null)
            {
                groundSpeed = ItemManager.Templates[ground.Id].Speed;
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
        #endregion

        #region Cache
        public int GetWalkCache(Position pos)
        {
	        if (!UseCacheMap())
		        return 2;

	        Position myPos = GetPosition();
	        if (myPos.Z != pos.Z)
		        return 0;

	        if (pos == myPos)
		        return 1;

	        int dx = Position.GetOffsetX(pos, myPos);
	        if (Math.Abs(dx) <= MaxWalkCacheWidth) {
		        int dy = Position.GetOffsetY(pos, myPos);
		        if (Math.Abs(dy) <= MaxWalkCacheHeight) {
			        if (LocalMapCache[MaxWalkCacheHeight + dy][MaxWalkCacheWidth + dx])
				        return 1;
				    return 0;
		        }
	        }

	        return 2;
        }
        protected virtual bool UseCacheMap()
        {
            return false;
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
                    tile = Map.GetTile(pos.X, pos.Y, pos.Z);
                    UpdateTileCache(tile, pos);
                }
            }
        }
        private void UpdateTileCache(Tile tile, Position pos)
        {
            Position myPos = Position;
            if (pos.Z == myPos.Z)
            {
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
        #endregion

        #region Pathfinding
        public bool GetPathTo(Position targetPos, ref Queue<Directions> directions, PathfindingParameters fpp)
        {
            return false; //TODO: Pathfinding
        }
		public bool GetPathTo(Position targetPos, ref Queue<Directions> directions, int minTargetDist, int maxTargetDist, bool fullPathSearch = true, bool clearSight = true, int maxSearchDist = 0)
        {
            return false; //TODO: Pathfinding
        }
        #endregion

        public void IncrementReferenceCounter()
        {
            //TODO: MICRO MEMORY MANAGEMENT
        }
        public void DecrementReferenceCounter()
        {
            //TODO: MICRO MEMORY MANAGEMENT
        }
    }
}
