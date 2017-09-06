using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessageEngine.SuperMCMCore;
using Keystone.AddIn.FormDesigner.Messages;
using MessageEngine;
using Keystone.AddIn.FormDesigner.Elements;
using System.Diagnostics;
using Keystone.Common.Messages;
using System.Drawing;
using Keystone.WellKnownMessage.Windows;
using Keystone.Common.Utility;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Keystone.Graph.Core.Messages;

namespace Keystone.AddIn.FormDesigner.Controllers.MouseAction
{
    class InsertElementController : baseFixGridFormController, IRenderableAction 
    {
        private static Pen InsertBorderPen = new Pen(Color.Blue, 1f);

        static InsertElementController()
        {
            InsertBorderPen.DashStyle = DashStyle.Custom;
            InsertBorderPen.DashPattern = new float[] { 2, 2 };
        }

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
        }

        private GraphControl designer = null;
        [MessageSubscriber( MessageEngine.SuperMCMCore.MMMODE.PASIUI )]
        private void on(FormLoadedMsg msg)
        {
            this.designer = msg.FormDesignerControl;
        }


        Elements.IViewElement[] selections = null;
        [MessageSubscriber]
        private void on(SelectionChangedMsg msg)
        {
            if (this.form == null)
                return;

            selections = msg.Elements;
        }

       
        public void DrawAction(System.Drawing.Graphics g, GraphViewer graphViewer)
        {
            if (this.element == null)
                return;

            if (this.CurrentActionStatus != ActionStatus.Running)
                return;

            Rectangle rect = GeoHelper.GetOutterRectangle(this.fixPointToRect(this.mouseDown.MousePosInControl), this.fixPointToRect(this.mouseCurrent));


            this.element.X = rect.X;
            this.element.Y = rect.Y;

            this.element.Width = rect.Width;
            this.element.Height = rect.Height;

            this.element.DrawElement(g, this.element, 0, 0);

            if (Math.Abs(this.mouseCurrent.X - rect.Left) < Math.Abs(this.mouseCurrent.X - rect.Right))
            {
                //左
                this.drawAssistLine_x(g, rect.Left);
            }
            else
            {
                //右
                this.drawAssistLine_x(g, rect.Right);
            }

            if (Math.Abs(this.mouseCurrent.Y - rect.Top ) < Math.Abs(this.mouseCurrent.Y - rect.Bottom))
            {
                //左
                this.drawAssistLine_y(g, rect.Top);
            }
            else
            {
                //右
                this.drawAssistLine_y(g, rect.Bottom);
            }
        }


        public int ActionRenderIndex
        {
            get { return 0; }
        }

        IViewElement element;
        bool multiTimes = false;

        [MessageSubscriber( MessageEngine.SuperMCMCore.MMMODE.PASIUI)]
        private void on(InsertElementTypeMsg msg)
        {
            this.element = msg.Element;
            this.multiTimes = msg.MultiTimes;

            //this.designer.Cursor = this.element == null ? Cursors.Default : Cursors.Cross;

#if DEBUG
            Debug.WriteLine(msg.Element == null ? "" : msg.Element.ToString());
            Debug.WriteLine(msg.MultiTimes ? "Multi" : "Single");
#endif
        }



        private MouseDownMoveUpMsg mouseDown = null;
        private Point mouseCurrent = Point.Empty;
        private IViewElementContainer container = null;

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

