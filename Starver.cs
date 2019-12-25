﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;
using System.Reflection;
using MySql;
using MySql.Data.MySqlClient;
using Terraria.ID;
using Starvers.TaskSystem;
using Starvers.AuraSystem;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using TShockAPI.Hooks;
using System.Threading;
using System.Windows.Forms;

namespace Starvers
{
	using BossSystem;
	using NPCSystem;
	using NPCSystem.NPCs;
	using WeaponSystem;

	using Vector = TOFOUT.Terraria.Server.Vector2;
	using BigInt = System.Numerics.BigInteger;
	using Skill = AuraSystem.Skills.Base.Skill;
	using StarverBoss = BossSystem.Bosses.Base.StarverBoss;
	using Calculator = Func<int, int>;
    using Starvers.Events;

    [ApiVersion(2, 1)]
	public class Starver : TerrariaPlugin
	{
		#region Fields
		private int curRelease;
		private Calculator UpGradeExp;
		private Calculator BagExp;
		private string FolderForTransfer;
		private string FolderBacking;
		private string FolderSending;
		private string ExchangeTips;
		private ExchangeItem[] Exchanges;
		private Forms.StarverManagerForm Manager;
		private IStarverPlugin[] Plugins;
		private static uint Timer;
		#endregion
		#region BaseProperties
		public override string Name => "Starver";
		public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;
		public override string Author => "TOFOUT & Clover";
		public override string Description => base.Description;
		public override bool Enabled => true;
		#endregion
		#region Properties
		public static DirectoryInfo MainFolder { get; private set; }
		public static DirectoryInfo PlayerFolder { get; private set; }
		public static DirectoryInfo BossFolder { get; private set; }
		public static DirectoryInfo NPCFolder { get; private set; }
		public static string SavePathMain { get; private set; }
		public static string SavePathPlayers { get; private set; }
		public static string SavePathBosses { get; private set; }
		public static string SavePathNPCs { get; private set; }
		public static Random Rand { get; private set; }
		public static StarverPlayer[] Players { get; private set; } 
		public static BaseNPC[] NPCs { get; private set; }
		public StarverAuraManager Aura { get; private set; }
		public static int CombatTextPacket { get; private set; }
		public static StarverConfig Config => StarverConfig.Config;
		public static bool IsPE { get; private set; }
		public static MySqlConnection DB => StarverPlayer.DB;
		public static Starver Instance { get; private set; }
		public static int NPCLevel => (int)(Math.Pow(2, Config.TaskNow / 3.0 + 2) + Config.TaskNow * Config.TaskNow * 20 + (Config.EvilWorld ? 10000 : 0));
		#endregion
		#region Ctor & Initialize & Dispose
		#region Ctor
		public Starver(Main game) : base(game)
		{
			Instance = this;
		}
		#endregion
		#region Initialize
		public override void Initialize()
		{
			{
				{
					var field = typeof(Main).GetFields().Where(fld => fld.Name == "curRelease").First();
					try
					{
						curRelease = (int)field.GetValue(null);
					}
					catch
					{
						curRelease = (int)field.GetRawConstantValue();
					}
				}
				{
					FolderForTransfer = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Stellaria", curRelease.ToString());
					FolderBacking = Path.Combine(FolderForTransfer, "Backings");
					FolderSending = Path.Combine(FolderForTransfer, "Sendings");
				}
				IsPE = Main.player.Length != 256;
				CombatTextPacket = IsPE ? (int)PacketTypes.CreateCombatText : (int)PacketTypes.CreateCombatTextExtended;
				Rand = new Random();
				BagExp = StarverAuraManager.BagExp;
				UpGradeExp = StarverAuraManager.UpGradeExp;
				Players = new StarverPlayer[TShock.Players.Length];
				Exchanges = new ExchangeItem[]
				{
					new ExchangeItem(ItemID.CopperOre,ItemID.TinOre),
					new ExchangeItem(ItemID.TinOre,ItemID.CopperOre),
					new ExchangeItem(ItemID.IronOre,ItemID.LeadOre),
					new ExchangeItem(ItemID.LeadOre,ItemID.IronOre),
					new ExchangeItem(ItemID.SilverOre,ItemID.TungstenOre),
					new ExchangeItem(ItemID.TungstenOre,ItemID.SilverOre),
					new ExchangeItem(ItemID.GoldOre,ItemID.PlatinumOre),
					new ExchangeItem(ItemID.PlatinumOre,ItemID.GoldOre),
					new ExchangeItem(ItemID.CobaltOre,ItemID.PalladiumOre),
					new ExchangeItem(ItemID.PalladiumOre,ItemID.CobaltOre),
					new ExchangeItem(ItemID.MythrilOre,ItemID.OrichalcumOre),
					new ExchangeItem(ItemID.OrichalcumOre,ItemID.MythrilOre),
					new ExchangeItem(ItemID.AdamantiteOre,ItemID.TitaniumOre),
					new ExchangeItem(ItemID.TitaniumOre,ItemID.AdamantiteOre),
					new ExchangeItem(ItemID.Ichor,ItemID.CursedFlame),
					new ExchangeItem(ItemID.CursedFlame,ItemID.Ichor),
					new ExchangeItem(ItemID.RottenChunk,ItemID.Vertebrae),
					new ExchangeItem(ItemID.Vertebrae,ItemID.RottenChunk),
					new ExchangeItem(ItemID.DemoniteOre,ItemID.CrimtaneOre),
					new ExchangeItem(ItemID.CrimtaneOre,ItemID.DemoniteOre),
					new ExchangeItem(ItemID.HallowedKey,ItemID.RainbowGun),
					new ExchangeItem(ItemID.CorruptionKey,ItemID.ScourgeoftheCorruptor),
					new ExchangeItem(ItemID.CrimsonKey,ItemID.VampireKnives),
					new ExchangeItem(ItemID.JungleKey,ItemID.PiranhaGun),
					new ExchangeItem(ItemID.FrozenKey,ItemID.StaffoftheFrostHydra),
					new ExchangeItem(ItemID.GoldenKey,ItemID.LockBox),
					new ExchangeItem(ItemID.VileMushroom,ItemID.ViciousMushroom),
					new ExchangeItem(ItemID.ViciousMushroom,ItemID.VileMushroom),
					new ExchangeItem(ItemID.LockBox,ItemID.Nazar),
					new ExchangeItem(ItemID.LunarTabletFragment,StarverBossManager.EndTrialSummoner,40,"放在背包最后一格")
				};
				Plugins = new IStarverPlugin[]
				{
					new StarverTaskManager(),
					new StarverBossManager(),
					new StarverAuraManager(),
					new StarverWeaponManager(),
					new StarverNPCManager()
				};
				{
					SavePathMain = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Starver");
					SavePathNPCs = Path.Combine(SavePathMain, "NPCs");
					SavePathPlayers = Path.Combine(SavePathMain, "Players");
					SavePathBosses = Path.Combine(SavePathMain, "Bosses");

					if (!File.Exists(SavePathMain))
					{
						Directory.CreateDirectory(SavePathMain);
					}
					if (!File.Exists(SavePathNPCs))
					{
						Directory.CreateDirectory(SavePathNPCs);
					}
					if (!File.Exists(SavePathPlayers))
					{
						Directory.CreateDirectory(SavePathPlayers);
					}
					if (!File.Exists(SavePathBosses))
					{
						Directory.CreateDirectory(SavePathBosses);
					}

					MainFolder = new DirectoryInfo(SavePathMain);
					NPCFolder = new DirectoryInfo(SavePathNPCs);
					PlayerFolder = new DirectoryInfo(SavePathPlayers);
					BossFolder = new DirectoryInfo(SavePathBosses);
				}
			}
			StarverConfig.Config = StarverConfig.Read();
			foreach (var plugin in Plugins)
			{
				try
				{
					if (plugin.Enabled)
					{
						plugin.Load();
					}
				}
				catch (Exception e)
				{
					TSPlayer.Server.SendErrorMessage(e.ToString());
				}
			}
			{
				int i = 0;
				StringBuilder SB = new StringBuilder(Exchanges.Length * 10);
				foreach (ExchangeItem item in Exchanges)
				{
					SB.Append(item);
					SB.Append(i++ % 2 == 0 ? "    " : "\n");
				}
				if (SB[SB.Length - 1] != '\n')
				{
					SB.Append('\r');
				}
				SB.Append("请将可以兑换的物品放置在背包第一格");
				ExchangeTips = SB.ToString();
			}
			{
				Commands.ChatCommands.Add(new Command(Perms.VIP.Invisible, Ghost, "invisible"));
				Commands.ChatCommands.Add(new Command(Perms.Manager, ManagerForm, "stform"));
				Commands.ChatCommands.Add(new Command(Perms.Test, PtrTest, "vt"));
				Commands.ChatCommands.Add(new Command(Perms.Exchange, ExchangeCommand, "exchange"));
				Commands.ChatCommands.Add(new Command(Perms.ShowInfo, ShowInfoCommand, "showinfo"));
				Commands.ChatCommands.Add(new Command(Perms.Normal, Command_Aura, "starver") { HelpText = HelpTexts.LevelSystem });
#if DEBUG
				Commands.ChatCommands.Add(new Command(Perms.Test, (CommandArgs args) => { new Thread(() => { throw new Exception(); }).Start(); }, "exception"));
#endif
			}
			{
				ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);
				ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreet);
				ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
				if (Config.EnableAura)
				{
					ServerApi.Hooks.ServerChat.Register(this, OnChat);
				}
				//ServerApi.Hooks.NpcKilled.Register(this, OnKill);
				ServerApi.Hooks.NpcStrike.Register(this, OnStrike);
				ServerApi.Hooks.NpcSpawn.Register(this, OnNPCSpawn);
				PlayerHooks.PlayerPostLogin += OnLogin;
				GetDataHandlers.PlayerDamage += OnDamage;
				if (Config.EvilWorld)
				{
					GetDataHandlers.KillMe += OnDeath;
				}
			}
			Aura = Plugins[2] as StarverAuraManager;
			if(Config.EnableNPC)
			{
				NPCs = new BaseNPC[Main.maxNPCs];
			}
#if DEBUG
			Console.BackgroundColor = ConsoleColor.Yellow;
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine($"Main.player.Length: {Main.player.Length}");
			Console.WriteLine($"IsPe: {IsPE}");
			Console.WriteLine($"CombatTextPacket: {(PacketTypes)CombatTextPacket}");
			Console.ResetColor();
#endif
		}
		#endregion
		#region Dispose
		protected override void Dispose(bool disposing)
		{
			foreach (var plugin in Plugins)
			{
				try
				{
					if (plugin.Enabled)
					{
						plugin.UnLoad();
					}
				}
				catch (Exception e)
				{
					TSPlayer.Server.SendErrorMessage(e.ToString());
				}
			}
			try
			{
				Config.Write();
				ServerApi.Hooks.GameUpdate.Deregister(this, OnUpdate);
				ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreet);
				ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
				ServerApi.Hooks.ServerChat.Deregister(this, OnChat);
				ServerApi.Hooks.NpcStrike.Deregister(this, OnStrike);
				//ServerApi.Hooks.NpcKilled.Deregister(this, OnKill);
				ServerApi.Hooks.NpcSpawn.Deregister(this, OnNPCSpawn);
				PlayerHooks.PlayerPostLogin -= OnLogin;
				GetDataHandlers.PlayerDamage -= OnDamage;
				if (Config.EvilWorld)
				{
					GetDataHandlers.KillMe -= OnDeath;
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
			// 不确定是否会出现问题
			if (false)
			{
				Instance = null;
				Players = null;
				Exchanges = null;
				Rand = null;
				ExchangeTips = null;
				Manager.Dispose();
				Manager = null;
				Plugins = null;
				NPCs = null;

				UpGradeExp = null;
				BagExp = null;

				MainFolder = null;
				PlayerFolder = null;
				BossFolder = null;

				SavePathMain = null;
				SavePathPlayers = null;
				SavePathBosses = null;
			}
			base.Dispose(disposing);
		}
		#endregion
		#endregion
		#region Event
		#region OnDeath
		private static void OnDeath(object sender, GetDataHandlers.KillMeEventArgs args)
		{
			//TShock.Players[args.PlayerId].Disconnect("你被神秘力量逐出了魔界");
		}
		#endregion
		#region OnStrike
		private void OnStrike(NpcStrikeEventArgs args)
		{
			if (Config.EnableAura && args.Npc.type != NPCID.SolarCrawltipedeTail)
			{
				args.Handled = true;
			}
			else
			{
				return;
			}
			NPC RealNPC;
			RealNPC = Main.npc[args.Npc.realLife >= 0 ? args.Npc.realLife : args.Npc.whoAmI];
			BaseNPC snpc = NPCs?[RealNPC.whoAmI];
			StarverBoss TheBoss = null;
			StarverPlayer player = Players[args.Player.whoAmI];
			float damageindex = 1;
			if (Config.EnableAura)
			{
				damageindex += player.Level * 0.015f;
			}
			float interdamage;
			interdamage = args.Damage * damageindex;
			interdamage -= args.Npc.defense / 2;
			if(args.Critical)
			{
				interdamage *= 2;
			}
			if (Config.EnableBoss && StarverBoss.AliveBoss > 0)
			{
				foreach (var boss in StarverBossManager.Bosses)
				{
					if (boss.Active && args.Npc.whoAmI == boss.Index)
					{
						interdamage *= boss.DamagedIndex;
						TheBoss = boss;
						break;
					}
				}
			}
			interdamage += Rand.Next(10);
			if (snpc != null)
			{
				interdamage *= snpc.DamagedIndex;
			}
			int realdamage = (int)interdamage;

			var NArgs = new NPCStrikeEventArgs(args.Npc, RealNPC, interdamage)
			{
				Crit = args.Critical,
				RealDamage = realdamage,
				RawDamage = args.Damage,
			};
			player.StrikingNPC(NArgs);
			realdamage = NArgs.RealDamage;

			if (args.Npc.dontTakeDamage)
			{
				realdamage = 0;
			}
			else
			{
				realdamage = Math.Max(1, realdamage);
			}
			if (Config.EnableAura)
			{
				args.Npc.SendCombatMsg(realdamage.ToString(), Color.Yellow);
			}
			if (TheBoss != null)
			{
				TheBoss.ReceiveDamage(realdamage);
				RealNPC.playerInteraction[player.Index] = true;
				player.TPlayer.OnHit((int)RealNPC.Center.X, (int)RealNPC.Center.Y, RealNPC);
				return;
				/*
				if (TheBoss.Life - realdamage < 1)
				{
					TheBoss.LifeDown();
					return;
				}
				*/
			}
			player.Exp += realdamage;
			RealNPC.life -= realdamage;
			RealNPC.playerInteraction[player.Index] = true;
			player.TPlayer.OnHit((int)RealNPC.Center.X, (int)RealNPC.Center.Y, RealNPC);
			if (RealNPC.life < 1)
			{
				if (!(snpc is null))
				{
					player.UPGrade(snpc.ExpGive);
					snpc.CheckDead();
				}
				else if(Config.EnableAura)
				{
					RealNPC.checkDead();
				}
			}
			else
			{
				if (Config.EnableAura)
				{
					Vector knockback = (Vector)(args.Npc.Center - player.Center);
					knockback.Length = args.KnockBack * RealNPC.knockBackResist;
					knockback *= Math.Min(Math.Max(player.Level - NPCLevel, 0), 10);
					RealNPC.velocity += knockback;
					if (!(snpc is null))
					{
						snpc.SendData();
					}
					else
					{
						NetMessage.SendData((int)PacketTypes.NpcUpdate, -1, -1, null, RealNPC.whoAmI);
					}

				}
				snpc?.OnStrike(realdamage, args.KnockBack, player);
				TheBoss?.OnStrike(realdamage, args.KnockBack, player);
			}
			player.StrikedNPC(NArgs);
		}
		#endregion
		#region OnKill
		/*
		private void OnKill(NpcKilledEventArgs args)
		{
			NPC RealNPc = args.npc;
			if (args.npc.realLife >= 0)
			{
				RealNPc = Main.npc[args.npc.realLife];
			}
			BaseNPC snpc = NPCs[RealNPc.whoAmI];
			if (snpc is null)
			{
				return;
			}
			snpc.OnDead();
		}
		*/
		#endregion
		#region OnChat
		private static void OnChat(ServerChatEventArgs args)
		{
			if (args.Text.IndexOf(TShock.Config.CommandSilentSpecifier) == 0 || args.Text.IndexOf(TShock.Config.CommandSpecifier) == 0)
			{
				return;
			}
			StarverPlayer player = Players[args.Who];
			string Text = string.Format(LvlPrefix(player.Level), LvlPrefixColor(player.Level)) + string.Format(TShock.Config.ChatFormat, "", TShock.Players[args.Who].Group.Prefix, TShock.Players[args.Who].Name, TShock.Players[args.Who].Group.Suffix, args.Text);
			Color color;
			if (!player.HasPerm(Perms.VIP.RainBowChat))
			unsafe
			{
				string[] RGB = TShock.Players[args.Who].Group.ChatColor.Split(',');
				byte* rgbs = stackalloc byte[3]
				{
					byte.Parse(RGB[0]),
					byte.Parse(RGB[1]),
					byte.Parse(RGB[2])
				};
				color = new Color(rgbs[0], rgbs[1], rgbs[2]);
			}
			else
			{
				color = new Color(Rand.Next(0, 256), Rand.Next(0, 256), Rand.Next(0, 256));
			}
			TSPlayer.All.SendMessage(Text, color);
			TSPlayer.Server.SendMessage(Text, color);
			TShock.Log.Write(Text, System.Diagnostics.TraceLevel.Info);
			args.Handled = true;
		}
		#endregion
		#region OnDamage
		private void OnDamage(object sender, GetDataHandlers.PlayerDamageEventArgs args)
		{
			if (Players[args.ID] != null)
			{
				args.Damage *= (short)(Config.TaskNow * Config.TaskNow * Config.TaskNow * 100 - Players[args.ID].Level);
			}
		}
		#endregion
		#region OnUpdate
		private static void OnUpdate(EventArgs args)
		{
			Timer++;
			#region clearNPC
			if(TShock.Config.DisableHardmode && Timer % 60 == 0)
			{
				foreach (var npc in Main.npc)
				{
					if (!npc.active)
					{
						continue;
					}
					if (
						npc.type == NPCID.DukeFishron ||
						npc.type == NPCID.SkeletronPrime ||
						npc.type == NPCID.TheDestroyer ||
						npc.type == NPCID.Spazmatism ||
						npc.type == NPCID.Retinazer
						)
					{
						npc.active = false;
						NetMessage.SendData((int)PacketTypes.NpcUpdate, -1, -1, null, npc.whoAmI);
					}
				}
			}
			#endregion
			#region Save
			if (Timer % (60 * Config.SaveInterval) == 0) 
			{
				Utils.SaveAll();
			}
			#endregion
			#region Fast
			foreach (var player in Players)
			{
				if (player is null || !player.Active || player.IsGuest)
				{
					continue;
				}
				AuraSystem.Skills.AvalonGradation.Update(player);
				player.Update();
			}
			#endregion
			#region Most
			if (Config.EnableAura && Timer % 60 == 0)
			{
				int liferegen;
				StringBuilder MsgBuilder = new StringBuilder(50);
				#region update mp and life
				if (Timer % (60 * 5) == 0)
				{
					foreach (StarverPlayer player in Players)
					{
						if (player == null || player.IsGuest)
						{
							continue;
						}
						try
						{
							player.MP += player.Level / 33 + 1;
							if (!player.Dead)
							{
								liferegen = (int)(50 * Math.Log((player.TPlayer.lifeRegen + player.TPlayer.statDefense) * (player.Level / 100)));
								liferegen = Math.Min(liferegen, player.TPlayer.statLife / 10);
								if (liferegen > 0)
								{
									player.TPlayer.statLife = Math.Min(player.TPlayer.statLifeMax2, player.TPlayer.statLife + liferegen);
									player.SendData(PacketTypes.PlayerHp, "", player.Index);
								}
							}
						}
						catch
						{

						}
					}
				}
				#endregion
				foreach (StarverPlayer player in Players)
				{
					if (player == null || !player.Active || player.IsGuest) 
					{
						continue;
					}
					try
					{
						if (Timer % (60 * 8) < 60 * 4)
						{
							MsgBuilder.Append($"Lv.{player.Level}, ");
							MsgBuilder.AppendLine($"Exp: {player.exp}/{player.UpGradeExp / (player.LessCost ? 3 : 1)}");
							MsgBuilder.AppendLine($"MP: [{player.MP}/{player.MaxMP}]");
						}
						else
						{
							MsgBuilder.Append($"CDs:  ");
							MsgBuilder.AppendLine($"{player.SkillCombineCD(0)}");
							for (int i = 1; i < Skill.MaxSlots; i++)
							{
								MsgBuilder.AppendLine($"      {player.SkillCombineCD(i)}");
							}
						}
						player.SendStatusMSG(MsgBuilder.ToString());
						MsgBuilder.Clear();
					}
					catch(Exception e)
					{
						StarverPlayer.All.SendMessage(e.ToString(), Color.Red);
						StarverPlayer.Server.SendInfoMessage(e.ToString());
					}
				}
			}
			#endregion
			#region NPC
			if (Config.EnableNPC)
			{
				UpdateNPCAI();
			}
			if(Config.EnableSelfCollide)
			{
				StarverNPC.Collide();
			}
			#endregion
		}
		#endregion
		#region OnLogin
		private void OnLogin(PlayerPostLoginEventArgs args)
		{
			//new Thread(() =>
			//{
			try
			{
				//Thread.Sleep(2000);
				Players[args.Player.Index] = null;
				if (Config.SaveMode == SaveModes.MySQL)
				{
					int ID;
					//由于TrPE的TS里是TSPlayer.Account,所以只能这么做
					dynamic ply = args.Player;
					if(!IsPE)
					{
						ID = ply.User.ID;
					}
					else
					{
						ID = ply.Account.ID;
					}
					try
					{
						Players[args.Player.Index] = StarverPlayer.Read(ID);
					}
					catch(Exception e)
					{
						string msg = e.ToString();
						StarverPlayer.Server.SendErrorMessage($"玩家{args.Player.Name}({args.Player.Index})Read StarverPlayer失败");
						args.Player.Disconnect(msg);
						TShock.Log.ConsoleError(msg);
					}
					UpdateForm(Players[args.Player.Index]);
				}
				if (Players[args.Player.Index].Level < Config.LevelNeed)
				{
					Players[args.Player.Index].Kick($"你的等级不足,该端口要求玩家最低等级为{Config.LevelNeed}", true);
				}
				else if (Config.EnableTestMode && !Players[args.Player.Index].HasPerm(Perms.Test))
				{
					Players[args.Player.Index].Kick("当前端口处于测试模式");
				}
			}
			catch (Exception E)
			{
				TShock.Log.ConsoleError(E.ToString());
			}
			//}).Start();
		}
		#endregion
		#region OnGreet
		private void OnGreet(GreetPlayerEventArgs args)
		{
			TSPlayer player = TShock.Players[args.Who];
			if (Config.SaveMode == SaveModes.MySQL)
			{
				if (player.IsLoggedIn)
				{
					int ID;
					dynamic ply = player;
					if(!IsPE)
					{
						ID = ply.User.ID;
					}
					else
					{
						ID = ply.Account.ID;
					}
					Players[args.Who] = StarverPlayer.Read(ID);
					Console.WriteLine($"Added:{Players[args.Who].Name}");
					UpdateForm(Players[args.Who]);
					if (Config.EnableTestMode && !Players[args.Who].HasPerm(Perms.Test))
					{
						Players[args.Who].TSPlayer.Disconnect("当前端口处于测试模式");
					}
				}
				else
				{
					Players[args.Who] = StarverPlayer.Guest;
					Players[args.Who].Index = args.Who;
				}
				if (player.IsLoggedIn && Players[args.Who].Level < Config.LevelNeed)
				{
					player.Disconnect($"你的等级不足,该处需要至少{Config.LevelNeed}级");
				}

			}
			else
			{
				string Name;
				dynamic ply = TShock.Players[args.Who];
				try
				{
					Name = ply.User.Name;
				}
				catch
				{
					Name = ply.Account.Name;
				}
				Players[args.Who] = StarverPlayer.Read(Name);
			}
#if DEBUG
			Players[args.Who].ShowInfos();
#endif
		}
		#endregion
		#region OnLeave
		private void OnLeave(LeaveEventArgs args)
		{
			if (Players[args.Who] == null)
			{
				return;
			}
			UpdateForm(Players[args.Who], true);
			Players[args.Who].Save();
			Players[args.Who] = null;
		}
		#endregion
		#region OnNPCSpawn
		private static void OnNPCSpawn(NpcSpawnEventArgs args)
		{
			NPC npc = Main.npc[args.NpcId];
			npc.whoAmI = args.NpcId;
			npc = Main.npc[npc.realLife > 0 ? npc.realLife : args.NpcId];
			//if (NPCs[npc.whoAmI] == null || !NPCs[npc.whoAmI]._active)
			{
				//	NPCs[npc.whoAmI] = new BaseNPC(npc.whoAmI);
			}
			//BaseNPC snpc = NPCs[npc.whoAmI];
			if (Config.EnableBoss && BossSystem.Bosses.Base.StarverBoss.AliveBoss > 0)
			{
				foreach (var boss in StarverBossManager.Bosses)
				{
					if (boss._active && args.NpcId == boss.Index)
					{
						return;
					}
				}
			}
			//if (Config.EnableNPC && NPCs[npc.whoAmI].Spawning)
			{
				//	return;
			}
			/*else*/
			if (Config.EnableStrongerNPC)
			{
				if (npc.friendly)
				{
					npc.life = npc.lifeMax = short.MaxValue;
					npc.defense = -10;
				}
				else
				{
					float scale = 1;
					switch (npc.type)
					{
						case NPCID.KingSlime:
							return;
						case NPCID.EyeofCthulhu:
							scale += 1.5f;
							break;
						case NPCID.EaterofWorldsBody:
						case NPCID.EaterofWorldsHead:
						case NPCID.EaterofWorldsTail:
						case NPCID.BrainofCthulhu:
							scale += 2f;
							break;
						case NPCID.QueenBee:
							scale += 2.5f;
							break;
						case NPCID.SkeletronHead:
							scale += 3f;
							break;
						case NPCID.WallofFlesh:
							scale += 4f;
							break;
						case NPCID.TheDestroyer:
						case NPCID.Retinazer:
						case NPCID.Spazmatism:
						case NPCID.SkeletronPrime:
						case NPCID.PrimeCannon:
						case NPCID.PrimeSaw:
						case NPCID.PrimeVice:
						case NPCID.PrimeLaser:
							scale += 6f;
							break;
						case NPCID.Plantera:
							scale += 7f;
							break;
						case NPCID.Golem:
						//case NPCID.GolemHead:
						case NPCID.GolemFistLeft:
						case NPCID.GolemFistRight:
							scale += 7.5f;
							scale /= 1.5f;
							break;
						case NPCID.DD2Betsy:
						case NPCID.DukeFishron:
						case NPCID.CultistBoss:
							scale += 8f;
							break;
						case NPCID.MoonLordCore:
						case NPCID.MoonLordHand:
						case NPCID.MoonLordHead:
							scale += 8.25f;
							npc.defense /= 10;
							if(npc.type != NPCID.MoonLordCore)
							{
								npc.life *= 10;
							}
							else
							{
								npc.life *= 2;
							}
							break;
						case NPCID.SolarCrawltipedeTail:
						case NPCID.SolarCrawltipedeHead:
						case NPCID.SolarCrawltipedeBody:
						case NPCID.GolemHead:
							return;
						case NPCID.TheDestroyerBody:
							npc.defense *= 60;
							npc.damage *= 10;
							goto senddata;
						default:
							npc.life = npc.lifeMax = StarverAuraManager.NPCLife(npc.lifeMax);
							npc.defense = StarverAuraManager.NPCDefense(npc.defense);
							npc.damage = StarverAuraManager.NPCDamage(npc.damage);
							goto senddata;
					}
					scale *= 10;
					npc.defense = (int)(npc.defense * scale);
					npc.life = npc.lifeMax = (int)(scale * npc.lifeMax);
					npc.damage = (int)(npc.damage * (1 + scale) * Config.TaskNow < 15 ? 0.1f : 0.6f);
				senddata:
					NetMessage.SendData((int)PacketTypes.NpcUpdate, -1, -1, null, npc.whoAmI);
					//snpc.ExpGive =  snpc.RealNPC.lifeMax * Config.TaskNow * Config.TaskNow;
					//snpc.SendData();

				}
			}
			//snpc.SendData();
		}
		#endregion
		#endregion
		#region UpdateNPCAI
		private static void UpdateNPCAI()
		{
			foreach (var npc in NPCs)
			{
				if (npc is null)
				{
					continue;
				}
				if (npc.Active)
				{
					npc.AI();
				}
			}
		}
		#endregion
		#region LvlPrefixColor
		private static string LvlPrefixColor(int lvl)
		{
			if (lvl < 1000)
			{
				return "e4c6d0";
			}
			else if (lvl < 2000)
			{
				return "edd1d8";
			}
			else if (lvl < 3000)
			{
				return "cca4e3";
			}
			else if (lvl < 4000)
			{
				return "0ba4e3";
			}
			else if (lvl < 5000)
			{
				return "4c8dae";
			}
			else if (lvl < 6000)
			{
				return "801dae";
			}
			else if (lvl < 7000)
			{
				return "4b5cc4";
			}
			else if (lvl < 8000)
			{
				return "bce672";
			}
			else if (lvl < 9000)
			{
				return "c9dd22";
			}
			else if (lvl < 10000)
			{
				return "ffff00";
			}
			else
			{
				return "ff0000";
			}
		}
		#endregion
		#region LvlPrefix
		private static string LvlPrefix(int lvl)
		{
			if (lvl < 10000)
			{
				return string.Format("[c/{0}:[][c/{0}:Lv.{1}][c/{0}:]]", "{0}", lvl);
			}
			else
			{
				return string.Format("[c/{0}:[][c/{0}:Lv.{1}][c/{0}:]]", "{0}", new string('?', Math.Min(6, (lvl - 5000) / 5000)));
			}
		}
		#endregion
		#region Transport
		public static void SendToEvil(StarverPlayer player)
		{
			File.Create(Path.Combine(Instance.FolderSending, player.Index.ToString())).Dispose();
		}
		public static void BackToHard(StarverPlayer player)
		{
			File.Create(Path.Combine(Instance.FolderBacking, player.Index.ToString())).Dispose();
		}
		#endregion
		#region Commands
		#region Ghost
		private static void Ghost(CommandArgs args)
		{
			int num = Projectile.NewProjectile(new Vector2(args.Player.X, args.Player.Y), Vector2.Zero, 170, 0, 0f, 255, 0f, 0f);
			Main.projectile[num].timeLeft = 0;
			NetMessage.SendData(27, -1, -1, null, num, 0f, 0f, 0f, 0, 0, 0);
			args.TPlayer.active = !args.TPlayer.active;
			NetMessage.SendData(14, -1, args.Player.Index, null, args.Player.Index, (float)args.TPlayer.active.GetHashCode(), 0f, 0f, 0, 0, 0);
			bool active = args.TPlayer.active;
			if (active)
			{
				NetMessage.SendData(4, -1, args.Player.Index, null, args.Player.Index, 0f, 0f, 0f, 0, 0, 0);
				NetMessage.SendData(13, -1, args.Player.Index, null, args.Player.Index, 0f, 0f, 0f, 0, 0, 0);
			}
			args.Player.SendSuccessMessage(string.Format("{0}abled Vanish.", args.TPlayer.active ? "Dis" : "En"));
		}
		#endregion
		#region PtrTest
		private static unsafe void PtrTest(CommandArgs args) { }
		#endregion
		#region FromAura
		private void Command_Aura(CommandArgs args)
		{
			string p = args.Parameters.Count < 1 ? "None" : args.Parameters[0];
			StarverPlayer player = args.SPlayer();
			switch (p.ToLower())
			{
				#region up
				case "up":
					int exp = player.Exp;
					int lvl = player.Level;
					int need = UpGradeExp(lvl);
					if (player.HasPerm(Perms.VIP.LessCost))
					{
						need /= 3;
						while (exp > need)
						{
							exp -= need;
							need = UpGradeExp(++lvl) / 3;
						}
					}
					else
					{
						while (exp > need)
						{
							exp -= need;
							need = UpGradeExp(++lvl);
						}
					}
					player.Level = lvl;
					player.Exp = exp;
					player.Save();
					player.SendInfoMessage("当前等级:{0}", player.Level);
					player.SendInfoMessage("所需经验:{0}", UpGradeExp(player.Level));
					break;
				#endregion
				#region ForceUp
				case "forceup":
					if (!player.HasPerm(Perms.Aura.ForceUp))
					{
						goto default;
					}
					int up;
					try
					{
						up = int.Parse(args.Parameters[1]);
					}
					catch
					{
						up = 1;
					}
					player.Level += up;
					player.SendInfoMessage("当前等级:{0}", player.Level);
					player.Save();
					break;
				#endregion
				#region toexp
				case "toexp":
					Item item = player.TPlayer.inventory[0];
					int bagexp;
					try
					{
						bagexp = item.stack * BagExp(item.type);
						item.netDefaults(0);
						if (bagexp == 0)
						{
							throw new Exception();
						}
						player.Exp += bagexp;
						player.SendData(PacketTypes.PlayerSlot, "", player.Index, 0);
						player.Save();
					}
					catch
					{
						player.SendMessage("请将可兑换为经验的物品(主要为boss袋子)放置在背包第一格", Color.Red);
					}
					break;
				#endregion
				#region Setlvl
				case "setlvl":
					if (!player.HasPerm(Perms.Aura.SetLvl))
					{
						goto default;
					}
					try
					{
						player.Level = int.Parse(args.Parameters[1]);
						player.SendInfoMessage("设置成功");
						player.SendInfoMessage("当前等级:{0}", player.Level);
					}
					catch
					{
						player.SendMessage("正确用法:    /aura setlvl <level>", Color.Red);
					}
					break;
				#endregion
				#region default
				default:
					player.SendInfoMessage(HelpTexts.LevelSystem);
					if (player.HasPerm(Perms.Aura.ForceUp))
					{
						player.SendInfoMessage("    forceup <levelup>:强制升级");
					}
					if (player.HasPerm(Perms.Aura.SetLvl))
					{
						player.SendInfoMessage("    setlvl <level>:设置等级");
					}
					break;
					#endregion
			}
		}
		#endregion
		#region ShowInfo
		private void ShowInfoCommand(CommandArgs args)
		{
			int i;
			if (args.Parameters.Count < 1)
			{
				args.SPlayer().ShowInfos(args.Player);
				return;
			}
			try
			{
				i = int.Parse(args.Parameters[0]);
				Players[i].ShowInfos(args.Player);
			}
			catch
			{
				try
				{
					var ID = StarverPlayer.GetUserIDByName(args.Parameters[0]);
					StarverPlayer player = StarverPlayer.Read((int)ID);
					player.ShowInfos(args.Player);
				}
				catch
				{
					args.Player.SendErrorMessage("无效的用户名");
				}
			}
		}
		#endregion
		#region Exchange
		private void ExchangeCommand(CommandArgs args)
		{
			try
			{
				Item item = args.TPlayer.inventory[0];
				int stack = item.stack;
				int p = item.type;
				foreach (ExchangeItem eit in Exchanges)
				{
					if (p == eit.From && stack / eit.Need > 0)
					{
						p = eit.To;
						args.TPlayer.inventory[0].netDefaults(0);
						NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, Terraria.Localization.NetworkText.Empty, args.Player.Index, 0);
						//int num = Item.NewItem(new Vector2(0,0),new Vector2(0,0),p,stack);
						args.SPlayer().GiveItem(p, stack/ eit.Need);
						stack %= eit.Need;
						args.SPlayer().GiveItem(eit.From, stack/ eit.Need);
						args.Player.SendInfoMessage("兑换完毕");
						return;
					}
				}
				throw new Exception("物品错误");
			}
			catch (Exception)
			{
				args.Player.SendErrorMessage(ExchangeTips);
				return;

			}
		}
		#endregion
		#region Form
		private void ManagerForm(CommandArgs args)
		{
			new Thread(() =>
			{
				Manager = new Forms.StarverManagerForm();
				Application.EnableVisualStyles();
				Application.Run(Manager);
			}).Start();
		}
		#endregion
		#endregion
		#region UpdateForm
		private void UpdateForm(StarverPlayer player, bool delete = false)
		{
			#region Temp
			//mod群的某人从灾厄搬出来的
			//说看不懂，就临时把代码放到这里帮他分析
			/*
			if (Main.projectile[TeaNPCGlobalProjectile.MiniSO].active)
			{
				Vector2 vector = Center;
				Vector Delta = Main.projectile[TeaNPCGlobalProjectile.MiniSO].Center - Center;
				double angle;
				float length;
				bool flag = true;
				while (flag)
				{
					length = 20;
					angle = Delta.Angle;
					float Distance = Delta.Length;
					if (Distance < 52)
					{
						length = (int)Distance - 16 * 2;
						flag = false;
					}
					//DeltaX *= LenDelta;
					//DeltaY *= LenDelta;
					//vector.X += DeltaX;
					//vector.Y += DeltaY;
					vector += Vector.FromPolar(angle, length);
					Delta = Main.projectile[TeaNPCGlobalProjectile.MiniSO].Center - vector;
					Color color2 = Lighting.GetColor((int)vector.X / 16, (int)(vector.Y / 16f));
					var Texture = mod.GetTexture("Extra/MiniSOChain");
					Main.spriteBatch.Draw(Texture, vector - Main.screenPosition, new Rectangle(0, 0, Texture.Width, length), Color.White, angle - PI / 2, new Vector2(Texture.Width * 0.5f, Texture.Height * 0.5f), 1f, SpriteEffects.None, 0f);
				}
			}
			*/
			#endregion
			if (delete)
			{
				try
				{
					if (Manager.PlayerList.SelectedIndex == player.Index)
					{
						Manager.PlayerList.SelectedIndex = -1;
						Manager.SLT.Text = string.Empty;
					}
					Manager.PlayerList.Items[player.Index] = string.Empty;
				}
				catch
				{
					//StarverPlayer.Server.SendInfoMessage(e.ToString());
				}
				return;
			}
			if (Manager != null)
			{
				Manager.PlayerList.Items[player.Index] = player.Name;
			}
		}
		#endregion
	}
}
