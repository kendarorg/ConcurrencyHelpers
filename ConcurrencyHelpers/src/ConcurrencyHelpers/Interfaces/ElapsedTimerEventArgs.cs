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
using System.Runtime;
using System.Timers;

namespace ConcurrencyHelpers.Interfaces
{
	public class ElapsedTimerEventArgs : EventArgs
	{
		private readonly DateTime _signalTime;

		public DateTime SignalTime
		{
			[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
			get
			{
				return _signalTime;
			}
		}

		internal ElapsedTimerEventArgs(ElapsedEventArgs elapsedEventArgs)
		{
			_signalTime = elapsedEventArgs.SignalTime;
		}

		internal ElapsedTimerEventArgs(int low, int high)
		{
			_signalTime = DateTime.FromFileTime((long)high << 32 | low & uint.MaxValue);
		}

		internal ElapsedTimerEventArgs(DateTime signalTime)
		{
			_signalTime = signalTime;
		}
	}
}
