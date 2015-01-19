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
using ConcurrencyHelpers.Caching;
using ConcurrencyHelpers.Coroutines;

namespace ConcurrencyHelpers.Test.SpecialCache
{
	public class CorrectResourceHandler : Coroutine
	{
		private readonly CoroutineMemoryCache _memoryCache;

		public CorrectResourceHandler(CoroutineMemoryCache memoryCache)
		{
			_memoryCache = memoryCache;
		}

		private bool _isReady;

		public override bool IsReady
		{
			get
			{
				return _isReady;
			}
			set
			{
				_isReady = value;
			}
		}

		private IEnumerable<object> ReadCacheData(string index, object model)
		{
			var result = InvokeLocalAndWait(() => DataProvider.GetData(model.ToString(), 100));
			yield return null;
			yield return result.Data;
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
