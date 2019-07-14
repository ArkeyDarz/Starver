﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starvers.BossSystem.Bosses
{
	using Base;
	using Microsoft.Xna.Framework;
	using Terraria.ID;
	using Vector = TOFOUT.Terraria.Server.Vector2;
	public class DestroyerEx : StarverBoss
	{
		#region Fields
		/// <summary>
		/// 身体节数
		/// </summary>
		private const int BodyMax = 80;
		private int[] Bodies = new int[BodyMax];
		#endregion
		#region ctor
		public DestroyerEx() : base(1)
		{
			RawType = NPCID.TheDestroyer;
			DefaultLife = 560000;
			DefaultLifes = 100;
			DefaultDefense = 40;
		}
		#endregion
		#region Spawn
		public override void Spawn(Vector2 where, int lvl = 2000)
		{
			base.Spawn(where, lvl);
			NewBody();
#if DEBUG
			StarverPlayer.All.SendDeBugMessage($"{DisplayName} Newed Body");
#endif
			unsafe
			{
				StarverAI[0] = 0;
			}
		}
		#endregion
		#region RealAI
		public override void RealAI()
		{
			#region Common
			UpdateVelocity();
#if DEBUG
			if(Timer % 120 == 0)
				unsafe
			{
				StarverPlayer.All.SendDeBugMessage($"StarverAI[0]:{StarverAI[0]}");
			}
#endif
			CheckBody();
#endregion
			#region Mode
			switch(Mode)
			{
				#region Laser
				case BossMode.WormLaser:
					if(Timer % 60 == 0)
					{
						Laser();
					}
					break;
				default:
					goto case BossMode.WormLaser;
				#endregion
				#region Storm
				case BossMode.WormStorm:
					if(Timer % 114 == 0)
					{
						Storm();
					}
					break;
				#endregion
			}
			#endregion
		}
		#endregion
		#region AIs
		#region CheckBody
		private unsafe void CheckBody()
		{
			fixed (int* begin = Bodies)
			{
				int* end = begin + BodyMax - 1;
				int* ptr = begin;
				while (ptr != end)
				{
					if(Terraria.Main.npc[*ptr].type != NPCID.TheDestroyerBody || !Terraria.Main.npc[*ptr].active)
					{
						*ptr = NewNPC((Vector)Center, Vector.Zero, NPCID.TheDestroyerBody, 0, ptr == begin ? Index : ptr[-1], 0, Index);
						Terraria.Main.npc[*ptr].defense = 150000;
						Terraria.Main.npc[*ptr].life = 300;
						if(ptr != begin)
						{
							Terraria.Main.npc[ptr[-1]].ai[0] = *ptr;
						}
					}
					ptr++;
				}
				if ( Terraria.Main.npc[*ptr].type != NPCID.TheDestroyerTail || !Terraria.Main.npc[*ptr].active)
				{
					*ptr = NewNPC((Vector)Center, Vector.Zero, NPCID.TheDestroyerTail, 0, ptr == begin ? Index : ptr[-1], 0, Index);
					Terraria.Main.npc[*ptr].defense = 50000;
						Terraria.Main.npc[*ptr].life = 300;
					Terraria.Main.npc[ptr[-1]].ai[0] = *ptr;
				}
			}
		}
		#endregion
		#region SummonFollows
		private unsafe new void SummonFollows()
		{
			fixed (int* begin = Bodies)
			{
				int* end = begin + BodyMax;
				int* ptr = begin;
				int i;
				while (ptr < end)
				{
					try
					{
						for (i = 0; i < 3; i++)
						{
							NewNPC((Vector)Terraria.Main.npc[*ptr].Center, NewByPolar(PI * 2 * i / 3, 18), NPCID.Probe, (int)1.25e4, (int)5e4);
						}
					}
					catch(Exception e)
					{
						StarverPlayer.All.SendMessage(e.ToString(), Color.Red);
						break;
					}
					ptr += BodyMax / 8;
				}
			}
		}
		#endregion
		#region Laser
		private unsafe void Laser()
		{
			fixed(int* begin = Bodies)
			{
				int* ptr = begin;
				int* end = ptr + BodyMax;
				while(ptr != end)
				{
					Vel = (Vector)(TargetPlayer.Center - Terraria.Main.npc[*ptr].Center);
					Vel.Length = 18;
					Proj(Terraria.Main.npc[*ptr++].Center, Vel, ProjectileID.DeathLaser, 180);
				}
			}
		}
		#endregion
		#region Storm
		private unsafe void Storm()
		{
			Vel = (Vector)(TargetPlayer.Center - Center);
			Vel.Y = 0;
			Vel.X = 10;
			fixed(int* begin = Bodies)
			{
				int* end = begin + BodyMax;
				int* ptr = begin;
				while(ptr != end)
				{
					Proj(Terraria.Main.npc[*ptr++].Center, Vel, ProjectileID.SandnadoHostile, 203); ;
				}
			}
		}
		#endregion
		#region UpdateVelocity
		private unsafe void UpdateVelocity()
		{
			if (StarverAI[0] > PI * 5)
			{
				if (Mode != BossMode.WormStorm)
				{
					Mode = BossMode.WormStorm;
				}
				if (StarverAI[0] > PI * 7)
				{
					if (Vector2.Distance(Center, vector) < 16 * 3)
					{
						StarverAI[0] = PI * 10;
					}
					if (StarverAI[0] > PI * 9)
					{
						StarverAI[0] = 0;
						Mode = BossMode.WormLaser;
						//SummonFollows();
					}
					else
					{
						FakeVelocity = vector - (Vector)Center;
						FakeVelocity.Length /= 10;
					}
				}
				else
				{
					StarverAI[0] = PI * 7.5f;
					vector = (Vector)Center.Symmetry(TargetPlayer.Center);
				}
			}
			else
			{
				StarverAI[0] += 2 * PI / 90;
				vector = NewByPolar(StarverAI[0], 16 * 50);
				FakeVelocity = (Vector)TargetPlayer.Center + vector - (Vector)Center;
				FakeVelocity.Length /= 10;
			}
		}
		#endregion
		#region NewBody
		private void NewBody() 
		{
			int ai1 = Index; //每一节的前一节
			int ai0 = Index; //可能是每一节的后一节
			int ai3 = Index; //ai3为头
			vector = NewByPolar(Rand.NextAngle(), 16 * 4.5f);
			for (int i = 0; i < BodyMax - 1; i++)
			{
#if DEBUG
				StarverPlayer.All.SendDeBugMessage($"第{i + 1}节身体");
#endif
				Bodies[i] = ai0 = NewNPC((Vector)Center, Vector.Zero, NPCID.TheDestroyerBody, 0, ai1, 0, ai3);
				Terraria.Main.npc[ai0].Center = Terraria.Main.npc[ai1].Center + vector;
				Terraria.Main.npc[ai0].defense = 100000;
				Terraria.Main.npc[ai1].ai[0] = ai0;
				ai1 = ai0;
			}
			Bodies[BodyMax - 1] = ai0 = NewNPC((Vector)Center, Vector.Zero, NPCID.TheDestroyerTail, 0, ai1, 0, ai3);
			Terraria.Main.npc[ai0].Center = Terraria.Main.npc[ai1].Center + vector;
			Terraria.Main.npc[ai1].ai[0] = ai0;
		}
		#endregion
		#endregion
	}
}
