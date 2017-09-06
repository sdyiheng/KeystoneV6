using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessageEngine.SuperMCMCore;
using Keystone.AddIn.FormDesigner.Messages;
using MessageEngine;

namespace Keystone.AddIn.FormDesigner.Controllers.ToolbarControllers
{
    class Toolbar_Cursor_Controller : mouseActionToolbarActionController
    {
        public Toolbar_Cursor_Controller(System.Windows.Forms.ToolStripButton _btn)
            : base(_btn)
        {
        }

        protected override void doMyClickAction(Keystone.AddIn.FormDesigner.Messages.ToolbarCommandClickedMsg msg)
        {
            if (this.button.Checked)
            {
                SuperMCMService.PostMessage(new InsertElementTypeMsg(null, msg.ClickType == MouseClickType.DoubleClick), ViewDesignerMainController.MESSAGECHANNEL);
            }
        }

        [MessageSubscriber( MessageEngine.SuperMCMCore.MMMODE.PASIUI )]
        private void on(InsertElementTypeMsg msg)
        {
            this.element = msg.Element;
            this.button.Checked = msg.Element == null;
        }
    }
}
