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

namespace Keystone.AddIn.FormDesigner.Controllers.ToolbarControllers
{
    class Toolbar_Cut_Controller : baseToolbarActionController
    {
        System.Windows.Forms.ToolStripMenuItem 剪贴ToolStripMenuItem;
        System.Windows.Forms.ToolStripButton btnCopy;
        System.Windows.Forms.ToolStripButton btnDelete;

        public Toolbar_Cut_Controller(System.Windows.Forms.ToolStripButton _btn
            , System.Windows.Forms.ToolStripButton _btnCopy
            , System.Windows.Forms.ToolStripButton _btnDelete
            , System.Windows.Forms.ToolStripMenuItem _剪贴ToolStripMenuItem
            )
            : base(_btn)
        {
            this.btnCopy = _btnCopy;
            this.btnDelete = _btnDelete;

            this.剪贴ToolStripMenuItem = _剪贴ToolStripMenuItem;
            this.剪贴ToolStripMenuItem.Enabled = false;
            this.剪贴ToolStripMenuItem.Click += new EventHandler(剪贴ToolStripMenuItem_Click);
        }

        void 剪贴ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.elements == null || this.elements.Length == 0)
                return;

            SuperMCMService.PostMessage(new ToolbarCommandClickedMsg(this.btnCopy, MouseClickType.ShortClick), ViewDesignerMainController.MESSAGECHANNEL);
            SuperMCMService.PostMessage(new ToolbarCommandClickedMsg(this.btnDelete, MouseClickType.ShortClick), ViewDesignerMainController.MESSAGECHANNEL, 100);
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
                bool rootOnly = true;
                foreach (IViewElement e in this.elements)
                {
                    if (e.IsRoot)
                        continue;

                    this.button.Enabled = true;
                    this.剪贴ToolStripMenuItem.Enabled = true;
                    rootOnly = false;
                    break;
                }

                if (rootOnly)
                {
                    this.button.Enabled = false;
                    this.剪贴ToolStripMenuItem.Enabled = false;
                }
            }
            else
            {
                this.button.Enabled = false;
                this.剪贴ToolStripMenuItem.Enabled = false;
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

                SuperMCMService.PostMessage(new ToolbarCommandClickedMsg(this.btnCopy, MouseClickType.ShortClick), ViewDesignerMainController.MESSAGECHANNEL);
                SuperMCMService.PostMessage(new ToolbarCommandClickedMsg(this.btnDelete, MouseClickType.ShortClick), ViewDesignerMainController.MESSAGECHANNEL, 100);
            }
        }

        [MessageSubscriber]
        private void on(KDM msg)
        {
            if (msg.KeyCode == System.Windows.Forms.Keys.X && msg.Control)
                SuperMCMService.PostMessage(new ToolbarCommandClickedMsg(this.button, MouseClickType.ShortClick));
        }


    }
}
