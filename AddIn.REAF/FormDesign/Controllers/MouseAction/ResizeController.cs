using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessageEngine.SuperMCMCore;
using System.Windows.Forms;
using Keystone.AddIn.FormDesigner.Messages;
using System.Drawing;
using Keystone.AddIn.FormDesigner.Elements;
using Keystone.Common.Messages;
using Keystone.WellKnownMessage.Windows;
using Keystone.Common.Utility;
using MessageEngine;
using System.Drawing.Drawing2D;
using MessageEngine.Messages;
using System.Diagnostics;

namespace Keystone.AddIn.FormDesigner.Controllers.MouseAction
{
    class ResizeController : baseFixGridFormController, IRenderableAction 
    {
        private static Pen ActionPen = new Pen(Color.Red, 2f);
        private static Font SizeTipPen = new Font("Arial", 16, FontStyle.Bold );
        private static SolidBrush tipBgBrushes = new SolidBrush(Color.White );//Color.FromArgb(64, Color.LightGray));

        static ResizeController()
        {
            //ActionPen.DashStyle = DashStyle.Custom;
            //ActionPen.DashPattern = new float[] { 1, 3 };
        }

        private GraphControl  viewControl;

        public ResizeController(GraphControl _viewControl)
        {
            this.viewControl = _viewControl;
        }

        private bool renderActionReged = false;
        [MessageSubscriber(MessageEngine.SuperMCMCore.MMMODE.PASIUI)]
        protected override void on(CurrentViewDefinitionMsg msg)
        {
            base.on(msg);
            if (!renderActionReged)
            {
                SuperMCMService.PostMessage(new RenderableActionSelfRegMsg(this), ViewDesignerMainController.MESSAGECHANNEL, 100);
                this.renderActionReged = true;
            }

            if (this.form == null)
                return;

            Size elementSize = this.form.GetNodeSize();
            if (elementSize.Width > 0 && elementSize.Height > 0)
            {
                this.viewControl.Size = elementSize;
            }
            else
            {
                if (elementSize == Size.Empty)
                    elementSize = new Size(600, 400);
                else 
                    elementSize = new Size(Math.Max(16, elementSize.Width), Math.Max(16, elementSize.Height));
                this.form.Width = elementSize.Width;
                this.form.Height = elementSize.Height;

                this.viewControl.Size = elementSize;
            }

        }


        IViewElement element;

        [MessageSubscriber()]
        private void on(InsertElementTypeMsg msg)
        {
            this.element = msg.Element;
        }

        private MouseDownMoveUpMsg mouseDown = null;
        private Point mouseCurrent = Point.Empty;
        private ResizeMode resizeMode = ResizeMode.None;
        private IViewElement actionElement = null;
        private Guid actionElementID = Guid.Empty;

