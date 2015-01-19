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


using System.Threading;
using ConcurrencyHelpers.Interfaces;
using ConcurrencyHelpers.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConcurrencyHelpers.Test.Utils
{
    [TestClass]
    public class SystemTimerTest
    {
        private int _elapsedCount;
        private SystemTimer _t;

        private void TimerElapsed(object sender, ElapsedTimerEventArgs e)
        {
            _elapsedCount++;
        }

        private void TimerElapsed100(object sender, ElapsedTimerEventArgs e)
        {
            Thread.Sleep(100);
            _elapsedCount++;
        }

        [TestInitialize]
        public void TestInitialize()
        {
            _elapsedCount = 0;
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _t.Dispose();
        }

        [TestMethod]
        public void SystemTimerDelayTimerStart()
        {
            _t = new SystemTimer(100, 100);
            _t.Elapsed += TimerElapsed;
            _t.Start();
            Thread.Sleep(90);
            Assert.AreEqual(0, _elapsedCount);
            Thread.Sleep(220);
            Assert.IsTrue(3 == _elapsedCount || 2 == _elapsedCount);
        }

        [TestMethod]
        public void SystemTimerDelayTimerStartChangingTimers()
        {
            _t = new SystemTimer(1, 1);
            _t.Elapsed += TimerElapsed;
            _t.Start(100, 100);
            Thread.Sleep(90);
            Assert.IsTrue(_elapsedCount <= 1);
            Thread.Sleep(220);
            Assert.IsTrue(_elapsedCount >= 2);
            Assert.AreEqual(_t.TimesRun, _elapsedCount);
        }

        [TestMethod]
        [Ignore]
        public void SystemTimerRemoveElapsed()
        {
            _t = new SystemTimer(1, 1);
            _t.Elapsed += TimerElapsed;
            _t.Start(100, 100);
            Thread.Sleep(90);
            Assert.AreEqual(0, _elapsedCount);
            Thread.Sleep(120);
            _t.Stop();
            var elapsed = _elapsedCount;

            _t.Elapsed -= TimerElapsed;

            _t.Start(100, 100);
            Thread.Sleep(90);
            Assert.AreEqual(elapsed, _elapsedCount);
            Thread.Sleep(220);
            _t.Stop();
            Assert.AreEqual(elapsed, _elapsedCount);
            Assert.AreNotEqual(_t.TimesRun, _elapsedCount);
        }

        [TestMethod]
        public void SystemTimerRunGivenTimes()
        {
            _t = new SystemTimer(100, 10);
            _t.Elapsed += TimerElapsed;
            _t.Start();
            Thread.Sleep(240);
            Assert.IsTrue(_t.Running);
            _t.Stop();
            Assert.AreEqual(3, _elapsedCount);
            Assert.IsFalse(_t.Running);
        }

        [TestMethod]
        public void SystemTimerInitialization()
        {
            _t = new SystemTimer(15, 101);
            Assert.AreEqual(15, _t.Period);
            _t.Start();
            _t.Dispose();
            _t = new SystemTimer(15);
        }

        [TestMethod]
        public void SystemTimerZeroWait()
        {
            _t = new SystemTimer(50);
            _t.Elapsed += TimerElapsed;
            _t.Start();
            Thread.Sleep(265);
            Assert.AreEqual(5, _elapsedCount);
        }

        [TestMethod]
        public void SystemTimerNoOverlap()
        {
            _t = new SystemTimer(50);
            _t.Elapsed += TimerElapsed100;
            _t.Start();
            Thread.Sleep(350);
            _t.Stop();
            Assert.AreEqual(3, _elapsedCount);
        }

        [TestMethod]
        public void SystemTimerStop()
        {
            _t = new SystemTimer(50);
            _t.Elapsed += TimerElapsed;
            _t.Start();
            Thread.Sleep(265);
            Assert.AreEqual(5, _elapsedCount);
            _t.Stop();
            Thread.Sleep(265);
            Assert.AreEqual(5, _elapsedCount);
        }
    }
}
