﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Timers;
using System.Reflection;
using System.Threading;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.VRageData;

using SEModAPIExtensions.API.Plugin;
using SEModAPIExtensions.API.Plugin.Events;
using SEModAPIExtensions.API;

using SEModAPIInternal.API.Common;
using SEModAPIInternal.API.Entity;
using SEModAPIInternal.API.Entity.Sector.SectorObject;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;
using SEModAPIInternal.API.Server;
using SEModAPIInternal.Support;

using SEModAPI.API;

using VRageMath;
using VRage.Common.Utils;




namespace SEAntiGrief
{
	[Serializable()]
	public struct SEAntiGriefSettings
	{
		public bool cockpitprotection;
		public int  maxSmallDrill;
		public int  maxLargeDrill;
		public bool drillProtection;
	}

	public class SEAntiGrief : PluginBase, ICubeBlockEventHandler, IChatEventHandler
	{
		
		#region "Attributes"
		SEAntiGriefSettings settings;

		#endregion

		#region "Constructors and Initializers"

		public void Core()
		{
			Console.WriteLine("SE Antigrief Plugin '" + Id.ToString() + "' constructed!");	
		}

		public override void Init()
		{
			settings.cockpitprotection = true;
			settings.maxLargeDrill = 9;
			settings.maxSmallDrill = 9;
			settings.drillProtection = true;
			Console.WriteLine("SE Antigrief Plugin '" + Id.ToString() + "' initialized!");
			loadXML();

		}

		#endregion

		#region "Properties"


