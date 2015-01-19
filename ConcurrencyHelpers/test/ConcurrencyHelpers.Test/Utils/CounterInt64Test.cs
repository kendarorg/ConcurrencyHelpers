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
using ConcurrencyHelpers.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConcurrencyHelpers.Test.Utils
{
	[TestClass]
	public class ConcurrentInt64Test
	{
		[TestMethod]
		public void ConcurrentInt64CompareExchangeOk()
		{
			var c = new ConcurrentInt64(1);
			var res = c.CompareExchange(1, 2);
			Assert.AreEqual(1, res);
		}

		[TestMethod]
		public void ConcurrentInt64CompareExchangeFail()
		{
			var c = new ConcurrentInt64(1);
			var res = c.CompareExchange(2, 2);
			Assert.AreEqual(1, res);
		}

		[TestMethod]
		public void ConcurrentInt64Constructor()
		{
			var c = new ConcurrentInt64(22);
			Assert.AreEqual(22, (Int64)c);
		}

		[TestMethod]
		public void ConcurrentInt64DecrementIncrement()
		{
			var c = new ConcurrentInt64(22);
			c.Decrement();
			Assert.AreEqual(21, (Int64)c);
			c.Increment();
			Assert.AreEqual(22, (Int64)c);
		}

		[TestMethod]
		public void ConcurrentInt64Int64Conversion()
		{
			const long int64Value = 28;
			const int int32Value = 28;
			var c = new ConcurrentInt64();
			c = int64Value;
			Assert.AreEqual(int64Value, (Int64)c);
			c = int32Value;
			Assert.AreEqual(int32Value, (Int32)c);
		}

		[TestMethod]
		public void ConcurrentInt64GetAndReset()
		{
			var c = new ConcurrentInt64(22);
			var result = c.GetAndReset();
			Assert.AreEqual(result, 22);
			Assert.AreEqual(0, (Int64)c);
		}

		[TestMethod]
		public void ConcurrentInt64GetAndSet()
		{
			var c = new ConcurrentInt64(22);
			var result = c.GetAndSet(38);
			Assert.AreEqual(result, 22);
			Assert.AreEqual(38, (Int64)c);
		}

		[TestMethod]
		public void ConcurrentInt64PlusMinusOperators()
		{
			var c = new ConcurrentInt64(22);
			c++;
			Assert.AreEqual(23, (Int64)c);
			c--;
			Assert.AreEqual(22, (Int64)c);
			c += 1;
			Assert.AreEqual(23, (Int64)c);
		}

		[TestMethod]
		public void ConcurrentInt64SumBetweenCounters()
		{
			var c = new ConcurrentInt64(22);
			var d = new ConcurrentInt64(23);
			var e = c + d;
			Assert.AreEqual(45, (Int64)e);
		}

		[TestMethod]
		public void ConcurrentInt64ToStringOperator()
		{
			var c = new ConcurrentInt64(22);
			Assert.AreEqual("22", c.ToString());
		}
	}
}
