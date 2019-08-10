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

	public class TheWorld : Skill
	{
		#region Fields
		/// <summary>
		/// 暂停的微秒数
		/// </summary>
		protected const int StopTime = 5000;
		protected static int TimeToStop;
		#endregion
		#region ctor
		public TheWorld() : base(SkillID.TheWorld)
		{
			MP = 110;
			CD = 120;
			Author = "逍遥";
			Description = $"让所有怪物以及弹幕暂停一段时间({StopTime / 1000}s)";
			Lvl = 3000;
			SetText();
		}
		#endregion
		#region Release
		public override void Release(StarverPlayer player, Vector2 vel)
		{
			if (NPCSystem.StarverNPC.TheWorld < 1)
			{
				new Thread(TheWorld_Release).Start();
			}
			else
			{
				TimeToStop = StopTime;
			}
		}
		#endregion
		#region Skill
		protected unsafe static void TheWorld_Release()
		{
			TimeToStop = StopTime;
			NPCSystem.StarverNPC.TheWorld++;
			Vector2* NPCVelocity = stackalloc Vector2[Terraria.Main.maxNPCs];
			Vector2* ProjVelocity = stackalloc Vector2[Terraria.Main.maxProjectiles];
			int* NPCAI = stackalloc int[Terraria.Main.maxNPCs];
			int* ProjAI = stackalloc int[Terraria.Main.maxProjectiles];
			Utils.ReadNPCState(NPCVelocity, NPCAI);
			Utils.ReadProjState(ProjVelocity, ProjAI);
			int Timer = 0;
			int sleepTime = 50;
			while (Timer < (TimeToStop) / sleepTime)
			{
				Timer++;
				Utils.UpdateNPCState();
				Utils.UpdateProjState();
				Thread.Sleep(sleepTime);
			}
			Utils.RestoreNPCState(NPCVelocity, NPCAI);
			Utils.RestoreProjState(ProjVelocity, ProjAI);
			NPCSystem.StarverNPC.TheWorld--;
		}
		#endregion
	}
}