        [MessageSubscriber( MessageEngine.SuperMCMCore.MMMODE.ASIS )]
        private void on(MouseDownMoveUpMsg msg)
        {
            if (this.form == null)
                return;

            if (this.element == null)
                return;

            if (msg.MouseButton != System.Windows.Forms.MouseButtons.Left)
                return;

            switch (this.CurrentActionStatus)
            {
                case ActionStatus.Stoped:
                    {
                        if (msg.Type == MouseDownMoveUpMsg.MsgType.MouseDown)
                        {
                            //if (this.selections != null && this.selections.Length == 1 && this.selections[0]
                            //    is IViewElementContainer &&( this.selections[0] as IViewElementContainer ).CanBeAChild(this.element ))
                            //{
                            //    mouseDown = msg;
                            //    container = this.selections[0] as IViewElementContainer;
                            //    mouseCurrent = msg.MousePosInControl;
                            //    return;
                            //}
                            //else
                            //{
                                
                               
                            //}
                            IViewElement[] elements = this.form.HitTest(msg.MousePosInControl, 0);
                            IViewElementContainer[] containers = BaseViewElement.ElementContainers(elements);
                            if (containers.Length > 0)
                            {
                                //从当前选择的元素中选取
                                if (this.selections != null && this.selections.Length > 0)
                                {
                                    foreach (IViewElement e in this.selections)
                                    {
                                        if (e.IsRoot)
                                            continue;

                                        if (e is IViewElementContainer && Array.IndexOf(containers, e) != 0
                                            && (e as IViewElementContainer).CanBeAChild(this.element))
                                        {
                                            mouseDown = msg;
                                            container = e as IViewElementContainer;
                                            mouseCurrent = msg.MousePosInControl;
                                            return;
                                        }
                                    }
                                }

                                foreach (IViewElementContainer c in containers)
                                {
                                    if (c.CanBeAChild(this.element))
                                    {
                                        mouseDown = msg;
                                        container = c;
                                        mouseCurrent = msg.MousePosInControl;
                                        return;
                                    }
                                }
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
                                this.buildAssistLine(this.form, null);
                                SuperMCMService.PostMessage(new ActionPaintRequestMsg(), ViewDesignerMainController.MESSAGECHANNEL);
                            }
                        }
                        else if (msg.Type == MouseDownMoveUpMsg.MsgType.MouseUp)
                        {
                            //
                            this.executeAction();
                            this.clearAction();
                            SuperMCMService.PostMessage(new ViewPaintRequestMsg(), ViewDesignerMainController.MESSAGECHANNEL);
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
                            SuperMCMService.PostMessage(new ViewPaintRequestMsg(), ViewDesignerMainController.MESSAGECHANNEL);
                        }
                    }
                    break;
            }
        }

        private void clearAction()
        {
            this.mouseDown = null;
            this.container = null;
            mouseCurrent = Point.Empty;
            this.clearAssistLine();
        }

        private void executeAction()
        {
            bool added = false;
            if (GeoHelper.IsMouseMoved(this.mouseCurrent, this.mouseDown.MousePosInControl))
            {
                //拖动添加
                Rectangle rect = GeoHelper.GetOutterRectangle(this.fixPointToRect(this.mouseDown.MousePosInControl), this.fixPointToRect(this.mouseCurrent));
                rect = this.fixValue(rect);

                //if (rect.Width < 24)
                //    rect.Width = 24;
                //if (rect.Height < 24)
                //    rect.Height = 24;

                Size minSize = this.element.GetMiniSize();
                if (rect.Width < minSize.Width)
                    rect.Inflate((minSize.Width - rect.Width) / 2, 0);
                if (rect.Height < minSize.Height)
                    rect.Inflate(0, (minSize.Height - rect.Height) / 2);

                rect = this.fixValue(rect);

                if (this.multiTimes)
                {
                    //IViewElement viewElement = Keystone.Common.Utility.ObjectReaderWriter.DeepCopyObject<IViewElement>(this.element);
                    IViewElement viewElement = this.element.GetType().GetConstructor(System.Type.EmptyTypes).Invoke(null) as IViewElement;
                    added = this.addElement(viewElement, rect);
                }
                else
                {
                    added = this.addElement(this.element, rect);
                }
            }
            else
            {
                //点击添加
                Rectangle rect = new Rectangle(this.fixValue(this.mouseDown.MousePosInControl), Size.Empty);
                Size minSize = this.element.GetMiniSize();
                rect.Inflate(minSize.Width/2, minSize.Height/2);
                //rect.Inflate(this.element.Width > 0 ? this.element.Width / 2 : 32, this.element.Height > 0 ? this.element.Height / 2 : 12);
                rect = this.fixValue(rect);

                if (this.multiTimes)
                {
                    //IViewElement viewElement = Keystone.Common.Utility.ObjectReaderWriter.DeepCopyObject<IViewElement>(this.element);
                    IViewElement viewElement = this.element.GetType().GetConstructor(System.Type.EmptyTypes).Invoke(null) as IViewElement;
                    added = this.addElement(viewElement, rect);
                }
                else
                {
                    added = this.addElement(this.element, rect);
                }
            }

            if (!this.multiTimes && added)
            {
                SuperMCMService.PostMessage(new InsertElementTypeMsg(null, false), ViewDesignerMainController.MESSAGECHANNEL);
            }
        }

        private bool addElement(IViewElement viewElement, Rectangle rect)
        {
            if (!this.container.CanBeAChild(viewElement))
            {
                return false;
            }

            if (this.container.AddNewElement(viewElement, rect))
            {
                SuperMCMService.PostMessage(new SelectionChangedMsg(new IViewElement[] { viewElement }), ViewDesignerMainController.MESSAGECHANNEL);
                SuperMCMService.PostMessage(new RefreshOutlineMsg(), ViewDesignerMainController.MESSAGECHANNEL);
                return true;
            }
            else
            {
                return false;
            }

        }
    }
}
