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


using System.Threading;
using ConcurrencyHelpers.Coroutines;
using ConcurrencyHelpers.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConcurrencyHelpers.Test.Integration
{
	[TestClass]
	public class NestedCoroutineCallsIntTest
	{

		[TestMethod]
		public void CoroutineIntItShouldBePossibleToDoOneLevelNesting()
		{
			var ft = new CoroutineThread(1);
			var co = new SingleNestedCoroutine();
			ft.AddCoroutine(co);
			ft.Start();
			Thread.Sleep(100);
			Assert.IsTrue(co.RunFinished);
			Assert.IsTrue(co.FirstCallFinished);
			ft.Stop();
		}

		[TestMethod]
		public void CoroutineIntItShouldBePossibleToDoTwoLevelNesting()
		{
			var ft = new CoroutineThread(1);
			var co = new NestedCoroutine();
			ft.AddCoroutine(co);
			ft.Start();
			Thread.Sleep(200);
			Assert.IsTrue(co.RunFinished);
			Assert.IsTrue(co.FirstCallFinished);
			Assert.IsTrue(co.SecondCallFinished);
			ft.Stop();
		}
	}
}
