// ===========================================================
// Copyright (C) 2014-2015 Kendar.org
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation 
// files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, 
// modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software 
// is furnished to do so, subject to the following conditions:
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS 
// BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF 
// OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
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