﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace Starvers.Enemies.Npcs
{
	public class StarverNPCManager
	{
		#region Properties
		private class Instance<T> where T : StarverNPC
		{
			public static T Value;
		}

		private StarverNPC[] roots;
		private StarverNPC[] npcs;
		public StarverNPC this[int index]
		{
			get
			{
				if (index < 0 || index >= Main.maxNPCs)
				{
					throw new IndexOutOfRangeException($"index: {index}");
				}
				return npcs[index];
			}
		}
		public int Count
		{
			get => Main.maxNPCs;
		}
		#endregion
		#region Ctor
		public StarverNPCManager()
		{
			Load();
		}
		#region Load & UnLoad
		public void Load()
		{
			// Commands.ChatCommands.Add(new Command(Perms.Test, TCommand, "snpc"));
			//ServerApi.Hooks.NpcKilled.Register(Starver.Instance, StarverNPC.OnNPCKilled);
			roots = new StarverNPC[]
			{
				new FloatingSkeleton(),
				new SkeletronExClone(),
				new SkeletronExHand()
			};
			foreach (var root in roots)
			{
				var type = typeof(Instance<>).MakeGenericType(new[] { root.GetType() });
				type.GetField("Value").SetValue(null, root);
			}
			npcs = new StarverNPC[Main.maxNPCs];
		}
		public void UnLoad()
		{
			// Commands.ChatCommands.RemoveAll(cmd => cmd.HasAlias("snpc"));
			//ServerApi.Hooks.NpcKilled.Deregister(Starver.Instance, StarverNPC.OnNPCKilled);
		}
		#endregion
		#endregion
		#region Update
		public void Update()
		{
			#region CheckSpawn
			var count = NPC.defaultMaxSpawns * Starver.Instance.Players.Count(p => p != null) - npcs.Count(npc => npc != null && npc.Active);
			foreach (var root in roots)
			{
				foreach (var player in Starver.Instance.Players)
				{
					if (player != null && count > 0 && player.Alive)
					{
						if (root.CheckSpawn(player))
						{
							if (root.TrySpawnNewNpc(player, out var npc))
							{
								npcs[npc.Index] = npc;
								count--;
							}
						}
					}
				}
			}
			#endregion
			#region AIUpdate
			for (int i = 0; i < npcs.Length; i++)
			{
				if (npcs[i] == null)
				{
					continue;
				}
				npcs[i].AI();
			}
			#endregion
		}
		public void PostUpdate()
		{
			for (int i = 0; i < npcs.Length; i++)
			{
				if (npcs[i] == null)
				{
					continue;
				}
				else if (npcs[i].Life == 0 && npcs[i].Active)
				{
					npcs[i].Kill();
				}
				if (!npcs[i].Active)
				{
					npcs[i] = null;
					continue;
				}
			}
		}
		#endregion
		#region OnDrop
		public void OnDrop(NpcLootDropEventArgs args)
		{TSPlayer.All.SendMessage($"{args.NpcArrayIndex}",255,255,0);
			if (npcs[args.NpcArrayIndex] != null && npcs[args.NpcArrayIndex].Active)
			{
				args.Handled = npcs[args.NpcArrayIndex].OverrideRawDrop;
				npcs[args.NpcArrayIndex].DropItems();
			}
		}
		#endregion
		#region SpawnNPC
		public T SpawnNPC<T>(Vector2 pos) where T : StarverNPC
		{
			T npc = (T)Instance<T>.Value.ForceSpawn((Vector)pos);
			npcs[npc.Index] = npc;
			return npc;
		}
		#endregion
		#region Command
#if false
// 手动召唤npc的位置
		private void TCommand(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				for (int i = 0; i < StarverNPC.RootNPCs.Count; i++)
				{
					args.Player.SendInfoMessage($"{i}: {StarverNPC.RootNPCs[i].Name}");
				}
			}
			else if (int.TryParse(args.Parameters[0], out int result))
			{
				int idx = StarverNPC.NewNPC((Vector)(args.TPlayer.Center + Starver.Rand.NextVector2(16 * 80, 16 * 50)), default, StarverNPC.RootNPCs[result]);
				if (StarverNPC.RootNPCs[result] is ElfHeliEx)
				{
					if (args.Parameters.Count > 1 && int.TryParse(args.Parameters[1], out int work))
					{
						var heli = npcs[idx] as ElfHeliEx;
						switch (work)
						{
							case 0:
								heli.Guard(args.TPlayer.Center);
								break;
							case 1:
								heli.Attack(args.SPlayer());
								break;
							case 2:
								heli.Wonder(args.TPlayer.Center, Starver.Rand.NextVector2(1));
								break;
							case 4:
								heli.WonderAttack(args.TPlayer.Center, new Vector(1, 0), 10, false);
								break;
						}
						if (args.Parameters.Count > 2 && int.TryParse(args.Parameters[2], out int shot))
						{
							if (0 <= shot && shot < ElfHeliEx.MaxShots)
							{
								heli.SetShot(shot);
							}
							else
							{
								throw new IndexOutOfRangeException(nameof(shot) + ": " + shot);
							}
						}
					}
				}
			}
		}
#endif
		#endregion
	}
}
