using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessageEngine.SuperMCMCore;
using System.Windows.Forms;
using Keystone.AddIn.FormDesigner.Messages;
using MessageEngine;
using System.Drawing;
using System.Drawing.Drawing2D;
using Keystone.AddIn.FormDesigner.Elements;
using Keystone.Common.Messages;
using Keystone.WellKnownMessage.Windows;
using Keystone.Common.Utility;

namespace Keystone.AddIn.FormDesigner.Controllers.MouseAction
{
    class SelectionController : baseFixGridFormController, IRenderableAction 
    {
        private static Pen ActionPen = new Pen(Color.Red, 1f);
        private static Brush selectBruesh;

        static SelectionController()
        {
            ActionPen.DashStyle = DashStyle.Custom;
            ActionPen.DashPattern = new float[] { 2, 2 };
            selectBruesh = new System.Drawing.Drawing2D.HatchBrush(System.Drawing.Drawing2D.HatchStyle.Percent20, Color.Red, Color.Transparent);
        }


        Elements.IViewElement[] selections = null;

        private bool renderActionReged = false;
        [MessageSubscriber]
        protected override void on(CurrentViewDefinitionMsg msg)
        {
            base.on(msg);
            if (!renderActionReged)
            {
                SuperMCMService.PostMessage(new RenderableActionSelfRegMsg(this), ViewDesignerMainController.MESSAGECHANNEL, 100);
                this.renderActionReged = true;
            }

            if (msg.ViewDefinition == null)
            {
                selections = null;
                SuperMCMService.PostMessage(new SelectionChangedMsg(null), ViewDesignerMainController.MESSAGECHANNEL);
            }
            else
            {
                //缺省选择
                selections = new Elements.IViewElement[] { msg.ViewDefinition };
                SuperMCMService.PostMessage(new SelectionChangedMsg(selections), ViewDesignerMainController.MESSAGECHANNEL);
            }
        }



        IViewElement element;

        [MessageSubscriber()]
        private void on(InsertElementTypeMsg msg)
        {
            this.element = msg.Element;
        }

        [MessageSubscriber]
        private void on(ContainerMouseClickMsg msg)
        {
            if (this.form == null)
                return;

            if (selections != null && selections.Length == 1 && selections[0] == this.form)
                return;

            selections = new Elements.IViewElement[] { this.form };
            SuperMCMService.PostMessage(new SelectionChangedMsg(selections), ViewDesignerMainController.MESSAGECHANNEL);
            SuperMCMService.PostMessage(new ActionPaintRequestMsg(), ViewDesignerMainController.MESSAGECHANNEL);
        }

        [MessageSubscriber]
        private void on(SelectionChangedMsg msg)
        {
            if (this.form == null)
                return;

            selections = msg.Elements;
        }

        //private bool isActionRunning
        //{
        //    get
        //    {
        //        return false;
        //    }
        //}

      
        public void DrawAction(System.Drawing.Graphics g, GraphViewer graphViewer)
        {
            switch (CurrentActionStatus)
            {
                case ActionStatus.Running:
                    {
                        Rectangle rectOfContainer = this.container.GetAbsRectangle();
                        rectOfContainer.Width -= 1;
                        rectOfContainer.Height -= 1;
                        g.DrawRectangle(ActionPen, rectOfContainer);

                        Rectangle rect = GeoHelper.GetOutterRectangle(this.mouseDown.MousePosInControl, this.mouseCurrent);
                        rect.Intersect(rectOfContainer);
                        //rect.Width -= 1;
                        //rect.Height -= 1;
                        g.FillRectangle(selectBruesh, rect);
                    }
                    break;

                case ActionStatus.Stoped:
                    {
                        if (selections != null)
                        {
                            //if(this.selections.Length > 0 && !this.selections[0].IsRoot )
                            //    this.drawFixedGrid(g);

                            //回执选中的元素
                            foreach (IViewElement e in this.selections)
                            {
                                Rectangle rectForSelection = e.GetAbsRectangle();
                                rectForSelection.Width = rectForSelection.Width - 1;
                                rectForSelection.Height = rectForSelection.Height - 1;
                                g.DrawRectangle(ActionPen, rectForSelection);
                                if( !e.IsRoot )
                                    g.FillRectangle(selectBruesh, rectForSelection);
                            }
                        }
                    }
                    break;
            }
        }

        public int ActionRenderIndex
        {
            get { return 0; }
        }

        ResizeMode resizeMode = ResizeMode.None;
        [MessageSubscriber( MessageEngine.SuperMCMCore.MMMODE.ASIS )]
        private void on(SetCursorByResizeModeMsg msg)
        {
            this.resizeMode = msg.Mode;
        }

        private MouseDownMoveUpMsg mouseDown = null;
        private Point mouseCurrent = Point.Empty;
        private IViewElementContainer container = null;
        private bool isControlKey = false;

        public ActionStatus CurrentActionStatus
        {
            get
            {
                if (mouseDown == null || container == null)
                    return ActionStatus.Stoped;
                else if (mouseDown.MousePosInControl == this.mouseCurrent)
                    return ActionStatus.Started;
                else
                    return ActionStatus.Running;
            }
        }

        private void clearAction()
        {
            this.mouseDown = null;
            this.container = null;
            this.mouseCurrent = Point.Empty;
            this.isControlKey = false;
            this.clearAssistLine();
        }

