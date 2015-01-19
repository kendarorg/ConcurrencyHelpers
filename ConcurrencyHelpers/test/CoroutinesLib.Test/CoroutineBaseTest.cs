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
using System.Linq;
using CoroutinesLib.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoroutinesLib.Test
{

	public class CoroutineBaseFake : CoroutineBase
	{
		public List<String> Called = new List<string>();
		public Exception Throws = null;
		public bool NoRun = false;
		public override void Initialize()
		{
			Called.Add("Initialize");
		}

		public override IEnumerable<ICoroutineResult> OnCycle()
		{
			if (NoRun)yield break;
			Called.Add("OnCycle");
			if (Throws != null)
			{
				throw Throws;
			}
			yield return CoroutineResult.YieldReturn(DateTime.UtcNow);
		}

		public override bool OnError(Exception exception)
		{
			Called.Add("OnError");
			return base.OnError(exception);
		}

		public override void OnEndOfCycle()
		{
			Called.Add("OnEndOfCycle");
		}

		public override void OnDestroy()
		{
			Called.Add("OnDestroy");
		}
	}
	[TestClass]
	public class CoroutineBaseTest
	{
		[TestMethod]
		public void CoroutineBase_ShouldBeRunnable()
		{
			var cr = new CoroutineBaseFake();
			var en = cr.Execute().GetEnumerator();
			en.MoveNext();
			Assert.AreEqual(2, cr.Called.Count);
			Assert.AreEqual("Initialize", cr.Called[0]);
			Assert.AreEqual("OnCycle", cr.Called[1]);

			en.MoveNext();
			Assert.AreEqual(4, cr.Called.Count);
			Assert.AreEqual("OnEndOfCycle", cr.Called[2]);
			Assert.AreEqual("OnCycle", cr.Called[3]);
		}


		[TestMethod]
		public void CoroutineBase_ShouldStopOnException()
		{
			Exception throws = null;
			var cr = new CoroutineBaseFake();
			var en = cr.Execute().GetEnumerator();
			en.MoveNext();
			en.MoveNext();

			Assert.AreEqual(4, cr.Called.Count);
			cr.Throws = new Exception();
			try
			{
				en.MoveNext();
			}
			catch (Exception ex)
			{
				throws = ex;
			}

			Assert.AreEqual(throws,cr.Throws);
			Assert.AreEqual(6, cr.Called.Count);
			Assert.AreEqual("OnEndOfCycle", cr.Called[4]);
			Assert.AreEqual("OnCycle", cr.Called[5]);
		}

		[TestMethod]
		public void CoroutineBase_ShouldWaitWhenNothingToDo()
		{
			var cr = new CoroutineBaseFake();
			var en = cr.Execute().GetEnumerator();
			en.MoveNext();
			en.MoveNext();

			Assert.AreEqual(4, cr.Called.Count);
			cr.NoRun = true;
			en.MoveNext();
			Assert.AreEqual(CoroutineResult.Wait,en.Current);

			Assert.AreEqual(6, cr.Called.Count);
			Assert.AreEqual("OnEndOfCycle", cr.Called[4]);
			Assert.AreEqual("OnEndOfCycle", cr.Called[5]);
		}
	}
}