using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessageEngine.SuperMCMCore;
using System.Windows.Forms;
using Keystone.AddIn.FormDesigner.Messages;
using Keystone.Common.Messages;
using System.Drawing;
using Keystone.WellKnownMessage.Windows;
using Keystone.Common.Utility;
using MessageEngine;
using MessageEngine.Messages;
using System.Diagnostics;
using Keystone.AddIn.FormDesigner.Elements;

namespace Keystone.AddIn.FormDesigner.Controllers.MouseAction
{
    class ContextMenuController : baseFormController
    {
        private ContextMenuStrip menuStrip;
        private GraphControl graphControl;
        private Form frm;

        public ContextMenuController(ContextMenuStrip _menuStrip, GraphControl _graphControl, Form _frm)
        {
            this.menuStrip = _menuStrip;
            this.graphControl = _graphControl;
            this.menuStrip.Closed += new ToolStripDropDownClosedEventHandler(menuStrip_Closed);
            this.frm = _frm;
        }

        void menuStrip_Closed(object sender, ToolStripDropDownClosedEventArgs e)
        {
            SuperMCMService.PostMessage(new ContextMenuClosedMsg(), ViewDesignerMainController.MESSAGECHANNEL,100);
        }

        private MouseDownMoveUpMsg mouseDown = null;
        private Point mouseCurrentInScreen = Point.Empty;

        public ActionStatus CurrentActionStatus
        {
            get
            {
                if (mouseDown == null )
                    return ActionStatus.Stoped;
                else if (mouseDown.MousePosInScreen == this.mouseCurrentInScreen)
                    return ActionStatus.Started;
                else
                    return ActionStatus.Running;
            }
        }

        private void clearAction()
        {
            this.mouseDown = null;
            this.mouseCurrentInScreen = Point.Empty;
        }


        [MessageSubscriber(MessageEngine.SuperMCMCore.MMMODE.ASIS)]
        private void on(MouseDownMoveUpMsg msg)
        {
            if (this.form == null)
                return;

            if (msg.MouseButton != System.Windows.Forms.MouseButtons.Right)
                return;

            switch (this.CurrentActionStatus)
            {
                case ActionStatus.Stoped:
                    {
                        if (msg.Type == MouseDownMoveUpMsg.MsgType.MouseDown)
                        {
                            mouseDown = msg;
                            mouseCurrentInScreen = msg.MousePosInScreen;
                        }
                    }
                    break;

                case ActionStatus.Started:
                    {
                        if (msg.Type == MouseDownMoveUpMsg.MsgType.MouseMove)
                        {
                            if (GeoHelper.IsMouseMoved(this.mouseCurrentInScreen, msg.MousePosInScreen))
                            {
                                this.clearAction();
                            }
                        }
                        else if (msg.Type == MouseDownMoveUpMsg.MsgType.MouseUp)
                        {
                            if (!GeoHelper.IsMouseMoved(this.mouseCurrentInScreen, msg.MousePosInScreen ))
                            {
                                SuperMCMService.PostMessage(new UISyncInvokeMessage<string>(this.executeAction, string.Format("{0},{1}", this.mouseCurrentInScreen.X, this.mouseCurrentInScreen.Y)), ViewDesignerMainController.MESSAGECHANNEL);
                            }
                            this.clearAction();
                        }
                    }
                    break;

                case ActionStatus.Running:
                    {
                        if (msg.Type == MouseDownMoveUpMsg.MsgType.MouseUp)
                        {
                            if (!GeoHelper.IsMouseMoved(this.mouseCurrentInScreen, this.mouseDown.MousePosInScreen ))
                            {
                                SuperMCMService.PostMessage(new UISyncInvokeMessage<string>(this.executeAction, string.Format("{0},{1}", this.mouseCurrentInScreen.X, this.mouseCurrentInScreen.Y )), ViewDesignerMainController.MESSAGECHANNEL);
                            }
                            this.clearAction();
                        }
                    }
                    break;
            }
        }

        private void executeAction(string para)
        {
            if (form == null)
                return;
            if (!this.frm.Visible)
                return;

            try
            {
                Debug.Assert(!string.IsNullOrEmpty(para));
                string[] items = para.Split(',');
                Debug.Assert(items.Length == 2);
                Point p = new Point(int.Parse(items[0]), int.Parse(items[1]));
                Point pInControl = graphControl.PointToClient(p);
                IViewElement[] elements = this.form.HitTest(pInControl, 0);
                IViewElement actionElement = null;
                foreach (IViewElement e in elements)
                {
                    if (isElementSelected(e))
                    {
                        actionElement = e;
                        break;
                    }
                }
                if(actionElement == null )
                    actionElement = BaseViewElement.TopMostElement(elements);
                if (actionElement == null)
                    return;
                
                if (!isElementSelected(actionElement))
                {
                    this.selections = new IViewElement[] { actionElement };
                    SuperMCMService.PostMessage(new SelectionChangedMsg(this.selections), ViewDesignerMainController.MESSAGECHANNEL);
                    SuperMCMService.PostMessage(new ActionPaintRequestMsg(), ViewDesignerMainController.MESSAGECHANNEL);
                }

                SuperMCMService.PostMessage(new ContextMenuActionElementsMsg(actionElement, this.selections, elements, pInControl), ViewDesignerMainController.MESSAGECHANNEL);
                this.menuStrip.Show(p);
            }
            catch(Exception e )
            {
                SuperMCMService.PostMessage(new ExceptionMessage(e));
            }
        }

        IViewElement[] selections = null;

        [MessageSubscriber]
        private void on(SelectionChangedMsg msg)
        {
            if (this.form == null)
                return;

            selections = msg.Elements;
        }
        private bool isElementSelected(IViewElement e)
        {
            return this.selections != null && Array.IndexOf(this.selections, e) != -1;
        }

        bool registered = false;
        [MessageSubscriber( MessageEngine.SuperMCMCore.MMMODE.PASIUI )]
        private void on(RegisterContextMenuItemMsg msg)
        {
            //
            msg.MenuItem.Tag = msg;

            if (!registered)
            {
                this.menuStrip.Items.Add(new ToolStripSeparator());
                registered = true;

                //将msg.MenuItem加入到快捷菜单上
                this.menuStrip.Items.Add(msg.MenuItem);

                return;
            }

            for (int i = this.menuStrip.Items.Count - 1; i >= 0; i--)
            {
                ToolStripItem item = this.menuStrip.Items[i];
                if (item is ToolStripSeparator)
                {
                    this.menuStrip.Items.Insert(i + 1, msg.MenuItem);
                    return;
                }
                else
                {
                    RegisterContextMenuItemMsg _msg = item.Tag as RegisterContextMenuItemMsg;
                    Debug.Assert(_msg != null);
                    if (msg.Index >= _msg.Index)
                    {
                        this.menuStrip.Items.Insert(i + 1, msg.MenuItem);
                        return;
                    }
                }
            }

            ////将msg.MenuItem加入到快捷菜单上
            //this.menuStrip.Items.Add(msg.MenuItem);
        }

    }
}
