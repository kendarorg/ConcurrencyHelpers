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


using ConcurrencyHelpers.Coroutines;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConcurrencyHelpers.Test.Coroutines
{
	public class CoroutineStackTestCoroutine : Coroutine
	{
		Exception _ex = null;
		bool _shouldTerminate;
		public CoroutineStackTestCoroutine(Exception ex = null)
		{
			_ex = ex;
		}

		public CoroutineStackTestCoroutine(bool shouldTerminate)
		{
			_shouldTerminate = shouldTerminate;
		}
		public override IEnumerable<Step> Run()
		{
			yield return Step.Current;
			if (_ex != null)
			{
				throw _ex;
			}
			if (_shouldTerminate) ShouldTerminate = true;
		}

		public override void OnError(Exception ex)
		{
			ShouldTerminate = true;
			ExceptionThrown = ex;
		}
	}

	[TestClass]
	public class CoroutineStackTest
	{
		[TestInitialize]
		public void TestInitialize()
		{

		}

		[TestCleanup]
		public void TestCleanup()
		{

		}

		[TestMethod]
		public void MoveNextShouldStopWithEmptyStack()
		{
			var steps = new List<Step>
			{
				Step.DataStep(new EnumeratorWrapper{
					Enum = new List<Step>().GetEnumerator()
				})
			};
			var root = new CoroutineStackTestCoroutine();
			var cst = new CoroutineStack(steps.GetEnumerator(), root);
			Assert.IsTrue(cst.MoveNext());
			Assert.IsFalse(cst.MoveNext());
		}

		[TestMethod]
		public void NestedEnumerableWithoutCoroutineOrTasksShoulBeConsidered()
		{
			var internalEnum = new List<Step>().GetEnumerator();
			var steps = new List<Step>
			{
				Step.DataStep(new EnumeratorWrapper{
					Enum = internalEnum
				})
			};
			var root = new CoroutineStackTestCoroutine();
			var cst = new CoroutineStack(steps.GetEnumerator(), root);
			Assert.IsTrue(cst.MoveNext());
			var current = cst.Current as Step;
			Assert.IsTrue(current.HasData);
			Assert.IsInstanceOfType(current.Data, typeof(EnumeratorWrapper));
			var enumData = current.Data as EnumeratorWrapper;
			Assert.AreEqual(internalEnum, enumData.Enum);
			Assert.IsFalse(cst.MoveNext());
		}

		[TestMethod]
		public void NestedEnumerableWithTasksShoulBeConsidered()
		{
			var taskInstance = Task.Run(() =>
			{
				Thread.Sleep(50);
			});
			var steps = new List<Step>
			{
				Step.DataStep(new EnumeratorWrapper{
					IsTask = true,
					TaskInstance = taskInstance
				})
			};
			var root = new CoroutineStackTestCoroutine();
			var cst = new CoroutineStack(steps.GetEnumerator(), root);
			Thread.Sleep(100);
			Assert.IsTrue(cst.MoveNext());
			var current = cst.Current as Step;
			Assert.IsTrue(current.HasData);
			Assert.IsInstanceOfType(current.Data, typeof(EnumeratorWrapper));
			var enumData = current.Data as EnumeratorWrapper;
			Assert.AreEqual(taskInstance, enumData.TaskInstance);
			Assert.IsTrue(cst.MoveNext());
			Assert.IsFalse(cst.MoveNext());
		}

		[TestMethod]
		[ExpectedException(typeof(Exception))]
		public void ExceptionsInTasksShouldBeHandled()
		{
			var taskInstance = Task.Run(() =>
			{
				throw new NotFiniteNumberException();
			});
			var steps = new List<Step>
			{
				Step.DataStep(new EnumeratorWrapper{
					IsTask = true,
					TaskInstance = taskInstance
				})
			};
			var root = new CoroutineStackTestCoroutine();
			var cst = new CoroutineStack(steps.GetEnumerator(), root);
			Thread.Sleep(100);
			Assert.IsTrue(cst.MoveNext());
			var current = cst.Current as Step;
			Assert.IsTrue(current.HasData);
			Assert.IsInstanceOfType(current.Data, typeof(EnumeratorWrapper));
			var enumData = current.Data as EnumeratorWrapper;
			Assert.AreEqual(taskInstance, enumData.TaskInstance);

			cst.MoveNext();
		}

		[TestMethod]
		public void NestedEnumerableWithCoroutinesShoulBeConsidered()
		{
			var subCoroutine = new CoroutineStackTestCoroutine();
			var steps = new List<Step>
			{
				Step.DataStep(new EnumeratorWrapper{
					IsCoroutine = true,
					SubCoroutine = subCoroutine,
				})
			};
			subCoroutine.Run().ToList();
			var root = new CoroutineStackTestCoroutine();
			var cst = new CoroutineStack(steps.GetEnumerator(), root);
			Assert.IsTrue(cst.MoveNext());
			var current = cst.Current as Step;
			Assert.IsTrue(current.HasData);
			Assert.IsInstanceOfType(current.Data, typeof(EnumeratorWrapper));
			var enumData = current.Data as EnumeratorWrapper;
			Assert.AreEqual(subCoroutine, enumData.SubCoroutine);
			Assert.IsTrue(cst.MoveNext());

			//Normally invoked by the thread
			subCoroutine.ShouldTerminate = true;
			Assert.IsTrue(cst.MoveNext());
			Assert.IsFalse(cst.MoveNext());
		}

		[TestMethod]
		[ExpectedException(typeof(Exception))]
		public void NestedEnumerableWithCoroutinesShoulBeConsideredWithException()
		{
			var ex = new Exception();
			var subCoroutine = new CoroutineStackTestCoroutine(ex);
			var steps = new List<Step>
			{
				Step.DataStep(new EnumeratorWrapper{
					IsCoroutine = true,
					SubCoroutine = subCoroutine
				})
			};
			try
			{
				subCoroutine.Run().ToList();
			}
			catch (Exception exx)
			{
				subCoroutine.OnError(exx);
			}
			var root = new CoroutineStackTestCoroutine();
			var cst = new CoroutineStack(steps.GetEnumerator(), root);
			Assert.IsTrue(cst.MoveNext());
			var current = cst.Current as Step;
			Assert.IsTrue(current.HasData);
			Assert.IsInstanceOfType(current.Data, typeof(EnumeratorWrapper));
			var enumData = current.Data as EnumeratorWrapper;
			Assert.AreEqual(subCoroutine, enumData.SubCoroutine);
			Assert.IsTrue(cst.MoveNext());
			Assert.IsFalse(cst.MoveNext());
		}

		[TestMethod]
		public void NestedEnumerableWithCoroutinesWantingToTerminate()
		{
			var subCoroutine = new CoroutineStackTestCoroutine(true);
			var steps = new List<Step>
			{
				Step.DataStep(new EnumeratorWrapper{
					IsCoroutine = true,
					SubCoroutine = subCoroutine,
				})
			};
			var root = new CoroutineStackTestCoroutine();
			var cst = new CoroutineStack(steps.GetEnumerator(), root);
			Assert.IsTrue(cst.MoveNext());
			var current = cst.Current as Step;
			Assert.IsTrue(current.HasData);
			Assert.IsInstanceOfType(current.Data, typeof(EnumeratorWrapper));
			var enumData = current.Data as EnumeratorWrapper;
			Assert.AreEqual(subCoroutine, enumData.SubCoroutine);
			subCoroutine.Run().ToList();
			Assert.IsTrue(cst.MoveNext());
			Assert.IsFalse(cst.MoveNext());
		}
	}
}
