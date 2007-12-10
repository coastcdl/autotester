/********************************************************************
*                      AutoTester     
*                        Wan,Yu
* AutoTester is a free software, you can use it in any commercial work. 
* But you CAN NOT redistribute it and/or modify it.
*--------------------------------------------------------------------
* Component: HTMLTestListBox.cs
*
* Description: This class defines the actions provide by Listbox.
*              the important actions include "Select" and "SelectByIndex"
*
* History: 2007/09/04 wan,yu Init version
*
*********************************************************************/

using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

using mshtml;

using Shrinerain.AutoTester.Interface;
using Shrinerain.AutoTester.Function;
using Shrinerain.AutoTester.Function.Interface;
using Shrinerain.AutoTester.Win32;

namespace Shrinerain.AutoTester.HTMLUtility
{
    //HTMLTestListBox is NOT a HTML control, it is a standard windows control.
    public class HTMLTestListBox : HTMLGuiTestObject, ISelectable, IWindows
    {
        #region fields

        //the item is selected.
        protected string _selectedValue;

        //all values of the listbox
        protected string[] _allValues;

        //size of listbox. if it has more items than the size, we can see scroll bar.
        protected int _itemCountPerPage;

        //height of each item.
        protected int _itemHeight;

        //handle for listbox, listbox is a windows control
        protected IntPtr _handle;

        protected HTMLSelectElementClass _htmlSelectClass;

        #endregion

        #region properties

        public int ItemCountPerPage
        {
            get { return _itemCountPerPage; }

        }

        public string SelectedValue
        {
            get { return _selectedValue; }

        }

        protected int ItemHeight
        {
            get { return _itemHeight; }
        }


        #endregion

        #region methods

        #region ctor

        public HTMLTestListBox(IHTMLElement element)
            : base(element)
        {
            try
            {
                _htmlSelectClass = (HTMLSelectElementClass)element;
            }
            catch
            {
                throw new CanNotBuildObjectException("HTML source element can not be null.");
            }
            try
            {
                this._allValues = this.GetAllValues();
            }
            catch
            {
                throw new CanNotBuildObjectException("Can not get the list values.");
            }
            try
            {
                this._selectedValue = this.GetSelectedValue();
            }
            catch
            {
                this._selectedValue = "";
            }
            try
            {
                this._itemCountPerPage = int.Parse(element.getAttribute("size", 0).ToString());
            }
            catch (Exception e)
            {
                throw new CanNotBuildObjectException("Can not get size of list box: " + e.Message);
            }
            try
            {
                this._class = "Internet Explorer_TridentLstBox";

                //get the windows handle
                IntPtr listboxHandle = Win32API.FindWindowEx(TestBrowser.IEServerHandle, IntPtr.Zero, this._class, null);
                while (listboxHandle != IntPtr.Zero)
                {
                    // get the rect of this control
                    Win32API.Rect tmpRect = new Win32API.Rect();
                    Win32API.GetWindowRect(listboxHandle, ref tmpRect);
                    int centerX = (tmpRect.right - tmpRect.left) / 2 + tmpRect.left;
                    int centerY = (tmpRect.bottom - tmpRect.top) / 2 + tmpRect.top;

                    //if the control has the same position with our HTML test object, that means we find it.
                    if ((centerX > this.Rect.Left && centerX < this.Rect.Left + this.Rect.Width) && (centerY > this.Rect.Top && centerY < this.Rect.Top + this.Rect.Height))
                    {
                        this._handle = listboxHandle;

                        break;
                    }
                    else
                    {
                        //else, go to next listbox
                        listboxHandle = Win32API.FindWindowEx(TestBrowser.IEServerHandle, listboxHandle, this._class, null);
                    }
                }

                if (this._handle == IntPtr.Zero)
                {
                    throw new CanNotBuildObjectException("Can not get windows handle of list box.");
                }
            }
            catch (CanNotBuildObjectException)
            {
                throw;
            }
            catch
            {
                throw new CanNotBuildObjectException("Can not get windows handle of list box.");
            }

            try
            {
                //get the height of each item.
                this._itemHeight = Win32API.SendMessage(this._handle, Convert.ToInt32(Win32API.LISTBOXMSG.LB_GETITEMHEIGHT), 0, 0);
            }
            catch
            {
                if (this.ItemCountPerPage < 1)
                {
                    this._itemHeight = this.Rect.Height;
                }
                else
                {
                    this._itemHeight = this.Rect.Height / this.ItemCountPerPage;
                }
            }
        }

        #endregion

        #region public methods

        /* void Select(string value)
        *  Select an item by text.
        *  This method will get the index by text, and call SelectByIndex method to perform action.
        */
        public virtual void Select(string value)
        {
            int index;

            //if input text is null, select the frist one.
            if (String.IsNullOrEmpty(value))
            {
                index = 0;
            }
            else
            {
                //get the actual index in the listbox items.
                index = GetIndexByString(value);
            }

            SelectByIndex(index);

        }

        public virtual void SelectByIndex(int index)
        {
            if (this._allValues == null)
            {
                this._allValues = GetAllValues();
            }

            //check the index, if the index is smaller than 0 or larger than the capacity.
            // set it to 0 or the last index.
            if (index < 0)
            {
                index = 0;
            }
            else if (index > this._allValues.Length)
            {
                index = this._allValues.Length - 1;
            }

            try
            {
                _actionFinished.WaitOne();

                Hover();

                //get the actual position on the screen.
                Point itemPosition = GetItemPosition(index);

                //click at this point.
                MouseOp.Click(itemPosition.X, itemPosition.Y);

                //refresh the selected value.
                this._selectedValue = this._allValues[index];

                _actionFinished.Set();

            }
            catch (ItemNotFoundException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new CanNotPerformActionException(e.ToString());
            }

        }

