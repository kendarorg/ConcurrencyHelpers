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
using System.Globalization;
using System.Threading;

namespace ConcurrencyHelpers.Utils
{
	/// <summary>
	/// Class to wrap the interlocking logic
	/// </summary>
	public class ConcurrentBool
	{
		private Int64 _value;

		public ConcurrentBool(bool value = false)
		{
			Value = value;
		}

		public bool Value
		{
			get { return Interlocked.Read(ref _value) == 1; }
			set { Interlocked.Exchange(ref _value, value ? 1 : 0); }
		}

		public bool GetAndReset()
		{
			return Interlocked.Exchange(ref _value, 0) == 1;
		}

		public bool GetAndSet(bool value)
		{
			return Interlocked.Exchange(ref _value, value ? 1 : 0) == 1;
		}

		public bool CompareExchange(bool compareWith, bool exchangeWith)
		{
			return Interlocked.CompareExchange(ref _value, exchangeWith ? 1 : 0, compareWith ? 1 : 0) == 1;
		}

		public static explicit operator bool(ConcurrentBool value)
		{
			return value.Value;
		}

		public static implicit operator ConcurrentBool(bool value)
		{
			return new ConcurrentBool(value);
		}

		public override string ToString()
		{
			return Value.ToString(CultureInfo.InvariantCulture);
		}
	}
}