using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessageEngine.SuperMCMCore;
using System.Windows.Forms;
using MessageEngine;
using Keystone.AddIn.FormDesigner.Messages;
using Keystone.AddIn.FormDesigner.Elements;
using Keystone.Common.Utility;
using System.Drawing;
using System.Diagnostics;

namespace Keystone.AddIn.FormDesigner.Controllers.CustomContextMenuItem
{
    class ImageLoaderController : baseCustomContextMenuItemController
    {
        protected override void doReg()
        {
            Debug.Assert(menuItem == null);

            menuItem = new ToolStripMenuItem();
            menuItem.Text = "加载图像内容";
            menuItem.Click += new EventHandler(menuItem_Click);
            menuItem.Visible = false;
            menuItem.Enabled = false;
            reged = true;

            SuperMCMService.PostMessage(new RegisterContextMenuItemMsg(menuItem), ViewDesignerMainController.MESSAGECHANNEL);
        }

        void menuItem_Click(object sender, EventArgs e)
        {
            if (this.contextElementMsg != null && this.contextElementMsg != null)
            {
                ImageElement imgElement = this.contextElementMsg.FocusOnElement as ImageElement;
                if (imgElement != null)
                {
                    OpenFileDialog dlg = new OpenFileDialog();
                    dlg.Filter = BitmapHelper.BMPFILTER ;
                    if (dlg.ShowDialog() == DialogResult.OK )
                    {
                        Bitmap bmp  = BitmapHelper.GetImageFromFile(dlg.FileName);
                        imgElement.Image = bmp;
                        SuperMCMService.PostMessage(new ViewPaintRequestMsg(), ViewDesignerMainController.MESSAGECHANNEL);
                        SuperMCMService.PostMessage(new RefreshPropertyValueMsg(), ViewDesignerMainController.MESSAGECHANNEL);
                    }
                }
            }
        }

        [MessageSubscriber(MessageEngine.SuperMCMCore.MMMODE.PASIUI)]
        protected override void onContextMenuActionElementsMsg(ContextMenuActionElementsMsg msg)
        {
            base.onContextMenuActionElementsMsg(msg);
            if (this.menuItem != null)
            {
                this.menuItem.Enabled = contextElementMsg != null && contextElementMsg.FocusOnElement is ImageElement;
                this.menuItem.Visible = this.menuItem.Enabled;
            }
        }

        

    }
}