		[Browsable(true)]
		[ReadOnly(true)]
		public string Location
		{
			get { return System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\"; }
		
		}
		[Category("SE-AntiGrief")]
		[Description("Cockpit Protection, warning disabling it will end current protection")]
		[Browsable(true)]
		[ReadOnly(false)]
		public bool cockpitProtection
		{
			get { return settings.cockpitprotection; }
			set { settings.cockpitprotection = value;  }
		}
		[Category("SE-AntiGrief")]
		[Description("Drill Protection, set a maximum number of drills per ship")]
		[Browsable(true)]
		[ReadOnly(false)]
		public bool drillProtection
		{
			get { return settings.drillProtection; }
			set { settings.drillProtection = value; }
		}
		[Category("SE-AntiGrief")]
		[Description("Maximum small drills per ship")]
		[Browsable(true)]
		[ReadOnly(false)]
		public int maxSmallDrill
		{
			get { return settings.maxSmallDrill; }
			set { settings.maxSmallDrill = value; }
		}
		[Category("SE-AntiGrief")]
		[Description("Maximum large drills per ship")]
		[Browsable(true)]
		[ReadOnly(false)]
		public int maxLargeDrill
		{
			get { return settings.maxLargeDrill; }
			set { settings.maxLargeDrill = value; }
		}
		#endregion

		#region "Methods"

		public void saveXML()
		{
			try
			{
				XmlSerializer x = new XmlSerializer(typeof(SEAntiGriefSettings));
				TextWriter writer = new StreamWriter(Location + "Configuration.xml");
				x.Serialize(writer, settings);
				writer.Close();
			}
			catch (Exception ex)
			{
				LogManager.APILog.WriteLineAndConsole("Could not save configuration: " + ex.ToString());
			}

		}
		public void loadXML(bool defaults)
		{
			try
			{

				
				//if(!defaults)
				//{
				//	if(File.Exists(MyFileSystem.SavesPath + "SE-AntiGrief.xml"))
				//	{
				//		XmlSerializer x = new XmlSerializer(typeof(SEAntiGriefSettings));
				//		TextReader reader = new StreamReader(MyFileSystem.SavesPath + "SE-AntiGrief.xml");
				//		SEAntiGriefSettings obj = (SEAntiGriefSettings)x.Deserialize(reader);
				//		settings.cockpitprotection = obj.cockpitprotection;
				//		reader.Close();
				//	}
				//}
				if (File.Exists(Location + "Configuration.xml"))
				{
					XmlSerializer x = new XmlSerializer(typeof(SEAntiGriefSettings));
					TextReader reader = new StreamReader(Location + "Configuration.xml");
					SEAntiGriefSettings obj = (SEAntiGriefSettings)x.Deserialize(reader);
					settings = obj;
					reader.Close();
				}
			}
			catch (Exception ex)
			{
				LogManager.APILog.WriteLineAndConsole("Could not load configuration: " + ex.ToString());
			}

		}
		public void loadXML()
		{
			loadXML(false);
		}
		public void cockpitCheckLoop(CockpitEntity obj, CockpitEntity security, long owner, MyOwnershipShareModeEnum shareMode)
		{
			if (SandboxGameAssemblyWrapper.IsDebugging)
			{
				LogManager.APILog.WriteLineAndConsole("Security Protocol Enabled: " + security.Name + " On " + obj.Name);
			}
			int sleepval = 1000;
			while (cockpitProtection)
			{
				//LogManager.APILog.WriteLine("cockpitCheckLoop");
				try
				{
					Thread.Sleep(sleepval);
					if (obj == null) throw new Exception("Obj not found.");
					if (security == null) throw new Exception("Security Object not found.");
					if (security.IntegrityPercent < 1) throw new Exception("Security Integrety Compromised.");
					if (obj.CustomName.ToLower() == "security") throw new Exception("Console changed to security console, breaking security protocal");
					if(obj.IntegrityPercent == 1)
					{
						obj.Owner = owner;
						obj.ShareMode = shareMode;
						//break;
						sleepval = 3000;
					}
					if (obj.BuildPercent > 0.6)
					{
						obj.Owner = owner;
						obj.ShareMode = shareMode;
						sleepval = 3000;
					}
				}
				catch (Exception ex)
				{
					if (SandboxGameAssemblyWrapper.IsDebugging)
					{
						LogManager.APILog.WriteLineAndConsole("Ending Security loop. " + ex.ToString());
					}
					break;
				}
				//loop till obj is deleted. 
			}

		}

		#region "EventHandlers"

		public override void Update()
		{

		}

		public override void Shutdown()
		{
			saveXML();
			cockpitProtection = false;
			return;
		}

		public void OnCubeBlockCreated(CubeBlockEntity obj)
		{
			if (settings.cockpitprotection && obj is CockpitEntity && obj.Owner != 0)
			{

				CubeGridEntity grid = obj.Parent;
				foreach (var cubeBlock in grid.CubeBlocks)
				{
					if (cubeBlock == obj) continue;
					if (cubeBlock is CockpitEntity)
					{
						try
						{
							CockpitEntity entity = (CockpitEntity)cubeBlock;
							CockpitEntity objentity = (CockpitEntity)obj;

							if (entity.Name == "PassengerSeat") continue;
							if (entity.CustomName.ToLower().Equals("security"))
							{
								obj.Owner = cubeBlock.Owner;
								obj.ShareMode = cubeBlock.ShareMode;
								Thread T = new Thread(() => cockpitCheckLoop(objentity, entity, cubeBlock.Owner, cubeBlock.ShareMode));
								T.Start();

								return;
							}
						}
						catch (Exception)
						{
							//skip no custom name set
							continue;
						}
					}
				}
			}
			if (drillProtection && obj is ShipDrillEntity)
			{
				int max = 0;
				int i = 0;
				ShipDrillEntity drill = (ShipDrillEntity)obj;
				CubeGridEntity grid = drill.Parent;
				if (grid.GridSizeEnum == MyCubeSize.Large)
				{
					 max = maxLargeDrill;
				}
				if(grid.GridSizeEnum == MyCubeSize.Small)
				{
					max = maxSmallDrill;
				}
				if (max == 0) return;//0 = infinite. 
				foreach (var cubeBlock in grid.CubeBlocks)
				{
					if (cubeBlock == obj) continue;
					if(cubeBlock is ShipDrillEntity)
					{
						i++;
						if (i >= max)
						{
							if(obj.Owner != 0)
							{
								try
								{
									LogManager.APILog.WriteLineAndConsole("owner: " + obj.Owner + " steamid " + PlayerMap.Instance.GetSteamId(obj.Owner));
									ChatManager.Instance.SendPrivateChatMessage( PlayerMap.Instance.GetSteamId(obj.Owner), "Error maximum number of drills exceeded.");
								}
								catch (Exception)
								{
									//do nothin
								}
							}

							obj.Dispose();
							
							grid.CubeBlocks.Remove(obj);
						
							grid.RefreshCubeBlocks();
							break;
						}
					}
				}

			}
			return;
		}
	
		public void OnCubeBlockDeleted(CubeBlockEntity obj)
		{
			return;
		}

		public void OnChatReceived(ChatManager.ChatEvent obj)
		{

			if (obj.sourceUserId == 0)
				return;


			if (obj.message[0] == '/')
			{
				bool isadmin = SandboxGameAssemblyWrapper.Instance.IsUserAdmin(obj.sourceUserId);
				string[] words = obj.message.Split(' ');
				//string rem = "";
				//proccess

				if (isadmin && words[0] == "/ag-cp-enable")
				{
					ChatManager.Instance.SendPrivateChatMessage(obj.sourceUserId, "Cockpit Protection enabled");
					cockpitProtection = true;
					return;
				}

				if (isadmin && words[0] == "/ag-cp-disable")
				{
					ChatManager.Instance.SendPrivateChatMessage(obj.sourceUserId, "Cockpit Protection disabled");
					cockpitProtection = false;
					return;
				}
				if (isadmin && words[0] == "/ag-dp-enable")
				{
					ChatManager.Instance.SendPrivateChatMessage(obj.sourceUserId, "Cockpit Protection enabled");
					drillProtection = true;
					return;
				}

				if (isadmin && words[0] == "/ag-dp-disable")
				{
					ChatManager.Instance.SendPrivateChatMessage(obj.sourceUserId, "Cockpit Protection disabled");
					drillProtection = false;
					return;
				}

				if (isadmin && words[0] == "/ag-set-small")
				{
					maxSmallDrill =  Convert.ToInt32(words[1]);
					ChatManager.Instance.SendPublicChatMessage("Small drill limit set to " + maxSmallDrill);
				}
				if (isadmin && words[0] == "/ag-set-large")
				{
					maxLargeDrill = Convert.ToInt32(words[1]);
					ChatManager.Instance.SendPublicChatMessage("Large drill limit set to " + maxLargeDrill);
				}

				if (isadmin && words[0] == "/ag-save")
				{

					saveXML();
					ChatManager.Instance.SendPrivateChatMessage(obj.sourceUserId, "Antigrief Configuration Saved.");
					return;
				}
				if (isadmin && words[0] == "/ag-load")
				{
					loadXML(false);
					ChatManager.Instance.SendPrivateChatMessage(obj.sourceUserId, "Antigrief Configuration Loaded.");
					return;
				}
				if (isadmin && words[0] == "/ag-loaddefault")
				{
					loadXML(true);
					ChatManager.Instance.SendPrivateChatMessage(obj.sourceUserId, "Antigrief Configuration Defaults Loaded.");
					return;
				}
			}
			return;
		}
		public void OnChatSent(ChatManager.ChatEvent obj)
		{
			//do nothing
			return;
		}		
		#endregion



		#endregion
	}
}
