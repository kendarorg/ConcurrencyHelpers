// ===========================================================
// Copyright (c) 2014-2015, Enrico Da Ros/kendar.org
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 
// * Redistributions of source code must retain the above copyright notice, this
//   list of conditions and the following disclaimer.
// 
// * Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// ===========================================================


using System;
using ConcurrencyHelpers.Utils;

namespace ConcurrencyHelpers.Monitor
{
	public abstract class BaseMetric
	{
		public bool Cumulative { get; private set; }
		protected int _msDuration;
		protected long _startTimestamp = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
		protected ConcurrentInt64 _lastTimestamp = new ConcurrentInt64(DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond);
		private long _lastCalcTimestamp;

		internal void UpdateLastTimestamp()
		{
			_lastTimestamp.Value = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
		}

		protected abstract void Calculate();

		internal void DoCalculation(DateTime time)
		{
			Calculate();
			_lastCalcTimestamp = time.Ticks / TimeSpan.TicksPerMillisecond;
		}

		public DateTime LastTimestamp
		{
			get
			{
				return FromMs(_lastTimestamp.Value);
			}
		}

		internal long LastCalculation
		{
			get
			{
				return _lastCalcTimestamp;
			}
		}

		public DateTime StartTimestamp
		{
			get
			{
				return FromMs(_startTimestamp);
			}
		}

		internal void Initialize(int msDuration, string id)
		{
			_startTimestamp = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			_lastCalcTimestamp = _startTimestamp;
			_msDuration = msDuration;
			Id = id;

		}

		public string Id { get; private set; }

		protected BaseMetric(bool cumulative)
		{
			Cumulative = cumulative;
		}

		internal long Intervals
		{
			get
			{
				return GetInterval(_startTimestamp);
			}
		}

		internal long GetInterval(long startTimestamp)
		{
			var now = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			return Math.Max(1, (now - startTimestamp) / _msDuration);
		}

		internal DateTime FromMs(long ms)
		{
			return new DateTime(ms * TimeSpan.TicksPerMillisecond);
		}

		internal long GetAvg(long value, long intervals = -1)
		{
			if (intervals <= 0) intervals = Intervals;
			if (value <= 0) return 0;
			return value / intervals;
		}
	}

}