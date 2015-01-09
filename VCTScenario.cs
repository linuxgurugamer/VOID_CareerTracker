// VOID_CareerTracker
//
// VCT_Master.cs
//
// Copyright © 2015, toadicus
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification,
// are permitted provided that the following conditions are met:
//
// 1. Redistributions of source code must retain the above copyright notice,
//    this list of conditions and the following disclaimer.
//
// 2. Redistributions in binary form must reproduce the above copyright notice,
//    this list of conditions and the following disclaimer in the documentation and/or other
//    materials provided with the distribution.
//
// 3. Neither the name of the copyright holder nor the names of its contributors may be used
//    to endorse or promote products derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
// INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
// WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using KSP;
using System;
using System.Collections.Generic;
using System.Linq;
using ToadicusTools;
using UnityEngine;
using VOID;

namespace VOID_CareerTracker
{
	[KSPScenario(
		ScenarioCreationOptions.AddToNewCareerGames | ScenarioCreationOptions.AddToExistingCareerGames,
		GameScenes.SPACECENTER, GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.TRACKSTATION
	)]
	public class VCTScenario : ScenarioModule
	{
		#region Static Members
		private const string TRANSACTION_KEY = "CURRENCY_TRANSACTION";
		private const string NEWGAME_KEY = "NewGame";

		public static VCTScenario Instance
		{
			get;
			private set;
		}

		public static IList<CurrencyTransaction> GetTransactionList
		{
			get
			{
				if (VCTScenario.Instance != null)
				{
					return VCTScenario.Instance.TransactionList;
				}
				else
				{
					return new List<CurrencyTransaction>().AsReadOnly();
				}
			}
		}
		#endregion

		private bool isNewGame;

		private List<CurrencyTransaction> transactionDatabase;
		private List<CurrencyTransaction> fundingTransactions;
		private List<CurrencyTransaction> scienceTransactions;
		private List<CurrencyTransaction> reputationTransactions;

		public IList<CurrencyTransaction> TransactionList
		{
			get
			{
				return this.transactionDatabase.AsReadOnly();
			}
		}

		public IList<CurrencyTransaction> FundingTranscations
		{
			get
			{
				return this.fundingTransactions.AsReadOnly();
			}
		}

		public IList<CurrencyTransaction> ScienceTransactions
		{
			get
			{
				return this.scienceTransactions.AsReadOnly();
			}
		}

		public IList<CurrencyTransaction> ReputationTransations
		{
			get
			{
				return this.reputationTransactions.AsReadOnly();
			}
		}

		public double CurrentFunds
		{
			get;
			private set;
		}

		public float CurrentScience
		{
			get;
			private set;
		}

		public float CurrentReputation
		{
			get;
			private set;
		}

		#region ScenarioModule Overrides
		public override void OnAwake()
		{
			VCTScenario.Instance = this;

			this.isNewGame = true;

			base.OnAwake();

			this.transactionDatabase = new List<CurrencyTransaction>();
			this.fundingTransactions = new List<CurrencyTransaction>();
			this.scienceTransactions = new List<CurrencyTransaction>();
			this.reputationTransactions = new List<CurrencyTransaction>();

			GameEvents.Modifiers.OnCurrencyModified.Add(this.OnCurrencyModifiedHandler);
			GameEvents.OnFundsChanged.Add(this.OnFundsChangedHandler);
			GameEvents.OnScienceChanged.Add(this.OnScienceChangedHandler);
			GameEvents.OnReputationChanged.Add(this.OnReputationChangedHandler);
		}

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);

			foreach (ConfigNode transNode in node.GetNodes(TRANSACTION_KEY))
			{
				CurrencyTransaction trans = new CurrencyTransaction();

				trans.Load(transNode);

				this.AddTransaction(trans);
			}

			CurrencyTransaction.TransactionSorter sorter = new CurrencyTransaction.TransactionSorter(
				CurrencyTransaction.TransactionSorter.SortType.Time,
				CurrencyTransaction.TransactionSorter.SortOrder.Ascending
			);

			this.transactionDatabase.Sort(sorter);
			this.fundingTransactions.Sort(sorter);
			this.scienceTransactions.Sort(sorter);
			this.reputationTransactions.Sort(sorter);

			this.isNewGame = node.GetValue(NEWGAME_KEY, this.isNewGame);

			if (this.isNewGame)
			{
				this.RebalanceDatabase();
			}

