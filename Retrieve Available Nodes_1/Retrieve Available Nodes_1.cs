/*
****************************************************************************
*  Copyright (c) 2024,  Skyline Communications NV  All Rights Reserved.    *
****************************************************************************

By using this script, you expressly agree with the usage terms and
conditions set out below.
This script and all related materials are protected by copyrights and
other intellectual property rights that exclusively belong
to Skyline Communications.

A user license granted for this script is strictly for personal use only.
This script may not be used in any way by anyone without the prior
written consent of Skyline Communications. Any sublicensing of this
script is forbidden.

Any modifications to this script by the user are only allowed for
personal use and within the intended purpose of the script,
and will remain the sole responsibility of the user.
Skyline Communications will not be responsible for any damages or
malfunctions whatsoever of the script resulting from a modification
or adaptation by the user.

The content of this script is confidential information.
The user hereby agrees to keep this confidential information strictly
secret and confidential and not to disclose or reveal it, in whole
or in part, directly or indirectly to any person, entity, organization
or administration without the prior written consent of
Skyline Communications.

Any inquiries can be addressed to:

	Skyline Communications NV
	Ambachtenstraat 33
	B-8870 Izegem
	Belgium
	Tel.	: +32 51 31 35 69
	Fax.	: +32 51 31 01 29
	E-mail	: info@skyline.be
	Web		: www.skyline.be
	Contact	: Ben Vandenberghe

****************************************************************************
Revision History:

DATE		VERSION		AUTHOR			COMMENTS

02/09/2024	1.0.0.1		DPR, Skyline	Initial version
****************************************************************************
*/

