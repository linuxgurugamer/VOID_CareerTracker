// VOID_CareerTracker © 2015 toadicus
//
// This work is licensed under the Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License. To view a
// copy of this license, visit http://creativecommons.org/licenses/by-nc-sa/3.0/

using KSP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ToadicusTools;
using UnityEngine;

namespace VOID.VOID_CareerTracker
{
	[VOID_Scenes(GameScenes.SPACECENTER, GameScenes.FLIGHT, GameScenes.EDITOR)]
	[VOID_GameModes(Game.Modes.CAREER)]
	public class VOID_CareerTracker : VOID_WindowModule
	{
		private static VCTScenario Tracker
		{
			get
			{
				return VCTScenario.Instance;
			}
		}

		private Table ledgerTable;

		private Table.Column<string> timeStampCol;
		private Table.Column<string> reasonCol;
		private Table.Column<string> fundsDeltaCol;
		private Table.Column<double> fundsTotalCol;
		private Table.Column<string> scienceDeltaCol;
		private Table.Column<float> scienceTotalCol;
		private Table.Column<string> repDeltaCol;
		private Table.Column<float> repTotalCol;

		private Vector2 scrollViewPos;
		private float scrollWidth;

		private bool clearTable;
		private short waitToResize;

		[AVOID_SaveValue("IncludeFunds")]
		private VOID_SaveValue<bool> includeFunds;

		[AVOID_SaveValue("IncludeScience")]
		private VOID_SaveValue<bool> includeScience;

		[AVOID_SaveValue("IncludeReputation")]
		private VOID_SaveValue<bool> includeReputation;

		protected override bool timeToUpdate
		{
			get
			{
				return (
					(this.core.updateTimer - this.lastUpdate) > (this.core.updatePeriod * 2d) ||
					(this.lastUpdate > this.core.updateTimer)
				);
			}
		}

		public bool IncludeFunds
		{
			get
			{
				return this.includeFunds.value;
			}
			private set
			{
				if (value != this.includeFunds && this.timeToUpdate)
				{
					this.clearTable = true;
					this.includeFunds.value = value;
				}
			}
		}

		public bool IncludeScience
		{
			get
			{
				return this.includeScience.value;
			}
			private set
			{
				if (value != this.includeScience && this.timeToUpdate)
				{
					this.clearTable = true;
					this.includeScience.value = value;
				}
			}
		}

		public bool IncludeReputation
		{
			get
			{
				return this.includeReputation.value;
			}
			private set
			{
				if (value != this.includeReputation && this.timeToUpdate)
				{
					this.clearTable = true;
					this.includeReputation.value = value;
				}
			}
		}

		public VOID_CareerTracker() : base()
		{
			this.Name = "Transaction Log";

			this.defHeight = 320f;

			this.ledgerTable = new Table();

			this.timeStampCol = new Table.Column<string>("Date", 20f);
			this.ledgerTable.Add(this.timeStampCol);

			this.reasonCol = new Table.Column<string>("Reason", 20f);
			this.ledgerTable.Add(this.reasonCol);

			this.fundsDeltaCol = new Table.Column<string>("ΔFunds", 20f);
			this.ledgerTable.Add(this.fundsDeltaCol);

			this.fundsTotalCol = new Table.Column<double>("Funds", 20f);
			this.fundsTotalCol.Format = "#,##0.00";
			this.ledgerTable.Add(this.fundsTotalCol);

			this.scienceDeltaCol = new Table.Column<string>("ΔScience", 20f);
			this.ledgerTable.Add(this.scienceDeltaCol);

			this.scienceTotalCol = new Table.Column<float>("Science", 20f);
			this.scienceTotalCol.Format = "#,##0";
			this.ledgerTable.Add(this.scienceTotalCol);

			this.repDeltaCol = new Table.Column<string>("ΔReputation", 20f);
			this.ledgerTable.Add(this.repDeltaCol);

			this.repTotalCol = new Table.Column<float>("Reputation", 20f);
			this.repTotalCol.Format = "#,##0";
			this.ledgerTable.Add(this.repTotalCol);

			this.scrollViewPos = Vector2.zero;
			this.scrollWidth = 0f;

			this.clearTable = true;
			this.waitToResize = 5;

			this.includeFunds = true;
			this.includeScience = true;
			this.includeReputation = true;

			this.core.onSkinChanged += (object sender) => {this.clearTable = true;};
		}