        private bool isElementSelected(IViewElement e)
        {
            return this.selections != null && Array.IndexOf(this.selections, e) != -1;
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
                        if (msg.Type == MouseDownMoveUpMsg.MsgType.MouseDown && this.resizeMode == ResizeMode.None)
                        {
                            if (DragMoveController.IsOnDragRegion(this.selections, msg.MousePosInControl))
                                return;

                            IViewElement[] elements = this.form.HitTest(msg.MousePosInControl,0);
                            IViewElementContainer topMostContainer = BaseViewElement.TopMostElementContainer(elements);
                            IViewElement topElement = BaseViewElement.TopMostElement(elements);
                            //if (msg.Control //强制选择
                            //    || !isElementSelected(topElement) //节点未选中
                            //    || !DragMoveController.IsOnDragRegion(topElement, msg.MousePosInControl )//未点击拖动区
                            //    )
                            {
                                //当前元素未被选中或者按下了Control
                                mouseDown = msg;
                                container = topMostContainer;
                                mouseCurrent = msg.MousePosInControl;
                                isControlKey = msg.Control;
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
                                mouseCurrent = msg.MousePosInControl;
                                SuperMCMService.PostMessage(new ActionPaintRequestMsg(), ViewDesignerMainController.MESSAGECHANNEL);
                            }
                        }
                        else if (msg.Type == MouseDownMoveUpMsg.MsgType.MouseUp)
                        {
                            //
                            this.executeAction();
                            this.clearAction();
                            SuperMCMService.PostMessage(new ActionPaintRequestMsg(), ViewDesignerMainController.MESSAGECHANNEL);
                        }
                    }
                    break;

                case ActionStatus.Running:
                    {
                        if (msg.Type == MouseDownMoveUpMsg.MsgType.MouseMove)
                        {
                            mouseCurrent = msg.MousePosInControl;
                            SuperMCMService.PostMessage(new ActionPaintRequestMsg(), ViewDesignerMainController.MESSAGECHANNEL);
                        }
                        else if (msg.Type == MouseDownMoveUpMsg.MsgType.MouseUp)
                        {
                            //
                            this.executeAction();
                            this.clearAction();
                            SuperMCMService.PostMessage(new ActionPaintRequestMsg(), ViewDesignerMainController.MESSAGECHANNEL);
                        }
                    }
                    break;
            }
        }

        private void executeAction()
        {
            if (this.mouseDown == null)
                return;

            if (GeoHelper.IsMouseMoved(this.mouseCurrent, this.mouseDown.MousePosInControl))
            {
                //鼠标移动了
                //执行点击选择
                if (this.container.HasChildren)
                {
                    //
                    Rectangle rect = GeoHelper.GetOutterRectangle(this.mouseCurrent, this.mouseDown.MousePosInControl);
                    List<IViewElement> list = new List<IViewElement>();
                    foreach (IViewElement e in this.container.Children)
                    {
                        if (e.IntersectsWith(rect))
                            list.Add(e);
                    }
                    if (list.Count == 0)
                    {
                        SuperMCMService.PostMessage(new SelectionChangedMsg(new IViewElement[] { this.container }), ViewDesignerMainController.MESSAGECHANNEL);
                    }
                    else
                    {
                        SuperMCMService.PostMessage(new SelectionChangedMsg(list.ToArray()), ViewDesignerMainController.MESSAGECHANNEL);
                    }
                }
                else
                {
                    //不包含子内容
                    SuperMCMService.PostMessage(new SelectionChangedMsg(new IViewElement[] { this.container }), ViewDesignerMainController.MESSAGECHANNEL);
                }
            }
            else
            {
                //鼠标未移动
                //执行点击选择
                IViewElement[] elements = this.form.HitTest(this.mouseDown.MousePosInControl,0);
                if (elements == null || elements.Length == 0)
                {
                    //未选中内容
                    SuperMCMService.PostMessage(new SelectionChangedMsg(new IViewElement[] { this.container }), ViewDesignerMainController.MESSAGECHANNEL);
                }
                else
                {
                    //Array.Sort(elements, BaseViewElementContainer.CompareByParentAndArea);
                    IViewElement actionElement = null;

                    //foreach (IViewElement e in elements)
                    //{
                    //    if (this.isElementSelected(e))
                    //    {
                    //        actionElement = e;
                    //        break;
                    //    }
                    //}
                    if (actionElement == null)
                    {
                        foreach (IViewElement e in elements)
                        {
                            //首先过滤根
                            if (e is ViewDefinition)
                                continue;

                            if (actionElement == null)
                                actionElement = e;
                            else if (actionElement.Area > e.Area)
                                actionElement = e;
                        }

                        if (actionElement == null)
                        {
                            foreach (IViewElement e in elements)
                            {
                                if (actionElement == null)
                                    actionElement = e;
                                else if (actionElement.Area > e.Area)
                                    actionElement = e;
                            }

                        }
                    }
                    if (actionElement == null)
                    {
                        SuperMCMService.PostMessage(new SelectionChangedMsg(new IViewElement[] { this.form }), ViewDesignerMainController.MESSAGECHANNEL);
                    }
                    else
                    {
                        SuperMCMService.PostMessage(new SelectionChangedMsg(new IViewElement[] { actionElement }), ViewDesignerMainController.MESSAGECHANNEL);
                    }
                }

            }
        }

        [MessageSubscriber ]
        private void on(RequestSelectElementMsg msg)
        {
            if (this.form == null)
                return;

            IViewElement e = this.form.GetElement(msg.ElementID);
            if (e == null)
                return;

            SuperMCMService.PostMessage(new SelectionChangedMsg(new IViewElement[] { e }), ViewDesignerMainController.MESSAGECHANNEL);
            SuperMCMService.PostMessage(new ActionPaintRequestMsg(), ViewDesignerMainController.MESSAGECHANNEL);
        }

    }
}
