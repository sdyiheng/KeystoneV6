using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessageEngine.SuperMCMCore;
using Keystone.AddIn.FormDesigner.Messages;
using System.Windows.Forms;
using Keystone.AddIn.FormDesigner.Elements;

namespace Keystone.AddIn.FormDesigner.Controllers.ToolbarControllers
{
    abstract class baseToolbarActionController : baseFormController
    {
        protected System.Windows.Forms.ToolStripButton button = null;
        public baseToolbarActionController(System.Windows.Forms.ToolStripButton _btn)
        {
            this.button = _btn;
            this.button.Enabled = false;
        }

    }

    abstract class mouseActionToolbarActionController : baseToolbarActionController
    {
        public mouseActionToolbarActionController(System.Windows.Forms.ToolStripButton _btn)
            : base(_btn)
        {
        }


        [MessageSubscriber(MessageEngine.SuperMCMCore.MMMODE.PASIUI)]
        protected override void on(CurrentViewDefinitionMsg msg)
        {
            base.on(msg);
            this.button.Enabled = this.form != null;
        }

        [MessageSubscriber(MessageEngine.SuperMCMCore.MMMODE.PASIUI)]
        protected virtual void on(ToolbarCommandClickedMsg msg)
        {
            if (actionButtonItems != null && Array.IndexOf(actionButtonItems, msg.Button) != -1)
            {
                this.button.Checked = msg.Button.Equals(this.button);
                this.doMyClickAction(msg);
            }
        }

        protected virtual void doMyClickAction(ToolbarCommandClickedMsg msg)
        {
            throw new Exception("Not Implemented!");
        }


        protected ToolStripItem[] actionButtonItems = null;
        [MessageSubscriber(MessageEngine.SuperMCMCore.MMMODE.PASIUI)]
        protected void on(MouseActionToolbarItemsMsg msg)
        {
            if( Array.IndexOf( msg.Items, this.button ) != -1 )
                this.actionButtonItems = msg.Items;
        }


        protected IViewElement element;

    }

}
