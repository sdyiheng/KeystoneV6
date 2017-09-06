using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessageEngine.SuperMCMCore;
using System.Windows.Forms;
using Keystone.AddIn.FormDesigner.Messages;
using Keystone.AddIn.FormDesigner.Elements;
using System.ComponentModel;
using System.Collections;
using Keystone.AddIn.FormDesigner.Elements.Property;
using System.Diagnostics;
using MessageEngine;
using Keystone.WellKnownMessage;

namespace Keystone.AddIn.FormDesigner.Controllers.ToolbarControllers
{
    class Toolbar_Property_Controller : mouseActionToolbarActionController
    {
        private PropertyGrid propertyGrid;
        private SplitContainer splitContainner;
        System.Windows.Forms.ToolStripMenuItem 属性ToolStripMenuItem;
        public Toolbar_Property_Controller(
            System.Windows.Forms.ToolStripButton _btn, PropertyGrid _pg, SplitContainer _splitContainner
            , System.Windows.Forms.ToolStripMenuItem _属性ToolStripMenuItem)
            : base(_btn)
        {
            this.propertyGrid = _pg;
            this.splitContainner = _splitContainner;
            this.propertyGrid.Dock = DockStyle.Fill;
            this.属性ToolStripMenuItem = _属性ToolStripMenuItem;
            this.属性ToolStripMenuItem.Enabled = false;

            this.属性ToolStripMenuItem.Click += new EventHandler(属性ToolStripMenuItem_Click);
        }