			this.Log("Loaded.\n{0}", this.ToString());
		}

		public override void OnSave(ConfigNode node)
		{
			base.OnSave(node);

			node.ClearNodes();

			foreach (CurrencyTransaction trans in this.transactionDatabase)
			{
				ConfigNode transNode = new ConfigNode(TRANSACTION_KEY);

				trans.Save(transNode);

				node.AddNode(transNode);
			}

			if (node.HasValue(NEWGAME_KEY))
			{
				node.SetValue(NEWGAME_KEY, bool.FalseString);
			}
			else
			{
				node.AddValue(NEWGAME_KEY, bool.FalseString);
			}

			this.Log("Saved.");
		}

		public override string ToString()
		{
			return string.Format(
				"[VCT_Master]: CurrentFunds={1}, CurrentScience={2}, CurrentReputation={3}, transctionDatabase {0}",
				#if DEBUG
				string.Format("\n\t{0}",
					string.Join("\n\t", this.transctionDatabase.Select(t => t.ToString()).ToArray())),
				#else
				string.Format("contains {0} Transactions", this.transactionDatabase.Count),
				#endif
				CurrentFunds,
				CurrentScience,
				CurrentReputation
			);
		}
		#endregion

		#region MonoBehaviour LifeCycle Methods
		public void OnDestroy()
		{
			GameEvents.Modifiers.OnCurrencyModified.Remove(this.OnCurrencyModifiedHandler);
			GameEvents.OnFundsChanged.Remove(this.OnFundsChangedHandler);
			GameEvents.OnScienceChanged.Remove(this.OnScienceChangedHandler);
			GameEvents.OnReputationChanged.Remove(this.OnReputationChangedHandler);

			VCTScenario.Instance = null;
		}
		#endregion

		#region Event Handlers
		public void OnCurrencyModifiedHandler(CurrencyModifierQuery query)
		{
			this.AddTransaction(query);
		}

		public void OnFundsChangedHandler(double funds, TransactionReasons reason)
		{
			if (funds != this.CurrentFunds)
			{
				if (Math.Abs(funds - this.CurrentFunds) < 0.01)
				{
					this.CurrentFunds = funds;
					return;
				}

				CurrencyTransaction correction = new CurrencyTransaction(
					Planetarium.GetUniversalTime(), 
					TransactionReasons.None,
					(float)(funds - this.CurrentFunds),
					0f,
					0f
				);

				this.AddTransaction(correction);

				this.Log("Detected discrepancy in funds totals: admin shows {0}, VCT shows {1}; correcting with {2}.",
					funds,
					this.CurrentFunds,
					correction
				);
			}
		}

		public void OnScienceChangedHandler(float science, TransactionReasons reason)
		{
			if (science != this.CurrentScience)
			{
				if (Mathf.Abs(science - this.CurrentScience) < 0.01)
				{
					this.CurrentScience = science;
					return;
				}

				CurrencyTransaction correction = new CurrencyTransaction(
					Planetarium.GetUniversalTime(), 
					TransactionReasons.None,
					0f,
					science - this.CurrentScience,
					0f
				);

				this.AddTransaction(correction);

				this.Log("Detected discrepancy in science totals: R&D shows {0}, VCT shows {1}; correcting with {2}.",
					science,
					this.CurrentScience,
					correction
				);
			}
		}

		public void OnReputationChangedHandler(float reputation, TransactionReasons reason)
		{
			if (reputation != this.CurrentReputation)
			{
				if (Mathf.Abs(reputation - this.CurrentReputation) < 0.01)
				{
					this.CurrentReputation = reputation;
					return;
				}

				CurrencyTransaction correction = new CurrencyTransaction(
					Planetarium.GetUniversalTime(), 
					TransactionReasons.None,
					0f,
					0f,
					reputation - this.CurrentReputation
				);

				this.AddTransaction(correction);

				this.Log("Detected discrepancy in rep totals: admin shows {0}, VCT shows {1}; correcting with {2}.",
					reputation,
					this.CurrentReputation,
					correction
				);
			}
		}
		#endregion

		#region Utility Methods
		internal void AddTransaction(CurrencyTransaction transaction)
		{
			if (transaction.FundsDelta == 0 && transaction.ScienceDelta == 0 && transaction.ReputationDelta == 0)
			{
				return;
			}

			this.transactionDatabase.Add(transaction);

			if (transaction.FundsDelta != 0f)
			{
				this.fundingTransactions.Add(transaction);
			}

			if (transaction.ScienceDelta != 0f)
			{
				this.scienceTransactions.Add(transaction);
			}

			if (transaction.ReputationDelta != 0f)
			{
				this.reputationTransactions.Add(transaction);
			}

			this.CurrentFunds += (double)transaction.FundsDelta;
			this.CurrentScience += transaction.ScienceDelta;
			this.CurrentReputation += transaction.ReputationDelta;

			this.LogDebug("Added transaction: {0}", transaction);
		}

		internal void AddTransaction(CurrencyModifierQuery query)
		{
			CurrencyTransaction transaction = new CurrencyTransaction(Planetarium.GetUniversalTime(), query);

			this.AddTransaction(transaction);
		}

		internal void RebalanceDatabase()
		{
			this.CurrentFunds = 0f;

			foreach (CurrencyTransaction trans in this.transactionDatabase)
			{
				this.CurrentFunds += trans.FundsDelta;
			}

			this.OnFundsChangedHandler(Funding.Instance.Funds, TransactionReasons.None);

			this.CurrentScience = 0f;

			foreach (CurrencyTransaction trans in this.transactionDatabase)
			{
				this.CurrentScience += trans.ScienceDelta;
			}

			this.OnScienceChangedHandler(ResearchAndDevelopment.Instance.Science, TransactionReasons.None);

			this.CurrentReputation = 0f;

			foreach (CurrencyTransaction trans in this.transactionDatabase)
			{
				this.CurrentReputation += trans.ReputationDelta;
			}

			this.OnReputationChangedHandler(Reputation.Instance.reputation, TransactionReasons.None);
		}
		#endregion
	}
}

