/********************************************************************
*                      AutoTester     
*                        Wan,Yu
* AutoTester is a free software, you can use it in any commercial work. 
* But you CAN NOT redistribute it and/or modify it.
*--------------------------------------------------------------------
* Component: TestFactory.cs
*
* Description: TestFactory load DLL use reflecting.
*              TestFactory read config from AutoConfig, and load expected
*              dll. 
*
* History: 2007/09/04 wan,yu Init version
*          2008/01/09 wan,yu update, add TestAppType enum, add _appType 
*                            and AppType to TestFactory
*  
* 
*********************************************************************/


using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

using Shrinerain.AutoTester.Core;
using Shrinerain.AutoTester.Core.TestExceptions;
using Shrinerain.AutoTester.Core.Helper;
using Shrinerain.AutoTester.Core.Interface;

namespace Shrinerain.AutoTester.Framework
{
    public enum TestAppType : int
    {
        Unknow = 0,
        Web = 1,
        Desktop = 2
    }

    public sealed class TestFactory
    {

        #region fields
        //assembly used to load dll.
        private static Assembly _assembly;

        //flag to indicate desktop application or browser
        // 0 means unknow, 1 means web, 2 means desktop application
        private static int _appType = 0;

        //return the application type
        public static TestAppType AppType
        {
            get
            {
                return (TestAppType)TestFactory._appType;
            }
        }

        //interface 
        private static ITestBrowser _browser;
        private static ITestApp _app;
        private static ITestObjectPool _objPool;
        private static ITestCheckPoint _testVP;

        //the dll path
        private static string _testAppDLL;
        private static string _testBrowserDLL;
        private static string _testObjPoolDLL;
        private static string _testVPDLL;
        private static string _testActionDLL;

        //class name for each interface.
        private static string _testAppClassName;
        private static string _testBrowserClassName;
        private static string _testObjectPoolClassName;
        private static string _testActionClassName;
        private static string _testVPClassName;

        #endregion

        #region properties


        #endregion

        #region methods

        #region ctor

        private TestFactory()
        {

        }

        static TestFactory()
        {
            GetAllDllPath();
        }

        #endregion

        #region public methods

        /* ITestApp CreateTestApp()
         * return expected app interface.
         * This interface is used to control the desktop application.
         */
        public static ITestApp CreateTestApp()
        {
            //if it is not desktop application, return null.
            if (_appType != 2)
            {
                return null;
            }

            if (String.IsNullOrEmpty(_testActionDLL))
            {
                throw new CannotLoadDllException("Test app dll can not be null.");
            }
            try
            {
                //load the dll, convert to expcted interface.
                _app = (ITestApp)LoadDll(_testAppDLL, _testAppClassName);
            }
            catch (Exception ex)
            {
                throw new CannotLoadDllException("Can not create instance of test app: " + ex.ToString());
            }

            if (_app == null)
            {
                throw new CannotLoadDllException("Can not create instance of test app.");
            }

            return _app;
        }

        /* static ITestBrowser CreateTestBrowser()
         * return expected ITestBrowser.
         * This interface is used to control the browser.
         */
        public static ITestBrowser CreateTestBrowser()
        {
            // if it is not web application, return null.
            if (_appType != 1)
            {
                return null;
            }

            if (String.IsNullOrEmpty(_testBrowserDLL))
            {
                throw new CannotLoadDllException("Test browser dll can not be null.");
            }

            try
            {
                //load the dll, convert to expcted interface.
                _browser = (ITestBrowser)LoadDll(_testBrowserDLL, _testBrowserClassName);
            }
            catch (Exception ex)
            {
                throw new CannotLoadDllException("Can not create instance of Test Browser: " + ex.ToString());
            }

            if (_browser == null)
            {
                throw new CannotLoadDllException("Can not create instance of test browser.");
            }
            else
            {
                return _browser;
            }

        }

        /*  ITestObjectPool CreateTestObjectPool()
         *  return the interface of ITestObjectPool.
         *  This interface is used to find the test object.
         */
        public static ITestObjectPool CreateTestObjectPool()
        {
            if (String.IsNullOrEmpty(_testObjPoolDLL))
            {
                throw new CannotLoadDllException("Test object dll can not be null.");
            }
            try
            {
                _objPool = (ITestObjectPool)LoadDll(_testObjPoolDLL, _testObjectPoolClassName);
            }
            catch (Exception ex)
            {
                throw new CannotLoadDllException("Can not create instance of test object pool: " + ex.ToString());
            }
            if (_objPool == null)
            {
                throw new CannotLoadDllException("Can not create instance of test object pool.");
            }
            else
            {
                return _objPool;
            }

        }

