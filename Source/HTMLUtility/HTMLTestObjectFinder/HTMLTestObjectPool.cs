/********************************************************************
*                      AutoTester     
*                        Wan,Yu
* AutoTester is a free software, you can use it in any commercial work. 
* But you CAN NOT redistribute it and/or modify it.
*--------------------------------------------------------------------
* Component: HTMLTestObjectPool.cs
*
* Description: This class implement ITestObjectPool interface.
*              we can get HTML object from HTMLTestObjectPool
*                        
*
*********************************************************************/


using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using mshtml;

using Shrinerain.AutoTester.Core;
using Shrinerain.AutoTester.Core.TestExceptions;
using Shrinerain.AutoTester.Core.Helper;
using Shrinerain.AutoTester.Core.Interface;
using Shrinerain.AutoTester.Win32;

namespace Shrinerain.AutoTester.HTMLUtility
{
    public sealed class HTMLTestObjectPool : ITestObjectPool
    {

        #region fields

        private HTMLTestPage _htmlTestPage;

        //the default similar pencent to  find an object, if 100, that means we should make sure 100% match. 
        private bool _useFuzzySearch = false;
        private const int _defaultPercent = 100;
        private int _similarPercentUpBound = 100;
        private int _similarPercentLowBound = 70;
        private int _similarPercentStep = 10;

        //IHTMLElement is the interface for mshtml html object. We build actual test object on IHTMLElement.
        private IHTMLElement _tempElement;
        //IHTMLElementCollection is an array of IHTMLElement, some functions return the array of IHTMLElement.
        private IHTMLElement[] _allElements;
        private TestObject[] _allObjects;

        //current object used.
        private TestObject _testObj;

        private HTMLTestDialog _lastDialog;
        private TestProperty[] _lastDialogProperties;

        //the max time we need to wait, eg: we may wait for 30s to find a test object.
        private int _searchTimeout = 30;
        //very time we sleep for 2 seconds, and find again.
        private const int Interval = 2;

