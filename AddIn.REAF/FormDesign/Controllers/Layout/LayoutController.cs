using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessageEngine.SuperMCMCore;
using Keystone.AddIn.FormDesigner.Messages;
using Keystone.AddIn.FormDesigner.Elements;
using System.Drawing;
using System.Diagnostics;
using MessageEngine;
using Keystone.AddIn.FormDesigner.Elements;
using Keystone.Common.Utility;

namespace Keystone.AddIn.FormDesigner.Controllers.Layout
{
    class LayoutController : BaseMessageListener
    {

        ViewDefinition currentView = null;

        [MessageSubscriber]
        private void on(CurrentViewDefinitionMsg msg)
        {
            currentView = msg.ViewDefinition;
        }

        [MessageSubscriber(MessageEngine.SuperMCMCore.MMMODE.ASIS)]
        private void on(LayoutMsg msg)
        {
            if (currentView == null)
                return;

            if (msg.ContainerElement.HasChildren)
            {

#if DEBUG
                Debug.WriteLine("LayoutController " + msg.ToString() + " At " + DateTime.Now.ToString());
#endif

                Debug.Assert(!msg.ContainerElement.LayoutSuspended);
                if (msg.ContainerElement.LayoutSuspended)
                    return;

                ElementLayoutMode layoutMode = msg.ContainerElement.GetElementLayoutMode();
                switch (layoutMode)
                {
                    case ElementLayoutMode.Free:
                        this.doLayout_Free(msg);
                        break;

                    case ElementLayoutMode.Tab:
                        this.doLayout_Tab(msg);
                        break;

                    //case ElementLayoutMode.Grid :
                    //    this.doLayout_Grid(msg);
                    //    break;

                    case ElementLayoutMode.Dock:
                        this.doLayout_Dock(msg);
                        break;

                    case ElementLayoutMode.Group:
                        this.doLayout_Group(msg);
                        break;

                    default:
                        throw new Exception("Not Handled!");
                }
                Debug.Assert(!msg.ContainerElement.LayoutSuspended);
            }

            //重新排序
            msg.ContainerElement.SortChildren();

            if (msg.ContainerElement.ParentElement != null 
                && !msg.ContainerElement.ParentElement.LayoutSuspended 
                && msg.ContainerElement.ParentElement.CheckLayout() )
            {
                this.on(new LayoutMsg(msg.ContainerElement.ParentElement, msg.ContainerElement.ParentElement.BuildChildrenLayoutInfo(null)));
            }
        }

