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


using System.Collections;
using System.Collections.Generic;

namespace ConcurrencyHelpers.Containers.Asyncs
{
	public class AsyncDictionaryLockFreeEnumerator<TKey, TValue> : IEnumerator<KeyValuePair<TKey, TValue>>
	{
		private IEnumerable<TKey> _keysList;
		private AsyncLockFreeDictionary<TKey, TValue> _dictionary;
		private IEnumerator<TKey> _enumerator;

		internal AsyncDictionaryLockFreeEnumerator(IEnumerable<TKey> keysList, AsyncLockFreeDictionary<TKey, TValue> dictionary)
		{
			_keysList = keysList;
			_dictionary = dictionary;
			_enumerator = _keysList.GetEnumerator();
		}

		public void Dispose()
		{
			if (_enumerator != null)
			{
				_enumerator.Dispose();
			}
		}

		private KeyValuePair<TKey, TValue> _current;
		public bool MoveNext()
		{
			TValue value;
			TKey key;
			var resultNext = _enumerator.MoveNext();
			while (resultNext)
			{
				key = _enumerator.Current;
				if (!key.Equals(default(TKey)) && _dictionary.TryGetValue(key, out value))
				{
					_current = new KeyValuePair<TKey, TValue>(key, value);
					return true;
				}
				resultNext = _enumerator.MoveNext();
			}
			return false;
		}

		public void Reset()
		{
			_enumerator.Reset();
		}

		public KeyValuePair<TKey, TValue> Current
		{
			get
			{
				return _current;
			}
		}

		object IEnumerator.Current
		{
			get { return _current; }
		}
	}

	public class AsyncLockFreeEnumerator<T> : IEnumerator<T>
	{
		private IEnumerable<T> _enumerated;
		private readonly IEnumerator<T> _enumerator;

		internal AsyncLockFreeEnumerator(IEnumerable<T> srcList)
		{
			_enumerated = srcList;
			_enumerator = _enumerated.GetEnumerator();
		}

		public void Dispose()
		{
			if (_enumerator != null)
			{
				_enumerator.Dispose();
			}
			_enumerated = null;
		}

		public bool MoveNext()
		{
			return _enumerator.MoveNext();
		}

		public void Reset()
		{
			_enumerator.Reset();
		}

		public T Current { get { return _enumerator.Current; } }

		object IEnumerator.Current
		{
			get { return _enumerator.Current; }
		}
	}
}
