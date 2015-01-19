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
using ConcurrencyHelpers.Coroutines;

namespace ConcurrencyHelpers.Test.Mocks
{
	class CoroutineWithSub : Coroutine
	{
		private readonly int _timerMs;

		public CoroutineWithSub(int timerMs = 100)
		{
			Cycles = 0;
			_timerMs = timerMs;
		}

		public override IEnumerable<Step> Run()
		{
			while (Thread.Status != CoroutineThreadStatus.Stopped)
			{
				InvokeAsTaskAndWait(() =>
													{
														System.Threading.Thread.Sleep(_timerMs);
														Cycles++;
													});
				yield return Step.Current;
				// ReSharper disable once UnusedVariable
				InvokeLocalAndWait(() => NestedCall());
				yield return Step.Current;
			}
		}

		private IEnumerable<object> NestedCall()
		{
			for (int i = 0; i < 10; i++)
			{
				yield return i;
			}
		}

		public override void OnError(Exception ex)
		{
			Console.WriteLine(ex);
		}

		public int Cycles { get; private set; }
	}
}
