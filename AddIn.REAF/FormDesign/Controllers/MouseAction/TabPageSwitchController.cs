using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessageEngine.SuperMCMCore;
using Keystone.AddIn.FormDesigner.Messages;
using Keystone.AddIn.FormDesigner.Elements;
using Keystone.Common.Messages;
using System.Drawing;
using Keystone.Common.Utility;
using Keystone.WellKnownMessage.Windows;
using Keystone.AddIn.FormDesigner.Elements;
using MessageEngine;

namespace Keystone.AddIn.FormDesigner.Controllers.MouseAction
{
    class TabPageSwitchController : baseFormController
    {
        Elements.IViewElement[] selections = null;

        [MessageSubscriber]
        private void on(SelectionChangedMsg msg)
        {
            if (this.form == null)
                return;

            selections = msg.Elements;
        }


        IViewElement element;
        [MessageSubscriber()]
        private void on(InsertElementTypeMsg msg)
        {
            this.element = msg.Element;
        }
        private MouseDownMoveUpMsg mouseDown = null;
        private Point mouseCurrent = Point.Empty;
        private TabElement tabElement = null;

        private void clearAction()
        {
            this.mouseDown = null;
            this.tabElement = null;
            this.mouseCurrent = Point.Empty;
        }
        private bool isElementSelected(IViewElement e)
        {
            return this.selections != null && Array.IndexOf(this.selections, e) != -1;
        }
        public ActionStatus CurrentActionStatus
        {
            get
            {
                if (mouseDown == null || this.tabElement == null)
                    return ActionStatus.Stoped;
                else
                    return ActionStatus.Started;
            }
        }

        [MessageSubscriber(MessageEngine.SuperMCMCore.MMMODE.ASIS)]
        private void on(MouseDownMoveUpMsg msg)
        {
            if (this.form == null)
                return;

            if (this.element != null)
                return;

            if (msg.MouseButton != System.Windows.Forms.MouseButtons.Left)
                return;

            switch (this.CurrentActionStatus)
            {
                case ActionStatus.Stoped:
                    {
                        if (msg.Type == MouseDownMoveUpMsg.MsgType.MouseDown)
                        {
                            if (DragMoveController.IsOnDragRegion(this.selections, msg.MousePosInControl))
                                return;

                            IViewElement[] elements = this.form.HitTest(msg.MousePosInControl, 0);
                            IViewElementContainer topMostContainer = BaseViewElement.TopMostElementContainer(elements);
                            if (topMostContainer is TabElement && this.isElementSelected(topMostContainer))
                            {
                                mouseDown = msg;
                                this.tabElement = topMostContainer as TabElement;
                                mouseCurrent = msg.MousePosInControl;
                            }
                            else
                            {
                                this.clearAction();
                            }
                        }
                    }
                    break;

                case ActionStatus.Started:
                    {
                        if (msg.Type == MouseDownMoveUpMsg.MsgType.MouseMove)
                        {
                            if (GeoHelper.IsMouseMoved(this.mouseCurrent, msg.MousePosInControl))
                            {
                                this.clearAction();
                            }
                        }
                        else if (msg.Type == MouseDownMoveUpMsg.MsgType.MouseUp)
                        {
                            //
                            this.executeAction();
                            this.clearAction();
                        }
                    }
                    break;

            }
        }

        private void executeAction()
        {
            if (this.mouseDown == null || this.tabElement == null)
                return;

            if (!GeoHelper.IsMouseMoved(this.mouseCurrent, this.mouseDown.MousePosInControl))
            {
                TabPageElement page = this.tabElement.HitTestTabPageByTitle(this.mouseDown.MousePosInControl);
                if (page != null)
                {
                    this.tabElement.ActivePage = page;
                    SuperMCMService.PostMessage(new ViewPaintRequestMsg(), ViewDesignerMainController.MESSAGECHANNEL);//, 100
                }
                //鼠标未移动
                //执行点击选择
                //IViewElement[] elements = this.form.HitTest(this.mouseDown.MousePosInControl, 0);
                //if (elements == null || elements.Length == 0)
                //{
                //    //未选中内容
                //    SuperMCMService.PostMessage(new SelectionChangedMsg(new IViewElement[] { this.container }), ViewDesignerMainController.MESSAGECHANNEL);
                //}
                //else
                //{
                //    //Array.Sort(elements, BaseViewElementContainer.CompareByParentAndArea);
                //    IViewElement actionElement = null;

                //    //foreach (IViewElement e in elements)
                //    //{
                //    //    if (this.isElementSelected(e))
                //    //    {
                //    //        actionElement = e;
                //    //        break;
                //    //    }
                //    //}
                //    if (actionElement == null)
                //    {
                //        foreach (IViewElement e in elements)
                //        {
                //            if (actionElement == null)
                //                actionElement = e;
                //            else if (actionElement.Area > e.Area)
                //                actionElement = e;
                //        }
                //    }
                //}

            }
        }


    }
}
