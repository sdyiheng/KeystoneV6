using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessageEngine.SuperMCMCore;
using Keystone.AddIn.FormDesigner.Messages;
using MessageEngine;
using Keystone.AddIn.FormDesigner.Elements;
using System.Windows.Forms;
using Keystone.Common.Messages;
using System.Drawing;
using Keystone.WellKnownMessage.Windows;
using Keystone.Common.Utility;
using MessageEngine.Messages;
using System.Diagnostics;

namespace Keystone.AddIn.FormDesigner.Controllers.MouseAction
{
    class DragScrollController : baseFormController, IRenderableAction 
    {
        private readonly ScrollableControl Container;
        public DragScrollController(ScrollableControl container)
        {
            this.Container = container;
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


       
        public void DrawAction(System.Drawing.Graphics g, GraphViewer graphViewer)
        {
            if (this.CurrentActionStatus == ActionStatus.Running)
            {
                g.Clear(SystemColors.ControlDark);
                int offsetX = this.mouseCurrent.X - this.mouseDown.MousePosInControl.X;
                int offsetY = this.mouseCurrent.Y - this.mouseDown.MousePosInControl.Y;

                graphViewer.Paint(g, offsetX, offsetY);

                //using (Graphics g2 = Container.CreateGraphics())
                //{
                //    g2.Clear(SystemColors.ControlDark);
                //    graphViewer.Paint(g2, offsetX, offsetY);
                //}
            }
        }

        public int ActionRenderIndex
        {
            get { return 2; }
        }

        private MouseDownMoveUpMsg mouseDown = null;
        private Point mouseCurrent = Point.Empty;

        private void executeAction(string para)
        {
            Debug.Assert(!string.IsNullOrEmpty(para));

            string[] items = para.Split(',');
            int offsetX = int.Parse(items[0]);
            int offsetY = int.Parse(items[1]);

            Point current = this.Container.AutoScrollPosition;
            current.X = -offsetX - current.X;
            current.Y = -offsetY - current.Y;
            this.Container.AutoScrollPosition = current;
        }
        private void clearAction()
        {
            this.mouseDown = null;
            this.mouseCurrent = Point.Empty;
        }
        public ActionStatus CurrentActionStatus
        {
            get
            {
                if (mouseDown == null)
                    return ActionStatus.Stoped;
                else if (mouseDown.MousePosInControl == this.mouseCurrent)
                    return ActionStatus.Started;
                else
                    return ActionStatus.Running;
            }
        }

        [MessageSubscriber(MessageEngine.SuperMCMCore.MMMODE.ASIS)]
        private void on(MouseDownMoveUpMsg msg)
        {
            if (this.form == null)
                return;

            if (msg.MouseButton != MouseButtons.Right)
            {
                this.clearAction();
                return;
            }

            switch (this.CurrentActionStatus)
            {
                case ActionStatus.Stoped:
                    {
                        if (msg.Type == MouseDownMoveUpMsg.MsgType.MouseDown)
                        {
                            this.mouseDown = msg;
                            this.mouseCurrent = msg.MousePosInControl;
                            return;
                        }
                    }
                    break;

                case ActionStatus.Started:
                    {
                        if (msg.Type == MouseDownMoveUpMsg.MsgType.MouseMove)
                        {
                            if (GeoHelper.IsMouseMoved(this.mouseCurrent, msg.MousePosInControl))
                            {
                                this.mouseCurrent = msg.MousePosInControl;
                                SuperMCMService.PostMessage(new ActionPaintRequestMsg(), ViewDesignerMainController.MESSAGECHANNEL);
                                return;
                            }
                        }
                    }
                    break;

                case ActionStatus.Running:
                    {
                        if (msg.Type == MouseDownMoveUpMsg.MsgType.MouseMove)
                        {
                            if (GeoHelper.IsMouseMoved(this.mouseCurrent, msg.MousePosInControl))
                            {
                                this.mouseCurrent = msg.MousePosInControl;
                                SuperMCMService.PostMessage(new ActionPaintRequestMsg(), ViewDesignerMainController.MESSAGECHANNEL);
                                return;
                            }
                        }
                        else if (msg.Type == MouseDownMoveUpMsg.MsgType.MouseUp)
                        {
                            int offsetX = this.mouseCurrent.X - this.mouseDown.MousePosInControl.X;
                            int offsetY = this.mouseCurrent.Y - this.mouseDown.MousePosInControl.Y;
                            SuperMCMService.PostMessage(
                                new UISyncInvokeMessage<string>(this.executeAction, string.Format("{0},{1}", offsetX, offsetY)), ViewDesignerMainController.MESSAGECHANNEL);
                            this.clearAction();
                            SuperMCMService.PostMessage(new ActionPaintRequestMsg(), ViewDesignerMainController.MESSAGECHANNEL);
                        }
                    }
                    break;
            }
        }

     
    }
}