		public override void ModuleWindow(int _)
		{
			if (this.timeToUpdate)
			{
				if (this.clearTable)
				{
					this.ledgerTable.ClearTable();

					this.ledgerTable.Add(this.timeStampCol);
					this.ledgerTable.Add(this.reasonCol);

					if (this.IncludeFunds)
					{
						this.ledgerTable.Add(this.fundsDeltaCol);
						this.ledgerTable.Add(this.fundsTotalCol);
					}

					if (this.IncludeScience)
					{
						this.ledgerTable.Add(this.scienceDeltaCol);
						this.ledgerTable.Add(this.scienceTotalCol);
					}

					if (this.IncludeReputation)
					{
						this.ledgerTable.Add(this.repDeltaCol);
						this.ledgerTable.Add(this.repTotalCol);
					}

					this.clearTable = false;

					this.waitToResize = 5;
				}
				else
				{
					this.ledgerTable.ClearColumns();
				}

				double aggregateFunds = Tracker.CurrentFunds;
				float aggregateScience = Tracker.CurrentScience;
				float aggregateReputation = Tracker.CurrentReputation;

				for (int i = Tracker.TransactionList.Count - 1; i >= 0; i--)
				{
					CurrencyTransaction trans = Tracker.TransactionList[i];

					bool skipTrans = true;

					if (this.IncludeFunds && trans.FundsDelta != 0f)
					{
						skipTrans = false;
					}

					if (this.IncludeScience && trans.ScienceDelta != 0f)
					{
						skipTrans = false;
					}

					if (this.IncludeReputation && trans.ReputationDelta != 0f)
					{
						skipTrans = false;
					}

					if (skipTrans)
					{
						continue;
					}

					this.timeStampCol.Add(VOID_Tools.FormatDate(trans.TimeStamp));

					this.reasonCol.Add(Enum.GetName(typeof(TransactionReasons), trans.Reason));

					this.fundsDeltaCol.Add(VOID_CareerStatus.formatDelta(trans.FundsDelta, "#,##0.00"));
					this.fundsTotalCol.Add(aggregateFunds);
					aggregateFunds -= (double)trans.FundsDelta;

					this.scienceDeltaCol.Add(VOID_CareerStatus.formatDelta(trans.ScienceDelta, "#,##0"));
					this.scienceTotalCol.Add(aggregateScience);
					aggregateScience -= trans.ScienceDelta;

					this.repDeltaCol.Add(VOID_CareerStatus.formatDelta(trans.ReputationDelta, "#,##0"));
					this.repTotalCol.Add(aggregateReputation);
					aggregateReputation -= trans.ReputationDelta;
				}
			}

			this.ledgerTable.ApplyHeaderStyle(VOID_Styles.labelCenterBold);
			this.ledgerTable.ApplyCellStyle(VOID_Styles.labelRight);

			this.timeStampCol.CellStyle = VOID_Styles.labelCenter;
			this.reasonCol.CellStyle = VOID_Styles.labelCenter;

			GUIStyle vertStyle = new GUIStyle(GUIStyle.none);
			RectOffset padding = GUI.skin.scrollView.padding;

			GUILayout.BeginVertical(vertStyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));

			GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));

			this.IncludeFunds  = GUILayout.Toggle(
				this.IncludeFunds,
				"Funds",
				GUI.skin.button,
				GUILayout.ExpandWidth(true)
			);

			this.IncludeScience = GUILayout.Toggle(
				this.IncludeScience,
				"Science",
				GUI.skin.button,
				GUILayout.ExpandWidth(true)
			);

			this.IncludeReputation = GUILayout.Toggle(
				this.IncludeReputation,
				"Reputation",
				GUI.skin.button,
				GUILayout.ExpandWidth(true)
			);

			GUILayout.EndHorizontal();

			GUILayout.EndVertical();

			padding.bottom = padding.top = 0;

			vertStyle.padding = GUI.skin.scrollView.padding;
			vertStyle.contentOffset = GUI.skin.scrollView.contentOffset;

			if (this.IncludeFunds | this.IncludeScience | this.IncludeReputation)
			{
				this.ledgerTable.RenderHeader(true);

				GUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

				this.scrollViewPos = GUILayout.BeginScrollView(
					this.scrollViewPos,
					false,
					true,
					GUIStyle.none,
					GUI.skin.verticalScrollbar,
					GUILayout.MinWidth(this.scrollWidth),
					GUILayout.ExpandWidth(true),
					GUILayout.ExpandHeight(true)
				);

				this.ledgerTable.Render(false);

				if (this.waitToResize >= 0)
				{
					if (this.waitToResize == 0)
					{
						this.scrollWidth = this.ledgerTable.Width + 60f;
						this.defWidth = this.ledgerTable.Width;
					}

					this.waitToResize--;
				}

				GUILayout.EndScrollView();

				GUILayout.EndVertical();
			}
			else
			{
				GUILayout.Label("Select a filter option to display transactions.");
			}

			GUI.DragWindow();
		}

		public override void DrawConfigurables()
		{
			if (GUILayout.Button("Write Transaction Database to CSV"))
			{
				string baseName = string.Format(
					"{0}-{1}.csv",
					HighLogic.CurrentGame.Title,
					(int)Planetarium.GetUniversalTime()
				);

				var file = KSP.IO.File.Create<VOID_CareerTracker>(baseName, null);

				UTF8Encoding enc = new UTF8Encoding(true);

				byte[] lineBytes = enc.GetPreamble();

				file.Write(lineBytes, 0, lineBytes.Length);

				string transLine = string.Format(
					"{0}, {1}, \"{2}\", \"{3}\", \"{4}\"\n",
					"TimeStamp",
					"Reason",
					"Funds Delta",
					"Science Delta",
					"Reputation Delta"
				);

				lineBytes = enc.GetBytes(transLine);

				file.Write(lineBytes, 0, lineBytes.Length);

				foreach (CurrencyTransaction trans in Tracker.TransactionList)
				{
					transLine = string.Format(
						"{0}, \"{1}\", {2}, {3}, {4}\n",
						trans.TimeStamp.ToString("F2"),
						Enum.GetName(typeof(TransactionReasons), trans.Reason),
						trans.FundsDelta.ToString("F2"),
						trans.ScienceDelta.ToString("F2"),
						trans.ReputationDelta.ToString("F2")
					);

					lineBytes = enc.GetBytes(transLine);

					file.Write(lineBytes, 0, lineBytes.Length);
				}
			}
		}
	}
}

