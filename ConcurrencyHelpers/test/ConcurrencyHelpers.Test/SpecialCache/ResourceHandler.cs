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
using ConcurrencyHelpers.Caching;
using ConcurrencyHelpers.Coroutines;

namespace ConcurrencyHelpers.Test.SpecialCache
{
	public class ResourceHandler : Coroutine
	{
		private readonly CoroutineMemoryCache _memoryCache;

		public ResourceHandler(CoroutineMemoryCache memoryCache)
		{
			_memoryCache = memoryCache;
		}

		private IEnumerable<object> ReadCacheData(string index, object model)
		{
			foreach (var item in DataProvider.GetData(model.ToString(), 100))
			{
				if (item == null) yield return null;
				else
				{
					yield return item;
					break;
				}
			}
		}

		public string Model { get; set; }
		public string ReadData { get; set; }
		public Exception Error { get; set; }

		public override IEnumerable<Step> Run()
		{
			var localPath = string.Empty;
			var result = InvokeLocalAndWait(() => _memoryCache.AddOrGet(localPath, () => ReadCacheData(localPath, Model)), false);
			yield return Step.Current;
			ReadData = ((CacheItem)result.Data).Data.ToString();
			while (Thread.Status == CoroutineThreadStatus.Running)
			{
				yield return Step.Current;
			}
		}

		public override void OnError(Exception ex)
		{
			Error = ex;
			ShouldTerminate = true;
		}
	}
}
