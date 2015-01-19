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