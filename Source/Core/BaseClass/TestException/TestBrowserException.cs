﻿/********************************************************************
*                      AutoTester     
*                        Wan,Yu
* AutoTester is a free software, you can use it in any commercial work. 
* But you CAN NOT redistribute it and/or modify it.
*--------------------------------------------------------------------
* Component: TestBrowserException.cs
*
* Description: This component defines exceptions used for web application.
*
* History: 2007/12/28 wan,yu Init version
*
*********************************************************************/


using System;
using System.Collections.Generic;
using System.Text;

namespace Shrinerain.AutoTester.Core
{
    public class TestBrowserException : TestException
    {
        public TestBrowserException()
            : this("TestBrowse exception.")
        {

        }

        public TestBrowserException(string message)
            : base(message)
        {

        }
    };

    public class TestBrowserNotFoundException : TestBrowserException
    {
        public TestBrowserNotFoundException()
            : this("Can not find test browser.")
        {

        }
        public TestBrowserNotFoundException(string message)
            : base(message)
        {

        }
    };
    public class CannotStartTestBrowserException : TestBrowserException
    {
        public CannotStartTestBrowserException()
            : this("Can not start test browser.")
        {

        }
        public CannotStartTestBrowserException(string message)
            : base(message)
        {

        }
    };
    public class CannotAttachTestBrowserException : TestBrowserException
    {
        public CannotAttachTestBrowserException()
            : this("Can not hook test browser.")
        {
        }
        public CannotAttachTestBrowserException(string message)
            : base(message)
        {

        }
    };
    public class CannotActiveTestBrowserException : TestBrowserException
    {
        public CannotActiveTestBrowserException()
            : this("Can not active test browser.")
        {

        }
        public CannotActiveTestBrowserException(string message)
            : base(message)
        {

        }
    };

    public class CannotStopTestBrowserException : TestBrowserException
    {
        public CannotStopTestBrowserException()
            : this("Can not stop test browser.")
        {

        }

        public CannotStopTestBrowserException(string message)
            : base(message)
        {

        }
    }

    public class CannotLoadUrlException : TestBrowserException
    {
        public CannotLoadUrlException()
            : this("Can not load the url, please check the url")
        {
        }
        public CannotLoadUrlException(string message)
            : base(message)
        {

        }
    }
    public class CannotNavigateException : TestBrowserException
    {
        public CannotNavigateException()
            : this("Can not navigate.")
        {
        }
        public CannotNavigateException(string message)
            : base(message)
        {

        }
    }
    public class CannotGetBrowserInfoException : TestBrowserException
    {
        public CannotGetBrowserInfoException()
            : this("Can not get information from test browser.")
        {
        }

        public CannotGetBrowserInfoException(string message)
            : base(message)
        {

        }
    }

    public class CannotPrintException : TestBrowserException
    {
        public CannotPrintException()
            : this("Can not print current page.")
        {
        }

        public CannotPrintException(string message)
            : base(message)
        {
        }
    }

}
