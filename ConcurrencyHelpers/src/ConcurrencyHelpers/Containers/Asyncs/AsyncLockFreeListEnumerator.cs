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
