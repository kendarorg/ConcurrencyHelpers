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
