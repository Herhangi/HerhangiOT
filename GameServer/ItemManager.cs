using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using HerhangiOT.GameServer.Enums;
using HerhangiOT.GameServer.Model;
using HerhangiOT.ServerLibrary;
using HerhangiOT.ServerLibrary.Utility;

namespace HerhangiOT.GameServer
{
    public class ItemManager
    {
        public static uint DwMajorVersion;
        public static uint DwMinorVersion;
        public static uint DwBuildNumber;

        public static Dictionary<ushort, ushort> ReverseItemDict = new Dictionary<ushort, ushort>(); //ClientId -> ServerId 
        public static readonly Dictionary<ushort, ItemTemplate> Templates = new Dictionary<ushort, ItemTemplate>();

        public static readonly ClientFluidTypes[] FluidMap =
        {
            ClientFluidTypes.Empty,
            ClientFluidTypes.Blue,
            ClientFluidTypes.Red,
            ClientFluidTypes.Brown1,
            ClientFluidTypes.Green,
            ClientFluidTypes.Yellow,
            ClientFluidTypes.White,
            ClientFluidTypes.Purple,
        };

        public static bool Load()
        {
            Logger.LogOperationStart("Loading Items");

            if (!LoadFromOtb())
                return false;
            
            if (!LoadFromXml())
                return false;
            
            Logger.LogOperationDone();
            return true;
        }

        private static bool LoadFromOtb()
        {
            FileLoader fileLoader = new FileLoader();
            if (!fileLoader.OpenFile("Data/Items/items.otb", "OTBI"))
            {
                Logger.LogOperationFailed("OTB file could not be loaded!");
                return false;
            }

            byte type;
            NodeStruct node = fileLoader.GetChildNode(null, out type);

            BinaryReader props;
            if (fileLoader.GetProps(node, out props))
            {
                //4 byte flags
		        //attributes
		        //0x01 = version data
		        props.ReadUInt32();
                props.ReadByte();
                byte attr = props.ReadByte();

                if (attr == 0x01)//ROOT_ATTR_VERSION
                {
                    ushort datalen = props.ReadUInt16();

                    if (datalen != 140) // 3 integers + 128 bytes 
                    {
                        Logger.LogOperationFailed("OTB file is in invalid format!");
                        return false;
                    }

                    DwMajorVersion = props.ReadUInt32();
                    DwMinorVersion = props.ReadUInt32();
                    DwBuildNumber = props.ReadUInt32();
                }
            }

            if (DwMajorVersion == 0xFFFFFFFF)
            {
                Logger.Log(LogLevels.Warning, "items.otb using generic client version!");
            }
            else if (DwMajorVersion != 3)
            {
                Logger.LogOperationFailed("Old version detected, a newer version of items.otb is required.");
                return false;
            }
            else if (DwMinorVersion < (uint)OtbClientVersion.V1076)
            {
                Logger.LogOperationFailed("A newer version of items.otb is required.");
                return false;
            }

            node = fileLoader.GetChildNode(node, out type);
            while (node != null)
            {
                if (!fileLoader.GetProps(node, out props))
                {
                    Logger.LogOperationFailed("OTB file is in invalid format!");
                    return false;
                }

                ItemFlags flags = (ItemFlags)props.ReadUInt32();

                ushort serverId = 0;
                ushort clientId = 0;
                ushort speed = 0;
                ushort wareId = 0;
                byte lightLevel = 0;
                byte lightColor = 0;
                byte alwaysOnTopOrder = 0;

                while (props.PeekChar() != -1)
                {
                    byte attrib = props.ReadByte();
                    ushort datalen = props.ReadUInt16();

                    switch ((ItemAttributes)attrib)
                    {
                        case ItemAttributes.ServerId:
                            serverId = props.ReadUInt16();

                            if (serverId > 30000 && serverId < 30100)
                                serverId -= 30000;
                            break;

                        case ItemAttributes.ClientId:
                            clientId = props.ReadUInt16();
                            break;

                        case ItemAttributes.Speed:
                            speed = props.ReadUInt16();
                            break;

                        case ItemAttributes.Light2:
                            lightLevel = (byte)props.ReadUInt16();
                            lightColor = (byte)props.ReadUInt16();
                            break;

                        case ItemAttributes.TopOrder:
                            alwaysOnTopOrder = props.ReadByte();
                            break;

                        case ItemAttributes.WareId:
                            wareId = props.ReadUInt16();
                            break;

                        default:
                            props.ReadBytes(datalen);
                            break;
                    }
                }

                if (!ReverseItemDict.ContainsKey(clientId))
                    ReverseItemDict[clientId] = serverId;

                if(!Templates.ContainsKey(serverId))
                    Templates[serverId] = new ItemTemplate();

                ItemTemplate item = Templates[serverId];

                item.DoesBlockSolid = flags.HasFlag(ItemFlags.BlocksSolid);
                item.DoesBlockProjectile = flags.HasFlag(ItemFlags.BlocksProjectile);
                item.DoesBlockPathFinding = flags.HasFlag(ItemFlags.BlocksPathFinding);
                item.HasHeight = flags.HasFlag(ItemFlags.HasHeight);
                item.IsUseable = flags.HasFlag(ItemFlags.Useable);
                item.IsPickupable = flags.HasFlag(ItemFlags.Pickupable);
                item.IsMoveable = flags.HasFlag(ItemFlags.Moveable);
                item.IsStackable = flags.HasFlag(ItemFlags.Stackable);
                item.IsAlwaysOnTop = flags.HasFlag(ItemFlags.AlwaysOnTop);
                item.IsVertical = flags.HasFlag(ItemFlags.Vertical);
                item.IsHorizontal = flags.HasFlag(ItemFlags.Horizontal);
                item.IsHangable = flags.HasFlag(ItemFlags.Hangable);
                item.AllowDistanceRead = flags.HasFlag(ItemFlags.AllowDistanceRead);
                item.IsRotatable = flags.HasFlag(ItemFlags.Rotatable);
                item.CanReadText = flags.HasFlag(ItemFlags.Readable);
                item.CanLookThrough = flags.HasFlag(ItemFlags.LookThrough);
                item.IsAnimation = flags.HasFlag(ItemFlags.Animation);
                // item.WalkStack = flags.HasFlag(ItemFlags.FullTile);
                item.ForceUse = flags.HasFlag(ItemFlags.ForceUse);


                item.Group = (ItemGroups)type;
                switch ((ItemGroups)type)
                {
                    case ItemGroups.Container:
                        item.Type = ItemTypes.Container;
                        break;
                    case ItemGroups.Door:
                        item.Type = ItemTypes.Door;
                        break;
                    case ItemGroups.MagicField:
                        item.Type = ItemTypes.MagicField;
                        break;
                    case ItemGroups.Teleport:
                        item.Type = ItemTypes.Teleport;
                        break;
                }

                item.Id = serverId;
                item.ClientId = clientId;
                item.Speed = speed;
                item.LightLevel = lightLevel;
                item.LightColor = lightColor;
                item.WareId = wareId;
                item.AlwaysOnTopOrder = alwaysOnTopOrder;

                node = fileLoader.GetNextNode(node, out type);
            }

            fileLoader.Close();
            return true;
        }

