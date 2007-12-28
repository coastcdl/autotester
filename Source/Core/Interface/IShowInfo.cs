/********************************************************************
*                      AutoTester     
*                        Wan,Yu
* AutoTester is a free software, you can use it in any commercial work. 
* But you CAN NOT redistribute it and/or modify it.
*--------------------------------------------------------------------
* Component: IShowInfo.cs
*
* Description: This interface defines the actions of an object which
*              will show some information to the end-users.
*              Like TextBox etc.
*
* History:  2007/09/04 wan,yu Init version
*
*********************************************************************/

using System;
using System.Collections.Generic;
using System.Text;

namespace Shrinerain.AutoTester.Core
{
    public interface IShowInfo : IVisible
    {
        //get the text on the object
        string GetText();

        //get the font style, like color=red, size=28
        string GetFontStyle();

        //get the font family, like comic font.
        string GetFontFamily();
    }
}