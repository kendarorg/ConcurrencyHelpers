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
