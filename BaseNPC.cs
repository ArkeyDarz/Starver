﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using BigInt = System.Numerics.BigInteger;
namespace Starvers
{
	public class BaseNPC : StarverEntity
	{
		#region Var
		public bool Spawning { get; protected set; }
		public string Name { get; protected set; }
		#endregion
		#region ctor
		protected BaseNPC()
		{

		}
		public BaseNPC(int idx) : base(idx)
		{
			Index = idx;
			_active = true;
			ExpGive = Main.npc[Index].lifeMax * StarverConfig.Config.TaskNow;
			//RealNPC.netUpdate = true;
			//RealNPC.netUpdate2 = true;
		}
		#endregion
		#region CheckDead
		public void CheckDead()
		{
			try
			{
				OnDead();
				RealNPC.checkDead();
			}
			catch(Exception e)
			{
				TShockAPI.TShock.Log.Info(e.ToString());
			}
			Starver.NPCs[Index] = NPCSystem.StarverNPC.NPCs[Index] = null;
			KillMe();
			_active = false;
		}
		#endregion
		#region ToString
		public override string ToString()
		{
			return $" lifeMax:{RealNPC.lifeMax},life:{RealNPC.life},ExpGive:{ExpGive}";
		}
		#endregion
	}
}