        /* ITestVP CreateTestVP()
         * Return ITestVP interface.
         * This interface is used to perform check point.
         */
        public static ITestCheckPoint CreateTestVP()
        {
            if (String.IsNullOrEmpty(_testVPDLL))
            {
                throw new CannotLoadDllException("Test VP dll can not be null.");
            }
            try
            {
                _testVP = (ITestCheckPoint)LoadDll(_testVPDLL, _testVPClassName);
            }
            catch (Exception ex)
            {
                throw new CannotLoadDllException("Can not create instance of test VP: " + ex.ToString());
            }
            if (_testVP == null)
            {
                throw new CannotLoadDllException("Can not create instance of test VP.");
            }
            else
            {
                return _testVP;
            }

        }

        #endregion

        #region private methods

        /* void GetAllDLLPath()
         * Get the dll path from framework config file.
         */
        private static void GetAllDllPath()
        {
            if (String.IsNullOrEmpty(_testObjPoolDLL))
            {
                try
                {
                    AutoConfig config = AutoConfig.GetInstance();

                    string domain = config.ProjectDomain;

                    string currentPath = Application.StartupPath;// Assembly.GetExecutingAssembly().Location;
                    currentPath = Path.GetDirectoryName(currentPath);

                    PluginInfo tmp = config.GetTestPluginByDomain(domain);

                    _testAppDLL = tmp._testAppDLL;
                    _testAppClassName = tmp._testApp;

                    _testBrowserDLL = tmp._testBrowserDLL;
                    _testBrowserClassName = tmp._testBrowser;

                    _testObjPoolDLL = tmp._testObjectPoolDLL;
                    _testObjectPoolClassName = tmp._testObjectPool;

                    _testActionDLL = tmp._testActionDLL;
                    _testActionClassName = tmp._testAction;

                    _testVPDLL = tmp._testVPDLL;
                    _testVPClassName = tmp._testVP;

                    //default , we think it is web application.
                    _appType = 1;

                    bool isApp = false;

                    //if the <TestApp> text in config file is not empty, we think it is desktop application.
                    if (!String.IsNullOrEmpty(_testAppDLL))
                    {
                        _appType = 2;
                        isApp = true;
                    }
                    else
                    {
                        if (String.IsNullOrEmpty(_testBrowserDLL))
                        {
                            _appType = 0;

                            throw new CannotLoadDllException(" Test app and Test browser can not be null.");
                        }
                    }

                    bool allFound = true;

                    //search each dll
                    if (isApp)
                    {
                        if (!SearchDll(currentPath, ref _testAppDLL))
                        {
                            allFound = false;
                        }
                    }
                    else
                    {
                        if (!SearchDll(currentPath, ref _testBrowserDLL))
                        {
                            allFound = false;
                        }
                    }

                    if (!SearchDll(currentPath, ref _testObjPoolDLL))
                    {
                        allFound = false;
                    }

                    if (!SearchDll(currentPath, ref _testActionDLL))
                    {
                        allFound = false;
                    }

                    if (!SearchDll(currentPath, ref _testVPDLL))
                    {
                        allFound = false;
                    }

                    if (!allFound)
                    {
                        throw new CannotLoadDllException("Can not find the plugin dll.");
                    }
                }
                catch (TestException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new CannotLoadDllException("Can not find the dll path: " + ex.ToString());
                }
            }
        }

        /* SearchDLL(string dir, ref string dllFullPath)
         * Return true if dll exist.
         */
        private static bool SearchDll(string dir, ref string dllFullPath)
        {

            if (File.Exists(dllFullPath))
            {
                return true;
            }

            string expectedPath = dir + "//" + dllFullPath;

            if (File.Exists(expectedPath))
            {
                dllFullPath = expectedPath;
                return true;
            }
            else
            {
                //search sub folder.
                string[] dirs = Directory.GetDirectories(dir);

                foreach (string d in dirs)
                {
                    if (SearchDll(d, ref dllFullPath))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /*  Object LoadDll(String dll, string className)
         *  Load dll, use reflecting.
         */
        private static Object LoadDll(String dll, string className)
        {

            //use reflecting to create instance for dll.
            _assembly = Assembly.LoadFrom(dll);
            Type type = _assembly.GetType(className);

            return Activator.CreateInstance(type);
        }

        #endregion

        #endregion

    }
}
