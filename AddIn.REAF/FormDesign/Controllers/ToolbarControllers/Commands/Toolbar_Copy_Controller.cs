using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessageEngine.SuperMCMCore;
using Keystone.AddIn.FormDesigner.Messages;
using Keystone.AddIn.FormDesigner.Elements;
using System.Windows.Forms;
using MessageEngine;
using System.Drawing;
using Keystone.Common.Utility;
using System.Diagnostics;
using Keystone.Common.Messages;
using Keystone.Graph.Core.Messages;

namespace Keystone.AddIn.FormDesigner.Controllers.ToolbarControllers
{
    class Toolbar_Copy_Controller : baseToolbarActionController
    {
        System.Windows.Forms.ToolStripMenuItem 复制ToolStripMenuItem;
        System.Windows.Forms.ToolStripMenuItem 粘贴ToolStripMenuItem;
        public Toolbar_Copy_Controller(System.Windows.Forms.ToolStripButton _btn
            , System.Windows.Forms.ToolStripMenuItem _复制ToolStripMenuItem
            , System.Windows.Forms.ToolStripMenuItem _粘贴ToolStripMenuItem)
            : base(_btn)
        {
            this.复制ToolStripMenuItem = _复制ToolStripMenuItem;
            this.复制ToolStripMenuItem.Enabled = false;
            this.复制ToolStripMenuItem.Click += new EventHandler(复制ToolStripMenuItem_Click);

            this.粘贴ToolStripMenuItem = _粘贴ToolStripMenuItem;
            this.粘贴ToolStripMenuItem.Enabled = false;
            this.粘贴ToolStripMenuItem.Click += new EventHandler(粘贴ToolStripMenuItem_Click);
        }

        void 粘贴ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.contextElementMsg == null)
                return;

            Point p = this.contextElementMsg.PointInControl;

            Debug.Assert(this.contextElementMsg.FocusOnElement is IViewElementContainer);
            if( !(this.contextElementMsg.FocusOnElement is IViewElementContainer ))
                return;

            if (this.copyedElements == null || this.copyedElements.Length == 0)
                return;

            IViewElement[] pastedElements = Keystone.Common.Utility.ObjectReaderWriter.DeepCopyObject(this.copyedElements) as IViewElement[];

            Point pActionAt = new Point(
                BaseViewElement.FixGridValue_Down(p.X),
                BaseViewElement.FixGridValue_Down(p.Y));

            (this.contextElementMsg.FocusOnElement as IViewElementContainer).DoPaste(
                pastedElements, pActionAt);

#if DEBUG
            foreach (IViewElement element in pastedElements)
            {
                Debug.Assert(element.ParentElement == this.contextElementMsg.FocusOnElement);
            }
#endif


            //if (this.contextElementMsg.FocusOnElement.LayoutUp(null))
            //{
            //    SuperMCMService.PostMessage(new resizeFormDesignerMsg(), ViewDesignerMainController.MESSAGECHANNEL);
            //}
            SuperMCMService.PostMessage(new RefreshOutlineMsg(), ViewDesignerMainController.MESSAGECHANNEL);
            SuperMCMService.PostMessage(new ViewPaintRequestMsg(), ViewDesignerMainController.MESSAGECHANNEL);
            SuperMCMService.PostMessage(new SelectionChangedMsg(pastedElements), ViewDesignerMainController.MESSAGECHANNEL);

            
        }

        void 复制ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SuperMCMService.PostMessage(new ToolbarCommandClickedMsg(this.button, MouseClickType.ShortClick),
                ViewDesignerMainController.MESSAGECHANNEL);
        }

        IViewElement[] elements = null;
        protected IViewElement element;

        IViewElement[] copyedElements = null;


        [MessageSubscriber(MessageEngine.SuperMCMCore.MMMODE.PASIUI)]
        private void on(SelectionChangedMsg msg)
        {
            this.elements = msg.Elements;
            refreshButtonStatus();
        }

        private void refreshButtonStatus()
        {
            if (this.form != null && this.elements != null && this.elements.Length > 0 && this.element == null)
            {
                bool rootOnly = true;
                foreach (IViewElement e in this.elements)
                {
                    if (e.IsRoot)
                        continue;

                    this.button.Enabled = true;
                    this.复制ToolStripMenuItem.Enabled = true;
                    rootOnly = false;
                    break;
                }

                if (rootOnly)
                {
                    this.button.Enabled = false;
                    this.复制ToolStripMenuItem.Enabled = false;
                }
            }
            else
            {
                this.button.Enabled = false;
                this.复制ToolStripMenuItem.Enabled = false;
            }

            
        }

        [MessageSubscriber(MessageEngine.SuperMCMCore.MMMODE.PASIUI)]
        private void on(InsertElementTypeMsg msg)
        {
            this.element = msg.Element;
            refreshButtonStatus();
        }

        [MessageSubscriber(MessageEngine.SuperMCMCore.MMMODE.PASIUI)]
        protected  void on(ToolbarCommandClickedMsg msg)
        {
            if (msg.Button == this.button)
            {
                if (this.elements == null || this.elements.Length == 0)
                    return;

                copyedElements = Keystone.Common.Utility.ObjectReaderWriter.DeepCopyObject(this.elements) as IViewElement[];
                foreach (IViewElement e in copyedElements)
                    e.ParentElementID = Guid.Empty;
                this.粘贴ToolStripMenuItem.Enabled = copyedElements != null && copyedElements.Length > 0;
            }
        }

        private ContextMenuActionElementsMsg contextElementMsg = null;

        [MessageSubscriber(MessageEngine.SuperMCMCore.MMMODE.PASIUI)]
        private void on(ContextMenuActionElementsMsg msg)
        {
            this.contextElementMsg = msg;
            this.粘贴ToolStripMenuItem.Enabled = msg.Selections != null && msg.Selections.Length > 0 
                && msg.FocusOnElement != null && msg.FocusOnElement is IViewElementContainer
                && this.copyedElements != null && this.copyedElements.Length > 0 
                && (msg.FocusOnElement as IViewElementContainer).CanPaste(copyedElements, msg.PointInControl );
        }

        [MessageSubscriber(MessageEngine.SuperMCMCore.MMMODE.PASIUI)]
        private void on(ContextMenuClosedMsg msg)
        {
            this.contextElementMsg = null;
        }


        [MessageSubscriber]
        private void on(KDM msg)
        {
            if (msg.KeyCode == System.Windows.Forms.Keys.C && msg.Control  )
                SuperMCMService.PostMessage(new ToolbarCommandClickedMsg(this.button, MouseClickType.ShortClick));
        }

    }
}
