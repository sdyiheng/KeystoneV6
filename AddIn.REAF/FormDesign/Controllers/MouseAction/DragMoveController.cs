using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessageEngine.SuperMCMCore;
using Keystone.AddIn.FormDesigner.Messages;
using MessageEngine;
using Keystone.AddIn.FormDesigner.Elements;
using System.Drawing;
using Keystone.WellKnownMessage.Windows;
using Keystone.Common.Messages;
using Keystone.Common.Utility;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace Keystone.AddIn.FormDesigner.Controllers.MouseAction
{
    class DragMoveController : baseFixGridFormController, IRenderableAction 
    {
        private static Pen ActionPen = new Pen(Color.Red, 1f);
        //private static Image icon = null;

        static DragMoveController()
        {
            ActionPen.DashStyle = DashStyle.Custom;
            ActionPen.DashPattern = new float[] { 2, 2 };
            //using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Keystone.AddIn.FormDesigner.Resources.sizeall.png"))
            //    icon = Bitmap.FromStream(stream);
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
#if DEBUG
                Debug.WriteLine("DragAction:" + this.GetType().Name);
#endif

                int offsetX = this.mouseCurrent.X - this.mouseDown.MousePosInControl.X;
                int offsetY = this.mouseCurrent.Y - this.mouseDown.MousePosInControl.Y;
                offsetX = fixValue(offsetX);
                offsetY = fixValue(offsetY);

                if (offsetX == 0 && offsetY == 0)
                    return;

                IViewElement parent = null;
                foreach (IViewElement e in this.selections)
                {
                    if (e.IsRoot)
                        continue;

                    parent = e.ParentElement;
                    if( parent == null )
                        continue;

                    Rectangle rect = parent.GetAbsRectangle();
                    e.DrawElement(g, e, rect.X + offsetX, rect.Y + offsetY);

                    //绘制选中矩形
                    Rectangle rectForSelection = e.GetAbsRectangle();
                    rectForSelection.Width = rectForSelection.Width - 1;
                    rectForSelection.Height = rectForSelection.Height - 1;
                    rectForSelection.Offset(offsetX, offsetY);
                    g.DrawRectangle(ActionPen, rectForSelection);

                    rectForSelection.Width = rectForSelection.Width + 1;
                    rectForSelection.Height = rectForSelection.Height + 1;

                    AssistLineMode mode = AssistLineMode.None;
                    mode |= AssistLineMode.Left;
                    mode |= AssistLineMode.Right;
                    mode |= AssistLineMode.Top;
                    mode |= AssistLineMode.Bottom;
                    this.drawAssistLine(g, rectForSelection, mode);
                     rect = GetDragRegion(e);
                    if (rect == Rectangle.Empty)
                        continue;

                    rect.Offset(offsetX, offsetY);

                    //g.DrawImage(icon, rect, new Rectangle(0, 0, 19, 19), GraphicsUnit.Pixel);
                    //using (Image imgTemp = icon.GetThumbnailImage(icon.Width, icon.Height, null, IntPtr.Zero))
                    //{
                    //    g.DrawImage(imgTemp, rect, new Rectangle(0, 0, 19, 19), GraphicsUnit.Pixel);
                    //}

                    g.FillRectangle(Brushes.White, rect);
                    System.Windows.Forms.Cursors.SizeAll.Draw(g, rect);
                    //ControlPaint.DrawGrabHandle(g, rect, true, true);
                }
            }
            else if (this.selections != null && this.selections.Length > 0)
            {
                foreach (IViewElement e in this.selections)
                {
                    if (e.IsRoot)
                        continue;

                    Rectangle rect = GetDragRegion(e);
                    if (rect == Rectangle.Empty)
                        continue;

                    //g.DrawImage(icon, rect, new Rectangle(0, 0, 19, 19), GraphicsUnit.Pixel);
                    //using (Image imgTemp = icon.GetThumbnailImage(icon.Width, icon.Height, null, IntPtr.Zero))
                    //{
                    //    g.DrawImage(imgTemp, rect, new Rectangle(0, 0, 19, 19), GraphicsUnit.Pixel);
                    //}
                    g.FillRectangle(Brushes.White, rect);
                    System.Windows.Forms.Cursors.SizeAll.Draw(g, rect);
                }
            }
        }

        Elements.IViewElement[] selections = null;
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

        IViewElement element;

        [MessageSubscriber()]
        private void on(InsertElementTypeMsg msg)
        {
            this.element = msg.Element;
        }

        public int ActionRenderIndex
        {
            get { return 1; }
        }

        public static bool IsOnDragRegion(IViewElement[] elements, Point pntInControl)
        {
            if (elements == null)
                return false;

            foreach (IViewElement e in elements)
            {
                Debug.Assert(e != null);

                if (IsOnDragRegion(e, pntInControl))
                {
                    return true;
                }
            }

            return false;
        }
        public static bool IsOnDragRegion(IViewElement element, Point pntInControl)
        {
            if (element.IsRoot)
                return false;

            Rectangle center = GetDragRegion(element);

            return center.Contains(pntInControl);
        }
        public static Rectangle GetDragRegion(IViewElement element)
        {
            if (element.IsRoot)
                return Rectangle.Empty ;

            Rectangle rect = element.GetAbsRectangle();
            rect.Inflate(-3, -3);

            Rectangle center = new Rectangle(GeoHelper.GetCenterPoint(rect), Size.Empty);
            center.Inflate(10, 10);

            center.Intersect(rect);
            if (center.Width > 19)
                center.Width = 19;
            if (center.Height > 19)
                center.Height = 19;

            return center;
        }

        private MouseDownMoveUpMsg mouseDown = null;
        private Point mouseCurrent = Point.Empty;

        private void executeAction()
        {
            if (this.CurrentActionStatus == ActionStatus.Running)
            {
                int offsetX = this.mouseCurrent.X - this.mouseDown.MousePosInControl.X;
                int offsetY = this.mouseCurrent.Y - this.mouseDown.MousePosInControl.Y;
                offsetX = fixValue(offsetX);
                offsetY = fixValue(offsetY);

                if (offsetX == 0 && offsetY == 0)
                    return; 
                
                if( this.selections == null || this.selections.Length == 0 )
                    return;

                IViewElementContainer container = this.selections[0].ParentElement;
                Debug.Assert( container != null );

                Dictionary<Guid, Rectangle> refLayout = container.BuildChildrenLayoutInfo( null );
                List<IViewElement> elementList = new List<IViewElement>();
                foreach (IViewElement e in this.selections)
                {
                    if (e.IsRoot)
                        continue;

                    elementList.Add( e );
                    e.SuspendLayout();
                    e.X = e.X + offsetX;
                    e.Y = e.Y + offsetY;
                    e.ResumeLayout();
                }

                if( !container.LayoutSuspended )
                    SuperMCMService.PostMessage(new LayoutMsg(container, elementList.ToArray(), refLayout), ViewDesignerMainController.MESSAGECHANNEL);

            }
        }
        private void clearAction()
        {
            this.mouseDown = null;
            this.mouseCurrent = Point.Empty;
            this.clearAssistLine();
        }

        private ResizeMode resizeMode;

        [MessageSubscriber(MessageEngine.SuperMCMCore.MMMODE.ASIS)]
        private void on(SetCursorByResizeModeMsg msg)
        {
            if (this.element != null)
            {
                resizeMode =  ResizeMode.None;
                return;
            }

            resizeMode = msg.Mode;
        }

        public ActionStatus CurrentActionStatus
        {
            get
            {
                if ( mouseDown == null || this.selections == null || this.selections.Length == 0)
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

            if (this.element != null)
                return;

            if (this.selections == null)
                return;

            if (msg.MouseButton != MouseButtons.Left)
            {
                this.clearAction();
                return;
            }

            if (resizeMode != ResizeMode.None)
                return;

            switch (this.CurrentActionStatus)
            {
                case ActionStatus.Stoped:
                    {
                        if (msg.MouseButton == MouseButtons.Left && msg.Type == MouseDownMoveUpMsg.MsgType.MouseDown)
                        {
                            bool onAction = false;
                            foreach (IViewElement e in this.selections)
                            {
                                if (IsOnDragRegion(e, msg.MousePosInControl))
                                {
                                    onAction = true;
                                    break;
                                }
                            }

                            if (onAction)
                            {
                                this.mouseDown = msg;
                                this.mouseCurrent = msg.MousePosInControl;
                                //SuperMCMService.PostMessage(new SetCursorMsg(Cursors.SizeAll), ViewDesignerMainController.MESSAGECHANNEL);
                                return;
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
                        if (msg.MouseButton == MouseButtons.Left && msg.Type == MouseDownMoveUpMsg.MsgType.MouseMove && msg.Capture)
                        {
                            if (GeoHelper.IsMouseMoved(this.mouseCurrent, msg.MousePosInControl))
                            {
                                this.mouseCurrent = msg.MousePosInControl;
                                this.buildAssistLine(this.selections != null && this.selections.Length > 0 ? this.selections[0] : null, this.selections );
                                SuperMCMService.PostMessage(new ActionPaintRequestMsg(), ViewDesignerMainController.MESSAGECHANNEL);
                                return;
                            }
                        }
                    }
                    break;

                case ActionStatus.Running:
                    {
                        if (msg.MouseButton == MouseButtons.Left && msg.Type == MouseDownMoveUpMsg.MsgType.MouseMove && msg.Capture)
                        {
                            this.mouseCurrent = msg.MousePosInControl;
                            SuperMCMService.PostMessage(new ActionPaintRequestMsg(), ViewDesignerMainController.MESSAGECHANNEL);
                            return;
                        }
                        else if (msg.MouseButton == MouseButtons.Left && msg.Type == MouseDownMoveUpMsg.MsgType.MouseUp && msg.Capture)
                        {
                            this.executeAction();
                            this.clearAction();
                            //SuperMCMService.PostMessage(new SetCursorMsg(Cursors.Default), ViewDesignerMainController.MESSAGECHANNEL);
                            SuperMCMService.PostMessage(new ViewPaintRequestMsg(), ViewDesignerMainController.MESSAGECHANNEL);
                        }
                    }
                    break;
            }
        }
    }
}