        //regex to match tag
        private static Regex _htmlReg = new Regex("< *[a-z]+[^>]+>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static Regex _tagReg = new Regex("<[a-zA-Z]+ ", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Dictionary<string, Regex> _regCache = new Dictionary<string, Regex>(17);

        public event TestObjectEventHandler OnObjectFound;

        #endregion

        #region Properties

        //set the browser for this object pool, we get objects from a browser
        public HTMLTestPage TestPage
        {
            get { return _htmlTestPage; }
            set { _htmlTestPage = value; }
        }

        public int SearchTimeout
        {
            get { return _searchTimeout; }
            set { _searchTimeout = value; }
        }

        public bool FuzzySearch
        {
            get { return _useFuzzySearch; }
            set { _useFuzzySearch = value; }
        }

        #endregion

        #region event

        #endregion

        #region public methods

        #region ctor

        public HTMLTestObjectPool()
        {
        }

        public HTMLTestObjectPool(ITestPage page)
        {
            _htmlTestPage = page as HTMLTestPage;
        }

        #endregion

        /* void SetTestBrowser(ITestBrowser brower)
         * Set the related browser, we will get object from this browser.
         * 
         */
        public void SetTestPage(ITestPage page)
        {
            this._htmlTestPage = page as HTMLTestPage;
        }

        public void SetTestWindow(ITestWindow win)
        {
            //for HTML tesing, no desktop application is needed.
        }

        #region ITestObjectPool

        /* Object GetObjectByID(string id)
         * return the test object by .id property.
         *
         */
        public TestObject GetObjectByID(string id)
        {
            BeforeObjectFound();

            if (String.IsNullOrEmpty(id))
            {
                throw new ObjectNotFoundException("Can not find object by id: id can not be empty.");
            }

            id = id.Trim();
            //we will try 30 seconds to find an object.
            int times = 0;
            while (times <= _searchTimeout)
            {
                try
                {
                    //get IHTMLElement interface
                    _tempElement = _htmlTestPage.GetElementByID(id);
                    if (_tempElement != null)
                    {
                        //build actual test object.
                        _testObj = HTMLTestObjectFactory.BuildHTMLTestObject(_tempElement, this._htmlTestPage);
                        AfterObjectFound(_testObj);
                        return _testObj;
                    }
                }
                catch (CannotBuildObjectException)
                {
                    throw;
                }
                catch
                {
                }

                times += Interval;
                Thread.Sleep(Interval * 1000);
            }

            throw new ObjectNotFoundException("Can not get object by id:" + id);
        }


        /*  Object GetObjectByName(string name)
         *  return the test object by .name property
         */
        public TestObject[] GetObjectsByName(string name)
        {
            BeforeObjectFound();

            if (string.IsNullOrEmpty(name))
            {
                throw new ObjectNotFoundException("Can not find object by name: name can not be empty.");
            }

            name = name.Trim();
            //we will try 30s to find a object
            int times = 0;
            while (times <= _searchTimeout)
            {
                try
                {
                    List<TestObject> result = new List<TestObject>();

                    IHTMLElement[] nameObjectsCol = _htmlTestPage.GetElementsByName(name);
                    for (int i = 0; i < nameObjectsCol.Length; i++)
                    {
                        _tempElement = (IHTMLElement)nameObjectsCol[i];
                        if (HTMLTestObjectFactory.IsVisible(_tempElement))
                        {
                            _testObj = HTMLTestObjectFactory.BuildHTMLTestObject(_tempElement, this._htmlTestPage);
                            AfterObjectFound(_testObj);
                            result.Add(_testObj);
                        }
                    }

                    if (result.Count > 0)
                    {
                        return result.ToArray();
                    }
                }
                catch (CannotBuildObjectException)
                {
                    throw;
                }
                catch
                {
                }

                times += Interval;
                Thread.Sleep(Interval * 1000);
            }

            throw new ObjectNotFoundException("Can not get objects by name:" + name);
        }

        /* Object GetObjectByIndex(int index)
         * return the test object by an integer index.
         */
        public TestObject GetObjectByIndex(int index)
        {
            BeforeObjectFound();

            if (index < 0)
            {
                index = 0;
            }

            int times = 0;
            while (times <= _searchTimeout)
            {
                try
                {
                    GetAllElements();

                    _tempElement = _allElements[index];
                    if (HTMLTestObjectFactory.IsVisible(_tempElement))
                    {
                        _testObj = HTMLTestObjectFactory.BuildHTMLTestObject(_tempElement, this._htmlTestPage);
                        AfterObjectFound(_testObj);
                        return _testObj;
                    }
                }
                catch (CannotBuildObjectException)
                {
                    throw;
                }
                catch
                {
                }

                times += Interval;
                Thread.Sleep(Interval * 1000);
            }

            throw new ObjectNotFoundException("Can not get object by index:" + index);
        }


        /* Object GetObjectByProperty(string property, string value)
         * return the test object by expect property.
         * eg: to find a image, we can find it by it's .src property, like .src="111.jpg"
         * we will use "Fuzzy Search" in this method
         */
        public TestObject[] GetObjectsByProperties(TestProperty[] properties)
        {
            BeforeObjectFound();

            if (properties == null || properties.Length == 0)
            {
                throw new ObjectNotFoundException("Object properties can not be empty.");
            }
            //the similar percent to find an object.
            int simPercent = _defaultPercent;
            if (_useFuzzySearch)
            {
                simPercent = _similarPercentUpBound;
            }

            //we will try 30s to find an object.
            int times = 0;
            while (times <= _searchTimeout)
            {
                IHTMLElement[] candidateElements = GetElementsByCommonProperty(properties);
                if (candidateElements == null)
                {
                    //get all HTML objects.
                    GetAllElements();
                    candidateElements = _allElements;
                }

                if (candidateElements != null || candidateElements.Length > 0)
                {
                    List<TestObject> result = new List<TestObject>();

                    bool isOnlyOneObject = false;
                    //if we have too many objects, we will try to find it's possible position to improve performance.
                    int possibleStartIndex = 0;
                    if (Searcher.IsNeedCalPossibleStartIndex(candidateElements.Length))
                    {
                        string searchVal = System.Web.HttpUtility.HtmlEncode(properties[0].Value.ToString());
                        if (!String.IsNullOrEmpty(searchVal))
                        {
                            string htmlContent = this._htmlTestPage.GetAllHTMLContent();
                            int startPos = htmlContent.IndexOf(searchVal);
                            if (startPos > 0)
                            {
                                //if we have too many objects, we will try to find it's possible position to improve performance.             
                                possibleStartIndex = Searcher.GetPossibleStartIndex(candidateElements.Length, _htmlReg, htmlContent, searchVal);
                                if (!properties[0].IsRegex && startPos == htmlContent.LastIndexOf(searchVal))
                                {
                                    isOnlyOneObject = true;
                                }
                            }
                        }
                    }
                    int[] searchOrder = Searcher.VibrationSearch(possibleStartIndex, 0, candidateElements.Length - 1);
                    // check object one by one, start from the possible position.
                    // the "|" means the start position, the "--->" means the search direction.            
                    //  -----------------------------------------------------------------------
                    //  step 1:                          |--->
                    //  step 2:                      <---|
                    //  step 3:                          |    --->
                    //  ...                      <---    |
                    foreach (int currentObjIndex in searchOrder)
                    {
                        try
                        {
                            //get element by index.
                            _tempElement = (IHTMLElement)candidateElements[currentObjIndex];
                            //if it is not an interactive object or the property is not found. 
                            if (HTMLTestObjectFactory.IsVisible(_tempElement))
                            {
                                if (CheckObjectProperties(_tempElement, HTMLTestObjectType.Unknown, properties, simPercent, out _testObj))
                                {
                                    AfterObjectFound(_testObj);
                                    result.Add(_testObj);
                                    if (isOnlyOneObject)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                        catch (CannotBuildObjectException)
                        {
                            throw;
                        }
                        catch
                        {
                        }
                    }

                    if (result.Count > 0)
                    {
                        return result.ToArray();
                    }
                }

                times += Interval;
                Thread.Sleep(Interval * 1000);

                //while current simpercent is bigger than the low bound,we can still try lower similarity
                if (_useFuzzySearch)
                {
                    if (simPercent > _similarPercentLowBound)
                    {
                        simPercent -= _similarPercentStep;
                    }
                    else
                    {
                        simPercent = _similarPercentUpBound;
                    }
                }
            }

            StringBuilder propertiesInfo = new StringBuilder();
            foreach (TestProperty tp in properties)
            {
                propertiesInfo.Append("[" + tp.Name + ":" + tp.Value.ToString() + "],");
            }

            throw new ObjectNotFoundException("Can not find object by properties: " + propertiesInfo.ToString());
        }


        public TestObject[] GetObjectsByType(string type, TestProperty[] properties)
        {
            BeforeObjectFound();

            if (type == null || type.Trim() == "")
            {
                throw new ObjectNotFoundException("Can not get object by type: type can not be empty.");
            }
            else
            {
                type = type.Trim();
            }

            //convert the TYPE text to valid internal type.
            // eg: "button" to HTMLTestObjectType.Button
            String typeValue = HTMLTestObjectFactory.GetHTMLTypeByString(type);
            if (typeValue == HTMLTestObjectType.Unknown)
            {
                throw new ObjectNotFoundException("Unknow type.");
            }

            //convert the type to HTML tags.
            //eg: convert Image to <img>, Button to <input type="button">,<input type="submit">...
            string[] tags = HTMLTestObjectFactory.GetObjectTags(typeValue);
            if (tags == null)
            {
                throw new ObjectNotFoundException("Tags can not be empty.");
            }

            //the similar percent to find an object.
            int simPercent = _defaultPercent;
            if (_useFuzzySearch)
            {
                simPercent = _similarPercentUpBound;
            }

            //we will try 30s to find an object.
            int times = 0;
            while (times <= _searchTimeout)
            {
                List<TestObject> result = new List<TestObject>();

                if (typeValue == HTMLTestObjectType.Dialog)
                {
                    HTMLTestDialog dialog = null;
                    if (_lastDialogProperties == properties && _lastDialog != null && _lastDialog.IsExists())
                    {
                        dialog = _lastDialog;
                    }
                    else
                    {
                        IntPtr dialogHandle = GetHTMLDialogObject(properties);
                        if (dialogHandle != IntPtr.Zero)
                        {
                            dialog = HTMLTestObjectFactory.BuildHTMLTestObject(dialogHandle, this._htmlTestPage) as HTMLTestDialog;
                            _lastDialog = dialog;
                            _lastDialogProperties = properties;
                        }
                    }

                    if (dialog != null)
                    {
                        _testObj = dialog;
                        AfterObjectFound(_testObj);
                        result.Add(_testObj);
                    }
                }
                else
                {
                    IHTMLElement[] candidateElements = GetElementsByCommonProperty(properties);

                    bool isOnlyOneObject = false;
                    //because we may convert one type to multi tags, so check them one by one.
                    //eg: Button to <input> and <button>
                    foreach (string tag in tags)
                    {
                        if (candidateElements == null)
                        {
                            candidateElements = this._htmlTestPage.GetElementsByTagName(tag);
                        }

                        if (candidateElements != null && candidateElements.Length > 0)
                        {
                            int possibleStartIndex = 0;
                            if (properties != null && properties.Length > 0 && Searcher.IsNeedCalPossibleStartIndex(candidateElements.Length))
                            {
                                string searchVal = System.Web.HttpUtility.HtmlEncode(properties[0].Value.ToString());
                                if (!String.IsNullOrEmpty(searchVal))
                                {
                                    Regex tagReg;
                                    if (!_regCache.TryGetValue(tag, out tagReg))
                                    {
                                        //create new regex to match objects from HTML code.
                                        tagReg = new Regex("<" + tag + "[^>]+>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
                                        _regCache.Add(tag, tagReg);
                                    }

                                    string htmlContent = this._htmlTestPage.GetAllHTMLContent();
                                    int startPos = htmlContent.IndexOf(searchVal);
                                    if (startPos > 0)
                                    {
                                        //if we have too many objects, we will try to find it's possible position to improve performance.             
                                        possibleStartIndex = Searcher.GetPossibleStartIndex(candidateElements.Length, tagReg, htmlContent, searchVal);
                                        if (!properties[0].IsRegex && startPos == htmlContent.LastIndexOf(searchVal))
                                        {
                                            isOnlyOneObject = true;
                                        }
                                    }
                                }
                            }
                            int[] searchOrder = Searcher.VibrationSearch(possibleStartIndex, 0, candidateElements.Length - 1);
                            // check object one by one, start from the possible position.
                            // the "|" means the start position, the "--->" means the search direction.            
                            //  -----------------------------------------------------------------------
                            //  step 1:                          |--->
                            //  step 2:                      <---|
                            //  step 3:                          |    --->
                            //  ...                      <---    |
                            foreach (int currentObjIndex in searchOrder)
                            {
                                try
                                {
                                    _tempElement = candidateElements[currentObjIndex];
                                    // check if it is a interactive object.
                                    if (HTMLTestObjectFactory.IsVisible(_tempElement))
                                    {
                                        //check object by type
                                        if (HTMLTestObjectFactory.GetObjectType(_tempElement) == typeValue)
                                        {
                                            if (CheckObjectProperties(_tempElement, typeValue, properties, simPercent, out _testObj))
                                            {
                                                AfterObjectFound(_testObj);
                                                result.Add(_testObj);
                                                if (isOnlyOneObject)
                                                {
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                                catch (CannotBuildObjectException)
                                {
                                    throw;
                                }
                                catch
                                {
                                }
                            }
                        }

                        if (isOnlyOneObject && result.Count > 0)
                        {
                            break;
                        }

                        candidateElements = null;
                    }
                }

                if (result.Count > 0)
                {
                    return result.ToArray();
                }

                //not found, sleep for 3 seconds, then try again.
                times += Interval;
                Thread.Sleep(Interval * 1000);

                //not found, we will try lower similarity
                if (_useFuzzySearch)
                {
                    if (simPercent > _similarPercentLowBound)
                    {
                        simPercent -= _similarPercentStep;
                    }
                    else
                    {
                        simPercent = _similarPercentUpBound;
                    }
                }
            }

            throw new ObjectNotFoundException("Can not find object by type [" + type + "]");
        }

        /* Object GetObjectByPoint(int x, int y)
         * return object from a expected point
         * x, y is the offset with browser, NOT screen.
         * 
         */
        public TestObject GetObjectByPoint(int x, int y)
        {
            BeforeObjectFound();

            int times = 0;
            while (times <= _searchTimeout)
            {
                try
                {
                    _tempElement = this._htmlTestPage.GetElementByPoint(x, y);
                    if (HTMLTestObjectFactory.IsVisible(_tempElement))
                    {
                        _testObj = HTMLTestObjectFactory.BuildHTMLTestObject(_tempElement, this._htmlTestPage);
                        AfterObjectFound(_testObj);

                        return _testObj;
                    }
                }
                catch (CannotBuildObjectException)
                {
                    throw;
                }
                catch
                {
                }

                times += Interval;
                Thread.Sleep(Interval * 1000);
            }

            throw new ObjectNotFoundException("Can not get object at point(" + x.ToString() + "" + y.ToString() + ")");
        }

        /* Object GetObjectByRect(int top, int left, int width, int height)
         * return object from a expected rect.
         */
        public TestObject GetObjectByRect(int left, int top, int width, int height, string typeStr, bool isPercent)
        {
            BeforeObjectFound();

            if (width < 1 || height < 1)
            {
                throw new ObjectNotFoundException("The width and height of rect can not be 0.");
            }

            String type = HTMLTestObjectFactory.GetHTMLTypeByString(typeStr);
            if (type == HTMLTestObjectType.Unknown)
            {
                throw new ObjectNotFoundException("Unknow type.");
            }

            int x = 0;
            int y = 0;
            if (isPercent)
            {
                isPercent = false;
                if ((left >= 1 && left <= 100) && (top >= 1 && top <= 100))
                {
                    HTMLTestBrowser browser = this._htmlTestPage.Browser as HTMLTestBrowser;
                    x = browser.ClientWidth * left / 100;
                    y = browser.ClientHeight * top / 100;
                }
                else
                {
                    throw new ObjectNotFoundException("Left and top percent must between 1 and 100.");
                }
            }
            else
            {
                x = left + width / 2;
                y = top + height / 2;
            }

            int originalTimeout = this._searchTimeout;
            this._searchTimeout = 3;

            //try to get 5 objects from different area.
            HTMLTestGUIObject[] tmpObj = new HTMLTestGUIObject[5];
            for (int i = 0; i < tmpObj.Length; i++)
            {
                try
                {
                    if (i == 0)
                    {
                        tmpObj[0] = (HTMLTestGUIObject)GetObjectByPoint(x, y);
                    }
                    else if (width > 3 && height > 3)
                    {
                        if (i == 1)
                        {
                            tmpObj[1] = (HTMLTestGUIObject)GetObjectByRect(x, y, width / 2, height / 2, typeStr, isPercent);
                        }
                        else if (i == 2)
                        {
                            tmpObj[2] = (HTMLTestGUIObject)GetObjectByRect(x + width / 2, y, width / 2, height / 2, typeStr, isPercent);
                        }
                        else if (i == 3)
                        {
                            tmpObj[3] = (HTMLTestGUIObject)GetObjectByRect(x + width / 2, y + height / 2, width / 2, height / 2, typeStr, isPercent);
                        }
                        else if (i == 4)
                        {
                            tmpObj[4] = (HTMLTestGUIObject)GetObjectByRect(x, y + height / 2, width / 2, height / 2, typeStr, isPercent);
                        }
                    }

                    if (tmpObj[i] != null && tmpObj[i].Type == type)
                    {
                        _testObj = tmpObj[i];
                        AfterObjectFound(_testObj);

                        return _testObj;
                    }
                }
                catch
                {
                    continue;
                }
            }

            this._searchTimeout = originalTimeout;

            throw new ObjectNotFoundException("Can not get object by rect.");
        }

        /* Object[] GetAllObjects()
         * return all object from the  browser.
         * we use _allObjects to store these object.
         */
        public TestObject[] GetAllObjects()
        {
            BeforeObjectFound();

            try
            {
                //firstly, get all IHTMLElement from the browser
                GetAllElements();
                _allObjects = new TestObject[this._allElements.Length];

                //convert IHTMLELementCollection to an array.
                for (int i = 0; i < this._allElements.Length; i++)
                {
                    _allObjects[i] = HTMLTestObjectFactory.BuildHTMLTestObject((IHTMLElement)this._allElements[i], this._htmlTestPage);
                }

                return _allObjects;
            }
            catch (TestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ObjectNotFoundException("Can not get all elements: " + ex.ToString());
            }
        }

        /* Object GetLastObject()
         * return the last object we have got.
         */
        public TestObject GetLastObject()
        {
            return _testObj;
        }

        #endregion

        #endregion

        #region help methods

        #region private methods

        /* void GetAllElements()
         * load all IHTMLElement from browser.
         * just reload when the _needRefresh flag is set to ture.
         * 
         */
        private IHTMLElement[] GetAllElements()
        {
            this._allElements = this._htmlTestPage.GetAllHTMLElements();
            return this._allElements;
        }

        //if the properties contains "id","name","tagName".
        private IHTMLElement[] GetElementsByCommonProperty(TestProperty[] properties)
        {
            if (properties != null && properties.Length > 0)
            {
                string id;
                string name;
                string tagName;
                if (HTMLTestObjectFactory.TryGetCommonProperties(properties, out id, out name, out tagName))
                {
                    if (!String.IsNullOrEmpty(id))
                    {
                        IHTMLElement element = this._htmlTestPage.GetElementByID(id);
                        if (element != null)
                        {
                            return new IHTMLElement[] { element };
                        }
                    }
                    else if (!String.IsNullOrEmpty(name))
                    {
                        IHTMLElement[] tmp = this._htmlTestPage.GetElementsByName(name);
                        if (tmp != null && tmp.Length > 0)
                        {
                            return tmp;
                        }
                    }
                    else if (!String.IsNullOrEmpty(tagName))
                    {
                        IHTMLElement[] tmp = this._htmlTestPage.GetElementsByTagName(tagName);
                        if (tmp != null && tmp.Length > 0)
                        {
                            return tmp;
                        }
                    }
                }
            }

            return null;
        }

        #region check test object

        private bool CheckObjectProperties(IHTMLElement element, String type, TestProperty[] properties, int simPercent, out TestObject obj)
        {
            obj = null;
            int totalResult = 0;
            if (properties == null)
            {
                totalResult = 100;
            }
            else
            {
                foreach (TestProperty tp in properties)
                {
                    bool isCurrentPropertyMatch = false;
                    if (String.Compare(tp.Name, "tag", true) == 0)
                    {
                        isCurrentPropertyMatch = tp.IsValueMatch(element.tagName);
                    }
                    else if (String.Compare(tp.Name, TestConstants.PROPERTY_VISIBLE, true) == 0)
                    {
                        isCurrentPropertyMatch = CheckVisibleProperty(element, type, tp, simPercent, out obj);
                    }
                    else
                    {
                        //get property value
                        string propertyValue;
                        if (HTMLTestObject.TryGetProperty(element, tp.Name, out propertyValue))
                        {
                            isCurrentPropertyMatch = tp.IsValueMatch(propertyValue, simPercent);
                        }
                    }

                    if (isCurrentPropertyMatch)
                    {
                        totalResult += tp.Weight;
                    }
                    else
                    {
                        totalResult -= tp.Weight;
                    }
                }
            }

            if (totalResult > 0)
            {
                if (obj == null)
                {
                    obj = HTMLTestObjectFactory.BuildHTMLTestObjectByType(element, type, this._htmlTestPage);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        private bool CheckVisibleProperty(IHTMLElement element, String type, TestProperty property, int simPercent, out TestObject obj)
        {
            obj = null;

            try
            {
                string visibleText = "";
                if (type == HTMLTestObjectType.Link || type == HTMLTestObjectType.Label)
                {
                    visibleText = element.innerText;
                }
                else if (type == HTMLTestObjectType.DropList || type == HTMLTestObjectType.ListBox)
                {
                    visibleText = ((IHTMLOptionElement)(element as IHTMLSelectElement).item(0, 0)).text;
                }
                else
                {
                    obj = HTMLTestObjectFactory.BuildHTMLTestObjectByType(element, type, this._htmlTestPage);
                    visibleText = ((HTMLTestGUIObject)obj).GetLabel();
                    string propertyValue;
                    if (String.IsNullOrEmpty(visibleText) && HTMLTestObject.TryGetProperty(element, "innerText", out propertyValue))
                    {
                        visibleText = propertyValue;
                    }
                }

                visibleText = visibleText.Trim();
                if (!String.IsNullOrEmpty(visibleText))
                {
                    return property.IsValueMatch(visibleText, simPercent);
                }
            }
            catch
            {
            }

            return false;
        }

        private static IntPtr GetHTMLDialogObject(TestProperty[] properties)
        {
            try
            {
                if (properties == null || properties.Length == 0)
                {
                    return WindowsAsstFunctions.GetActiveDialog();
                }
                else
                {
                    String className = TestConstants.WIN_Dialog_Class;
                    String text = null;

                    foreach (TestProperty tp in properties)
                    {
                        if (tp.Name.IndexOf("class", StringComparison.CurrentCultureIgnoreCase) >= 0)
                        {
                            className = tp.Value.ToString();
                        }
                        else if (String.Compare(tp.Name, TestConstants.PROPERTY_VISIBLE, true) == 0 ||
                                 tp.Name.IndexOf("text", StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                                 tp.Name.IndexOf("caption", StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                                 tp.Name.IndexOf("name", StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                                 tp.Name.IndexOf("window", StringComparison.CurrentCultureIgnoreCase) >= 0)
                        {
                            text = tp.Value.ToString();
                        }
                    }

                    return Win32API.FindWindow(className, text);
                }
            }
            catch
            {
            }

            return IntPtr.Zero;
        }

        #endregion

        private void BeforeObjectFound()
        {
            if (this._htmlTestPage == null)
            {
                throw new BrowserNotFoundException("Can not find HTML test browser for HTMLTestObjectPool.");
            }

            int times = 0;
            while (times < _searchTimeout && this._htmlTestPage.Browser.IsLoading())
            {
                times += Interval;
                Thread.Sleep(Interval * 1000);
            }
        }

        private void AfterObjectFound(TestObject obj)
        {
            if (OnObjectFound != null)
            {
                OnObjectFound(obj, null);
            }
        }

        #endregion

        #endregion
    }
}
