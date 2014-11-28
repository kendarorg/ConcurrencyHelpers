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


namespace ConcurrencyHelpers.Monitor
{
	public class RunAndExcutionCounterMetric : BaseMetric
	{
		public RunAndExcutionCounterMetric(bool cumulative = true)
			: base(cumulative)
		{
			InternalRunCount = new MetricVariable( this);
			InternalElapsedCount = new MetricVariable( this);
		}

		internal MetricVariable InternalRunCount;
		internal MetricVariable InternalElapsedCount;
		public long RunValue { get { return InternalRunCount.Value; } }
		public long RunAvg { get { return InternalRunCount.Avg; } }
		public long ElapsedValue { get { return InternalElapsedCount.Value; } }
		public long ElapsedAvg { get { return InternalElapsedCount.Avg; } }
		public long ElapsedMin { get { return InternalElapsedCount.Min; } }
		public long ElapsedMax { get { return InternalElapsedCount.Max; } }

		protected override void Calculate()
		{
			InternalRunCount.Calculate();
			InternalElapsedCount.Calculate();
		}
	}
}