        void 属性ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!this.button.Checked)
            {
                SuperMCMService.PostMessage(new ToolbarCommandClickedMsg(this.button, MouseClickType.ShortClick), ViewDesignerMainController.MESSAGECHANNEL);
            }
            this.showProperty();
        }

        ContextMenuActionElementsMsg contextElementMsg = null;
        [MessageSubscriber]
        private void on(ContextMenuActionElementsMsg msg)
        {
            contextElementMsg = msg;
            this.selectedElements = msg.Selections;
        }

        [MessageSubscriber]
        private void on(ContextMenuClosedMsg msg)
        {
            this.contextElementMsg = null;
        }

        [MessageSubscriber(MessageEngine.SuperMCMCore.MMMODE.PASIUI)]
        protected override void on(CurrentViewDefinitionMsg msg)
        {
            base.on(msg);
            this.button.Enabled = this.form != null;
            this.属性ToolStripMenuItem.Enabled = this.form != null;
        }


        [MessageSubscriber(MessageEngine.SuperMCMCore.MMMODE.PASIUI)]
        protected override void on(ToolbarCommandClickedMsg msg)
        {
            if (actionButtonItems != null && Array.IndexOf(actionButtonItems, msg.Button) != -1)
            {
                if (button.Checked && msg.Button.Equals(this.button))
                {
                    this.button.Checked = false;
                    this.splitContainner.Panel2Collapsed = true;
                }
                else
                {
                    this.button.Checked = msg.Button.Equals(this.button);
                    if (this.button.Checked)
                    {
                        this.propertyGrid.BringToFront();
                        if( this.splitContainner.Panel2Collapsed)
                            this.splitContainner.Panel2Collapsed = false;
                    }
                }
            }
        }

        [MessageSubscriber(MessageEngine.SuperMCMCore.MMMODE.PASIUI)]
        private void on(InsertElementTypeMsg msg)
        {
            this.element = msg.Element;
            this.showProperty();
        }

        IViewElement[] selectedElements = null;
        Dictionary<string, CustomPropertySourceWrap> customProperties = new Dictionary<string, CustomPropertySourceWrap>();

        [MessageSubscriber( MessageEngine.SuperMCMCore.MMMODE.PASIUI)]
        private void on(SelectionChangedMsg msg)
        {
            selectedElements = msg.Elements;
            if (this.contextElementMsg != null && this.contextElementMsg.Selections != msg.Elements)
                this.contextElementMsg = null;
            this.showProperty();
        }

        private void showProperty()
        {
            //SuperMCMService.PostMessage(new ShowShortTipInfoMsg("showProperty"+DateTime.Now.ToString()));
            IViewElement currentElement = null;
            CustomPropertySourceWrap bag = this.getCurrentPropertySourceWrap(out currentElement);
            if (bag != null)
            {
                bindPropertyValue(currentElement, bag);
            }
            this.propertyGrid.Enabled = bag != null;
            this.propertyGrid.SelectedObject = bag;
            this.propertyGrid.Refresh();
        }

        private CustomPropertySourceWrap getCurrentPropertySourceWrap(out IViewElement currentElement)
        {
            currentElement = null;

            ////正在添加
            //if (this.element != null)
            //    return null;

            if (this.selectedElements == null || this.selectedElements.Length == 0)
                return null;

            if (this.contextElementMsg != null && this.contextElementMsg.Selections == this.selectedElements)
            {
                currentElement = this.contextElementMsg.FocusOnElement;
            }
            else
            {
                currentElement = this.selectedElements[0];
            }

            string elementTypename = currentElement.GetType().ToString();

            if (customProperties.ContainsKey(elementTypename))
            {
                return customProperties[elementTypename];
            }
            else
            {
                //在此添加图形类型对应的属性袋描述
                CustomPropertySourceWrap wrap = new CustomPropertySourceWrap();
                wrap.AddRange(currentElement.GetCustomProperties());
                this.customProperties.Add(elementTypename, wrap);

                return wrap;
            }
        }

        private static void bindPropertyValue(ICustomPropertySource propertySource, CustomPropertySourceWrap propertySourceWrap)
        {
            Debug.Assert(propertySource != null);
            Debug.Assert(propertySourceWrap != null);

            if (propertySource == null || propertySourceWrap == null)
                return;

            //绑定ID
            propertySourceWrap.CustomPropertySourceID = propertySource.CustomPropertySourceID;

            foreach (CustomProperty cp in propertySourceWrap)
            {
                //绑定属性值
                object value = propertySource.GetPropertyValue(cp.PropertyName);
                //SuperMCMService.PostMessage(new ShowShortTipInfoMsg(
                //    string.Format("GetPropertyValue 【PropertyName】:{0} 【Value】:{1}", cp.PropertyName, value)));
                if (value == null)
                    continue;
                //Debug.Assert(value != null);
                cp.Value = value;
            }
        }

        [MessageSubscriber(MessageEngine.SuperMCMCore.MMMODE.PASIUI)]
        private void on(UpdatePropertyValueMsg msg)
        {
            if (this.element != null)
                return;
            if (this.selectedElements == null || this.selectedElements.Length == 0)
                return;

            IViewElement  currentElement = this.selectedElements[0];

            CustomPropertySourceWrap wrap = msg.Component as CustomPropertySourceWrap;
            Debug.Assert(wrap != null);
            Debug.Assert(wrap.CustomPropertySourceID == currentElement.CustomPropertySourceID);

            if (wrap.CustomPropertySourceID == currentElement.CustomPropertySourceID)
            {
                currentElement.SetPropertyValue(msg.Property.PropertyName, msg.Property.Value);
                //if (currentElement is IViewElementContainer)
                //    (currentElement as IViewElementContainer).LayoutDown(null);
                //currentElement.LayoutUp(null);
                SuperMCMService.PostMessage(new ViewElementPropertyChangedMsg(currentElement, msg.Property.PropertyName), ViewDesignerMainController.MESSAGECHANNEL);
                SuperMCMService.PostMessage(new ViewPaintRequestMsg(), ViewDesignerMainController.MESSAGECHANNEL);
                this.showProperty();
            }
        }

        [MessageSubscriber( MessageEngine.SuperMCMCore.MMMODE.PASIUI)]
        private void on(RefreshPropertyValueMsg msg)
        {
            this.showProperty();
        }
    }
}