        /* String[] GetAllValues()
         * Return the all items text of the listbox
         */
        public String[] GetAllValues()
        {
            string[] values = new string[_htmlSelectClass.length];
            try
            {
                HTMLOptionElementClass optionClass;
                for (int i = 0; i < _htmlSelectClass.length; i++)
                {
                    optionClass = (HTMLOptionElementClass)_htmlSelectClass.item(i, i);
                    values[i] = optionClass.innerText;
                }
                return values;
            }
            catch
            {
                throw;
            }

        }

        #region IWindows Interface

        public virtual IntPtr GetHandle()
        {
            return this._handle;
        }

        public virtual string GetClass()
        {
            return this._class;
        }

        #endregion

        #region IInteractive interface

        public virtual void Focus()
        {
            Hover();
            MouseOp.Click();
        }

        public virtual object GetDefaultAction()
        {
            return "Select";
        }

        public virtual void PerformDefaultAction(object para)
        {
            int index;
            if (int.TryParse(para.ToString(), out index))
            {
                SelectByIndex(index);
            }
            else
            {
                Select(para.ToString());
            }

        }

        #endregion

        public virtual string GetText()
        {
            return this._selectedValue;
        }

        public virtual string GetFontStyle()
        {
            return null;
        }

        public virtual string GetFontFamily()
        {
            return null;
        }

        #endregion

        #region private methods

        /* int GetSelectedIndex()
         * get the index of selected value.
         */
        protected virtual int GetSelectedIndex()
        {
            return _htmlSelectClass.selectedIndex;
        }

        /* string GetSelectedValue()
         * return the selected value.
         */
        protected virtual string GetSelectedValue()
        {
            if (this._allValues == null)
            {
                this._allValues = GetAllValues();
            }

            try
            {
                return this._allValues[_htmlSelectClass.selectedIndex];
            }
            catch
            {
                return "";
            }

        }

        /* Point GetItemPosition(int index)
         * Get the actual position on screen of the expected item index.
         */
        protected virtual Point GetItemPosition(int index)
        {
            //get the the first item index we can  see currently.
            int startIndex = GetTopIndex();

            //if the index is smaller than 0 or larger than capacity
            if (startIndex < 0 || startIndex >= this._allValues.Length)
            {
                throw new ItemNotFoundException("Can not get the position of item at index: " + index.ToString());
            }

            //0 for visible, 1 for smaller than TopIndex,2 for larger than LastIndex.
            //if 0, we can see it directly without move the scroll bar.
            //if 1, we need to move the scroll bar upward to see it.
            //if 2, we need to move the scroll bar downward to see it.
            int positionFlag = -1;

            if (index < startIndex)
            {
                positionFlag = 1;
            }
            else
            {
                if (index > startIndex + this._itemCountPerPage)
                {
                    positionFlag = 2;
                }
                else
                {
                    positionFlag = 0;
                }
            }

            if (positionFlag == -1)
            {
                throw new ItemNotFoundException("Can not get item: " + index.ToString());
            }

            //find the position of the first visible item.
            int itemX = this.Rect.Width / 3 + this.Rect.Left;
            int itemY = this.Rect.Top + this.ItemHeight / 2;

            if (positionFlag == 0)
            {
                //currently, we can see it, just click it.
                itemY += (index - startIndex) * this.ItemHeight;
            }
            else if (positionFlag == 1)
            {
                //we need to move the scroll bar upward.
                Win32API.SendMessage(this._handle, Convert.ToInt32(Win32API.LISTBOXMSG.LB_SETTOPINDEX), index, 0);
            }
            else if (positionFlag == 2)
            {
                //we need to move the scroll bar downward, and recalculate the position.
                int expectedStartIndex = index - this._itemCountPerPage + 1;
                Win32API.SendMessage(this._handle, Convert.ToInt32(Win32API.LISTBOXMSG.LB_SETTOPINDEX), expectedStartIndex, 0);

                itemY += (this._itemCountPerPage - 1) * this.ItemHeight;
            }

            return new Point(itemX, itemY);

        }

        /* int GetTopIndex()
         * return the first item index we can see currently.
         */
        protected virtual int GetTopIndex()
        {
            try
            {
                return Win32API.SendMessage(this._handle, Convert.ToInt32(Win32API.LISTBOXMSG.LB_GETTOPINDEX), 0, 0);
            }
            catch
            {
                throw new PropertyNotFoundException("Can not get the first visible item.");
            }

        }

        /* int GetIndexByString(string value)
         * get the index of expected text.
         */
        protected virtual int GetIndexByString(string value)
        {
            if (this._allValues == null)
            {
                this._allValues = GetAllValues();
            }


            for (int i = 0; i < this._allValues.Length; i++)
            {
                if (String.Compare(_allValues[i], value, true) == 0)
                {
                    return i;
                }
            }

            throw new ItemNotFoundException("Can not find item: " + value);

        }

        /* void HighLightRectCallback(object obj)
         * HTMLTestListBox is NOT a HTML object, it is a standard Windwos control.
         * So we need to override this function.
         */
        protected override void HighLightRectCallback(object obj)
        {
            base.HighLightRect(true);
        }

        #endregion

        #endregion



    }

}