        private static bool LoadFromXml()
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load("Data/Items/items.xml");
            }
            catch
            {
                Logger.LogOperationFailed("LoadFromXml: Xml file could not be loaded!");
                return false;
            }

            foreach (XmlNode itemNode in doc.GetElementsByTagName("item"))
            {
                if(itemNode.Attributes == null) continue;
                
                XmlAttribute idAttribute = itemNode.Attributes["id"];
                if (idAttribute != null)
                {
                    ParseItemNode(itemNode, ushort.Parse(idAttribute.InnerText));
                    continue;
                }

                XmlAttribute fromIdAttribute = itemNode.Attributes["fromid"];
                if (fromIdAttribute == null)
                {
                    Logger.LogOperationFailed("LoadFromXml: No itemid found!");
                    return false;
                }

                XmlAttribute toIdAttribute = itemNode.Attributes["toid"];
                if (toIdAttribute == null)
                {
                    Logger.LogOperationFailed("LoadFromXml: fromid(" + fromIdAttribute.Value + ") without toid!");
                    return false;
                }

                ushort id = ushort.Parse(fromIdAttribute.InnerText);
                ushort toId = ushort.Parse(toIdAttribute.InnerText);

                while(id <= toId)
                    ParseItemNode(itemNode, id++);
            }
            return true;
        }

        private static void ParseItemNode(XmlNode node, ushort id)
        {
            if (id > 30000 && id < 30100)
            {
                id -= 30000;

                if (!Templates.ContainsKey(id))
                    Templates.Add(id, new ItemTemplate());
            }
            
            ItemTemplate item = Templates[id];
            item.Id = id;

            Debug.Assert(node.Attributes != null, "node.Attributes != null");
            item.Name = node.Attributes["name"].InnerText;

            XmlAttribute articleAttribute = node.Attributes["article"];
            if (articleAttribute != null)
                item.Article = articleAttribute.InnerText;

            XmlAttribute pluralAttribute = node.Attributes["plural"];
            if (pluralAttribute != null)
                item.PluralName = pluralAttribute.InnerText;

            if (node.HasChildNodes)
            {
                foreach (XmlNode attributeNode in node.ChildNodes)
                {
                    if(attributeNode.Attributes == null) continue;

                    XmlAttribute keyAttribute = attributeNode.Attributes["key"];
                    XmlAttribute valueAttribute = attributeNode.Attributes["value"];

                    if(keyAttribute == null || valueAttribute == null) continue;

                    switch (keyAttribute.InnerText.ToLowerInvariant())
                    {
                        case "type":
                            switch (valueAttribute.InnerText.ToLowerInvariant())
                            {
                                case "key":
                                    item.Type = ItemTypes.Key;
                                    break;
                                case "magicfield":
                                    item.Type = ItemTypes.MagicField;
                                    break;
                                case "container":
                                    item.Group = ItemGroups.Container;
                                    item.Type = ItemTypes.Container;
                                    break;
                                case "depot":
                                    item.Type = ItemTypes.Depot;
                                    break;
                                case "mailbox":
                                    item.Type = ItemTypes.Mailbox;
                                    break;
                                case "trashholder":
                                    item.Type = ItemTypes.TrashHolder;
                                    break;
                                case "teleport":
                                    item.Type = ItemTypes.Teleport;
                                    break;
                                case "door":
                                    item.Type = ItemTypes.Door;
                                    break;
                                case "bed":
                                    item.Type = ItemTypes.Bed;
                                    break;
                                case "rune":
                                    item.Type = ItemTypes.Rune;
                                    break;
                                default:
                                    Logger.Log(LogLevels.Warning, "ParseItemNode: Unknown type: "+valueAttribute.InnerText);
                                    break;
                            }
                            break;
                        case "description":
                            item.Description = valueAttribute.InnerText;
                            break;
                        case "runespellname":
                            item.RuneSpellName = valueAttribute.InnerText;
                            break;
                        case "weight":
                            item.Weight = uint.Parse(valueAttribute.InnerText);
                            break;
                        case "showcount":
                            item.ShowCount = int.Parse(valueAttribute.InnerText) != 0;
                            break;
                        case "armor":
                            item.Armor = ushort.Parse(valueAttribute.InnerText);
                            break;
                        case "defense":
                            item.Defense = ushort.Parse(valueAttribute.InnerText);
                            break;
                        case "extradef":
                            item.ExtraDefense = short.Parse(valueAttribute.InnerText);
                            break;
                        case "attack":
                            item.Attack = ushort.Parse(valueAttribute.InnerText);
                            break;
                        case "rotateto":
                            item.RotateTo = ushort.Parse(valueAttribute.InnerText);
                            break;
                        case "movable":
                        case "moveable":
                            item.IsMoveable = int.Parse(valueAttribute.InnerText) != 0;
                            break;
                        case "blockprojectile":
                            item.DoesBlockProjectile = int.Parse(valueAttribute.InnerText) != 0;
                            break;
                        case "pickupable":
                        case "allowpickupable":
                            item.DoesAllowPickupable = int.Parse(valueAttribute.InnerText) != 0;
                            break;
                        case "floorchange":
                            if (!Enum.TryParse(valueAttribute.InnerText, true, out item.FloorChange))
                                Logger.Log(LogLevels.Warning, "ParseItemNode: Unknown floorchange: " + valueAttribute.InnerText);
                            break;
                        case "corpsetype":
                            if (!Enum.TryParse(valueAttribute.InnerText, true, out item.CorpseType))
                                Logger.Log(LogLevels.Warning, "ParseItemNode: Unknown corpsetype: " + valueAttribute.InnerText);
                            break;
                        case "containersize":
                            item.MaxItems = ushort.Parse(valueAttribute.InnerText);
                            break;
                        case "fluidsource":
                            if (!Enum.TryParse(valueAttribute.InnerText, true, out item.FluidSource))
                                Logger.Log(LogLevels.Warning, "ParseItemNode: Unknown fluidsource: " + valueAttribute.InnerText);
                            break;
                        case "readable":
                            item.CanReadText = int.Parse(valueAttribute.InnerText) != 0;
                            break;
                        case "writeable":
                            item.CanWriteText = int.Parse(valueAttribute.InnerText) != 0;
                            item.CanReadText = item.CanWriteText;
                            break;
                        case "maxtextlen":
                            item.MaxTextLength = ushort.Parse(valueAttribute.InnerText);
                            break;
                        case "writeonceitemid":
                            item.WriteOnceItemId = ushort.Parse(valueAttribute.InnerText);
                            break;
                        case "weapontype":
                            if (!Enum.TryParse(valueAttribute.InnerText, true, out item.WeaponType))
                                Logger.Log(LogLevels.Warning, "ParseItemNode: Unknown weapontype: " + valueAttribute.InnerText);
                            break;
                        case "slottype":
                            switch (valueAttribute.InnerText.ToLowerInvariant())
                            {
                                case "head":
                                    item.SlotPosition |= SlotPositionFlags.Head;
                                    break;
                                case "body":
                                    item.SlotPosition |= SlotPositionFlags.Armor;
                                    break;
                                case "legs":
                                    item.SlotPosition |= SlotPositionFlags.Legs;
                                    break;
                                case "feet":
                                    item.SlotPosition |= SlotPositionFlags.Feet;
                                    break;
                                case "backpack":
                                    item.SlotPosition |= SlotPositionFlags.Backpack;
                                    break;
                                case "two-handed":
                                    item.SlotPosition |= SlotPositionFlags.TwoHand;
                                    break;
                                case "right-hand":
                                    item.SlotPosition &= ~SlotPositionFlags.LeftHand;
                                    break;
                                case "left-hand":
                                    item.SlotPosition &= ~SlotPositionFlags.RightHand;
                                    break;
                                case "necklace":
                                    item.SlotPosition |= SlotPositionFlags.Necklace;
                                    break;
                                case "ring":
                                    item.SlotPosition |= SlotPositionFlags.Ring;
                                    break;
                                case "ammo":
                                    item.SlotPosition |= SlotPositionFlags.Ammo;
                                    break;
                                case "hand":
                                    item.SlotPosition |= SlotPositionFlags.Hand;
                                    break;
                                default:
                                    Logger.Log(LogLevels.Warning, "ParseItemNode: Unknown slottype: " + valueAttribute.InnerText);
                                    break;
                            }
                            break;
                        case "ammotype":
                            if (!Enum.TryParse(valueAttribute.InnerText, true, out item.AmmoType))
                                Logger.Log(LogLevels.Warning, "ParseItemNode: Unknown ammotype: " + valueAttribute.InnerText);
                            break;
                        case "shoottype":
                            if (!Enum.TryParse(valueAttribute.InnerText, true, out item.ShootType))
                                Logger.Log(LogLevels.Warning, "ParseItemNode: Unknown shoottype: " + valueAttribute.InnerText);
                            break;
                        case "effect":
                            //if (!Enum.TryParse(valueAttribute.InnerText, true, out item.MagicEffect))
                            //    Logger.Log(LogLevels.Warning, "ParseItemNode: Unknown magiceffect: " + valueAttribute.InnerText);
                            break;
                        case "range":
                            item.ShootRange = byte.Parse(valueAttribute.InnerText);
                            break;
                        case "stopduration":
                            item.StopTime = int.Parse(valueAttribute.InnerText) != 0;
                            break;
                        case "decayto":
                            item.DecayTo = ushort.Parse(valueAttribute.InnerText);
                            break;
                        case "transformequipto":
                            item.TransformEquipTo = ushort.Parse(valueAttribute.InnerText);
                            break;
                        case "transformdeequipto":
                            item.TransformDequipTo = ushort.Parse(valueAttribute.InnerText);
                            break;
                        case "duration":
                            item.DecayTime = uint.Parse(valueAttribute.InnerText);
                            break;
                        case "showduration":
                            item.ShowDuration = int.Parse(valueAttribute.InnerText) != 0;
                            break;
                        case "charges":
                            item.Charges = uint.Parse(valueAttribute.InnerText);
                            break;
                        case "showcharges":
                            item.ShowCharges = int.Parse(valueAttribute.InnerText) != 0;
                            break;
                        case "showattributes":
                            item.ShowAttributes = int.Parse(valueAttribute.InnerText) != 0;
                            break;
                        case "hitchance":
                            item.HitChance = Math.Min((sbyte)100, Math.Max((sbyte)-100, sbyte.Parse(valueAttribute.InnerText)));
                            break;
                        case "maxhitchance":
                            item.MaxHitChance = Math.Min(100, int.Parse(valueAttribute.InnerText));
                            break;
                        case "replaceable":
                            item.IsReplaceable = int.Parse(valueAttribute.InnerText) != 0;
                            break;
                        case "leveldoor":
                            item.LevelDoor = uint.Parse(valueAttribute.InnerText);
                            break;
                        case "malesleeper":
                        case "maletransformto":
                            ushort value = ushort.Parse(valueAttribute.InnerText);

                            if(item.TransformToOnUse == null)
                                item.TransformToOnUse = new ushort[2];

                            item.TransformToOnUse[(int)Genders.Male] = value;

                            ItemTemplate other = Templates[id];
                            if (other.TransformToFree == 0)
                                other.TransformToFree = item.Id;

                            if (item.TransformToOnUse[(int)Genders.Female] == 0)
                                item.TransformToOnUse[(int)Genders.Female] = value;
                            break;
                        case "femalesleeper":
                        case "femaletransformto":
                            value = ushort.Parse(valueAttribute.InnerText);

                            if(item.TransformToOnUse == null)
                                item.TransformToOnUse = new ushort[2];

                            item.TransformToOnUse[(int)Genders.Female] = value;

                            other = Templates[id];
                            if (other.TransformToFree == 0)
                                other.TransformToFree = item.Id;

                            if (item.TransformToOnUse[(int)Genders.Male] == 0)
                                item.TransformToOnUse[(int)Genders.Male] = value;
                            break;
                        case "transformto":
                            item.TransformToFree = ushort.Parse(valueAttribute.InnerText);
                            break;
                        case "walkstack":
                            item.WalkStack = int.Parse(valueAttribute.InnerText) != 0;
                            break;
                        case "blocking":
                            item.DoesBlockSolid = int.Parse(valueAttribute.InnerText) != 0;
                            break;
                        case "allowdistread":
                            item.AllowDistanceRead = Tools.ConvertLuaBoolean(valueAttribute.InnerText);
                            break;
                        case "invisible":
                            item.GetAbilitiesWithInitializer().Invisible = Tools.ConvertLuaBoolean(valueAttribute.InnerText);
                            break;
                        case "speed":
                            item.GetAbilitiesWithInitializer().Speed = int.Parse(valueAttribute.InnerText);
                            break;
                        case "healthgain":
                            Abilities abilities = item.GetAbilitiesWithInitializer();
                            abilities.Regeneration = true;
                            abilities.HealthGain = uint.Parse(valueAttribute.InnerText);
                            break;
                        case "healthticks":
                            abilities = item.GetAbilitiesWithInitializer();
                            abilities.Regeneration = true;
                            abilities.HealthTicks = uint.Parse(valueAttribute.InnerText);
                            break;
                        case "managain":
                            abilities = item.GetAbilitiesWithInitializer();
                            abilities.Regeneration = true;
                            abilities.ManaGain = uint.Parse(valueAttribute.InnerText);
                            break;
                        case "manaticks":
                            abilities = item.GetAbilitiesWithInitializer();
                            abilities.Regeneration = true;
                            abilities.ManaTicks = uint.Parse(valueAttribute.InnerText);
                            break;
                        case "manashield":
                            item.GetAbilitiesWithInitializer().ManaShield = Tools.ConvertLuaBoolean(valueAttribute.InnerText);
                            break;
                        case "skillsword":
                            abilities = item.GetAbilitiesWithInitializer();
                            abilities.InitializeSkillModifiers();
                            abilities.SkillModifiers[(int)Skills.Sword] = int.Parse(valueAttribute.InnerText);
                            break;
                        case "skillaxe":
                            abilities = item.GetAbilitiesWithInitializer();
                            abilities.InitializeSkillModifiers();
                            abilities.SkillModifiers[(int)Skills.Axe] = int.Parse(valueAttribute.InnerText);
                            break;
                        case "skillclub":
                            abilities = item.GetAbilitiesWithInitializer();
                            abilities.InitializeSkillModifiers();
                            abilities.SkillModifiers[(int)Skills.Club] = int.Parse(valueAttribute.InnerText);
                            break;
                        case "skilldist":
                            abilities = item.GetAbilitiesWithInitializer();
                            abilities.InitializeSkillModifiers();
                            abilities.SkillModifiers[(int)Skills.Distance] = int.Parse(valueAttribute.InnerText);
                            break;
                        case "skillfish":
                            abilities = item.GetAbilitiesWithInitializer();
                            abilities.InitializeSkillModifiers();
                            abilities.SkillModifiers[(int)Skills.Fishing] = int.Parse(valueAttribute.InnerText);
                            break;
                        case "skillshield":
                            abilities = item.GetAbilitiesWithInitializer();
                            abilities.InitializeSkillModifiers();
                            abilities.SkillModifiers[(int)Skills.Shield] = int.Parse(valueAttribute.InnerText);
                            break;
                        case "skillfist":
                            abilities = item.GetAbilitiesWithInitializer();
                            abilities.InitializeSkillModifiers();
                            abilities.SkillModifiers[(int)Skills.Fist] = int.Parse(valueAttribute.InnerText);
                            break;
                        case "maxhitpoints":
                            abilities = item.GetAbilitiesWithInitializer();
                            abilities.InitializeStatModifiers();
                            abilities.StatModifiers[(int)Stats.MaxHitPoints] = int.Parse(valueAttribute.InnerText);
                            break;
                        case "maxhitpointspercent":
                            abilities = item.GetAbilitiesWithInitializer();
                            abilities.InitializeStatModifiersPercent();
                            abilities.StatModifiersPercent[(int)Stats.MaxHitPoints] = int.Parse(valueAttribute.InnerText);
                            break;
                        case "maxmanapoints":
                            abilities = item.GetAbilitiesWithInitializer();
                            abilities.InitializeStatModifiers();
                            abilities.StatModifiers[(int)Stats.MaxManaPoints] = int.Parse(valueAttribute.InnerText);
                            break;
                        case "maxmanapointspercent":
                            abilities = item.GetAbilitiesWithInitializer();
                            abilities.InitializeStatModifiersPercent();
                            abilities.StatModifiersPercent[(int)Stats.MaxManaPoints] = int.Parse(valueAttribute.InnerText);
                            break;
                        case "magicpoints":
                        case "magiclevelpoints":
                            abilities = item.GetAbilitiesWithInitializer();
                            abilities.InitializeStatModifiers();
                            abilities.StatModifiers[(int)Stats.MagicPoints] = int.Parse(valueAttribute.InnerText);
                            break;
                        case "magicpointspercent":
                            abilities = item.GetAbilitiesWithInitializer();
                            abilities.InitializeStatModifiersPercent();
                            abilities.StatModifiersPercent[(int)Stats.MagicPoints] = int.Parse(valueAttribute.InnerText);
                            break;
                        case "fieldabsorbpercentenergy":
                            abilities = item.GetAbilitiesWithInitializer();
                            abilities.InitializeFieldAbsorbPercent();
                            abilities.FieldAbsorbPercent[(int)CombatTypeFlags.EnergyDamage] += short.Parse(valueAttribute.InnerText);
                            break;
                        case "fieldabsorbpercentfire":
                            abilities = item.GetAbilitiesWithInitializer();
                            abilities.InitializeFieldAbsorbPercent();
                            abilities.FieldAbsorbPercent[Tools.FlagEnumToArrayPoint((uint)CombatTypeFlags.FireDamage)] += short.Parse(valueAttribute.InnerText);
                            break;
                        case "fieldabsorbpercentpoison":
                        case "fieldabsorpercentearth":
                            abilities = item.GetAbilitiesWithInitializer();
                            abilities.InitializeFieldAbsorbPercent();
                            abilities.FieldAbsorbPercent[Tools.FlagEnumToArrayPoint((uint)CombatTypeFlags.EarthDamage)] += short.Parse(valueAttribute.InnerText);
                            break;
                        case "absorbpercentall":
                        case "absorbpercentallelements":
                            short shortVal = short.Parse(valueAttribute.InnerText);
                            abilities = item.GetAbilitiesWithInitializer();
                            abilities.InitializeAbsorbPercent();

                            for (CombatTypeFlags i = CombatTypeFlags.First; i <= CombatTypeFlags.Last; i++)
                                abilities.AbsorbPercent[Tools.FlagEnumToArrayPoint((uint)i)] += shortVal;
                            break;
                        case "absorbpercentelements":
                            shortVal = short.Parse(valueAttribute.InnerText);
                            abilities = item.GetAbilitiesWithInitializer();
                            abilities.InitializeAbsorbPercent();
                            abilities.AbsorbPercent[Tools.FlagEnumToArrayPoint((uint)CombatTypeFlags.EnergyDamage)] += shortVal;
                            abilities.AbsorbPercent[Tools.FlagEnumToArrayPoint((uint)CombatTypeFlags.EarthDamage)] += shortVal;
                            abilities.AbsorbPercent[Tools.FlagEnumToArrayPoint((uint)CombatTypeFlags.FireDamage)] += shortVal;
                            abilities.AbsorbPercent[Tools.FlagEnumToArrayPoint((uint)CombatTypeFlags.IceDamage)] += shortVal;
                            break;
                        case "absorbpercentmagic":
                            shortVal = short.Parse(valueAttribute.InnerText);
                            abilities = item.GetAbilitiesWithInitializer();
                            abilities.InitializeAbsorbPercent();
                            abilities.AbsorbPercent[Tools.FlagEnumToArrayPoint((uint)CombatTypeFlags.EnergyDamage)] += shortVal;
                            abilities.AbsorbPercent[Tools.FlagEnumToArrayPoint((uint)CombatTypeFlags.DeathDamage)] += shortVal;
                            abilities.AbsorbPercent[Tools.FlagEnumToArrayPoint((uint)CombatTypeFlags.EarthDamage)] += shortVal;
                            abilities.AbsorbPercent[Tools.FlagEnumToArrayPoint((uint)CombatTypeFlags.HolyDamage)] += shortVal;
                            abilities.AbsorbPercent[Tools.FlagEnumToArrayPoint((uint)CombatTypeFlags.FireDamage)] += shortVal;
                            abilities.AbsorbPercent[Tools.FlagEnumToArrayPoint((uint)CombatTypeFlags.IceDamage)] += shortVal;
                            break;
                        case "absorbpercentenergy":
                            abilities = item.GetAbilitiesWithInitializer();
                            abilities.InitializeAbsorbPercent();
                            abilities.AbsorbPercent[Tools.FlagEnumToArrayPoint((uint)CombatTypeFlags.EnergyDamage)] += short.Parse(valueAttribute.InnerText);
                            break;
                        case "absorbpercentfire":
                            abilities = item.GetAbilitiesWithInitializer();
                            abilities.InitializeAbsorbPercent();
                            abilities.AbsorbPercent[Tools.FlagEnumToArrayPoint((uint)CombatTypeFlags.FireDamage)] += short.Parse(valueAttribute.InnerText);
                            break;
                        case "absorbpercentpoison":
                        case "absorbpercentearth":
                            abilities = item.GetAbilitiesWithInitializer();
                            abilities.InitializeAbsorbPercent();
                            abilities.AbsorbPercent[Tools.FlagEnumToArrayPoint((uint)CombatTypeFlags.EarthDamage)] += short.Parse(valueAttribute.InnerText);
                            break;
                        case "absorbpercentice":
                            abilities = item.GetAbilitiesWithInitializer();
                            abilities.InitializeAbsorbPercent();
                            abilities.AbsorbPercent[Tools.FlagEnumToArrayPoint((uint)CombatTypeFlags.IceDamage)] += short.Parse(valueAttribute.InnerText);
                            break;
                        case "absorbpercentholy":
                            abilities = item.GetAbilitiesWithInitializer();
                            abilities.InitializeAbsorbPercent();
                            abilities.AbsorbPercent[Tools.FlagEnumToArrayPoint((uint)CombatTypeFlags.HolyDamage)] += short.Parse(valueAttribute.InnerText);
                            break;
                        case "absorbpercentdeath":
                            abilities = item.GetAbilitiesWithInitializer();
                            abilities.InitializeAbsorbPercent();
                            abilities.AbsorbPercent[Tools.FlagEnumToArrayPoint((uint)CombatTypeFlags.DeathDamage)] += short.Parse(valueAttribute.InnerText);
                            break;
                        case "absorbpercentlifedrain":
                            abilities = item.GetAbilitiesWithInitializer();
                            abilities.InitializeAbsorbPercent();
                            abilities.AbsorbPercent[Tools.FlagEnumToArrayPoint((uint)CombatTypeFlags.LifeDrain)] += short.Parse(valueAttribute.InnerText);
                            break;
                        case "absorbpercentmanadrain":
                            abilities = item.GetAbilitiesWithInitializer();
                            abilities.InitializeAbsorbPercent();
                            abilities.AbsorbPercent[Tools.FlagEnumToArrayPoint((uint)CombatTypeFlags.ManaDrain)] += short.Parse(valueAttribute.InnerText);
                            break;
                        case "absorbpercentdrown":
                            abilities = item.GetAbilitiesWithInitializer();
                            abilities.InitializeAbsorbPercent();
                            abilities.AbsorbPercent[Tools.FlagEnumToArrayPoint((uint)CombatTypeFlags.DrownDamage)] += short.Parse(valueAttribute.InnerText);
                            break;
                        case "absorbpercentphysical":
                            abilities = item.GetAbilitiesWithInitializer();
                            abilities.InitializeAbsorbPercent();
                            abilities.AbsorbPercent[Tools.FlagEnumToArrayPoint((uint)CombatTypeFlags.PhysicalDamage)] += short.Parse(valueAttribute.InnerText);
                            break;
                        case "absorbpercenthealing":
                            abilities = item.GetAbilitiesWithInitializer();
                            abilities.InitializeAbsorbPercent();
                            abilities.AbsorbPercent[Tools.FlagEnumToArrayPoint((uint)CombatTypeFlags.Healing)] += short.Parse(valueAttribute.InnerText);
                            break;
                        case "absorbpercentundefined":
                            abilities = item.GetAbilitiesWithInitializer();
                            abilities.InitializeAbsorbPercent();
                            abilities.AbsorbPercent[Tools.FlagEnumToArrayPoint((uint)CombatTypeFlags.UndefinedDamage)] += short.Parse(valueAttribute.InnerText);
                            break;
                        case "suppressdrunk":
                            if (Tools.ConvertLuaBoolean(valueAttribute.InnerText))
                                item.GetAbilitiesWithInitializer().ConditionSupressions |= ConditionFlags.Drunk;
                            break;
                        case "suppressenergy":
                            if (Tools.ConvertLuaBoolean(valueAttribute.InnerText))
                                item.GetAbilitiesWithInitializer().ConditionSupressions |= ConditionFlags.Energy;
                            break;
                        case "suppressfire":
                            if (Tools.ConvertLuaBoolean(valueAttribute.InnerText))
                                item.GetAbilitiesWithInitializer().ConditionSupressions |= ConditionFlags.Fire;
                            break;
                        case "suppresspoison":
                            if (Tools.ConvertLuaBoolean(valueAttribute.InnerText))
                                item.GetAbilitiesWithInitializer().ConditionSupressions |= ConditionFlags.Poison;
                            break;
                        case "suppressdrown":
                            if (Tools.ConvertLuaBoolean(valueAttribute.InnerText))
                                item.GetAbilitiesWithInitializer().ConditionSupressions |= ConditionFlags.Drown;
                            break;
                        case "suppressphysical":
                            if (Tools.ConvertLuaBoolean(valueAttribute.InnerText))
                                item.GetAbilitiesWithInitializer().ConditionSupressions |= ConditionFlags.Bleeding;
                            break;
                        case "suppressfreeze":
                            if (Tools.ConvertLuaBoolean(valueAttribute.InnerText))
                                item.GetAbilitiesWithInitializer().ConditionSupressions |= ConditionFlags.Freezing;
                            break;
                        case "suppressdazzle":
                            if (Tools.ConvertLuaBoolean(valueAttribute.InnerText))
                                item.GetAbilitiesWithInitializer().ConditionSupressions |= ConditionFlags.Dazzled;
                            break;
                        case "suppresscurse":
                            if (Tools.ConvertLuaBoolean(valueAttribute.InnerText))
                                item.GetAbilitiesWithInitializer().ConditionSupressions |= ConditionFlags.Cursed;
                            break;
                        case "elementice":
                            abilities = item.GetAbilitiesWithInitializer();
                            abilities.ElementDamage = ushort.Parse(valueAttribute.InnerText);
                            abilities.ElementType = CombatTypeFlags.IceDamage;
                            break;
                        case "elementearth":
                            abilities = item.GetAbilitiesWithInitializer();
                            abilities.ElementDamage = ushort.Parse(valueAttribute.InnerText);
                            abilities.ElementType = CombatTypeFlags.EarthDamage;
                            break;
                        case "elementfire":
                            abilities = item.GetAbilitiesWithInitializer();
                            abilities.ElementDamage = ushort.Parse(valueAttribute.InnerText);
                            abilities.ElementType = CombatTypeFlags.FireDamage;
                            break;
                        case "elementenergy":
                            abilities = item.GetAbilitiesWithInitializer();
                            abilities.ElementDamage = ushort.Parse(valueAttribute.InnerText);
                            abilities.ElementType = CombatTypeFlags.EnergyDamage;
                            break;
                        case "partnerdirection":
                            if (!Enum.TryParse(valueAttribute.InnerText, true, out item.BedPartnerDirection))
                                Logger.Log(LogLevels.Warning, "ParseItemNode: Unknown partnerdirection: " + valueAttribute.InnerText);
                            break;
                        case "field":
                            //WILL BE IMPLEMENTED LATER
                            break;
                        default:
                            Logger.Log(LogLevels.Warning, "ParseItemNode: Unknown key value: " + keyAttribute.InnerText);
                            break;
                        // CURRENTLY SKIPPING ATTRIBUTES
                    }
                }
            }
        }
    }

    public enum OtbClientVersion : uint
    {
	    V750 = 1,
	    V755 = 2,
	    V760 = 3,
	    V770 = 3,
	    V780 = 4,
	    V790 = 5,
	    V792 = 6,
	    V800 = 7,
	    V810 = 8,
	    V811 = 9,
	    V820 = 10,
	    V830 = 11,
	    V840 = 12,
	    V841 = 13,
	    V842 = 14,
	    V850 = 15,
	    V854Bad = 16,
	    V854 = 17,
	    V855 = 18,
	    V860Old = 19,
	    V860 = 20,
	    V861 = 21,
	    V862 = 22,
	    V870 = 23,
	    V871 = 24,
	    V872 = 25,
	    V873 = 26,
	    V900 = 27,
	    V910 = 28,
	    V920 = 29,
	    V940 = 30,
	    V944V1 = 31,
	    V944V2 = 32,
	    V944V3 = 33,
	    V944V4 = 34,
	    V946 = 35,
	    V950 = 36,
	    V952 = 37,
	    V953 = 38,
	    V954 = 39,
	    V960 = 40,
	    V961 = 41,
	    V963 = 42,
	    V970 = 43,
	    V980 = 44,
	    V981 = 45,
	    V982 = 46,
	    V983 = 47,
	    V985 = 48,
	    V986 = 49,
	    V1010 = 50,
	    V1020 = 51,
	    V1021 = 52,
	    V1030 = 53,
	    V1031 = 54,
	    V1035 = 55,
        V1076 = 56,
    }
}
