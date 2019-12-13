﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starvers.AuraSystem.Skills
{
	using Base;
	using Microsoft.Xna.Framework;
	using System.Threading;
	using Terraria;
	using Terraria.ID;
	using Vector = TOFOUT.Terraria.Server.Vector2;
	public class NatureRage : Skill
	{
		private int[] ProjsToLaunch =
		{
			ProjectileID.SeedlerNut,
			ProjectileID.SeedlerNut,
			ProjectileID.SeedlerNut,
			ProjectileID.SeedlerNut,
			ProjectileID.SeedlerNut,
			ProjectileID.SeedlerNut,
			ProjectileID.SeedlerThorn,
			ProjectileID.SeedlerThorn,
			ProjectileID.SeedlerThorn,
			ProjectileID.SeedlerThorn,
			ProjectileID.FlowerPowPetal,
			ProjectileID.FlowerPowPetal,
			ProjectileID.FlowerPowPetal,
			ProjectileID.FlowerPowPetal,
			ProjectileID.PineNeedleFriendly,
			ProjectileID.PineNeedleFriendly,
			ProjectileID.PineNeedleFriendly,
			ProjectileID.PineNeedleFriendly,
			ProjectileID.PineNeedleFriendly,
		};
		public NatureRage() : base(SkillIDs.NatureRage)
		{
			MP = 110;
			Level = 250;
			CD = 30;
			Author = "zhou_Qi";
			Description = @"[可成长]向前方发射花叶，潜藏着净化的威能
""软弱的花叶亦可以锐如刀刃，温和的自然也具有它独特的侵略性""";
			SetText();
		}
		public override void Release(StarverPlayer player, Vector2 vel)
		{
			AsyncRelease(player, vel);
		}
		private async void AsyncRelease(StarverPlayer player,Vector2 vel)
		{
			await Task.Run(() =>
			{
				player.SetBuff(BuffID.DryadsWard, 10 * 60);


				Vector Start = (Vector)player.Center;
				Start -= (Vector)vel.ToLenOf(16 * 3);
				Vector Line = (Vector)vel.Vertical().ToLenOf(16 * 5f);
				player.ProjLine(player.Center + Line, player.Center - Line, Vector2.Zero, 10, 0, ProjectileID.SporeCloud);
				int tag = 0;
				int MaxTag = 40;
				while (tag < MaxTag)
				{
					Line.Length = (float)Rand.NextDouble(-16 * 5, 16 * 5);
					player.NewProj(player.Center + Line, vel * (float)Rand.NextDouble(4, 8), ProjsToLaunch.Next(), 200 + Rand.Next(-50, 50), 10f);
					Line.Length = (float)Rand.NextDouble(-16 * 5, 16 * 5);
					player.NewProj(player.Center + Line, vel * (float)Rand.NextDouble(4, 8), ProjsToLaunch.Next(), 200 + Rand.Next(-50, 50), 10f);
					Line.Length = (float)Rand.NextDouble(-16 * 5, 16 * 5);
					player.NewProj(player.Center + Line, vel * (float)Rand.NextDouble(4, 8), ProjsToLaunch.Next(), 200 + Rand.Next(-50, 50), 10f);
					Thread.Sleep(50);
					tag++;
				}
				if(NPC.downedMechBossAny)
				{
					float radium = 2 * 16;
					for (int i = 0; i < 4; i++)
					{
						player.ProjCircle(player.Center, radium, 0.1f, ProjectileID.PureSpray, (int)(radium * 2 * Math.PI / 36), 0, 2);
						radium += 16 * 3f;
						Thread.Sleep(175);
					}
				}


				player.RemoveBuff(BuffID.DryadsWard);
			});
		}
	}
}
