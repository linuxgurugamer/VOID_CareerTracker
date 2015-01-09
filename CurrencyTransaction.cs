// VOID_CareerTracker
//
// SavableCurrencyModifiedQuery.cs
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
using ToadicusTools;
using UnityEngine;

namespace VOID_CareerTracker
{
	public class CurrencyTransaction : IConfigNode
	{
		private const string REASON_KEY = "reason";
		private const string FUNDING_KEY = "fundingDelta";
		private const string SCIENCE_KEY = "scienceDelta";
		private const string REPUTATION_KEY = "reputationDelta";
		private const string TIMESTAMP_KEY = "timeStamp";

		public double TimeStamp
		{
			get;
			internal set;
		}

		public TransactionReasons Reason
		{
			get;
			internal set;
		}

		public float FundsDelta
		{
			get;
			internal set;
		}

		public float ScienceDelta
		{
			get;
			internal set;
		}

		public float ReputationDelta
		{
			get;
			internal set;
		}

		internal CurrencyTransaction()
			: this(Planetarium.fetch != null ? Planetarium.GetUniversalTime() : 0d, TransactionReasons.None, 0f, 0f, 0f)
		{}

		public CurrencyTransaction(double timestamp, CurrencyModifierQuery query)
			: this(
				timestamp,
				query.reason,
				(float)((double)query.GetEffectDelta(Currency.Funds) + (double)query.GetInput(Currency.Funds)),
				query.GetEffectDelta(Currency.Science) + query.GetInput(Currency.Science),
				query.GetEffectDelta(Currency.Reputation) + query.GetInput(Currency.Reputation))
		{}

		public CurrencyTransaction(double timestamp, TransactionReasons reason, float f0, float s0, float r0)
		{
			this.TimeStamp = timestamp;
			this.Reason = reason;
			this.FundsDelta = f0;
			this.ScienceDelta = s0;
			this.ReputationDelta = r0;
		}

		public void Load(ConfigNode node)
		{
			string reasonValue;

			reasonValue = node.GetValue(REASON_KEY, "None");

			try
			{
				this.Reason = (TransactionReasons)Enum.Parse(typeof(TransactionReasons), reasonValue);
			}
			catch (ArgumentException)
			{
				this.Reason = TransactionReasons.None;
			}

			this.FundsDelta = node.GetValue(FUNDING_KEY, 0f);
			this.ScienceDelta = node.GetValue(SCIENCE_KEY, 0f);
			this.ReputationDelta = node.GetValue(REPUTATION_KEY, 0f);

			this.TimeStamp = node.GetValue(TIMESTAMP_KEY, 0d);
		}

		public void Save(ConfigNode node)
		{
			string reasonValue;

			reasonValue = Enum.GetName(typeof(TransactionReasons), Reason);

			if (node.HasValue(REASON_KEY))
			{
				node.SetValue(REASON_KEY, reasonValue);
			}
			else
			{
				node.AddValue(REASON_KEY, reasonValue);
			}

			if (node.HasValue(FUNDING_KEY))
			{
				node.SetValue(FUNDING_KEY, this.FundsDelta.ToString());
			}
			else
			{
				node.AddValue(FUNDING_KEY, this.FundsDelta.ToString());
			}

			if (node.HasValue(SCIENCE_KEY))
			{
				node.SetValue(SCIENCE_KEY, this.ScienceDelta.ToString());
			}
			else
			{
				node.AddValue(SCIENCE_KEY, this.ScienceDelta.ToString());
			}

			if (node.HasValue(REPUTATION_KEY))
			{
				node.SetValue(REPUTATION_KEY, this.ReputationDelta.ToString());
			}
			else
			{
				node.AddValue(REPUTATION_KEY, this.ReputationDelta.ToString());
			}

			if (node.HasValue(TIMESTAMP_KEY))
			{
				node.SetValue(TIMESTAMP_KEY, this.TimeStamp.ToString());
			}
			else
			{
				node.AddValue(TIMESTAMP_KEY, this.TimeStamp.ToString());
			}
		}

		public override string ToString()
		{
			return string.Format(
				"[CurrencyTransaction: TimeStamp={0}, Reason={1}," +
				"FundsDelta={2}, ScienceDelta={3}, ReputationDelta={4}]",
				TimeStamp, Reason, FundsDelta, ScienceDelta, ReputationDelta
			);
		}

		public class TransactionSorter : IComparer<CurrencyTransaction>
		{
			public SortType sortType
			{
				get;
				private set;
			}

			public SortOrder sortOrder
			{
				get;
				private set;
			}

			public TransactionSorter(SortType type, SortOrder order)
			{
				this.sortType = type;
				this.sortOrder = order;
			}

			public TransactionSorter() : this(SortType.Time, SortOrder.Ascending) {}

			public int Compare(CurrencyTransaction x, CurrencyTransaction y)
			{
				switch (this.sortType)
				{
					case SortType.Funds:
						if (this.sortOrder == SortOrder.Ascending)
							return x.FundsDelta.CompareTo(y.FundsDelta);
						else
							return -x.FundsDelta.CompareTo(y.FundsDelta);
					case SortType.Science:
						if (this.sortOrder == SortOrder.Ascending)
							return x.ScienceDelta.CompareTo(y.ScienceDelta);
						else
							return -x.ScienceDelta.CompareTo(y.ScienceDelta);
					case SortType.Reputation:
						if (this.sortOrder == SortOrder.Ascending)
							return x.ReputationDelta.CompareTo(y.ReputationDelta);
						else
							return -x.ReputationDelta.CompareTo(y.ReputationDelta);
					case SortType.Time:
					default:
						if (this.sortOrder == SortOrder.Ascending)
							return x.TimeStamp.CompareTo(y.TimeStamp);
						else
							return -x.TimeStamp.CompareTo(y.TimeStamp);
				}
			}

			public enum SortType
			{
				Time,
				Funds,
				Science,
				Reputation
			}

			public enum SortOrder
			{
				Ascending,
				Descending
			}
		}
	}
}

