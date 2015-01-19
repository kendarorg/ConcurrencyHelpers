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
	public class ValueSample
	{
		public ValueSample()
		{
			_startTimestamp = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			_updatetTimestamp = new ConcurrentInt64(DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond);
		}

		private readonly long _startTimestamp;
		private readonly ConcurrentInt64 _updatetTimestamp;
		private readonly ConcurrentInt64 _lastValue = new ConcurrentInt64();

		private long UpdateLastTimestamp()
		{
			var now = DateTime.UtcNow.Ticks/TimeSpan.TicksPerMillisecond;
			_updatetTimestamp.Value = now;
			return now;
		}
		/*
		internal void UpdateLastTimestamp()
		{
			_updatetTimestamp.Value = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
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
		protected int _msDuration;
		
		*/


		public void SetValue(long value)
		{
			_lastValue.Value = value;
			UpdatePartials(value);
		}

		private void UpdatePartials(long value)
		{
			throw new NotImplementedException();
		}
	}
}
