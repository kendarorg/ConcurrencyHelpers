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
			Status = RunningStatus.Exception;
			return true;
		}

		public abstract void Initialize();

		public abstract IEnumerable<ICoroutineResult> OnCycle();
		public abstract void OnEndOfCycle();


		public string InstanceName { get; set; }
		public ILogger Log { get; set; }
	}
}
