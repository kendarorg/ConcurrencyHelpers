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
using System.Threading;
using ConcurrencyHelpers.Coroutines;
using ConcurrencyHelpers.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConcurrencyHelpers.Test.Integration
{
	[TestClass]
	public class CoroutineIntTest
	{
		[TestMethod]
		public void CoroutineIntShouldCallTheCoroutine()
		{
			// ReSharper disable once ConvertToConstant.Local
			int hundred = 100;
			var ft = new CoroutineThread(1);
			var co = new CoroutineCallingLocal();

			ft.AddCoroutine(co);
			ft.Start();
			Thread.Sleep(100);
			ft.Stop();
			Thread.Sleep(300);
			Assert.IsTrue(co.LocalCalled);
			Assert.IsTrue(co.LocalSetted);

		}
		[TestMethod]
		public void CoroutineIntShouldCallTheCoroutineWaitingForIt()
		{
			// ReSharper disable once ConvertToConstant.Local
			int hundred = 100;
			var ft = new CoroutineThread(1);
			var co = new LocalCoroutineWithWait();

			ft.AddCoroutine(co);
			ft.Start();
			Thread.Sleep(1000);
			ft.Stop();
			Thread.Sleep(300);
			Assert.IsTrue(co.LocalCalled);
			Assert.IsTrue(co.LocalSetted);
			Assert.AreEqual(0, co.Counter);

		}
		[TestMethod]
		public void CoroutineIntShouldCallTheCoroutineWithData()
		{
			// ReSharper disable once ConvertToConstant.Local
			int hundred = 100;
			var data = new Exception();
			var ft = new CoroutineThread(1);
			var co = new LocalCoroutineWithData(data);

			ft.AddCoroutine(co);
			ft.Start();
			Thread.Sleep(1000);
			ft.Stop();
			Thread.Sleep(300);
			Assert.IsTrue(co.LocalCalled);
			Assert.IsTrue(co.LocalSetted);
			Assert.AreEqual(data,co.Result);
		}
	}
}
