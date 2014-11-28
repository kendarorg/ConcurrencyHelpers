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
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using ConcurrencyHelpers.Containers.Asyncs;
using ConcurrencyHelpers.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelpers;

namespace ConcurrencyHelpers.Test.Integration
{
	/// <summary>
	/// Summary description for AsyncDictionaryTest
	/// </summary>
	[TestClass]
	public class AsyncLockFreeDictionaryIntTest
	{
		private AsyncLockFreeDictionary<string, string> _dict;
		private ConcurrentInt64 _removed;
		private ConcurrentInt64 _added;
		private ConcurrentInt64 _read;

		[TestMethod]
		public void AsyncLockFreeDictionaryIntItShouldBePossibleToReadConcurrently()
		{
			ResetData();
			for (int i = 0; i < 100; i++)
			{
				_dict.Add(i.ToString(CultureInfo.InvariantCulture), i.ToString(CultureInfo.InvariantCulture));
			}

			var signal = new ManualResetEvent(false);
			var threads = new List<TestThread>();
			for (int j = 0; j < 100; j++)
			{
				var tt = CreateReadThread(j, signal);
				threads.Add(tt);
			}
			signal.Set();
			Thread.Sleep(1000);
			foreach (var th in threads)
			{
				th.Stop();
			}
			Thread.Sleep(1000);
			foreach (var th in threads)
			{
				Assert.IsNull(th.ExceptionThrown);
			}
		}

		private void ResetData()
		{
			_dict = new AsyncLockFreeDictionary<string, string>();
			_removed = new ConcurrentInt64();
			_added = new ConcurrentInt64();
			_read = new ConcurrentInt64();
		}

		[TestMethod]
		public void AsyncLockFreeDictionaryIntItShouldBePossibleToRemoveInsertAddConcurrently()
		{
			ResetData();
			for (int i = 0; i < 100; i++)
			{
				_dict.Add(i.ToString(CultureInfo.InvariantCulture), i.ToString(CultureInfo.InvariantCulture));
			}

			var signal = new ManualResetEvent(false);
			var threads = new List<TestThread>();
			var readThreads = new List<TestThread>();
			var insertThreads = new List<TestThread>();
			var removeThreads = new List<TestThread>();
			for (int j = 0; j < 33; j++)
			{
				var tt = CreateReadThread(j, signal);
				threads.Add(tt);
				readThreads.Add(tt);
				tt = CreateInsertThread(j, signal);
				threads.Add(tt);
				insertThreads.Add(tt);
				tt = CreateRemoveThread(j, signal);
				threads.Add(tt);
				removeThreads.Add(tt);
			}
			signal.Set();
			Thread.Sleep(1000);
			_dict.Clear();
			Thread.Sleep(100);
			foreach (var th in threads)
			{
				th.Stop();
			}
			Thread.Sleep(1000);
			foreach (var th in threads)
			{
				Assert.IsNull(th.ExceptionThrown);//, th.ExceptionThrown.ToString());
			}
			var total = 0;
			foreach (var th in readThreads)
			{
				total += th.Cycles;
			}
			Assert.IsTrue(total * 10 <= _read.Value, total + " " + _read);
			total = 0;
			foreach (var th in insertThreads)
			{
				total += th.Cycles;
			}
			Assert.IsTrue(total * 10 <= _added.Value, total + " " + _added);
			total = 0;
			foreach (var th in removeThreads)
			{
				total += th.Cycles;
			}
			Assert.IsTrue(total * 10 <= _removed.Value, total + " " + _removed);
		}

		// ReSharper disable once UnusedParameter.Local
		private TestThread CreateReadThread(int mainCounter, ManualResetEvent signal)
		{
			var tt = TestThread.Start(th =>
																{
																	var enumerator = _dict.GetEnumerator();
																	while (enumerator.MoveNext())
																	{
																		var result = enumerator.Current;
																		Assert.IsNotNull(result.Key);
																		_read++;
																	}
																	return true;
																}, signal);
			return tt;
		}

		// ReSharper disable once UnusedParameter.Local
		private TestThread CreateInsertThread(int mainCounter, ManualResetEvent signal)
		{
			var tt = TestThread.Start(th =>
			{
				for (int i = 0; i < 10; i++)
				{
					_dict.TryAdd("" + i, "" + i);
					_added++;
				}
				return true;
			}, signal);
			return tt;
		}

