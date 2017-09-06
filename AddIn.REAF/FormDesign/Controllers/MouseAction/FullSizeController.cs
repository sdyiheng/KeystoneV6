using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Keystone.AddIn.FormDesigner.Elements;
using MessageEngine.SuperMCMCore;
using Keystone.AddIn.FormDesigner.Messages;
using System.Drawing;
using Keystone.AddIn.FormDesigner.Elements;
using MessageEngine;

namespace Keystone.AddIn.FormDesigner.Controllers.MouseAction
{
    class FullSizeController : baseFormController
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
        private bool isElementSelected(IViewElement e)
        {
            return this.selections != null && Array.IndexOf(this.selections, e) != -1;
        }


        [MessageSubscriber( MessageEngine.SuperMCMCore.MMMODE.ASIS)]
        private void on(DesignerMouseClickMsg msg)
        {
            if (this.form == null)
                return;

            if (msg.ClickType != MouseClickType.DoubleClick)
                return;

            if (this.selections == null || this.selections.Length == 0)
                return;

            IViewElement[] elements = this.form.HitTest(msg.MouseInControl, 0);
            IViewElementContainer topMostContainer = BaseViewElement.TopMostElementContainer(elements);
            IViewElement topMostElement = BaseViewElement.TopMostElement(elements);

            if (topMostElement != null && this.isElementSelected(topMostElement))
            {
                //if (topMostElement is SubFormElement)
                //{
                //    SubFormElement form = topMostElement as SubFormElement;
                //    if (form.SubFormID != Guid.Empty)
                //    {
                //        SuperMCMService.PostMessage(new RequestShowFormViewMsg(form.SubFormID));
                //    }
                //    return;
                //}
                this.fullSizeElement(topMostElement);
            }
            else if (topMostContainer != null && this.isElementSelected(topMostContainer))
            {
                this.fullSizeElement(topMostContainer);
            }

        }

        OriginalElementRectMsg msg;
        [MessageSubscriber( MessageEngine.SuperMCMCore.MMMODE.ASIS)]
        private void on(OriginalElementRectMsg _msg)
        {
            this.msg = _msg;
        }

        private void fullSizeElement(IViewElement e)
        {
            OriginalElementRectMsg _msg = this.msg;
            if (e == null || e.ParentElement == null)
                return;

            if (e.ParentElement.IsChildFullSize(e))
            {
                //恢复原来的
                if (_msg != null && e.ElementID == _msg.ElementID)
                {
                    e.ParentElement.ResetElementSize(_msg.OriginalRect , e);
                }
            }
            else
            {
                e.ParentElement.FullSizeChild(e);
            }

            if (this.msg == _msg)
                this.msg = null;
        }

    }
}
