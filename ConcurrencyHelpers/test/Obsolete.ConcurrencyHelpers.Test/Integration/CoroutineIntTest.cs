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
