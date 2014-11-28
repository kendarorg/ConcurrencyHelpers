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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace ConcurrencyHelpers.Coroutines
{
	[Obsolete]
	[ExcludeFromCodeCoverage]
	public abstract class Coroutine : ICoroutine
	{
		static Coroutine()
		{
			InterceptExternalCalls = false;
		}

		protected Coroutine()
		{
			InterceptLocalCalls = false;
		}

		private static readonly NullCoroutine _nullCoroutine = new NullCoroutine();
		public abstract IEnumerable<Step> Run();
		public abstract void OnError(Exception ex);
		public CoroutineThread Thread { get; set; }
		public bool ShouldTerminate { get; set; }
		public object Data { get; set; }

		public static Func<object, Container, InterceptedStep> Interceptor { get; set; }
		public static bool InterceptExternalCalls { get; set; }
		public bool InterceptLocalCalls { get; set; }

		/// <summary>
		/// The empty coroutine (does nothing)
		/// </summary>
		public static ICoroutine NullCoroutine
		{
			get
			{
				return _nullCoroutine;
			}
		}

		public void CheckException()
		{
			if (ExceptionThrown != null)
			{
				throw new Exception("Coroutine Exception", ExceptionThrown);
			}
		}

		private static IEnumerable<Step> MakeStepEnumerable(IEnumerable enumerable)
		{
			foreach (var item in enumerable)
			{
				var stepItem = item as Step;
				if (item == null) yield return Step.Current;
				else if (stepItem == null) yield return Step.DataStep(item);
				else yield return stepItem;
			}
		}

		/// <summary>
		/// Wait for completion and store the result into the container object
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="func"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		public static Step InvokeLocalAndWait<T>(Func<IEnumerable<T>> func, Container result = null)
		{
			if (InterceptExternalCalls)
			{
				var stepResult = Interceptor(func, result);
				if (stepResult.TerminateHere) return stepResult.RealStep;
			}
			if (typeof(T) != typeof(Step))
			{
				return InvokeLocalAndWait(() => MakeStepEnumerable((IEnumerable)func()), result);
			}

			if (result == null) result = new Container();
			var enumerator = ((IEnumerable<Step>)func()).GetEnumerator();
			return Step.DataStep(new EnumeratorWrapper { Enum = (IEnumerator<Step>)enumerator, Result = result });
		}

		/// <summary>
		/// Invoke an action inside a task and wait
		/// </summary>
		/// <param name="action"></param>
		/// <returns></returns>
		public static Step InvokeAsTaskAndWait(Action action)
		{
			var currentCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
			if (InterceptExternalCalls)
			{
				var result = Interceptor(action, null);
				if (result.TerminateHere) return result.RealStep;
			}
			return InvokeTaskAndWait(Task.Factory.StartNew(() =>
												{
													System.Threading.Thread.CurrentThread.CurrentCulture = currentCulture;
													action();
												}));
		}

		/// <summary>
		/// Invoke a task and wait
		/// </summary>
		/// <param name="task"></param>
		/// <returns></returns>
		public static Step InvokeTaskAndWait(Task task)
		{
			if (InterceptExternalCalls)
			{
				var result = Interceptor(task, null);
				if (result.TerminateHere) return result.RealStep;
			}
			var enumeratorWrapper = new EnumeratorWrapper();
			enumeratorWrapper.IsTask = true;
			task.ContinueWith((t) => enumeratorWrapper.TaskCompleted = true);
			enumeratorWrapper.TaskInstance = task;
			return Step.DataStep(enumeratorWrapper);

		}

		public Step InvokeCoroutineAndWait(ICoroutine action, bool adding = true)
		{
			if (InterceptLocalCalls)
			{
				var result = Interceptor(action, null);
				if (result.TerminateHere) return result.RealStep;
			}
			return InvokeCoroutineAndWait(Thread, action, adding);
		}

		public static Step InvokeCoroutineAndWait(CoroutineThread thread, ICoroutine action, bool adding = true)
		{
			if (InterceptExternalCalls)
			{
				var result = Interceptor(action, null);
				if (result.TerminateHere) return result.RealStep;
			}
			if (adding) thread.AddCoroutine(action);
			return Step.DataStep(new EnumeratorWrapper
								 {
									 Culture = System.Threading.Thread.CurrentThread.CurrentCulture,
									 IsCoroutine = true,
									 SubCoroutine = action,
								 });
		}


		public static object CallCoroutine(IEnumerable<Step> coroutineEnumerable)
		{
			var enumerator = coroutineEnumerable.GetEnumerator();
			var coroutineStack = new CoroutineStack(enumerator, null);
			object result = null;
			while (coroutineStack.MoveNext())
			{
				var current = coroutineStack.Current;
				var step = current as Step;
				if (step != null && step.HasData)
				{
					if (!(step.Data is EnumeratorWrapper))
					{
						result = step.Data;
					}
				}
				System.Threading.Thread.Sleep(0);
			}
			return result;
		}

		// ReSharper disable PossibleMultipleEnumeration
		public static object[] CallCoroutines(params IEnumerable<Step>[] coroutineEnumerable)
		{
			var enumerator = coroutineEnumerable.Select(a => a.GetEnumerator());
			var coroutineStacks = enumerator.Select(a => new CoroutineStack(a, null)).ToArray();
			var result = new object[enumerator.Count()];
			var moveNext = true;
			while (moveNext)
			{
				var moveNextCount = coroutineStacks.Count();
				for (int index = 0; index < coroutineStacks.Length; index++)
				{
					var coroutineStack = coroutineStacks[index];
					if (!coroutineStack.MoveNext())
					{
						moveNextCount--;
						continue;
					}

					var current = coroutineStack.Current;
					var step = current as Step;
					if (step != null && step.HasData)
					{
						if (!(step.Data is EnumeratorWrapper))
						{
							result[index] = step.Data;
						}
					}
				}
				System.Threading.Thread.Sleep(0);
				moveNext = moveNextCount != 0;
			}
			return result;
		}
		// ReSharper restore PossibleMultipleEnumeration

		public Exception ExceptionThrown { get; set; }
	}
}