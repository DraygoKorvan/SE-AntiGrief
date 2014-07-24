using System;
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
	public class SEAntiGrief : PluginBase, SEModAPIExtensions.API.Plugin.Events.ICubeBlockEventHandler
	{
		
		#region "Attributes"
		[field: NonSerialized()]
		private string m_motd = "";
		[field: NonSerialized()]
		private DateTime m_lastupdate;
		[field: NonSerialized()]
		private double m_interval = 300;
		[field: NonSerialized()]
		private bool m_enable = true;	

		#endregion

		#region "Constructors and Initializers"

		public void Core()
		{
			Console.WriteLine("SE Antigrief Plugin '" + Id.ToString() + "' constructed!");	
		}

		public override void Init()
		{

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

		#endregion

		#region "Methods"

		public void saveXML()
		{

			XmlSerializer x = new XmlSerializer(typeof(SEMotd));
			TextWriter writer = new StreamWriter(Location + "Configuration.xml");
			x.Serialize(writer, this);
			writer.Close();

		}
		public void loadXML()
		{
			try
			{
				if (File.Exists(Location + "Configuration.xml"))
				{
					XmlSerializer x = new XmlSerializer(typeof(SEAntiGrief));
					TextReader reader = new StreamReader(Location + "Configuration.xml");
					SEAntiGrief obj = (SEAntiGrief)x.Deserialize(reader);
					reader.Close();
				}
			}
			catch (Exception ex)
			{
				LogManager.APILog.WriteLineAndConsole("Could not load configuration: " + ex.ToString());
			}

		}

		public void sendMotd()
		{
			if(m_motd != "")
				ChatManager.Instance.SendPublicChatMessage(m_motd);
		}

		#region "EventHandlers"

		public override void Update()
		{

		}

		public override void Shutdown()
		{
			saveXML();
			return;
		}

		public void OnCubeBlockCreated(CubeBlockEntity obj)
		{
			if (obj.Owner == 0) return;
			CubeGridEntity grid = obj.Parent;
			long ownerid = 0;
			//filter through cubeblocks in cubegrid find a cockpit named "Security"
			foreach ( var cubeBlock in grid.CubeBlocks)
			{
				LogManager.APILog.WriteLineAndConsole("BlockName: " + cubeBlock.Name.ToString() + " Subtype: " + cubeBlock.Subtype.ToString());
				if(cubeBlock.Name.Equals("Security"))
				{
					ownerid = cubeBlock.Owner;
				}
			}
			if (ownerid > 0)
				obj.Owner = ownerid;
			return;
		}
	
		public void OnCubeBlockDeleted(CubeBlockEntity obj)
		{
			return;
		}
		#endregion



		#endregion
	}
}
