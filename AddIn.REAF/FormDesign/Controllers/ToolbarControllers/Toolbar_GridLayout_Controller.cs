using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessageEngine.SuperMCMCore;
using MessageEngine;
using Keystone.AddIn.FormDesigner.Messages;
using Keystone.AddIn.FormDesigner.Elements.Layout;

namespace Keystone.AddIn.FormDesigner.Controllers.ToolbarControllers
{
    class Toolbar_GridLayout_Controller : mouseActionToolbarActionController
    {
        public Toolbar_GridLayout_Controller(System.Windows.Forms.ToolStripButton _btn)
            : base(_btn)
        {
        }

        protected override void doMyClickAction(Keystone.AddIn.FormDesigner.Messages.ToolbarCommandClickedMsg msg)
        {
            if (this.button.Checked)
            {
                bool multiTimes = msg.ClickType == MouseClickType.DoubleClick;
                if (this.element != null && this.element is GridLayoutElement && !multiTimes)
                    return;

                SuperMCMService.PostMesage(new InsertElementTypeMsg(new GridLayoutElement(), msg.ClickType == MouseClickType.DoubleClick), ViewDesignerMainController.MESSAGECHANNEL);
            }
        }


        [MessageSubscriber(MessageEngine.SuperMCMCore.UIThreadSynchronizationMode.PostAsynchronousInUIThread)]
        private void on(InsertElementTypeMsg msg)
        {
            this.element = msg.Element;
            this.button.Checked = msg.Element is GridLayoutElement;
        } 
    }
}
