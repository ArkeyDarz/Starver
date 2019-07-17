﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Starvers.AuraSystem.Skills.Base;
using Terraria;
using Terraria.ID;

namespace Starvers.AuraSystem.Skills
{
	public class NStrike:Skill
	{
		private const float rec = 16 * 30;
		public NStrike() : base(SkillID.NStrike)
		{
			MP = 350;
			CD = 120;
			Lvl = 50000;
			Author = "wither";
			Description = "能够和咖喱棒平起平坐的技能!\n对一定范围内的敌对生物发动攻击";
			BossBan = true;
			SetText();
		}
		public override void Release(StarverPlayer player, Vector2 vel)
		{
			foreach (NPC npc in Main.npc)
			{
				if (npc.active && (npc.Center - player.TPlayer.Center).Length() <= rec + Math.Min(player.Level / 5, 16 * 30)) 
				{
					npc.velocity = new Vector2(0);
					NetMessage.SendData((int)PacketTypes.NpcUpdate, -1, -1, null, npc.whoAmI);
					player.NewProj(npc.Center, Vector2.Zero, ProjectileID.StardustGuardianExplosion, player.Level * 10, 0);
				}
			}
			if (player.TPlayer.hostile)
			{
				foreach (Player ply in Main.player)
				{
					if ((!ply.hostile) || (ply.Center - player.TPlayer.Center).Length() > rec + player.Level || ply.whoAmI == player.Index || (ply.team == player.TSPlayer.Team && ply.team != 0 && ply.team != 1))
					{
						continue;
					}
					ply.velocity = Vector2.Zero;
					NetMessage.SendData((int)PacketTypes.PlayerUpdate, -1, -1, null, ply.whoAmI);
					player.NewProj(ply.Center, Vector2.Zero, ProjectileID.StardustGuardianExplosion, player.Level * 10, 0);
				}
			}
			player.SetBuff(BuffID.ShadowDodge);
		}
	}
}
