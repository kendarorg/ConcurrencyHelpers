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