        public ActionStatus CurrentActionStatus
        {
            get
            {
                if (mouseDown == null ||  actionElement == null || this.resizeMode == ResizeMode.None || this.actionElementID == Guid.Empty)
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
            this.mouseCurrent = Point.Empty;
            this.resizeMode = ResizeMode.None;
            this.actionElement = null;
            this.actionElementID = Guid.Empty;
            this.clearAssistLine();
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

        [MessageSubscriber(MessageEngine.SuperMCMCore.MMMODE.ASIS)]
        private void on(MouseDownMoveUpMsg msg)
        {
            if (this.form == null)
                return;

            if (this.element != null)
                return;

            if (!msg.Capture && msg.MouseButton == MouseButtons.None && msg.Type == MouseDownMoveUpMsg.MsgType.MouseMove )
            {
                //动作侦测
                //在选中的元素内侦测
                
                if (this.selections != null)
                {
                    foreach (IViewElement e in this.selections)
                    {
                        if ( e.CanBeResized && IsOnResizeBorder(e, msg.MousePosInControl))
                        {
                            this.actionElementID = e.ElementID;
                            this.resizeMode = GetResizeMode(msg.MousePosInControl, e.GetAbsRectangle());
                            SuperMCMService.PostMessage(new SetCursorByResizeModeMsg(this.resizeMode), ViewDesignerMainController.MESSAGECHANNEL);
                            return;
                        }
                    }
                }
                this.actionElementID = Guid.Empty;
                this.resizeMode = ResizeMode.None;
                SuperMCMService.PostMessage(new SetCursorByResizeModeMsg(ResizeMode.None), ViewDesignerMainController.MESSAGECHANNEL);

                //IViewElement[] hittest = this.form.HitTest(msg.MousePosInControl,2);
                //IViewElement topmost = BaseViewElement.TopMostElement(hittest);
                //if (topmost == null || !isElementSelected(topmost) || !IsOnResizeBorder(topmost, msg.MousePosInControl ))
                //{
                //    this.actionElementID = Guid.Empty;
                //    SuperMCMService.PostMessage(new SetCursorByResizeModeMsg(ResizeMode.None), ViewDesignerMainController.MESSAGECHANNEL);
                //}
                //else
                //{
                //    this.actionElementID = topmost.ElementID;
                //    this.resizeMode = GetResizeMode(msg.MousePosInControl, topmost.GetAbsRectangle());
                //    SuperMCMService.PostMessage(new SetCursorByResizeModeMsg(this.resizeMode), ViewDesignerMainController.MESSAGECHANNEL);
                //}
                return;
            }

            if (msg.MouseButton != MouseButtons.Left)
                return;

            switch (this.CurrentActionStatus)
            {
                case ActionStatus.Stoped:
                    {
                        if (msg.Type == MouseDownMoveUpMsg.MsgType.MouseDown
                            && this.actionElementID != Guid.Empty && !msg.Control && this.resizeMode != ResizeMode.None)
                        {

                            //
                            this.actionElement = this.form.GetElement( this.actionElementID );
                            if (this.actionElement != null)
                            {
                                this.mouseDown = msg;
                                this.mouseCurrent = msg.MousePosInControl;

                                //SuperMCMService.PostMessage(new SetCursorByResizeModeMsg(this.resizeMode), ViewDesignerMainController.MESSAGECHANNEL);
                            }
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
                                this.buildAssistLine(this.actionElement, this.selections );
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
                            this.mouseCurrent = msg.MousePosInControl;
                            SuperMCMService.PostMessage(new ActionPaintRequestMsg(), ViewDesignerMainController.MESSAGECHANNEL);
                            return;
                        }
                        else if (msg.Type == MouseDownMoveUpMsg.MsgType.MouseUp)
                        {
                            this.executeAction();
                            this.clearAction();
                            //SuperMCMService.PostMessage(new SetCursorByResizeModeMsg(ResizeMode.None), ViewDesignerMainController.MESSAGECHANNEL);
                            SuperMCMService.PostMessage(new ViewPaintRequestMsg(), ViewDesignerMainController.MESSAGECHANNEL);
                        }
                    }
                    break;
            }
        }


        private void executeAction()
        {
            Rectangle actionResultrect = this.calculateResult();
            if (actionResultrect == this.actionElement.GetAbsRectangle())
                return;

            if (this.actionElement == this.form)
            {
                Rectangle rect = this.calculateResult();
                this.actionElement.Width = this.fixValue(Math.Max(32, rect.Width));
                this.actionElement.Height = this.fixValue(Math.Max(32, rect.Height));
                //if (this.actionElement is IViewElementContainer)
                //    (this.actionElement as IViewElementContainer).LayoutDown(null);
                //this.actionElement.LayoutUp(null);
                SuperMCMService.PostMessage(new UISyncInvokeMessage<string>(this.resizeFormDesigner, string.Empty), ViewDesignerMainController.MESSAGECHANNEL);
            }
            else
            {
                int dx = this.mouseCurrent.X - this.mouseDown.MousePosInControl.X;
                int dy = this.mouseCurrent.Y - this.mouseDown.MousePosInControl.Y;

                bool resizeFormDesigner = false;
                foreach (IViewElement e in this.selections)
                {
                    if (!e.CanBeResized)
                        continue;

                    Rectangle rect = CalculateResult(e, dx, dy, this.resizeMode, 24,24);
                    rect = this.fixValue(rect);
                    IViewElementContainer container = e as IViewElementContainer;
                    if (container == null)
                        container = e.ParentElement;
                    Dictionary<Guid, Rectangle> dic = container.BuildChildrenLayoutInfo(null);

                    Rectangle parentAbsRect = e.ParentElement.GetAbsRectangle();

                    e.SuspendLayout();
                    Debug.Assert(e.ParentElement != null);
                    if (e.X != rect.X - parentAbsRect.X)
                        e.X = rect.X - parentAbsRect.X;
                    if (e.Y != rect.Y - parentAbsRect.Y)
                        e.Y = rect.Y - parentAbsRect.Y;

                    if (e.Width != rect.Width && e.Height != rect.Height)
                        e.SetSize(rect.Width, rect.Height);
                    else if (e.Width != rect.Width)
                        e.Width = rect.Width;
                    else if (e.Height != rect.Height)
                        e.Height = rect.Height;

                    e.ResumeLayout();

                    if (!container.LayoutSuspended)
                        SuperMCMService.PostMessage(new LayoutMsg(container, e, dic), ViewDesignerMainController.MESSAGECHANNEL);

                    resizeFormDesigner = true;
                }

                if (resizeFormDesigner)
                    SuperMCMService.PostMessage(new UISyncInvokeMessage<string>(this.resizeFormDesigner, string.Empty), ViewDesignerMainController.MESSAGECHANNEL);

            }


        }

        [MessageSubscriber(MessageEngine.SuperMCMCore.MMMODE.ASIS)]
        private void on(ViewElementPropertyChangedMsg msg)
        {
            if (msg.Element == this.form && (msg.PropertyName == "Width" || msg.PropertyName == "Height"))
            {
                SuperMCMService.PostMessage(new UISyncInvokeMessage<string>(this.resizeFormDesigner, string.Empty), ViewDesignerMainController.MESSAGECHANNEL);
            }
        }

        [MessageSubscriber(MessageEngine.SuperMCMCore.MMMODE.PASIUI)]
        private void on(resizeFormDesignerMsg msg)
        {
            this.resizeFormDesigner(string.Empty);
        }
        private void resizeFormDesigner(string para)
        {
            viewControl.Parent.SuspendLayout();
            if (viewControl.Width != this.form.Width + 2)
                viewControl.Width = this.form.Width + 2;
            if (viewControl.Height != this.form.Height + 2)
                viewControl.Height = this.form.Height + 2;
            viewControl.Parent.ResumeLayout();
        }


       
        public void DrawAction(Graphics g, GraphViewer graphViewer)
        {
            if (this.CurrentActionStatus == ActionStatus.Running)
            {
                int dx = this.mouseCurrent.X - this.mouseDown.MousePosInControl.X;
                int dy = this.mouseCurrent.Y - this.mouseDown.MousePosInControl.Y;

                foreach (IViewElement e in this.selections)
                {
                    Rectangle originalRect = e.GetAbsRectangle();
                    Rectangle rect = CalculateResult(e, dx, dy, this.resizeMode, 24, 24);
                    if (!e.IsRoot)
                    {
                        rect = this.fixValue(rect);
                    }
                    g.DrawRectangle(ActionPen, rect);

                    Rectangle tipRect = new Rectangle(rect.Location, new Size(72,24));
                    tipRect.Intersect(rect);
                    //tipRect.Inflate(-4, -4);
                    g.FillRectangle(tipBgBrushes, tipRect);
                    g.DrawString(string.Format("{0}*{1}", rect.Width / 12, rect.Height / 12), SizeTipPen, Brushes.Red, rect.Location);

                    if (originalRect.Top != rect.Top)
                        this.drawAssistLine_y(g, rect.Top);
                    if (originalRect.Bottom != rect.Bottom)
                        this.drawAssistLine_y(g, rect.Bottom);
                    if (originalRect.Left != rect.Left)
                        this.drawAssistLine_x(g, rect.Left);
                    if (originalRect.Right != rect.Right)
                        this.drawAssistLine_x(g, rect.Right);
                }
            }
            //else if (this.selections != null && this.selections.Length > 0)
            //{
            //    foreach (IViewElement e in this.selections)
            //    {
            //        Rectangle rect = e.GetAbsRectangle();
            //        Rectangle tipRect = new Rectangle(rect.Location, new Size(72, 24));
            //        tipRect.Intersect(rect);
            //        tipRect.Inflate(-4, -4);
            //        g.FillRectangle(Brushes.White, tipRect);
            //        g.DrawString(string.Format("{0}*{1}", rect.Width / 12, rect.Height / 12), SizeTipPen, Brushes.Red, rect.Location);
            //    }
            //}
        }

        public int ActionRenderIndex
        {
            get { return 6; }
        }
        private Rectangle calculateResult()
        {
            int dx = this.mouseCurrent.X - this.mouseDown.MousePosInControl.X;
            int dy = this.mouseCurrent.Y - this.mouseDown.MousePosInControl.Y;
            Rectangle rect = CalculateResult(this.actionElement, dx, dy, this.resizeMode, 24, 24);

            if (this.actionElement.IsRoot)
                return rect;
            else
                return this.fixValue(rect);
        }

        private static Rectangle CalculateResult(IViewElement element, int dx, int dy, ResizeMode resizeMode, int minWidth, int minHeight)
        {
            Rectangle rect = element.GetAbsRectangle();

            Debug.Assert(minWidth >= 0);
            Debug.Assert(minHeight >= 0);
            Debug.Assert(resizeMode != ResizeMode.None);

            Size miniSize = element.GetMiniSize();
            if (element is DockContainerElement)
                miniSize = (element as DockContainerElement).GetMiniSizeForResize();
            minWidth = Math.Max(minWidth, miniSize.Width);
            minHeight = Math.Max(minHeight, miniSize.Height);

            switch (resizeMode)
            {
                case ResizeMode.AdjustSize_Corner1:
                    {
                        if (rect.Left + dx + minWidth >= rect.Right)
                            dx = rect.Width - minWidth;
                        if (rect.Top + dy + minHeight >= rect.Bottom)
                            dy = rect.Height - minHeight;
                        rect = GeoHelper.GetOutterRectangle(new Point(rect.Left + dx, rect.Top + dy), new Point(rect.Right, rect.Bottom));
                        break;
                    }
                case ResizeMode.AdjustSize_Corner2:
                    {
                        if (rect.Right + dx - minWidth <= rect.Left)
                            dx = -(rect.Width - minWidth);
                        if (rect.Top + dy + minHeight >= rect.Bottom)
                            dy = rect.Height - minHeight;
                        rect = GeoHelper.GetOutterRectangle(new Point(rect.Right + dx, rect.Top + dy), new Point(rect.Left, rect.Bottom));

                        break;
                    }
                case ResizeMode.AdjustSize_Corner3:
                    {
                        if (rect.Right + dx - minWidth <= rect.Left)
                            dx = -(rect.Width - minWidth);
                        if (rect.Top >= rect.Bottom + dy - minHeight)
                            dy = -(rect.Height - minHeight);
                        rect = GeoHelper.GetOutterRectangle(new Point(rect.Left, rect.Top), new Point(rect.Right + dx, rect.Bottom + dy));
                        break;
                    }
                case ResizeMode.AdjustSize_Corner4:
                    {
                        if (rect.Left + dx >= rect.Right - minWidth)
                            dx = rect.Width - minWidth;
                        if (rect.Top >= rect.Bottom + dy - minHeight)
                            dy = -(rect.Height - minHeight);
                        rect = GeoHelper.GetOutterRectangle(new Point(rect.Right, rect.Top), new Point(rect.Left + dx, rect.Bottom + dy));

                        break;
                    }
                case ResizeMode.AdjustSize_Top:
                    {
                        if (rect.Top + dy + minHeight >= rect.Bottom)
                            dy = rect.Height - minHeight;
                        rect = GeoHelper.GetOutterRectangle(new Point(rect.Left, rect.Top + dy), new Point(rect.Right, rect.Bottom));
                        break;
                    }
                case ResizeMode.AdjustSize_Right:
                    {
                        if (rect.Right + dx <= rect.Left+minWidth )
                            dx = -(rect.Width - minWidth);
                        rect = GeoHelper.GetOutterRectangle(new Point(rect.Left, rect.Top), new Point(rect.Right + dx, rect.Bottom));

                        break;
                    }
                case ResizeMode.AdjustSize_Bottom:
                    {
                        if (rect.Top >= rect.Bottom + dy - minHeight)
                            dy = -(rect.Height - minHeight);
                        rect = GeoHelper.GetOutterRectangle(new Point(rect.Left, rect.Top), new Point(rect.Right, rect.Bottom + dy));

                        break;
                    }
                case ResizeMode.AdjustSize_Left:
                    {
                        if (rect.Left + dx >= rect.Right-minWidth )
                            dx = rect.Width - minWidth;
                        rect = GeoHelper.GetOutterRectangle(new Point(rect.Left + dx, rect.Top), new Point(rect.Right, rect.Bottom));

                        break;
                    }
            }

            //rect.Width = Math.Max(minSize, rect.Width);
            //rect.Height = Math.Max(minSize, rect.Height);

            return rect;
        }
        private static bool IsOnResizeBorder(IViewElement e, Point pntInControl)
        {
            if (e.IsRoot)
            {
                Rectangle rect = e.GetAbsRectangle();

                Rectangle rect3 = new Rectangle(rect.Right, rect.Bottom, 0, 0);
                rect3.Inflate(3, 3);

                Rectangle rectRight = GeoHelper.GetOutterRectangle(new Point(rect.Right, rect.Top), new Point(rect.Right, rect.Bottom));
                rectRight.Inflate(3, 0);
                Rectangle rectBottom = GeoHelper.GetOutterRectangle(new Point(rect.Right, rect.Bottom), new Point(rect.Left, rect.Bottom));
                rectBottom.Inflate(0, 3);

                return rect3.Contains(pntInControl) || rectRight.Contains(pntInControl) || rectBottom.Contains(pntInControl);

            }
            else if (e.Dock == DockType.Fill)
                return false;
            else
            {
                Rectangle rect = e.GetAbsRectangle();
                Rectangle rectInner = rect;
                rectInner.Inflate(-3, -3);
                bool isOnBorder = rect.Contains(pntInControl) && !rectInner.Contains(pntInControl);

                if (!isOnBorder)
                    return false;

                ResizeMode resizeMode = GetResizeMode(pntInControl, rect);
                if (resizeMode == ResizeMode.None)
                    return false;

                if (e.Dock == DockType.Fill)
                    return false;

                if ((resizeMode & ResizeMode.AdjustSize_Left) == ResizeMode.AdjustSize_Left && e.Dock == DockType.Left)
                    resizeMode = resizeMode ^ ResizeMode.AdjustSize_Left;

                if ((resizeMode & ResizeMode.AdjustSize_Right) == ResizeMode.AdjustSize_Right && e.Dock == DockType.Right)
                    resizeMode = resizeMode ^ ResizeMode.AdjustSize_Right;

                if ((resizeMode & ResizeMode.AdjustSize_Top) == ResizeMode.AdjustSize_Top && e.Dock == DockType.Top)
                    resizeMode = resizeMode ^ ResizeMode.AdjustSize_Top;

                if ((resizeMode & ResizeMode.AdjustSize_Bottom) == ResizeMode.AdjustSize_Bottom && e.Dock == DockType.Bottom)
                    resizeMode = resizeMode ^ ResizeMode.AdjustSize_Bottom;

                return resizeMode != ResizeMode.None;
            }
        }
        private static ResizeMode GetResizeMode(Point pnt, Rectangle rect)
        {
            ResizeMode op = ResizeMode.None;
            if (rect != Rectangle.Empty)
            {
                Rectangle rect1 = new Rectangle(rect.Left, rect.Top, 0, 0);
                Rectangle rect2 = new Rectangle(rect.Right, rect.Top, 0, 0);
                Rectangle rect3 = new Rectangle(rect.Right, rect.Bottom, 0, 0);
                Rectangle rect4 = new Rectangle(rect.Left, rect.Bottom, 0, 0);
                rect1.Inflate(3, 3);
                rect2.Inflate(3, 3);
                rect3.Inflate(3, 3);
                rect4.Inflate(3, 3);

                Rectangle rectTop = GeoHelper.GetOutterRectangle(new Point(rect.Left, rect.Top), new Point(rect.Right, rect.Top));
                rectTop.Inflate(0, 3);
                Rectangle rectRight = GeoHelper.GetOutterRectangle(new Point(rect.Right, rect.Top), new Point(rect.Right, rect.Bottom));
                rectRight.Inflate(3, 0);
                Rectangle rectBottom = GeoHelper.GetOutterRectangle(new Point(rect.Right, rect.Bottom), new Point(rect.Left, rect.Bottom));
                rectBottom.Inflate(0, 3);
                Rectangle rectLeft = GeoHelper.GetOutterRectangle(new Point(rect.Left, rect.Bottom), new Point(rect.Left, rect.Top));
                rectLeft.Inflate(3, 0);

                if (rect1.Contains(pnt))
                {
                    op = ResizeMode.AdjustSize_Corner1;
                }
                else if (rect2.Contains(pnt))
                {
                    op = ResizeMode.AdjustSize_Corner2;
                }
                else if (rect3.Contains(pnt))
                {
                    op = ResizeMode.AdjustSize_Corner3;
                }
                else if (rect4.Contains(pnt))
                {
                    op = ResizeMode.AdjustSize_Corner4;
                }
                else if (rectTop.Contains(pnt))//GeoHelper.IsPointOn(new Point(rect.Left, rect.Top), new Point(rect.Right, rect.Top), pnt))
                {
                    op = ResizeMode.AdjustSize_Top;
                }
                else if (rectRight.Contains(pnt))// (GeoHelper.IsPointOn(new Point(rect.Right, rect.Top), new Point(rect.Right, rect.Bottom), pnt))
                {
                    op = ResizeMode.AdjustSize_Right;
                }
                else if (rectBottom.Contains(pnt))//(GeoHelper.IsPointOn(new Point(rect.Right, rect.Bottom), new Point(rect.Left, rect.Bottom), pnt))
                {
                    op = ResizeMode.AdjustSize_Bottom;
                }
                else if (rectLeft.Contains( pnt ))//(GeoHelper.IsPointOn(new Point(rect.Left, rect.Bottom), new Point(rect.Left, rect.Top), pnt))
                {
                    op = ResizeMode.AdjustSize_Left;
                }

            }
            return op;
        }

        
    }
}
