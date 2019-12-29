﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using TShockAPI;
using MySql;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System.IO;
using Microsoft.Xna.Framework;
using System.Reflection;
using Terraria.ID;
using Terraria.Localization;
using System.Runtime.InteropServices;

namespace Starvers
{
	using Events;
	using TaskSystem;
	using DB;
	using BigInt = System.Numerics.BigInteger;
	using Vector = TOFOUT.Terraria.Server.Vector2;
	using Skill = AuraSystem.Skills.Base.Skill;
	public class StarverPlayer
	{
		#region FromTS
		#region Heal
		public void Heal(int health = -1)
		{
			if (health == -1)
			{
				health = TPlayer.statLifeMax2;
			}
			TSPlayer.Heal(health);
		}
		#endregion
		#region GodMode
		public bool GodMode
		{
			get => TSPlayer.GodMode;
			set => TSPlayer.GodMode = value;
		}
		#endregion
		#region SetBuff
		public void SetBuff(int type, int time = 3600, bool bypass = false)
		{
			TSPlayer.SetBuff(type, time, bypass);
		}
		#endregion
		#region RemoveBuff
		public void RemoveBuff(int type)
		{
			int idx = -1;
			for(int i=0;i<TPlayer.buffType.Length;i++)
			{
				if(TPlayer.buffType[i] == type)
				{
					idx = i;
					break;
				}
			}
			if (idx != -1)
			{
				TPlayer.buffTime[idx] = 0;
				TPlayer.buffType[idx] = 0;
				SendData(PacketTypes.PlayerBuff, "", Index);
			}
		}
		#endregion
		#region Sends
		/// <summary>
		/// 发送悬浮文字
		/// </summary>
		/// <param name="msg"></param>
		/// <param name="color"></param>
		public void SendCombatMSsg(string msg, Color color)
		{
			if (Index == -2)
			{
				Console.WriteLine(msg);
			}
			else if (Index >= 0)
			{
				TPlayer.SendCombatMsg(msg, color);
			}
		}
		public void SendMessage(string msg, byte R, byte G, byte B)
		{
			if (UserID == -2)
			{
				Console.WriteLine(msg);
			}
			else if (UserID == -1)
			{
				TSPlayer.All.SendMessage(msg, R, G, B);
			}
			else if (Index >= 0)
			{
				TSPlayer.SendMessage(msg, R, G, B);
				/*
				if (msg.Contains("\n"))
				{
					foreach (string msg2 in msg.Split(new char[]
					{
					'\n'
					}))
					{
						SendMessage(msg2, R, G, B);
					}
					return;
				}
				this.SendData(PacketTypes.SmartTextMessage, msg, 255, R, G, B, -1);
				*/
			}
			else
			{
				TSPlayer.SendInfoMessage(msg);
			}
		}
		public void SendSuccessMessage(string msg)
		{
			SendMessage(msg, Color.DarkGreen);
		}
		public void SendFailMessage(string msg)
		{
			SendMessage(msg, Color.Blue);
		}
		public void SendDeBugMessage(string msg)
		{
#if DEBUG
			SendMessage(msg, Color.Blue);
#endif
		}
		public void SendMessage(string msg, Color color)
		{
			if (Index == -2)
			{
				Console.WriteLine(msg);
			}
			else if (Index >= -1)
			{
				SendMessage(msg, color.R, color.G, color.B);
			}
			else
			{
				TSPlayer.SendMessage(msg, color);
			}
		}
		public void SendInfoMessage(string msg)
		{
			if (Index == -2)
			{
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine(msg);
				Console.ResetColor();
			}
			else if (Index >= -1)
			{
				SendMessage(msg, Color.Yellow);
			}
			else
			{
				TSPlayer.SendInfoMessage(msg);
			}
		}
		public void SendErrorMessage(string msg)
		{
			if (Index == -2)
			{
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine(msg);
				Console.ResetColor();
			}
			else if (Index >= -1)
			{
				SendMessage(msg, Color.Red);
			}
			else
			{
				TSPlayer.SendInfoMessage(msg);
			}
		}
		public void SendInfoMessage(string msg, params object[] args)
		{
			if (Index == -2)
			{
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine(msg, args);
				Console.ResetColor();
			}
			else if (Index >= -1)
			{
				SendInfoMessage(string.Format(msg, args));
			}
			else
			{
				TSPlayer.SendInfoMessage(msg, args);
			}
		}
		public void SendData(PacketTypes msgType, string text = "", int number = 0, float number2 = 0, float number3 = 0, float number4 = 0, int number5 = 0)
		{
			NetMessage.SendData((int)msgType, Index, -1, NetworkText.FromLiteral(text), number, number2, number3, number4, number5);
		}
		private static string EndLine19 = new string('\n', 19);
		private static string EndLine20 = new string('\n', 20);
		public void SendStatusMSG(string msg)
		{
			msg = EndLine19 + msg + EndLine20;
			SendData(PacketTypes.Status, msg);
		}
		#endregion
		#region HasPerm
		public bool HasPerm(string perm)
		{
			return TSPlayer.HasPermission(perm);
		}
		#endregion
		#endregion
		#region FromTPlayer
		#region Others
		public int Team
		{
			get
			{
				return TPlayer.team;
			}
			set
			{
				TPlayer.team = value;
				SendData(PacketTypes.PlayerTeam, "", Index);
			}
		}
		#endregion
		#region Center
		public Vector2 Center
		{
			get
			{
				return TPlayer.Center;
			}
			set
			{
				TPlayer.Center = value;
				SendData(PacketTypes.PlayerUpdate, "", Index);
			}
		}
		#endregion
		#region Position
		public Vector2 Position
		{
			get
			{
				return TPlayer.position;
			}
			set
			{
				TPlayer.position = value;
				SendData(PacketTypes.PlayerUpdate, "", Index);
			}
		}
		#endregion
		#region Velocity
		public Vector2 Velocity
		{
			get
			{
				return TPlayer.velocity;
			}
			set
			{
				TPlayer.velocity = value;
				SendData(PacketTypes.PlayerUpdate, "", Index);
			}
		}
		#endregion
		#endregion
		#region Properties
		#region Zones
		public bool ZoneDirtLayerHeight => TilePoint.Y <= Main.rockLayer && TilePoint.Y > Main.worldSurface;
		public bool ZoneBeach => ZoneOverworldHeight && (TilePoint.X < 380 || TilePoint.X > Main.maxTilesX - 380);
		public bool ZoneOverworldHeight => TilePoint.Y <= Main.worldSurface && TilePoint.Y > Main.worldSurface * 0.349999994039536;
		public bool ZoneRockLayerHeight => TilePoint.Y <= Main.maxTilesY - 200 && (double)TilePoint.Y > Main.rockLayer;
		public bool ZoneRain => Main.raining && TilePoint.Y <= Main.worldSurface;
		public bool ZoneUnderworldHeight => TilePoint.Y > Main.maxTilesY - 200;
		public bool ZoneSkyHeight => TilePoint.Y <= Main.worldSurface * 0.349999994039536;
		#endregion
		#region HeldItem
		public Item HeldItem => TPlayer.inventory[TPlayer.selectedItem];
		#endregion
		#region ActiveChest
		public int ActiveChest
		{
			get
			{
				return TSPlayer.ActiveChest;
			}
			set
			{
				TSPlayer.ActiveChest = value;
				SendData(PacketTypes.ChestOpen, "", value);
			}
		}
		#endregion
		#endregion
		#region Methods
		#region Spawn
		public void Spawn()
		{
			TSPlayer.Spawn();
		}
		#endregion
		#region BranchTaskEnd
		public void BranchTaskEnd(bool success)
		{
			if(success)
			{
				SendInfoMessage($"支线任务{BranchTask}已完成");
				RandomRocket(3, 10);
			}
			else
			{
				SendFailMessage($"支线任务{BranchTask}失败");
			}
			BranchTask = null;
		}
		#endregion
		#region RandRocket
		public void RandomRocket(int min, int Max)
		{
			short[] rockets =
			{
				ProjectileID.RocketFireworkBlue,
				ProjectileID.RocketFireworkGreen,
				ProjectileID.RocketFireworkRed,
				ProjectileID.RocketFireworkYellow
			};
			int count = Starver.Rand.Next(min, Max);
			while (count-- > 0)
			{
				NewProj(Center + Starver.Rand.NextVector2(16 * 15, 16 * 15), Vector2.Zero, rockets.Next(), 0);
			}
		}
		#endregion
		#region GiveItem
		public void GiveItem(int type, int stack = 1, int prefix = 0)
		{
			int number = Item.NewItem((int)Center.X, (int)Center.Y, TPlayer.width, TPlayer.height, type, stack, true, prefix, true, false);
			SendData(PacketTypes.ItemDrop, "", number);
		}
		#endregion
		#region HasItem
		public bool HasItem(int type)
		{
			return TPlayer.HasItem(type);
		}
		#endregion
		#region GetUser
		public static int? GetUserIDByName(string name)
		{
			var reader = TShockAPI.DB.DbExt.QueryReader(TShock.DB, "select * from users where Username=@0", name);
			if (reader.Read())
			{
				return reader.Get<int?>("ID");
			}
			return null;
		}
		public static string GetUserNameByID(int id)
		{
			var reader = TShockAPI.DB.DbExt.QueryReader(TShock.DB, "select * from users where ID=@0", id);
			if (reader.Read())
			{
				return reader.Get<string>("Username");
			}
			return null;
		}
		#endregion
		#region GetBiomes
		public NPCSystem.BiomeType GetBiomes()
		{
			NPCSystem.BiomeType biome = default;
			bool Grass = true;
			#region Zones
			if (TPlayer.ZoneDesert)
			{
				Grass = false;
				biome |= NPCSystem.BiomeType.Dessert;
			}
			if (TPlayer.ZoneHoly)
			{
				Grass = false;
				biome |= NPCSystem.BiomeType.Holy;
			}
			if (TPlayer.ZoneCorrupt)
			{
				Grass = false;
				biome |= NPCSystem.BiomeType.Corrupt;
			}
			if (TPlayer.ZoneCrimson)
			{
				Grass = false;
				biome |= NPCSystem.BiomeType.Crimson;
			}
			if (ZoneDirtLayerHeight || ZoneRockLayerHeight)
			{
				Grass = false;
				biome |= NPCSystem.BiomeType.UnderGround;
			}
			if (TPlayer.ZoneJungle)
			{
				Grass = false;
				biome |= NPCSystem.BiomeType.Jungle;
			}
			if (TPlayer.ZoneSnow)
			{
				Grass = false;
				biome |= NPCSystem.BiomeType.Icy;
			}
			if (TPlayer.ZoneMeteor)
			{
				Grass = false;
				biome |= NPCSystem.BiomeType.Metor;
			}
			if (ZoneRain)
			{
				Grass = false;
				biome |= NPCSystem.BiomeType.Rain;
			}
			if (ZoneUnderworldHeight)
			{
				Grass = false;
				biome |= NPCSystem.BiomeType.Hell;
			}
			if (TPlayer.ZoneTowerSolar)
			{
				Grass = false;
				biome |= NPCSystem.BiomeType.TowerSolar;
			}
			if (TPlayer.ZoneTowerNebula)
			{
				Grass = false;
				biome |= NPCSystem.BiomeType.TowerNebula;
			}
			if (TPlayer.ZoneTowerStardust)
			{
				Grass = false;
				biome |= NPCSystem.BiomeType.TowerStardust;
			}
			if (TPlayer.ZoneTowerVortex)
			{
				Grass = false;
				biome |= NPCSystem.BiomeType.TowerVortex;
			}
			if (ZoneSkyHeight)
			{
				biome |= NPCSystem.BiomeType.Sky;
			}
			if (ZoneBeach)
			{
				biome |= NPCSystem.BiomeType.Beach;
			}
			if (Grass)
			{
				biome |= NPCSystem.BiomeType.Grass;
			}
			#endregion
			return biome;
		}
		#endregion
		#region GetConditions
		public NPCSystem.SpawnConditions GetConditions()
		{
			NPCSystem.SpawnConditions condition = default;
			if (Main.dayTime)
			{
				condition |= NPCSystem.SpawnConditions.Day;
				if (Main.eclipse)
				{
					condition |= NPCSystem.SpawnConditions.Eclipse;
				}
			}
			else
			{
				condition |= NPCSystem.SpawnConditions.Night;
				if (Main.bloodMoon)
				{
					condition |= NPCSystem.SpawnConditions.BloodMoon;
				}
			}
			return condition;
		}
		#endregion
		#region GetSpawnChecker
		public NPCSystem.SpawnChecker GetSpawnChecker()
		{
			return new NPCSystem.SpawnChecker() { Biome = GetBiomes(), Condition = GetConditions() };
		}
		#endregion
		#region EatItems
		/// <summary>
		/// 吃掉玩家背包里从begin起不包括end的物品
		/// </summary>
		/// <param name="begin"></param>
		/// <param name="end"></param>
		public void EatItems(int begin, int end)
		{
			for (; begin < end; begin++)
			{
				TPlayer.inventory[begin].netDefaults(0);
				NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, null, Index, begin);
			}
		}
		#endregion
		#region HasWeapon
		public bool HasWeapon(WeaponSystem.Weapons.Weapon weapon)
		{
			return Weapon[weapon.Career, weapon.Index] > 0;
		}
		#endregion
		#region Update
		public void Update()
		{
			timer++;
			BranchTask?.Updating(timer);
			UpdateCD();
			UpdateTilePoint();
			BranchTask?.Updated(timer);
		}
		protected void UpdateTilePoint()
		{
			TilePoint = Center.ToTileCoordinates();
		}
		protected void UpdateCD()
		{
			for (int i = 0; i < CDs.Length; i++)
			{
				CDs[i] = Math.Max(0, CDs[i] - 1);
			}
		}
		public void UpdateMoon()
		{
			if (Dead)
			{
				if (MoonIndex > 0)
				{
					Main.npc[MoonIndex].active = false;
				}
				return;
			}
			if (MoonIndex < 0)
			{
				MoonIndex = NewMoon();
			}
			else if (
				Main.npc[MoonIndex].type == NPCID.MoonLordCore &&
				Main.npc[MoonIndex].active == false)
			{
				Main.npc[MoonIndex].active = true;
				Main.npc[MoonIndex].SetDefaults(NPCID.MoonLordCore);
			}
			Main.npc[MoonIndex].type = NPCID.MoonLordCore;
			Main.npc[MoonIndex].aiStyle = -1;
			Main.npc[MoonIndex].Center = Center;
			NetMessage.SendData((int)PacketTypes.NpcUpdate, -1, -1, null, MoonIndex);
		}
		public void UpdateMoonClear()
		{
			if (MoonIndex < 0)
			{
				return;
			}
			else if (
				Main.npc[MoonIndex].type == NPCID.MoonLordCore &&
				Main.npc[MoonIndex].active != false &&
				Main.npc[MoonIndex].aiStyle == -1)
			{
				Main.npc[MoonIndex].active = false;
			}
			Main.npc[MoonIndex].type = 0;
			Main.npc[MoonIndex].aiStyle = -1;
			Main.npc[MoonIndex].Center = Center;
			NetMessage.SendData((int)PacketTypes.NpcUpdate, -1, -1, null, MoonIndex);
		}
		#endregion
		#region UPGrade
		/// <summary>
		/// 消耗一定经验升级
		/// </summary>
		/// <param name="ExpGet"></param>
		public void UPGrade(BigInt ExpGet)
		{
			int lvl = level;
			ExpGet += exp;
			int need = AuraSystem.StarverAuraManager.UpGradeExp(lvl);
			if (HasPerm(Perms.VIP.LessCost))
			{
				need /= 3;
			}
			while (ExpGet > need)
			{
				ExpGet -= need;
				++lvl;
				need = AuraSystem.StarverAuraManager.UpGradeExp(lvl);
				if (HasPerm(Perms.VIP.LessCost))
				{
					need /= 3;
				}
			}
			Level = lvl;
			exp = (int)ExpGet;
		}
		#endregion
		#region DataOperations
		#region Read
		/// <summary>
		/// 读取数据(仅限MySQL)
		/// </summary>
		/// <param name="UserID">玩家ID</param>
		/// <returns></returns>
		public static StarverPlayer Read(int UserID)
		{
			StarverPlayer player = new StarverPlayer(UserID);
			if (SaveMode == SaveModes.MySQL)
			{
				using MySqlDataReader result = db.QueryReader("SELECT * FROM Starver WHERE UserID=@0;", UserID);
				if (result.Read())
				{
					player.ReadFromReader(result);
				}
				else
				{
					TSPlayer.Server.SendInfoMessage("StarverPlugins: 玩家{0}不存在,已新建", player.Name);
					AddNewUser(player);
				}
			}
			else
			{
				throw new Exception("非MySQL存储模式下不能通过UserID读取玩家数据");
			}
			player.MP = (player.MaxMP = player.level / 3 + 100) / 2;
			return player;
		}
		public static StarverPlayer Read(string Name, bool suffixed = false)
		{
			StarverPlayer player = new StarverPlayer();
			string tmp = SavePath + "//" + Name;
			if (!suffixed)
			{
				tmp += ".json";
			}
			if (File.Exists(tmp))
			{
				player = JsonConvert.DeserializeObject<StarverPlayer>(File.ReadAllText(tmp));
#if DEBUG
				TSPlayer.Server.SendInfoMessage("Name:{0}", player.Name);
#endif
			}
			else
			{
				player.Name = Name;
			}
			if (!suffixed)
			{
				player.Name = Name;
			}
			player.MP = (player.MaxMP = player.level / 3 + 100) / 2;
			return player;
		}
		#endregion
		#region ReadFromReader
		private static StarverPlayer ReadFromReaderStatic(MySqlDataReader reader)
		{
			StarverPlayer player = new StarverPlayer();
			player.Weapon = JsonConvert.DeserializeObject<byte[,]>(reader.GetString("Weapons"));
			player.ReadSkillFromBinary((byte[])reader.GetValue(reader.GetOrdinal("Skills")));
			player.TBCodes = JsonConvert.DeserializeObject<int[]>(reader.GetString("TBCodes"));
			player.level = reader.GetInt32("Level");
			player.exp = reader.GetInt32("Exp");
			return player;
		}
		private void ReadFromReader(MySqlDataReader reader)
		{
			Weapon = JsonConvert.DeserializeObject<byte[,]>(reader.GetString("Weapons"));
			ReadSkillFromBinary((byte[])reader.GetValue(reader.GetOrdinal("Skills")));
			TBCodes = JsonConvert.DeserializeObject<int[]>(reader.GetString("TBCodes"));
			level = reader.GetInt32("Level");
			exp = reader.GetInt32("Exp");
		}
		#endregion
		#region Add
		/// <summary>
		/// 添加用户
		/// </summary>
		/// <param name="player"></param>
		public static void AddNewUser(StarverPlayer player)
		{
			int UserID = player.UserID;
			int Level = player.Level;
			int Exp = player.Exp;
			string Weapon = JsonConvert.SerializeObject(player.Weapon);
			byte[] buffer = player.SerializeSkill();
			string TBCodes = JsonConvert.SerializeObject(player.TBCodes);
			db.Excute("INSERT INTO Starver (UserId, Weapons,Skills,Level,Exp,TBCodes) VALUES ( @0 ,@1, @2, @3, @4, @5);", UserID, Weapon, buffer, Level, Exp, TBCodes);
		}
		#endregion
		#region Save
		/// <summary>
		/// 保存
		/// </summary>
		public void Save()
		{
			if (UserID == -1)
			{
				return;
			}
			byte[] buffer = SerializeSkill();
			if (SaveMode == SaveModes.MySQL)
			{
				string _Weapon = JsonConvert.SerializeObject(Weapon);
				string _TBCodes = JsonConvert.SerializeObject(TBCodes);
				db.Excute("UPDATE Starver SET Weapons=@0 WHERE UserID=@1;", _Weapon, UserID);
				db.Excute("UPDATE Starver SET Skills=@0 WHERE UserID=@1;", buffer, UserID);
				db.Excute("UPDATE Starver SET TBCodes=@0 WHERE UserID=@1;", _TBCodes, UserID);
				db.Excute("UPDATE Starver SET Level=@0 WHERE UserID=@1;", level, UserID);
				db.Excute("UPDATE Starver SET Exp=@0 WHERE UserID=@1;", Exp, UserID);
			}
			else
			{
				File.WriteAllText(SavePath + "//" + Name + ".json", JsonConvert.SerializeObject(this, Formatting.Indented));
			}
		}
		#endregion
		#region Reload
		/// <summary>
		/// 重新加载
		/// </summary>
		public void Reload()
		{
			StarverPlayer tempplayer;
			if (SaveMode == SaveModes.MySQL)
			{
				tempplayer = Read(UserID);
			}
			else
			{
				tempplayer = Read(Name);
			}
			level = tempplayer.level;
			Skills = tempplayer.Skills;
			Exp = tempplayer.Exp;
			TBCodes = tempplayer.TBCodes;
			Weapon = tempplayer.Weapon;
			Save();
		}
		#endregion
		#region ReadSkillFromBinary
		private void ReadSkillFromBinary(byte[] buffer)
		{
			if (buffer.Length < Skills.Length * sizeof(int))
			{
				throw new ArgumentException($"无效的buffer长度: {buffer.Length}(要求: {Skills.Length * sizeof(int)})", nameof(buffer));
			}
			for (int i = 0; i < Skills.Length; i++)
			{
				Skills[i] += buffer[i * sizeof(int) + 3];
				Skills[i] <<= sizeof(byte);

				Skills[i] += buffer[i * sizeof(int) + 2];
				Skills[i] <<= sizeof(byte);

				Skills[i] += buffer[i * sizeof(int) + 1];
				Skills[i] <<= sizeof(byte);

				Skills[i] += buffer[i * sizeof(int) + 0];
			}
		}
		#endregion
		#region SerializeSkill
		private byte[] SerializeSkill()
		{
			byte[] buffer = new byte[Skills.Length * sizeof(int)];
			var writer = new BinaryWriter(new MemoryStream(buffer));
			for (int i = 0; i < Skills.Length; i++)
			{
				writer.Write(Skills[i]);
			}
			return buffer;
		}
		#endregion
		#endregion
		#region SetLifeMax
		public void SetLifeMax()
		{
			if (TPlayer.statLifeMax2 < 500)
			{
				return;
			}
			int Life = 500 + Utils.CalculateLife(level);
			TPlayer.SetLife(Life);
		}
		#endregion
		#region Projs
		#region FromPolar
		/// <summary>
		/// 极坐标获取角度
		/// </summary>
		/// <param name="rad">所需角度(弧度)</param>
		/// <param name="length"></param>
		/// <returns></returns>
		public Vector FromPolar(double rad, float length)
		{
			return Vector.FromPolar(rad, length);
		}
		#endregion
		#region NewProj
		/// <summary>
		/// 生成弹幕
		/// </summary>
		/// <param name="position"></param>
		/// <param name="velocity"></param>
		/// <param name="Type"></param>
		/// <param name="Damage"></param>
		/// <param name="KnockBack"></param>
		/// <param name="ai0"></param>
		/// <param name="ai1"></param>
		/// <returns></returns>
		public int NewProj(Vector2 position, Vector2 velocity, int Type, int Damage, float KnockBack = 20f, float ai0 = 0, float ai1 = 0)
		{
			return Utils.NewProj(position, velocity, Type, Damage, KnockBack, Index, ai0, ai1);
		}
		#endregion
		#region ProjCircle
		/// <summary>
		/// 弹幕圆
		/// </summary>
		/// <param name="Center"></param>
		/// <param name="r"></param>
		/// <param name="Vel">速率</param>
		/// <param name="Type"></param>
		/// <param name="number">弹幕总数</param>
		/// <param name="direction">0:不动 1:向内 2:向外</param>
		public void ProjCircle(Vector2 Center, float r, float Vel, int Type, int number, int Damage, byte direction = 1, float ai0 = 0, float ai1 = 0)
		{
			switch (direction)
			{
				case 0:
					Vel = 0;
					break;
				case 1:
					Vel *= 1;
					break;
				case 2:
					Vel *= -1;
					break;
			}
			double averagerad = Math.PI * 2 / number;
			for (int i = 0; i < number; i++)
			{
				NewProj(Center + FromPolar(averagerad * i, r), FromPolar(averagerad * i, -Vel), Type, Damage, 4f, ai0, ai1);
			}
		}
		/// <summary>
		/// 弹幕圆
		/// </summary>
		/// <param name="Center"></param>
		/// <param name="angle">偏转角</param>
		/// <param name="r"></param>
		/// <param name="Vel">速率</param>
		/// <param name="Type"></param>
		/// <param name="number">弹幕总数</param>
		/// <param name="direction">0:不动 1:向内 2:向外</param>
		public void ProjCircleEx(Vector2 Center, double angle, float r, float Vel, int Type, int number, int Damage, byte direction = 1, float ai0 = 0, float ai1 = 0)
		{
			switch (direction)
			{
				case 0:
					Vel = 0;
					break;
				case 1:
					Vel *= 1;
					break;
				case 2:
					Vel *= -1;
					break;
			}
			double averagerad = Math.PI * 2 / number;
			for (int i = 0; i < number; i++)
			{
				NewProj(Center + FromPolar(averagerad * i, r), FromPolar(angle + averagerad * i, -Vel), Type, Damage, 4f, ai0, ai1);
			}
		}
		#endregion
		#region ProjCircle
		/// <summary>
		/// 弹幕圆
		/// </summary>
		/// <param name="Center"></param>
		/// <param name="r"></param>
		/// <param name="Vel">速率</param>
		/// <param name="Type"></param>
		/// <param name="number">弹幕总数</param>
		/// <param name="direction">0:不动 1:向内 2:向外</param>
		public int[] ProjCircleRet(Vector2 Center, float r, float Vel, int Type, int number, int Damage, byte direction = 1, float ai0 = 0, float ai1 = 0)
		{
			switch (direction)
			{
				case 0:
					Vel = 0;
					break;
				case 1:
					Vel *= 1;
					break;
				case 2:
					Vel *= -1;
					break;
			}
			double averagerad = Math.PI * 2 / number;
			int[] arr = new int[number];
			for (int i = 0; i < number; i++)
			{
				arr[i] = NewProj(Center + FromPolar(averagerad * i, r), FromPolar(averagerad * i, -Vel), Type, Damage, 4f, ai0, ai1);
			}
			return arr;
		}
		#endregion
		#region ProjSector
		/// <summary>
		/// 扇形弹幕
		/// </summary>
		/// <param name="Center">圆心</param>
		/// <param name="Vel">速率</param>
		/// <param name="r">半径</param>
		/// <param name="interrad">中心半径的方向</param>
		/// <param name="rad">张角</param>
		/// <param name="Damage">伤害(带加成)</param>
		/// <param name="Type"></param>
		/// <param name="num">数量</param>
		/// <param name="direction">0:不动 1:向内 2:向外</param>
		/// <param name="ai0"></param>
		/// <param name="ai1"></param>
		public void ProjSector(Vector2 Center, float Vel, float r, double interrad, double rad, int Damage, int Type, int num, byte direction = 2, float ai0 = 0, float ai1 = 0)
		{
			double start = interrad - rad / 2;
			double average = rad / num;
			switch (direction)
			{
				case 0:
					Vel *= 0;
					break;
				case 1:
					Vel *= -1;
					break;
				case 2:
					Vel *= 1;
					break;
			}
			for (int i = 0; i < num; i++)
			{
				NewProj(Center + FromPolar(start + i * average, r), FromPolar(start + i * average, Vel), Type, Damage, 4f, ai0, ai1);
			}
		}
		#endregion
		#region ProjLine
		/// <summary>
		/// 制造速度平行的弹幕直线
		/// </summary>
		/// <param name="Begin">起点</param>
		/// <param name="End">终点</param>
		/// <param name="Vel">速度</param>
		/// <param name="num">数量</param>
		/// <param name="Damage"></param>
		/// <param name="type"></param>
		/// <param name="ai0"></param>
		/// <param name="ai1"></param>
		public void ProjLine(Vector2 Begin, Vector2 End, Vector2 Vel, int num, int Damage, int type, float ai0 = 0, float ai1 = 0)
		{
			Vector2 average = End - Begin;
			average /= num;
			for (int i = 0; i < num; i++)
			{
				NewProj(Begin + average * i, Vel, type, Damage, 3f, ai0, ai1);
			}
		}
		/// <summary>
		/// 制造速度平行的弹幕直线
		/// </summary>
		/// <param name="Begin">起点</param>
		/// <param name="End">终点</param>
		/// <param name="Vel">速度</param>
		/// <param name="num">数量</param>
		/// <param name="Damage"></param>
		/// <param name="type"></param>
		/// <param name="ai0"></param>
		/// <param name="ai1"></param>
		public int[] ProjLineReturns(Vector2 Begin, Vector2 End, Vector2 Vel, int num, int Damage, int type, float ai0 = 0, float ai1 = 0)
		{
			int[] arr = new int[num];
			Vector2 average = End - Begin;
			average /= num;
			for (int i = 0; i < num; i++)
			{
				arr[i] = NewProj(Begin + average * i, Vel, type, Damage, 3f, ai0, ai1);
			}
			return arr;
		}
		#endregion
		#endregion
		#region Operator
		public static implicit operator TSPlayer(StarverPlayer player)
		{
			return player.TSPlayer;
		}
		public static implicit operator Player(StarverPlayer player)
		{
			return player.TPlayer;
		}
		public static implicit operator int(StarverPlayer player)
		{
			return player.Index;
		}
		#endregion
		#region SetSkill
		/// <summary>
		/// 设置技能
		/// </summary>
		/// <param name="name">技能名称(可以只取前几位字母)</param>
		/// <param name="slot">槽位</param>
		/// <param name="ServerDoThis">是否无视等级设置</param>
		public unsafe void SetSkill(string name, int slot, bool ServerDoThis = false)
		{
			slot -= 1;
			if (slot < 0 || slot > AuraSystem.SkillManager.Skills.Length)
			{
				TSPlayer.SendErrorMessage("槽位错误!");
			}
			else
			{
				name = name.ToLower();
				foreach (var skill in AuraSystem.SkillManager.Skills)
				{
					if (skill.Name.ToLower().IndexOf(name) == 0)
					{
						if (ServerDoThis == false && !skill.CanSet(this))
						{
							SendErrorMessage("设置失败");
						}
						else
						{
							Skills[slot] = skill.Index;
							if (!ServerDoThis)
							{
								TSPlayer.SendSuccessMessage("设置成功");
							}
						}
						if (!ServerDoThis)
						{
							TSPlayer.SendInfoMessage(skill.Text);
						}
						return;
					}
				}
				if (!ServerDoThis)
				{
					TSPlayer.SendErrorMessage("错误的技能名");
					TSPlayer.SendErrorMessage("list:	查看技能列表");
				}
			}
		}
		#endregion
		#region Damage
		public void Damage(int damage)
		{
			damage = Math.Min(23000, damage);
			TSPlayer.DamagePlayer(damage);
			//NetMessage.SendPlayerHurt(Index, PlayerDeathReason.LegacyDefault(), damage, Index, false, false, 0);
		}
		public void Damage(int damage, Color effectTextColor)
		{
			damage = Math.Min(23000, damage);
			TSPlayer.DamagePlayer(damage);
			SendCombatMSsg(damage.ToString(), effectTextColor);
			//NetMessage.SendPlayerHurt(Index, PlayerDeathReason.LegacyDefault(), damage, Index, false, false, 0);
		}
		#endregion
		#region ShowInfos
		/// <summary>
		/// 调试使用
		/// </summary>
		/// <param name="ToWho">展示信息给谁</param>
		public void ShowInfos(TSPlayer ToWho = null)
		{
			ToWho = ToWho ?? TSPlayer.Server;
			PropertyInfo[] infos = GetType().GetProperties();
			foreach (var info in infos)
			{
				try
				{
					if (info.CanWrite)
						ToWho.SendInfoMessage("\"{0}\" :{1}", info.Name, JsonConvert.SerializeObject(info.GetValue(this)));
				}
				catch
				{

				}
			}
		}
		#endregion
		#region Kick
		public void Disconnect(string reason)
		{
			TSPlayer.Disconnect(reason);
		}
		public void Kick(string reason, bool silence = false)
		{
			Disconnect(reason);
			if(!silence)
			{
				StarverPlayer.All.SendErrorMessage($"玩家{Name} 被 Kick了: {reason}");
			}
		}
		#endregion
		#region GetSkill
		public unsafe Skill GetSkill(int slot)
		{
#if DEBUG
			if (slot < 0 && slot >= Skill.MaxSlots)
			{
				throw new IndexOutOfRangeException($"slot: {slot}");
			}
#endif
			return AuraSystem.SkillManager.Skills[Skills[slot]];
		}
		#endregion
		#region SkillCombineCD
		public string SkillCombineCD(int slot)
		{
			Skill skill = GetSkill(slot);
			return $"{skill.Name}({(CDs[slot] + 59) / 60})";
		}
		#endregion
		#region TryGetPlayer
		public static bool TryGetTempPlayer(string Name, out StarverPlayer player)
		{
			player = null;
			switch (SaveMode)
			{
				case SaveModes.MySQL:
					{
						int? ID = GetUserIDByName(Name);
						if (!ID.HasValue)
						{
							return false;
						}
						using MySqlDataReader reader = db.QueryReader("select * from starver where UserID=@0", ID.Value);
						if (reader.Read())
						{
							player = new StarverPlayer(ID.Value, true);
							player.ReadFromReader(reader);
							return true;
						}
						return false;
					}
				case SaveModes.Json:
					{
						string path = Path.Combine(SavePath, $"{Name}.json");
						if (File.Exists(path))
						{
							try
							{
								player = JsonConvert.DeserializeObject<StarverPlayer>(File.ReadAllText(path));
								player.Temp = true;
							}
							catch (Exception e)
							{
								TShock.Log.Error(e.ToString());
								return false;
							}
							return true;
						}
						return false;
					}
			}
			return false;
		}
		#endregion
		#region KillMe
		public void KillMe()
		{
			TSPlayer.KillPlayer();
		}
		#endregion
		#endregion
		#region Hooks
		public void StrikingNPC(NPCStrikeEventArgs args)
		{
			BranchTask?.StrikingNPC(args);
		}
		public void StrikedNPC(NPCStrikeEventArgs args)
		{
			BranchTask?.StrikedNPC(args);
		}
		public void ReleasingSkill(ReleaseSkillEventArgs args)
		{
			BranchTask?.ReleasingSkill(args);
		}
		public void ReleasedSkill(ReleaseSkillEventArgs args)
		{
			BranchTask?.ReleasedSkill(args);
		}
		#endregion
		#region Datas
		/// <summary>
		/// 技能CD
		/// </summary>
		public int[] CDs { get; set; }
		/// <summary>
		/// 技能ID列表
		/// </summary>
		public int[] Skills { get; set; }
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
		public bool Temp { get; set; }
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
		public BranchTask BranchTask { get; set; }
		public int AvalonGradation { get; set; }
		public string Name { get; set; }
		/// <summary>
		/// 上一次捕获到释放技能
		/// </summary>
		public DateTime LastHandle { get; set; } = DateTime.Now;
		/// <summary>
		/// 玩家升级经验是否更少
		/// </summary>
		public bool LessCost => HasPerm(Perms.VIP.LessCost);
		/// <summary>
		/// 玩家存活且在线
		/// </summary>
		public bool Active => TPlayer.active && !TPlayer.dead;
		public bool Dead => TPlayer.dead;
		public bool IgnoreCD { get; set; }
		public bool ForceIgnoreCD { get; set; }
		public bool IsGuest => UserID == -3;
		/// <summary>
		/// 在MySql中的UserID
		/// <para> -1代表All</para>
		/// <para>-2代表服务器</para>
		/// -3为Guest(未注册)
		/// </summary>
		public int UserID { get; private set; }
		/// <summary>
		/// 玩家索引
		/// </summary>
		public int Index { get; internal set; }
		/// <summary>
		/// 当前等级
		/// 更改后会自动改变血量以及MP上限
		/// </summary>
		public int Level
		{
			get
			{
				return level;
			}
			set
			{
				if (level == int.MaxValue)
				{
					return;
				}
				level = value;
				if (Temp)
				{
					return;
				}
				SetLifeMax();
				MaxMP = 100 + (level / 3);
			}
		}
		/// <summary>
		/// 当前经验
		/// </summary>
		public int Exp
		{
			get
			{
				return exp;
			}
			set
			{
				if (value < exp)
				{
					exp = value;
					return;
				}
				if (Temp)
				{
					exp = value;
					return;
				}
				long expNow = value;
				long lvl = level;
				int Need = AuraSystem.StarverAuraManager.UpGradeExp((int)lvl);
				if (HasPerm(Perms.VIP.LessCost))
				{
					Need /= 3;
				}
				while (expNow > UpGradeExp)
				{
					expNow -= Need;
					lvl++;
					Need = AuraSystem.StarverAuraManager.UpGradeExp((int)lvl);
					if (HasPerm(Perms.VIP.LessCost))
					{
						Need /= 3;
					}
				}
				Level = (int)lvl;
				exp = (int)Math.Max(0, expNow);
			}
		}
		public int MP
		{
			get
			{
				return mp;
			}
			set
			{
				mp = Math.Min(value, MaxMP);
				mp = Math.Max(0, mp);
			}
		}
		/// <summary>
		/// 当前升级所需经验
		/// </summary>
		public int UpGradeExp => AuraSystem.StarverAuraManager.UpGradeExp(level);
		public int MaxMP { get; set; }
		public byte[,] Weapon { get; set; } =
		{
			{ 0, 0, 0, 0 },
			{ 0, 0, 0, 0 },
			{ 0, 0, 0, 0 },
			{ 0, 0, 0, 0 }
		};
		public int Life
		{
			get
			{
				return TPlayer.statLife;
			}
			set
			{
				TPlayer.statLife = Math.Min(value, TPlayer.statLifeMax2);
				SendData(PacketTypes.PlayerHp, string.Empty, Index);
			}
		}
		//以下4个属性均为支线任务(未启用)使用
		public int FishCode
		{
			get => TBCodes[0];
			set => TBCodes[0] = value;
		}
		public int MineCode
		{
			get => TBCodes[1];
			set => TBCodes[1] = value;
		}
		public int CollectCode
		{
			get => TBCodes[2];
			set => TBCodes[2] = value;
		}
		public int HunterCode
		{ 
			get => TBCodes[3]; 
			set => TBCodes[3] = value;
		}
		/// <summary>
		/// 数据库连接
		/// </summary>
		internal static MySqlConnection DB
		{
			get => db;
			set => db = value;
		}
		/// <summary>
		/// 代表服务器
		/// </summary>
		internal static StarverPlayer Server => server;
		/// <summary>
		/// 代表all(仅用于SendMessage和SendData)
		/// </summary>
		internal static StarverPlayer All => all;
		/// <summary>
		/// 用于未注册用户(退出后消失)
		/// </summary>
		internal static StarverPlayer Guest => new StarverPlayer() { Name = "Guest", level = 0, UserID = -3, Index = -3 };
		/// <summary>
		/// 上一次使用的技能
		/// </summary>
		internal int LastSkill;
		internal int mp;
		internal int exp;
		/// <summary>
		/// 获取对应Terraria.Player
		/// </summary>
		internal Player TPlayer => TSPlayer.TPlayer;
		/// <summary>
		/// 获取对应TSPlayer
		/// </summary>
		internal TSPlayer TSPlayer => UserID switch
		{
			-2 => TSPlayer.Server,
			-1 => TSPlayer.All,
			_ => TShock.Players[Index]
		};
		/// <summary>
		/// 支线任务使用,暂时没用
		/// </summary>
		internal int[] TBCodes = { 0, 0, 0, 0 };
		private int level = StarverConfig.Config.DefaultLevel;
		#endregion
		#region Privates
		#region Fields
		private Point TilePoint;
		private int timer;
		private bool IsServer;
		private int MoonIndex = -1;
		private static StarverPlayer all = new StarverPlayer() { Name = "All", level = int.MaxValue, Index = -1, UserID = -1 };
		private static StarverPlayer server = new StarverPlayer() { Name = "Server", level = int.MaxValue, Index = -2, UserID = -2, IsServer = true };
		private static string SavePath => Starver.SavePathPlayers;
		private static SaveModes SaveMode = StarverConfig.Config.SaveMode;
		private static MySqlConnection db;
		#endregion
		#region Ctor
		private StarverPlayer(bool temp = false)
		{
			Temp = temp;
			Skills = new int[Skill.MaxSlots];
			CDs = new int[Skill.MaxSlots];
		}
		private StarverPlayer(int userID, bool temp = false) : this(temp)
		{
			UserID = userID;
			Name = GetUserNameByID(UserID);
			if (temp)
				return;
			if (!Starver.IsPE)
			{
				dynamic ply;
				for (int i = 0; i < Starver.Players.Length; i++)
				{
					ply = TShock.Players[i];
					if (TShock.Players[i] == null || TShock.Players[i].Active == false || ply.User.ID != userID)
					{
						continue;
					}
					Index = i;
					break;
				}
			}
			else
			{
				dynamic ply;
				for (int i = 0; i < Starver.Players.Length; i++)
				{
					ply = TShock.Players[i];
					if (TShock.Players[i] == null || TShock.Players[i].Active == false || ply.Account.ID != userID)
					{
						continue;
					}
					Index = i;
					break;
				}
			}
		}
		#endregion
		#region NewMoon
		private static int NewMoon()
		{
			int i;
			for (i = 0; i < 200; i++)
			{
				if (!Main.npc[i].active)
				{
					Main.npc[i] = new NPC();
					Main.npc[i].SetDefaults(NPCID.MoonLordCore);
					Main.npc[i].aiStyle = -1;
					NetMessage.SendData((int)PacketTypes.NpcUpdate, -1, -1, null, i);
					break;
				}
			}
			return i;
		}
		#endregion
		#region NewConnection
		private static MySqlConnection NewConnection()
		{
			string[] dbHost = StarverConfig.Config.MySQLHost.Split(':');
			return new MySqlConnection()
			{
				ConnectionString = string.Format("Server={0}; Port={1}; Database={2}; UserName={3}; Password={4}; Allow User Variables=True;",
					dbHost[0],
					dbHost[1],
					StarverConfig.Config.MySQLDBName,
					StarverConfig.Config.MySQLUserName,
					StarverConfig.Config.MySQLPassword)
			};
		}
		#endregion
		#region cctor
		static StarverPlayer()
		{
			//StarverConfig.Config = StarverConfig.Read();
			switch (StarverConfig.Config.SaveMode)
			{
				case SaveModes.MySQL:
					{
						db = NewConnection();
						var db2 = new MySqlConnection(db.ConnectionString);
						db2.Open();
						var creator = new TableCreator(db2);
						var Table = new SQLTable("Starver",
							new SQLColumn { Name = "UserID", DataType = MySqlDbType.Int32, Length = 4 },
							new SQLColumn { Name = "Level", DataType = MySqlDbType.Int32, Length = 4 },
							new SQLColumn { Name = "Exp", DataType = MySqlDbType.Int32, Length = 4 },
							new SQLColumn { Name = "TBCodes", DataType = MySqlDbType.Text, Length = 20 },
							new SQLColumn { Name = "Skills", DataType = MySqlDbType.VarBinary, Length = 30 },
							new SQLColumn { Name = "Weapons", DataType = MySqlDbType.Text, Length = 80 }
							);
						creator.CreateTable(Table);
						break;
					}
				case SaveModes.Json:
					{
						SaveMode = SaveModes.Json;
						break;
					}
			}
			TSPlayer.Server.SendInfoMessage("Config.SaveMode:{0}", StarverConfig.Config.SaveMode);
			TSPlayer.Server.SendInfoMessage("SaveMode:{0}", SaveMode);
		}
		#endregion
		#endregion
	}
}