		// ReSharper disable once UnusedParameter.Local
		private TestThread CreateRemoveThread(int mainCounter, ManualResetEvent signal)
		{
			var tt = TestThread.Start(th =>
			{
				for (int i = 0; i < 10; i++)
				{
					_dict.Remove("" + i);
					_removed++;
				}
				return true;
			}, signal);
			return tt;
		}

		[TestMethod]
		public void AsyncLockFreeDictionaryIntItShouldBePossibleToDoOperationsOnValuesAndKeysCollections()
		{
			ResetData();
			for (int i = 0; i < 100; i++)
			{
				_dict.Add(i.ToString(CultureInfo.InvariantCulture), i.ToString(CultureInfo.InvariantCulture));
			}

			var signal = new ManualResetEvent(false);
			var threads = new List<TestThread>();
			for (int j = 0; j < 100; j++)
			{
				var tt = CreateValuesKeysThread(j, signal);
				threads.Add(tt);
			}
			signal.Set();
			Thread.Sleep(1000);
			foreach (var th in threads)
			{
				th.Stop();
			}
			Thread.Sleep(1000);
			foreach (var th in threads)
			{
				Assert.IsNull(th.ExceptionThrown);
			}
		}

		// ReSharper disable once UnusedParameter.Local
		private TestThread CreateValuesKeysThread(int mainCounter, ManualResetEvent signal)
		{
			var tt = TestThread.Start(th =>
																{
																	// ReSharper disable once UnusedVariable
																	foreach (var value in _dict.Values)
																	{

																	}
																	foreach (var key in _dict.Keys)
																	{
																		// ReSharper disable once UnusedVariable
																		var result = _dict.ContainsKey(key);
																		var value = _dict[key];
																		// ReSharper disable once SpecifyACultureInStringConversionExplicitly
																		_dict[key] = new Random(100).Next(1000).ToString() + value;
																	}
																	return true;
																}, signal);
			return tt;
		}

		[TestMethod]
		public void AsyncLockFreeDictionaryIntItShouldBePossibleToEnumeratThroughAChangedDictionary()
		{
			var dict = new AsyncLockFreeDictionary<string, string>();
			dict.Add("1", "v1");
			dict.Add("2", "v2");
			dict.Add("3", "v3");

			Thread.Sleep(100);

			var enumerator1 = dict.GetEnumerator();
			enumerator1.MoveNext();
			var current = enumerator1.Current;
			Assert.AreEqual("v1", current.Value);

			dict.Remove("2");

			Thread.Sleep(100);

			enumerator1.MoveNext();
			current = enumerator1.Current;
			Assert.AreEqual("v3", current.Value);
		}

		[TestMethod]
		public void AsyncLockFreeDictionaryIntItShouldBePossibleToDisposeEnumeratorsConcurrently()
		{
			var dict = new AsyncLockFreeDictionary<string, string>();
			dict.Add("1", "v1");
			dict.Add("2", "v2");
			dict.Add("3", "v3");

			Thread.Sleep(100);

			var enumerator1 = dict.GetEnumerator();
			var enumerator2 = dict.GetEnumerator();
			enumerator1.MoveNext();
			enumerator2.MoveNext();

			enumerator2.Dispose();

			var current = enumerator1.Current;
			Assert.AreEqual("v1", current.Value);

			dict.Remove("2");

			Thread.Sleep(100);

			enumerator1.MoveNext();
			current = enumerator1.Current;
			Assert.AreEqual("v3", current.Value);
		}

		[TestMethod]
		public void AsyncLockFreeDictionaryIntItShouldBePossibleToResetEnumerators()
		{
			var dict = new AsyncLockFreeDictionary<string, string>();
			dict.Add("1", "v1");
			dict.Add("2", "v2");
			dict.Add("3", "v3");

			Thread.Sleep(100);

			var enumerator1 = dict.GetEnumerator();
			enumerator1.MoveNext();
			var current = enumerator1.Current;
			Assert.AreEqual("v1", current.Value);

			enumerator1.Reset();

			enumerator1.MoveNext();
			current = enumerator1.Current;
			Assert.AreEqual("v1", current.Value);
		}
	}
}
