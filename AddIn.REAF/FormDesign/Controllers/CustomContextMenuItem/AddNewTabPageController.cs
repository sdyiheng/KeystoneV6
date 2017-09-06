using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using MessageEngine;
using Keystone.AddIn.FormDesigner.Messages;
using MessageEngine.SuperMCMCore;
using System.Drawing;
using Keystone.AddIn.FormDesigner.Elements;
using Keystone.Common.Utility;

namespace Keystone.AddIn.FormDesigner.Controllers.CustomContextMenuItem
{
    class AddNewTabPageController : baseCustomContextMenuItemController
    {
        protected override void doReg()
        {
            Debug.Assert(menuItem == null);

            menuItem = new ToolStripMenuItem();
            menuItem.Text = "添加新Tab页";
            menuItem.Click += new EventHandler(menuItem_Click);
            menuItem.Visible = false;
            menuItem.Enabled = false;
            reged = true;

            SuperMCMService.PostMessage(new RegisterContextMenuItemMsg(menuItem), ViewDesignerMainController.MESSAGECHANNEL);
        }

        void menuItem_Click(object sender, EventArgs e)
        {
            if( this.form == null )
                return;

            if (this.contextElementMsg != null && this.contextElementMsg != null)
            {
                TabElement tabElement = this.getTabElement(this.contextElementMsg.AllHitTestElements);
                if (tabElement == null)
                    return;

                //tabElement.AddChild(
                tabElement.AddNewElement(new TabPageElement(), Rectangle.Empty);
            }

            //SuperMCMService.PostMessage(new resizeFormDesignerMsg(), ViewDesignerMainController.MESSAGECHANNEL);
            SuperMCMService.PostMessage(new ViewPaintRequestMsg(), ViewDesignerMainController.MESSAGECHANNEL);
        }

        [MessageSubscriber(MessageEngine.SuperMCMCore.MMMODE.PASIUI)]
        protected override void onContextMenuActionElementsMsg(ContextMenuActionElementsMsg msg)
        {
            base.onContextMenuActionElementsMsg(msg);
            if (this.menuItem != null)
            {
                this.menuItem.Enabled = contextElementMsg != null && getTabElement(contextElementMsg.AllHitTestElements) != null;
                this.menuItem.Visible = this.menuItem.Enabled;
            }
        }

        private TabElement getTabElement(IViewElement[] elements)
        {
            if (elements == null || elements.Length == 0)
                return null;

            for (int i = elements.Length - 1; i >= 0; i--)
            {
                if (elements[i] is TabElement)
                    return elements[i] as TabElement;
            }

            return null;
        }

    }
}
