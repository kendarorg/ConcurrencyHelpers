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
using System.Linq;
using ConcurrencyHelpers.Coroutines;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ConcurrencyHelpers.Interfaces;

namespace ConcurrencyHelpers.Test
{
	[TestClass]
	public class JustForCoverage
	{
		[TestMethod]
		public void JfcTestTheBool()
		{
			var boolob = new Bool();
			Assert.AreEqual(false, boolob.Value);
		}

		[TestMethod]
		public void JfcTestTheNullCoroutine()
		{
			var res = Coroutine.NullCoroutine;
			Assert.IsInstanceOfType(res, typeof(NullCoroutine));

			var result = res.Run().ToList();
			Assert.AreEqual(1, result.Count);
			res.CheckException();
		}


		[TestMethod]
		public void JfcShouldGetDataFromStep()
		{
			var expData = DateTime.Now;
			var step = Step.DataStep(expData);
			Assert.IsTrue(step.HasData);
			var res = step.Data;
			Assert.IsInstanceOfType(res, typeof(DateTime));

			var data = step.GetData<DateTime>();
			Assert.AreEqual(expData, data);
		}

		[TestMethod]
		public void JfcShouldSetDataOnStep()
		{
			var expData = DateTime.Now;
			var step = Step.Current;
			step.Data = expData;
			Assert.IsTrue(step.HasData);
			var res = step.Data;
			Assert.IsInstanceOfType(res, typeof(DateTime));

			var data = step.GetData<DateTime>();
			Assert.AreEqual(expData, data);
		}

		[TestMethod]
		public void JfcElapsedTimerStep()
		{
			var ticks = (long)504911310949672970;

			var expData = new ElapsedTimerEventArgs(10, 10);
			Assert.AreEqual(ticks, expData.SignalTime.Ticks);
		}

		[TestMethod]
		public void JfcNullCoroutineData()
		{
			var nc = new NullCoroutine();
			nc.CheckException();
			nc.OnError(null);
			var res = nc.Run().ToList();
			Assert.AreEqual(1,res.Count);
		}

		[TestMethod]
		public void JfcContainerItems()
		{
			var ct = new Container(new object());
			Assert.IsNotNull(ct.RawData);


			var ctt = new Container<DateTime>(DateTime.Now);
			Assert.IsNotNull(ctt.Data);
			Assert.IsInstanceOfType(ctt.Data,typeof(DateTime));
		}
	}
}
