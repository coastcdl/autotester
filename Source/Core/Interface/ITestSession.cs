﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Shrinerain.AutoTester.Core.Interface
{
    public interface ITestSession
    {
        ITestApp App { get; }
        ITestBrowser Browser { get; }
        ITestObjectMap Objects { get; }
        ITestEventDispatcher Event { get; }
        ITestCheckPoint CheckPoint { get; }
    }
}
