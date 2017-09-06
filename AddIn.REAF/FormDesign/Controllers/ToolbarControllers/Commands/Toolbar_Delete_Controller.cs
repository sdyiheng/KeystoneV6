using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessageEngine.SuperMCMCore;
using Keystone.AddIn.FormDesigner.Elements;
using Keystone.AddIn.FormDesigner.Messages;
using System.Diagnostics;
using MessageEngine;
using Keystone.Common.Messages;
using Keystone.Graph.Core.Messages;

namespace Keystone.AddIn.FormDesigner.Controllers.ToolbarControllers
{
    class Toolbar_Delete_Controller : baseToolbarActionController
    {
        System.Windows.Forms.ToolStripMenuItem 删除ToolStripMenuItem;
        public Toolbar_Delete_Controller(System.Windows.Forms.ToolStripButton _btn
            , System.Windows.Forms.ToolStripMenuItem _删除ToolStripMenuItem)
            : base(_btn)
        {
            this.删除ToolStripMenuItem = _删除ToolStripMenuItem;
            this.删除ToolStripMenuItem.Enabled = false;
            this.删除ToolStripMenuItem.Click += new EventHandler(删除ToolStripMenuItem_Click);
        }

        void 删除ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.doDelete();
        }

        IViewElement[] elements = null;
        protected IViewElement element;

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
                foreach (IViewElement e in this.elements)
                {
                    if (e.IsRoot)
                        continue;

                    this.button.Enabled = true;
                    this.删除ToolStripMenuItem.Enabled = true;
                    return;
                }

                this.button.Enabled = false;
                this.删除ToolStripMenuItem.Enabled = false;
            }
            else
            {
                this.button.Enabled = false;
                this.删除ToolStripMenuItem.Enabled = false;
            }
        }

        [MessageSubscriber(MessageEngine.SuperMCMCore.MMMODE.PASIUI)]
        private void on(InsertElementTypeMsg msg)
        {
            this.element = msg.Element;
            refreshButtonStatus();
        }


        [MessageSubscriber(MessageEngine.SuperMCMCore.MMMODE.PASIUI)]
        protected void on(ToolbarCommandClickedMsg msg)
        {
            if (msg.Button == this.button)
            {
                if (this.elements == null || this.elements.Length == 0)
                    return;

                this.doDelete();
            }
        }

        private void doDelete()
        {

            if (!Keystone.Common.Utility.MessageAlertHelper.ShowConfirmMsg("确定要删除选中的元素吗？"))
            {
                return;
            }

            foreach (IViewElement e in this.elements)
            {
                if (e.IsRoot)
                    continue;

                Debug.Assert(e.ParentElement is IViewElementContainer);
                (e.ParentElement as IViewElementContainer).RemoveChild(this.elements);
                break;
            }

            this.form.ReIndex();
            this.button.Enabled = false;
            this.删除ToolStripMenuItem.Enabled = false;

            SuperMCMService.PostMessage(new RefreshOutlineMsg(), ViewDesignerMainController.MESSAGECHANNEL);
            SuperMCMService.PostMessage(new SelectionChangedMsg(new IViewElement[] { this.form }), ViewDesignerMainController.MESSAGECHANNEL);
            SuperMCMService.PostMessage(new ViewPaintRequestMsg(), ViewDesignerMainController.MESSAGECHANNEL);
        }

        [MessageSubscriber]
        private void on(KDM msg)
        {
            if (msg.KeyCode == System.Windows.Forms.Keys.Delete)
                SuperMCMService.PostMessage(new ToolbarCommandClickedMsg(this.button, MouseClickType.ShortClick));
        }
    }
}
