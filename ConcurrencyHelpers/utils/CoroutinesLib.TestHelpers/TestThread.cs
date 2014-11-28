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
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace TestHelpers
{
	public class TestThread
	{
		private Thread _thread;
		private Func<TestThread,bool> _action;
		private ManualResetEvent _resetEvent;
		public int _msBetweenCycles;
		public bool IsRunning { get; private set; }
		public bool InternallyTerminated { get; private set; }
		public int Cycles { get; private set; }
		public long ElapsedMs { get; private set; }
		public Exception ExceptionThrown { get; private set; }
		public Dictionary<string,object> Variables { get; private set; }

		private TestThread()
		{
			Cycles = 0;
			ElapsedMs = 0;
			Variables = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
		}

		public static TestThread Start(Func<TestThread,bool> threadAction, ManualResetEvent resetEvent = null, int msBetweenCycles = 10)
		{
			if (resetEvent != null)
			{
				resetEvent.Reset();
			}
			var tt = new TestThread
							 {
								 IsRunning = true,
								 _msBetweenCycles = msBetweenCycles,
								 _action = threadAction,
								 _thread = new Thread(RunThread),
								 _resetEvent = resetEvent
							 };
			tt._thread.Start(tt);
			return tt;
		}

		public void Stop()
		{
			IsRunning = false;
		}

		private static void RunThread(object obj)
		{
			var tt = (TestThread)obj;
			if (tt._resetEvent != null)
			{
				tt._resetEvent.WaitOne(10000);
			}
			tt.IsRunning = true;
			var stopwatch = new Stopwatch();
			var sw2 = new Stopwatch();
			stopwatch.Start();
			while (tt.IsRunning)
			{
				sw2.Reset();
				sw2.Start();
				try
				{
					if (!tt._action(tt))
					{
						tt.InternallyTerminated = true;
						break;
					}
					tt.Cycles++;
				}
				catch (Exception ex)
				{
					stopwatch.Stop();
					tt.ExceptionThrown = ex;
					Console.WriteLine("Exception " + ex);
					break;
				}
				sw2.Stop();
				if (sw2.ElapsedMilliseconds < tt._msBetweenCycles)
				{
					Thread.Sleep((int) (tt._msBetweenCycles - sw2.ElapsedMilliseconds));
				}
				else
				{
					Thread.Sleep(1);
				}
			}
			stopwatch.Stop();
			tt.IsRunning = false;
			tt.ElapsedMs = stopwatch.ElapsedMilliseconds;
		}
	}
}
