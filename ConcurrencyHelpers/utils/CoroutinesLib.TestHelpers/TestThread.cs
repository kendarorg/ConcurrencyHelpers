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
