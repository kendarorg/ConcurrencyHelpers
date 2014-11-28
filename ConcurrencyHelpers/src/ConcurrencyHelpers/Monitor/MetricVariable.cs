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
	public class MetricVariable
	{
		public MetricVariable(BaseMetric container)
		{
			_container = container;
		}

		protected ConcurrentInt64 _internalRunCount = new ConcurrentInt64();
		protected ConcurrentInt64 _internalMin = new ConcurrentInt64(Int64.MaxValue);
		protected ConcurrentInt64 _internalMax = new ConcurrentInt64(Int64.MinValue);

		private readonly BaseMetric _container;

		internal void SetValue(long value)
		{
			_container.UpdateLastTimestamp();
			_internalRunCount.Value += value;
			_internalMin.Value = Math.Min(_internalMin.Value, _internalRunCount.Value);
			_internalMax.Value = Math.Max(_internalMax.Value, _internalRunCount.Value);

		}

		internal void Calculate()
		{
			if (!_container.Cumulative)
			{
				Value = _internalRunCount.GetAndReset();
				Max = _internalMax.GetAndSet(0);
				Min = _internalMax.GetAndSet(0);
				var intervals = _container.GetInterval(_container.LastCalculation);
				Avg = _container.GetAvg(Value, intervals);
			}
			else
			{
				Max = _internalMax.Value;
				Min = _internalMax.Value;
				Value = _internalRunCount.Value;
				Avg = _container.GetAvg(Value);
			}
		}

		public long Avg { get; private set; }
		public long Value { get; private set; }
		public long Min { get; private set; }
		public long Max { get; private set; }
	}
}