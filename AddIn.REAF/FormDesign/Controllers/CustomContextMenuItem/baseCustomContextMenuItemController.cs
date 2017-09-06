using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MessageEngine.SuperMCMCore;
using Keystone.AddIn.FormDesigner.Messages;

namespace Keystone.AddIn.FormDesigner.Controllers.CustomContextMenuItem
{
    abstract class baseCustomContextMenuItemController : baseFormController
    {
        protected bool reged = false;
        protected ToolStripMenuItem menuItem = null;
        [MessageSubscriber(MessageEngine.SuperMCMCore.MMMODE.PASIUI)]
        protected override void on(Messages.CurrentViewDefinitionMsg msg)
        {
            base.on(msg);
            if (!reged)
            {
                this.doReg();
                this.reged = true;
            }
        }

        protected abstract void doReg();

        protected  ContextMenuActionElementsMsg contextElementMsg = null;
        [MessageSubscriber(MessageEngine.SuperMCMCore.MMMODE.PASIUI)]
        protected virtual void onContextMenuActionElementsMsg(ContextMenuActionElementsMsg msg)
        {
            contextElementMsg = msg;
        }

        [MessageSubscriber]
        protected virtual void on(ContextMenuClosedMsg msg)
        {
            this.contextElementMsg = null;
        }
    }
}
