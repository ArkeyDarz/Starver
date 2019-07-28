﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;
using Terraria.Localization;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security;
using System.IO;

namespace Starvers.BossSystem.Bosses.Base
{
	using Vector = TOFOUT.Terraria.Server.Vector2;
	using BigInt = System.Numerics.BigInteger;
	public abstract partial class StarverBoss : StarverEntity
	{
		#region Properties
		public int TaskNeed { get; protected set; } = 0;
		public int Life { get { return RealNPC.life; } set { RealNPC.life = value; } }
		public int LifeMax { get { return RealNPC.lifeMax; } set { RealNPC.lifeMax = value; } }
		public int Defense { get { return RealNPC.defense; } set { RealNPC.defense = value; } }
		public int RawType { get; protected set; } = NPCID.BlueSlime;
		public float MaxDistance { get; protected set; } = 500f;
		public bool IgnoreDistance { get; protected set; } = true;
		public bool Downed { get; protected set; }
		public virtual bool CanSpawn => StarverConfig.Config.TaskNow >= TaskNeed;
		/// <summary>
		/// 召唤时无提示
		/// </summary>
		public bool Silence { get; protected set; } 
		public string Name { get; protected set; }
		public Vector2 LastCenter { get; protected set; }
		public string DownedMessage { get; protected set; }
		public string ComingMessage { get; protected set; } = "这将是一场灾难...";
		public Color ComingMessageColor { get; protected set; } = Color.DarkGreen;
		public Color DownedMessageColor { get; protected set; } = Color.DarkGreen;
		/// <summary>
		/// boss伤害系数
		/// </summary>
		public override float DamageIndex
		{
			get
			{
				return (float)(Math.Sqrt(LifesMax / (Lifes + 1)) + (float)Level / CriticalLevel);
			}
		}
		/// <summary>
		/// boss当前状态
		/// </summary>
		protected BossMode Mode
		{
			get
			{
				return mode;
			}
			set
			{
				mode = value;
				LastCenter = Center;
#if DEBUG
				TSPlayer.All.SendInfoMessage("{0}的模式由{1}更改为{2}", Name, lastMode, mode);
#endif
			}
		}
		/// <summary>
		/// 相对位置(要加上玩家坐标)
		/// </summary>
		protected Vector2 RelativePos
		{
			get
			{
				return Center - TargetPlayer.Center;
			}
			set
			{
				Center = TargetPlayer.Center + value;
			}
		}
		#endregion
		#region Fields
		protected bool NightBoss = true;
		protected bool ExVersion;
		/// <summary>
		/// 是否检查NPC种类
		/// </summary>
		protected bool CheckType = true;
		protected string DisplayName;
		protected string FullName;
		protected int Lifes;
		protected int LifesMax;
		protected int DefaultLifes;
		protected int DefaultLife;
		protected int DefaultDefense = 10;
		protected unsafe float* StarverAI;
		protected uint modetime;
		protected byte LifeperPlayerType = ByLife;
		protected Vector vector = new Vector(10, 10);
		protected Vector WhereToGo;
		protected BossMode lastMode;
		private string DataPath;
		private string TypeName;
		private int[] Walls;
		private int TimeSpan;
		private bool WallStop;
		private StarverPlayer WallTarget;
		private Vector[] WallVector;
		private Vector StopPos;
		private bool WallActive;
		private BossMode mode;
		private readonly int NumOfAIs;
		private const int MaxWalls = 30;
		private const int WallDamage = 2250;
		#endregion
		#region ctor
		private StarverBoss()
		{
			TypeName = Name = GetType().Name;
		}
		protected unsafe StarverBoss(int ainum) : this()
		{
			string path = Path.Combine(Starver.SavePathBosses, TypeName);
			DataPath = path;
			if (!File.Exists(path))
			{
				using (BinaryWriter writer = new BinaryWriter(new FileStream(path, FileMode.Create)))
				{
					writer.Write(Downed);
				}
			}
			else
			{
				using (BinaryReader reader = new BinaryReader(new FileStream(path, FileMode.Open)))
				{
					Downed = reader.ReadBoolean();
				}
			}
			NumOfAIs = ainum;
			StarverAI = (float*)Marshal.AllocHGlobal(sizeof(float) * NumOfAIs).ToPointer();
			Level = CriticalLevel;
			DefaultLife = 40000;
			DefaultDefense = 60;
			DefaultLifes = 90;
		}
		#endregion
		#region dtor
		unsafe ~StarverBoss()
		{
			if (NumOfAIs > 0)
			{
				Marshal.FreeHGlobal(new IntPtr(StarverAI));
			}
		}
		#endregion
		#region KillMe
		public override void KillMe()
		{
			if(!_active)
			{
				return;
			}
			AliveBoss--;
			Lifes = 0;
			base.KillMe();
			_active = false;
			Index = -1;
		}
		#endregion
		#region BeDown
		protected virtual void BeDown()
		{
			if(DownedMessage != null)
			{
				StarverPlayer.All.SendMessage(DownedMessage, DownedMessageColor);
			}
			if(Downed != true)
			{
				using(BinaryWriter writer = new BinaryWriter(new FileStream(DataPath,FileMode.Create)))
				{
					writer.Write(Downed = true);
				}
			}
			_active = false;
			ExpGive = LifeMax;
			ExpGive *= LifesMax;
			for (int i = 0; i < 40; ++i)
			{
				if (RealNPC.playerInteraction[i])
				{
					Starver.Players[i].UPGrade(ExpGive);
				}
			}
			if (Drops != null)
			{
				DropItems();
			}
			KillMe();
		}
		#endregion
		#region DropItems
		protected void DropItems()
		{
			foreach (var item in Drops)
			{
				item.Drop(Main.npc[Index]);
			}
		}
		#endregion
		#region CheckTarget
		public void CheckTarget()
		{
			if (Target < 0 || Target > 40 || TargetPlayer == null || !TargetPlayer.Active)
			{
				TargetClosest();
				if (Target == -1)
				{
					if (AlivePlayers() < 0 && TShock.Utils.ActivePlayers() > 0)
					{
						OnFail();
					}
					else
					{
						KillMe();
					}
				}
			}
		}
		#endregion
		#region SummonFollows
		protected unsafe void SummonFollows()
		{
			int alive = AlivePlayers();
			int idx = NPC.NewNPC((int)Center.X, (int)Center.Y, (int)StarverAI[2], Index);
			Main.npc[idx].life = Main.npc[idx].lifeMax = alive * 400;
			Main.npc[idx].defense = 78;
			Main.npc[idx].velocity = Rand.NextVector2(26.66f);
			NetMessage.SendData((int)PacketTypes.NpcUpdate, -1, -1, null, idx);
			idx = NPC.NewNPC((int)Center.X, (int)Center.Y, (int)StarverAI[3], Index);
			Main.npc[idx].life = Main.npc[idx].lifeMax = alive * 400;
			Main.npc[idx].defense = 78;
			Main.npc[idx].velocity = Rand.NextVector2(26.66f);
			NetMessage.SendData((int)PacketTypes.NpcUpdate, -1, -1, null, idx);
		}
		#endregion
		#region OnFail
		public virtual void OnFail()
		{

		}
		#endregion
		#region GoToTarget
		public void GotoTarget()
		{
			Center = TargetPlayer.Center + NewByPolar(Rand.NextAngle(), 16 * 16);
			SendData();
		}
		#endregion
		#region ResetMode
		protected void ResetMode()
		{
			lastMode = mode;
			mode = BossMode.WaitForMode;
		}
		#endregion
		#region Rush Begin
		protected void RushBegin(bool NoRush = false)
		{
			modetime = 0;
			lastMode = mode;
			mode = BossMode.Rush;
			if (!NoRush)
			{
				Center = TargetPlayer.Center + Rand.NextVector2(16 * 38f);
				Rush();
			}
		}
		#endregion
		#region Rush
		protected void Rush()
		{
			FakeVelocity = (Vector)(TargetPlayer.Center - Center) ;
			FakeVelocity.Length = 26 * DamageIndex;
		}
		#endregion
		#region Spawn
		public unsafe virtual void Spawn(Vector2 where, int lvl = CriticalLevel)
		{
			if (_active)
			{
				return;
			}
			_active = true;
			Level = lvl;
			ExVersion = Level > CriticalLevel;
			AliveBoss++;
			Index = 0;
			Index = NPC.NewNPC((int)where.X, (int)where.Y, RawType);
			SendData();
			Defense = DefaultDefense * (int)(100 * Math.Log(Level + Math.E));
			RealNPC.aiStyle = -1;
			RealNPC.Center += Rand.NextVector2(54f, 54f);
			RealNPC.noTileCollide = true;
			//FakeVelocity = new Vector(ref RealNPC.velocity);
			LifeMax = DefaultLife;
			LifesMax = (int)(Level / (float)CriticalLevel * DefaultLifes);
			int count = TShock.Utils.ActivePlayers();
			switch (LifeperPlayerType)
			{
				case ByLife:
					LifeMax *= count;
					break;
				case ByLifes:
					LifesMax *= count;
					break;
			}
			Lifes = LifesMax;
			LastCenter = Center;
			Timer = 0;
			if (ExVersion && FullName != null)
			{
				DisplayName = FullName;
			}
			else
			{
				DisplayName = Name;
			}
			if (!Silence)
			{
				TSPlayer.All.SendMessage(Language.GetTextValue("Announcement.HasAwoken", DisplayName), Color.MediumPurple);
			}
			modetime = 0;
			LastCenter = Center;
			for (float* begin = StarverAI, end = StarverAI + NumOfAIs; begin != end;)
			{
				*begin++ = 0;
			}
			mode = BossMode.WaitForMode;
		}
		#endregion
		#region Spawn_Clover
		protected unsafe void Spawn_Clover(Vector2 where, int lvl = CriticalLevel)
		{
			if (_active)
			{
				return;
			}
			_active = true;
			Level = lvl;
			ExVersion = Level > CriticalLevel;
			AliveBoss++;
			Index = 0;
			for(int i=0;i<Main.npc.Length;i++)
			{
				if(!Main.npc[i].active)
				{
					Index = i;
					break;
				}
			}
			Main.npc[Index] = new NPC
			{
				type = NPCID.MoonLordCore
			};
			SendData();
			Defense = DefaultDefense * (int)(100 * Math.Log(Level));
			RealNPC.aiStyle = -1;
			RealNPC.Center += Rand.NextVector2(54f, 54f);
			RealNPC.noTileCollide = true;
			//FakeVelocity = new Vector(ref RealNPC.velocity);
			LifeMax = DefaultLife;
			LifesMax = (int)(Level / (float)CriticalLevel * DefaultLifes);
			int count = TShock.Utils.ActivePlayers();
			switch (LifeperPlayerType)
			{
				case ByLife:
					LifeMax *= count;
					break;
				case ByLifes:
					LifesMax *= count;
					break;
			}
			Lifes = LifesMax;
			LastCenter = Center;
			Timer = 0;
			if (ExVersion && FullName != null)
			{
				DisplayName = FullName;
			}
			else
			{
				DisplayName = Name;
			}
			if (!Silence)
			{
				TSPlayer.All.SendMessage(Language.GetTextValue("Announcement.HasAwoken", DisplayName), Color.MediumPurple);
			}
			modetime = 0;
			LastCenter = Center;
			for (float* begin = StarverAI, end = StarverAI + NumOfAIs; begin != end;)
			{
				*begin++ = 0;
			}
			mode = BossMode.WaitForMode;
		}
		#endregion
		#region ToString
		public override string ToString()
		{
			return string.Format("[Lv.{0}]{1}\nHP:{2}/{3}    Lifes:{4}/{5}\nMode:{6}", Level, Name, Life, LifeMax, Lifes, LifesMax, mode);
		}
		#endregion
		#region lifes--
		public virtual void LifeDown()
		{
			Lifes--;
			if(Lifes < 1)
			{
				TSPlayer.All.SendMessage($"{DisplayName} 被打败了", Color.Blue);
				BeDown();
			}
			else
			{
#if DEBUG
				TSPlayer.All.SendMessage($"{Name}当前生命:{Lifes}/[c/0fff00:{LifesMax}]", Color.Yellow);
#endif
				if(Lifes % 10 == 0)
				{
					TSPlayer.All.SendMessage($"{DisplayName}被打掉了1条生命",Color.Blue);
					TSPlayer.All.SendMessage($"{DisplayName}当前生命:{Lifes / 10}/[c/0fff00:{(int)(LifesMax / 10f + 0.999f)}]", Color.Blue);
				}
				LastCenter = Center;
				RealNPC.life = RealNPC.lifeMax;
				RealNPC.active = true;
				SendData();
			}
		}
		#endregion
		#region checkactive
		public virtual bool CheckActive()
		{
			if (!_active)
			{
				return _active;
			}
			else if (Index == -1 || (!RealNPC.active) || (CheckType && RealNPC.type != RawType)) 
			{
#if DEBUG
				StarverPlayer.All.SendDeBugMessage($"{TypeName} ReSpawned");
#endif
				ReSpawn();
				LifeDown();
				return false;
			}
			else if (Lifes < 0)
			{
				KillMe();
				return false;
			}
			return true;
		}
		#endregion
		#region CheckDistance
		protected bool CheckDistance()
		{
			if (Vector2.Distance(Center, TargetPlayer.Center) > MaxDistance)
			{
				if (IgnoreDistance)
				{
					GotoTarget();
				}
				else
				{
					KillMe();
					return false;
				}
			}
			return true;
		}
		#endregion
		#region RealAI
		public abstract void RealAI();
		#endregion
		#region ReSpawn
		protected void ReSpawn()
		{
			Index = NewNPC((Vector)LastCenter, Vector.Zero, RawType, (int)(Level / (float)CriticalLevel * DefaultLifes), DefaultDefense * (int)(100 * Math.Log(Level + Math.E)));
			RealNPC.type = RawType;
			RealNPC.SetDefaults(RawType);
			RealNPC.aiStyle = None;
		}
		#endregion
		#region AI
		public unsafe override void AI(object args = null)
		{
			if (!CheckActive())
			{
				return;
			}
			if (Target < 0 || Target >= 40 || TargetPlayer == null || !TargetPlayer.Active)
			{
				TargetClosest();
				if (Target < 0 || Target >= 40 || TargetPlayer == null || !TargetPlayer.Active)
				{
					//if (TShock.Utils.ActivePlayers() > 0)
					{
						OnFail();
					}
					KillMe();
					return;
				}
			}
			if(Vector2.Distance(Center,TargetPlayer.Center) > 16 * 300)
			{
				if(IgnoreDistance)
				{
					Center = TargetPlayer.Center + Rand.NextVector2(16 * 20);
				}
				else
				{
					KillMe();
					return;
				}
			}
			if(NightBoss && Main.dayTime)
			{
				TSPlayer.Server.SetTime(false, 0);
			}
			++Timer;
			RealAI();
			if(WallActive)
			{
				UpdateWall();
			}
			++modetime;
			#region Wasted
			/*
			if (Timer % 60 == 0)
			{
				Vector2 v = -RelativePos;
				RealNPC.direction = (int)v.X;
				RealNPC.directionY = (int)v.Y;
			}
			*/
			#endregion
			Velocity = FakeVelocity ;
			SendData();
		}
		#endregion
		#region StartWall
		protected void StartWall(int target)
		{
			if (WallActive)
			{
				return;
			}
			WallActive = true;
			if (Walls is null)
			{
				Walls = new int[MaxWalls];
			}
			if (WallVector is null)
			{
				WallVector = new Vector[MaxWalls];
			}
			WallTarget = Starver.Players[target];
			for (int i = 0; i < MaxWalls; i++)
			{
				WallVector[i] = NewByPolar(PI * 4 * i / (3f * MaxWalls), 16 * 20);
				Walls[i] = Proj(WallTarget.Center + WallVector[i], Vector2.Zero, ProjectileID.NebulaSphere, WallDamage, 0);
			}
			WallStop = false;
			TimeSpan = 0;
		}
		#endregion
		#region UpdateWall
		private void UpdateWall()
		{
			if (WallTarget == null || !WallTarget.Active)
			{
				WallActive = false;
			}
			if (!WallStop)
			{
				if (TimeSpan > 60 * 20)
				{
					WallStop = true;
					StopPos = (Vector)WallTarget.Center;
				}
			}
			for (int i = 0; i < MaxWalls; i++)
			{
				if (Main.projectile[Walls[i]].active == false)
				{
					Walls[i] = Proj(WallTarget.Center + WallVector[i], Vector2.Zero, ProjectileID.NebulaSphere, WallDamage, 0);
				}
				if (!WallStop)
				{
					Main.projectile[Walls[i]].Center = WallVector[i] + WallTarget.Center;
				}
				else
				{
					Main.projectile[Walls[i]].Center = WallVector[i] + StopPos;
				}
				if (!WallStop)
				{
					WallVector[i].Angle += PI * 4 / 3 / MaxWalls;
				}
			}
			TimeSpan++;
			if (TimeSpan > 60 * 30)
			{
				WallActive = false;
				for (int i = 0; i < MaxWalls; i++)
				{
					Main.projectile[Walls[i]].active = false;
				}
			}
			for (int i = 0; i < MaxWalls; i++)
			{
				NetMessage.SendData((int)PacketTypes.ProjectileNew, Client, -1, null, Walls[i]);
			}
		}
		#endregion
		#region statics
		public static int EndTrialProcess { get; internal set; }
		public static bool EndTrial { get; internal set; }
		public static int AliveBoss { get; internal set; }
		protected const byte ByLife = 0;
		protected const byte ByLifes = 1;
		#endregion
	}
}
