/********************************************************************
*                      AutoTester     
*                        Wan,Yu
* AutoTester is a free software, you can use it in any commercial work. 
* But you CAN NOT redistribute it and/or modify it.
*--------------------------------------------------------------------
* Component: IVisible.cs
*
* Description: This interface defines the actions of an object which
*              is visible to end-users.
*
* History: 2007/09/04 wan,yu Init version
*
*********************************************************************/


namespace Shrinerain.AutoTester.Core
{
    using System;
    using System.Drawing;

    public interface IVisible : IHighLight
    {
        //some visible information for object.
        string GetLabel();

        //get the center point of the object
        Point GetCenterPoint();

        //get the rectangle of the object on screen
        Rectangle GetRectOnScreen();

        //get the image of the object
        Bitmap GetControlPrint();

        //move mouse to the top of control
        void Hover();
        void MouseClick(int x, int y);
        void KeyboardInput(String value);

        //status of visible object.
        bool IsVisible();
        bool IsEnable();
        bool IsReadonly();
        bool IsFocused();
    }
}
