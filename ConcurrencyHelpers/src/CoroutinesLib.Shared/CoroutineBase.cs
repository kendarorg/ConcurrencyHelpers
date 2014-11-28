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


using CoroutinesLib.Shared.Enums;
using System;
using System.Collections.Generic;
using CoroutinesLib.Shared.Logging;

namespace CoroutinesLib.Shared
{
	public abstract class CoroutineBase : ICoroutineThread,ILoggable
	{
		protected CoroutineBase()
		{
			Id = Guid.NewGuid();
		}

		public Guid Id { get; private set; }

		public IEnumerable<ICoroutineResult> Execute()
		{
			Initialize();
			Status = RunningStatus.Running;
			
			while (Status.Is(RunningStatus.Running))
			{
				bool someRun = false;
				if (!Status.Is(RunningStatus.Paused))
				{
					foreach(var item in OnCycle())
					{
						someRun = true;
						yield return item;
					}
					OnEndOfCycle();
				}
				if (!someRun)
				{
					yield return CoroutineResult.Wait;
				}
			}
			if (Status.Is(RunningStatus.Stopping))
			{
				Status = RunningStatus.Stopped;
			}
			TerminateElaboration();
		}

		protected void TerminateElaboration()
		{
			Status = RunningStatus.Stopping;
		}

		public RunningStatus Status { get; protected set; }

		public virtual void OnDestroy()
		{
			
		}

		public object Result { get; set; }

		public virtual bool OnError(Exception exception)
		{
			return true;
		}

		public abstract void Initialize();

		public abstract IEnumerable<ICoroutineResult> OnCycle();
		public abstract void OnEndOfCycle();


		public string InstanceName { get; set; }
		public ILogger Log { get; set; }
	}
}