// Ignore Spelling: Gqi Dms
namespace RetrieveAvailableNodes_1
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;
	using AlarmLevel = Skyline.DataMiner.Core.DataMinerSystem.Common.AlarmLevel;
	using ElementState = Skyline.DataMiner.Core.DataMinerSystem.Common.ElementState;

	[GQIMetaData(Name = "Collect_All_Nodes")]

	public class CollectNodes : IGQIDataSource, IGQIOnInit
	{
		private IDms _dms;

		public GQIColumn[] GetColumns()
		{
			return new GQIColumn[]
			{
				new GQIStringColumn("Index"),
				new GQIStringColumn("Name"),
				new GQIStringColumn("State"),
				new GQIStringColumn("Protocol"),
				new GQIStringColumn("Alarm State"),
				new GQIIntColumn("X"),
				new GQIIntColumn("Y"),
			};
		}

		public GQIPage GetNextPage(GetNextPageInputArgs args)
		{
			return GetData();
		}

		public OnInitOutputArgs OnInit(OnInitInputArgs args)
		{
			_dms = DmsFactory.CreateDms(new GqiDmsConnection(args.DMS));
			return default;
		}

		private GQIPage GetData()
		{
			var elements = GetElements();

			if (elements.Count == 0)
			{
				return new GQIPage(new List<GQIRow>().ToArray()) { HasNextPage = false };
			}

			var rows = new List<GQIRow>();

			foreach (var element in elements)
			{
				var elementState = element.State;
				if (!elementState.Equals(ElementState.Active) && !elementState.Equals(ElementState.Paused) && !elementState.Equals(ElementState.Masked))
				{
					continue;
				}

				var alarmState = elementState.Equals(ElementState.Active) ? Enum.GetName(typeof(AlarmLevel), element.GetAlarmLevel()) : "N/A";

				var nodeEdgeX = element.Properties.FirstOrDefault(x => x.Equals("NodeEdgeX"));
				var nodeEdgeY = element.Properties.FirstOrDefault(x => x.Equals("NodeEdgeY"));
				var nodeEdgeXValue = "N/A";
				var nodeEdgeYValue = "N/A";
				if (nodeEdgeX != null && nodeEdgeY != null)
				{
					nodeEdgeXValue = nodeEdgeX.Value;
					nodeEdgeYValue = nodeEdgeY.Value;
				}

				var cells = new List<GQICell>
				{
					new GQICell { Value = $"{element.AgentId}/{element.Id}"},
					new GQICell { Value = element.Name},
					new GQICell { Value = element.State.ToString()},
					new GQICell { Value = element.Protocol},
					new GQICell { Value = alarmState},
					new GQICell { Value = int.TryParse(nodeEdgeXValue, out int nodeEdgeXInt) ? nodeEdgeXInt : -1 },
					new GQICell { Value = int.TryParse(nodeEdgeYValue, out int nodeEdgeYInt) ? nodeEdgeYInt : -1 },
				};

				var rowData = new GQIRow(cells.ToArray());
				rows.Add(rowData);
			}

			return new GQIPage(rows.ToArray()) { HasNextPage = false };
		}

		private List<IDmsElement> GetElements()
		{
			try
			{
				var elementsRetrieved = new List<IDmsElement>();

				var huaweiElements = _dms.GetElements().Where(x => String.Equals(x.Protocol.Name, "Huawei Manager") && String.Equals(x.Protocol.Version, "Production")).ToList();
				var juniperElements = _dms.GetElements().Where(x => String.Equals(x.Protocol.Name, "Juniper Networks Manager") && String.Equals(x.Protocol.Version, "Production")).ToList();
				var ciscoElements = _dms.GetElements().Where(x => String.Equals(x.Protocol.Name, "CISCO ASR Manager") && String.Equals(x.Protocol.Version, "Production")).ToList();

				elementsRetrieved.AddRange(huaweiElements);
				elementsRetrieved.AddRange(ciscoElements);
				elementsRetrieved.AddRange(juniperElements);

				return elementsRetrieved;
			}
			catch (Exception)
			{
				return new List<IDmsElement>();
			}
		}

		public class Element
		{
			public string Index { get; set; }

			public string Name { get; set; }

			public string State { get; set; }

			public double Protocol { get; set; }

			public string AlarmState { get; set; }
		}
	}

	public class GqiDmsConnection : ICommunication
	{
		private readonly GQIDMS _gqiDms;

		public GqiDmsConnection(GQIDMS gqiDms)
		{
			_gqiDms = gqiDms ?? throw new ArgumentNullException(nameof(gqiDms));
		}

		public DMSMessage[] SendMessage(DMSMessage message)
		{
			return _gqiDms.SendMessages(message);
		}

		public DMSMessage SendSingleResponseMessage(DMSMessage message)
		{
			return _gqiDms.SendMessage(message);
		}

		public DMSMessage SendSingleRawResponseMessage(DMSMessage message)
		{
			return _gqiDms.SendMessage(message);
		}

		public void AddSubscriptionHandler(NewMessageEventHandler handler)
		{
			throw new NotSupportedException();
		}

		public void AddSubscriptions(NewMessageEventHandler handler, string handleGuid, SubscriptionFilter[] subscriptions)
		{
			throw new NotSupportedException();
		}

		public void AddSubscriptions(NewMessageEventHandler handler, string setId, string internalHandleIdentifier, SubscriptionFilter[] subscriptions)
		{
			throw new NotSupportedException();
		}

		public void ClearSubscriptionHandler(NewMessageEventHandler handler)
		{
			throw new NotSupportedException();
		}

		public void ClearSubscriptions(NewMessageEventHandler handler, string handleGuid, bool replaceWithEmpty = false)
		{
			throw new NotSupportedException();
		}

		public void ClearSubscriptions(string setId, string internalHandleIdentifier, SubscriptionFilter[] subscriptions, bool force = false)
		{
			throw new NotSupportedException();
		}

		public void AddSubscriptions(NewMessageEventHandler handler, string setId, string internalHandleIdentifier, SubscriptionFilter[] subscriptions, TimeSpan subscribeTimeout)
		{
			throw new NotImplementedException();
		}

		public void ClearSubscriptions(string setId, string internalHandleIdentifier, SubscriptionFilter[] subscriptions, TimeSpan subscribeTimeout, bool force = false)
		{
			throw new NotImplementedException();
		}

		public DMSMessage[] SendMessages(DMSMessage[] messages)
		{
			throw new NotImplementedException();
		}
	}
}
