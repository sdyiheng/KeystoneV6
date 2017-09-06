using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using Keystone.AddIn.FormDesigner.Messages;
using Keystone.AddIn.FormDesigner.Elements;
using MessageEngine.SuperMCMCore;

namespace Keystone.AddIn.FormDesigner.Controllers.MouseAction
{
    class CursorController : baseFormController
    {

        private readonly GraphControl  viewControl;

        public CursorController(GraphControl _viewControl)
        {
            this.viewControl = _viewControl;
        }
        IViewElement element;

        [MessageSubscriber()]
        private void on(InsertElementTypeMsg msg)
        {
            this.element = msg.Element;
        }

        [MessageSubscriber( MessageEngine.SuperMCMCore.MMMODE.PASIUI )]
        private void on(SetCursorByResizeModeMsg msg)
        {
            if (this.element != null)
                return;

            this.updateCursorByResizeMode(msg.Mode);
        }

        private void updateCursorByResizeMode(ResizeMode mode)
        {
            try
            {
                switch (mode)
                {
                    case ResizeMode.AdjustSize_Corner1:
                    case ResizeMode.AdjustSize_Corner3:
                        if (this.viewControl.Cursor != Cursors.SizeNWSE)
                            this.viewControl.Cursor = Cursors.SizeNWSE;
                        break;
                    case ResizeMode.AdjustSize_Corner2:
                    case ResizeMode.AdjustSize_Corner4:
                        if (this.viewControl.Cursor != Cursors.SizeNESW)
                            this.viewControl.Cursor = Cursors.SizeNESW;
                        break;
                    case ResizeMode.AdjustSize_Top:
                    case ResizeMode.AdjustSize_Bottom:
                        if (this.viewControl.Cursor != Cursors.SizeNS)
                            this.viewControl.Cursor = Cursors.SizeNS;
                        break;
                    case ResizeMode.AdjustSize_Left:
                    case ResizeMode.AdjustSize_Right:
                        if (this.viewControl.Cursor != Cursors.SizeWE)
                            this.viewControl.Cursor = Cursors.SizeWE;
                        break;
                    case ResizeMode.None:
                    default:
                        if (this.viewControl.Cursor != Cursors.Default)
                            this.viewControl.Cursor = Cursors.Default;
                        break;
                }
            }
            catch
            {
            }
        }

    }

    public enum ResizeMode
    {
        /// <summary>
        /// 无操作
        /// </summary>
        None,

        /// <summary>
        /// 调整尺寸
        /// </summary>
        AdjustSize_Left,
        AdjustSize_Right,
        AdjustSize_Top,
        AdjustSize_Bottom,
        AdjustSize_Corner1,
        AdjustSize_Corner2,
        AdjustSize_Corner3,
        AdjustSize_Corner4,
    }

}