        private void doLayout_Free(LayoutMsg msg)
        {
            if (!msg.ContainerElement.HasChildren)
                return;

            try
            {
                bool layoutChanged = false;
                
                //获取所有内部元素的边界矩形
                List<Rectangle> rectList = new List<Rectangle>();
                foreach (IViewElement e in msg.ContainerElement.Children)
                {
                    Debug.Assert(e.Dock == DockType.None);
                    rectList.Add(e.GetRectangle());
                }

                msg.ContainerElement.SuspendLayout();
                Rectangle rect = GeoHelper.GetOutterRectangle(rectList.ToArray());
                if (rect.X < 0)
                {
                    //平移
                    foreach (IViewElement e in msg.ContainerElement.Children)
                    {
                        e.SuspendLayout();
                        e.X -= rect.X;
                        e.ResumeLayout();
                    }

                    rect.X = 0;
                }
                if (rect.Y < 0)
                {
                    //平移
                    foreach (IViewElement e in msg.ContainerElement.Children)
                    {
                        e.SuspendLayout();
                        e.Y -= rect.Y;
                        e.ResumeLayout();
                    }

                    rect.Y = 0;
                }

                if (msg.ContainerElement.Width < rect.Right && msg.ContainerElement.Height < rect.Bottom)
                {
                    msg.ContainerElement.SetSize(rect.Right, rect.Bottom);
                    layoutChanged = true;
                }
                else if (msg.ContainerElement.Height < rect.Bottom)
                {
                    msg.ContainerElement.Height = rect.Bottom;
                    layoutChanged = true;
                }
                else if (msg.ContainerElement.Width < rect.Right)
                {
                    msg.ContainerElement.Width = rect.Right;
                    layoutChanged = true;
                }
                msg.ContainerElement.ResumeLayout();

                //布局发生变化，更新
                if (layoutChanged && this.currentView != null)
                {
                    if (this.currentView == msg.ContainerElement)
                        SuperMCMService.PostMessage(new resizeFormDesignerMsg(), ViewDesignerMainController.MESSAGECHANNEL);
                    SuperMCMService.PostMessage(new ViewPaintRequestMsg(), ViewDesignerMainController.MESSAGECHANNEL);//, 100
                }
            }
            finally
            {
            }
#if DEBUG
            List<Rectangle> rect2List = new List<Rectangle>();
            foreach (IViewElement e in msg.ContainerElement.Children)
                rect2List.Add(e.GetRectangle());

            if (rect2List.Count > 0)
            {
                Rectangle r = GeoHelper.GetOutterRectangle(rect2List.ToArray());
                Debug.Assert(msg.ContainerElement.Width >= r.Right);
                Debug.Assert(msg.ContainerElement.Height >= r.Bottom);
            }
#endif

        }
        private void doLayout_Group(LayoutMsg msg)
        {
            if (!msg.ContainerElement.HasChildren)
                return;

            try
            {
                bool layoutChanged = false;
                
                //获取所有内部元素的边界矩形
                List<Rectangle> rectList = new List<Rectangle>();
                foreach (IViewElement e in msg.ContainerElement.Children)
                {
                    Debug.Assert(e.Dock == DockType.None);
                    rectList.Add(e.GetRectangle());
                }

                msg.ContainerElement.SuspendLayout();
                Rectangle rect = GeoHelper.GetOutterRectangle(rectList.ToArray());
                if (rect.X < 0)
                {
                    //平移
                    foreach (IViewElement e in msg.ContainerElement.Children)
                    {
                        e.SuspendLayout();
                        e.X -= rect.X;
                        e.ResumeLayout();
                    }

                    rect.X = 0;
                }
                if (rect.Y < 24)
                {
                    //平移
                    foreach (IViewElement e in msg.ContainerElement.Children)
                    {
                        e.SuspendLayout();
                        e.Y += 24 - rect.Y;
                        e.ResumeLayout();
                    }

                    rect.Y = 24;
                }

                if (msg.ContainerElement.Width < rect.Right && msg.ContainerElement.Height < rect.Bottom)
                {
                    msg.ContainerElement.SetSize(rect.Right, rect.Bottom);
                    layoutChanged = true;
                }
                else if (msg.ContainerElement.Height < rect.Bottom)
                {
                    msg.ContainerElement.Height = rect.Bottom;
                    layoutChanged = true;
                }
                else if (msg.ContainerElement.Width < rect.Right)
                {
                    msg.ContainerElement.Width = rect.Right;
                    layoutChanged = true;
                }
                msg.ContainerElement.ResumeLayout();

                //布局发生变化，更新
                if (layoutChanged && this.currentView != null)
                {
                    if (this.currentView == msg.ContainerElement)
                        SuperMCMService.PostMessage(new resizeFormDesignerMsg(), ViewDesignerMainController.MESSAGECHANNEL);
                    SuperMCMService.PostMessage(new ViewPaintRequestMsg(), ViewDesignerMainController.MESSAGECHANNEL);//, 100
                }
            }
            finally
            {
            }
#if DEBUG
            List<Rectangle> rect2List = new List<Rectangle>();
            foreach (IViewElement e in msg.ContainerElement.Children)
                rect2List.Add(e.GetRectangle());

            if (rect2List.Count > 0)
            {
                Rectangle r = GeoHelper.GetOutterRectangle(rect2List.ToArray());
                Debug.Assert(msg.ContainerElement.Width >= r.Right);
                Debug.Assert(msg.ContainerElement.Height >= r.Bottom);
            }
#endif

        }
        private void doLayout_Tab(LayoutMsg msg)
        {
            if (!msg.ContainerElement.HasChildren)
                return;

            try
            {
                bool changed = false;
                foreach (IViewElement e in msg.ContainerElement.Children)
                {
                    e.SuspendLayout();
                }
                List<Rectangle> rectList = new List<Rectangle>();
                foreach (TabPageElement tabPage in msg.ContainerElement.Children)
                {
                    rectList.Add(tabPage.GetAllChildrenOutterRect());
                }
                Rectangle rect = GeoHelper.GetOutterRectangle(rectList.ToArray());// msg.ContainerElement.GetAllChildrenOutterRect();

                if (rect.X < 0)
                {
                    foreach (TabPageElement tabPage in msg.ContainerElement.Children)
                        foreach (IViewElement e in msg.ContainerElement.Children)
                            e.X -= rect.X;
                    changed = true;
                }
                if (rect.Y < 0)
                {
                    foreach (TabPageElement tabPage in msg.ContainerElement.Children)
                        foreach (IViewElement e in msg.ContainerElement.Children)
                            e.Y -= rect.Y;
                    changed = true;
                }

                rectList.Clear();
                foreach (TabPageElement tabPage in msg.ContainerElement.Children)
                {
                    rectList.Add(tabPage.GetAllChildrenOutterRect());
                }
                rect = GeoHelper.GetOutterRectangle(rectList.ToArray());// msg.ContainerElement.GetAllChildrenOutterRect();

                if (msg.ContainerElement.Width < rect.Right || msg.ContainerElement.Height < rect.Bottom + TabElement.TABHEADERSPAN)
                {
                    msg.ContainerElement.SetSize(Math.Max(msg.ContainerElement.Width, rect.Right),
                        Math.Max(msg.ContainerElement.Height, rect.Bottom + TabElement.TABHEADERSPAN));
                    changed = true;
                }

                foreach (IViewElement e in msg.ContainerElement.Children)
                {
                    e.X = 0;
                    e.Y = TabElement.TABHEADERSPAN;
                    e.SetSize(msg.ContainerElement.Width, msg.ContainerElement.Height - TabElement.TABHEADERSPAN);
                }

                if (changed)
                {
                    SuperMCMService.PostMessage(new ViewPaintRequestMsg(), ViewDesignerMainController.MESSAGECHANNEL);//, 100
                }
            }
            finally
            {
                foreach (IViewElement e in msg.ContainerElement.Children)
                {
                    Debug.Assert(e.LayoutSuspended);
                    e.ResumeLayout();
                }
            }
        }
        private void doLayout_Dock(LayoutMsg msg)
        {
            if (msg.ContainerElement.LayoutSuspended)
                return;

            Debug.Assert(msg.ContainerElement.GetElementLayoutMode() == ElementLayoutMode.Dock);

            Dictionary<Guid, Rectangle> dic = msg.ContainerElement.ParentElement.BuildChildrenLayoutInfo(null);

            IViewElement preFillElement = msg.Tag as IViewElement;
            if (preFillElement != null && preFillElement.Dock != DockType.Fill )
            {
                preFillElement.SuspendLayout();
                Size s = preFillElement.GetMiniSize();
                preFillElement.SetSize(s.Width, s.Height);
                preFillElement.ResumeLayout();
            }

            Size minSize = msg.ContainerElement.GetMiniSize();
            Size mySize = msg.ContainerElement.GetNodeSize();
            bool sizeChangedAndLayoutParent = mySize.Width < minSize.Width || mySize.Height < minSize.Height;
            bool layoutChanged = sizeChangedAndLayoutParent;

            
            msg.ContainerElement.SuspendLayout();
            if (msg.ContainerElement.ParentElement != null)
                msg.ContainerElement.ParentElement.SuspendLayout();

            if (sizeChangedAndLayoutParent)
            {
                //尺寸发生变化
                msg.ContainerElement.SetSize(Math.Max(mySize.Width, minSize.Width), Math.Max(mySize.Height, minSize.Height));
            }

            Rectangle rectLeft = new Rectangle(Point.Empty, msg.ContainerElement.GetNodeSize());

            //调整内部元素位置
            IViewElement fillElement = null;
            foreach (IViewElement e in msg.ContainerElement.Children)
            {
                Debug.Assert(e.Dock != DockType.None);

                if (e.Dock == DockType.Fill)
                {
                    Debug.Assert(fillElement == null);
                    fillElement = e;
                }
                else if( e.Dock == DockType.Top )
                {
                    e.SuspendLayout();
                    e.X = rectLeft.X;
                    e.Y = rectLeft.Y;
                    e.Width = rectLeft.Width;
                    rectLeft.Y = e.Y + e.Height;
                    rectLeft.Height -= e.Height;
                    e.ResumeLayout();
                }
                else if (e.Dock == DockType.Bottom)
                {
                    e.SuspendLayout();
                    e.X = rectLeft.X;
                    e.Y = rectLeft.Bottom - e.Height;
                    e.Width = rectLeft.Width;
                    rectLeft.Height -= e.Height;
                    e.ResumeLayout();
                }
                else if (e.Dock == DockType.Left)
                {
                    e.SuspendLayout();
                    e.Y = rectLeft.Y;
                    e.X = rectLeft.X;
                    e.Height = rectLeft.Height;
                    rectLeft.X = e.X + e.Width;
                    rectLeft.Width -= e.Width;
                    e.ResumeLayout();
                }
                else if (e.Dock == DockType.Right)
                {
                    e.SuspendLayout();
                    e.Y = rectLeft.Y;
                    e.X = rectLeft.Right - e.Width;
                    e.Height = rectLeft.Height;
                    rectLeft.Width -= e.Width;
                    e.ResumeLayout();
                }
            }

            if (fillElement != null)
            {
                if (fillElement.GetRectangle() != rectLeft)
                {
                    fillElement.SuspendLayout();
                    fillElement.X = rectLeft.X;
                    fillElement.Y = rectLeft.Y;
                    fillElement.SetSize(rectLeft.Width, rectLeft.Height);
                    fillElement.ResumeLayout();
                    if( fillElement is IViewElementContainer )
                        SuperMCMService.PostMessage(new LayoutMsg(fillElement as IViewElementContainer, dic), ViewDesignerMainController.MESSAGECHANNEL);
                    layoutChanged = true;
                }
            }



            msg.ContainerElement.ResumeLayout();
            if (msg.ContainerElement.ParentElement != null)
                msg.ContainerElement.ParentElement.ResumeLayout();

            //布局发生变化，更新
            if (layoutChanged)
            {
                SuperMCMService.PostMessage(new ViewPaintRequestMsg(), ViewDesignerMainController.MESSAGECHANNEL);//, 100
            }
            if (sizeChangedAndLayoutParent && msg.ContainerElement.ParentElement !=null && !msg.ContainerElement.ParentElement.LayoutSuspended)
            {
                SuperMCMService.PostMessage(new LayoutMsg(msg.ContainerElement.ParentElement, msg.ContainerElement, dic), ViewDesignerMainController.MESSAGECHANNEL);
            }

#if DEBUG
            List<Rectangle> rectList = new List<Rectangle>();
            foreach (IViewElement e in msg.ContainerElement.Children)
                rectList.Add(e.GetRectangle());

            if (rectList.Count > 0)
            {
                Rectangle r = GeoHelper.GetOutterRectangle(rectList.ToArray());
                Debug.Assert(msg.ContainerElement.Width >= r.Right);
                Debug.Assert(msg.ContainerElement.Height >= r.Bottom);
            }
#endif
        }
        //private void doLayout_Grid(LayoutMsg msg)
        //{
        //}
    }
}
