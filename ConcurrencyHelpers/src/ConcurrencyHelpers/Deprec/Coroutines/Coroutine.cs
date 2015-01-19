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