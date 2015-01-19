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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConcurrencyHelpers.Test.Mocks
{
	class LocalCoroutineWithData : Coroutine
	{
		private readonly object _toPass;

		public LocalCoroutineWithData(object toPass)
		{
			_toPass = toPass;
		}

		public int Counter = 10;
		public object Result = null;
		public bool LocalCalled = false;
		public bool LocalSetted = false;
		public override IEnumerable<Step> Run()
		{
			Container result = new Container();
			yield return InvokeLocalAndWait(() => LocalCall(_toPass), result);
			Result = result.RawData;
			LocalSetted = LocalCalled;
		}

		public IEnumerable<Step> LocalCall(object toPass)
		{
			while (Counter > 0)
			{
				System.Threading.Thread.Sleep(10);
				yield return Step.Current;
				Counter--;
			}
			LocalCalled = true;
			yield return Step.DataStep(_toPass);
		}

		public override void OnError(Exception ex)
		{
			ShouldTerminate = true;
		}
	}
}
