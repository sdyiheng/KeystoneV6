using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using Keystone.Common.Utility;
using MessageEngine;

namespace Keystone.AddIn.Entity
{
    internal partial class frmEntityEditor : Form
    {
        public frmEntityEditor()
        {
            InitializeComponent();

            this.txtEditor.LostFocus += new EventHandler(txtEditor_LostFocus);
            this.comEditor.LostFocus += new EventHandler(comEditor_LostFocus);
            this.listView1.LostFocus += new EventHandler(listView1_LostFocus);
            this.dataGridView1.DataError += new DataGridViewDataErrorEventHandler(dataGridView1_DataError);
        }

        void dataGridView1_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            MessageAlertHelper.ShowErrorMsg("修正输入的错误值。");
        }

        void listView1_LostFocus(object sender, EventArgs e)
        {
            this.listView1.SelectedItems.Clear();
        }


        EntityNode _entity = null;
        internal void BindEntity(EntityNode entity)
        {
            Debug.Assert(entity != null);
            this._entity = entity;
            this.doBindData();
        }

        private bool dataBinding = false;
        private void doBindData()
        {
            dataBinding = true;
            try
            {
                //this.btnDeleteItem.Enabled = false;

                Debug.Assert(this._entity != null);
                this.txtEntityName.Text = this._entity.EntityName;
                this.txtEntityName_CN.Text = this._entity.EntityNameCH;
                this.txtNamespace.Text = this._entity.Namespace;
                this.txtEntityDescription.Text = string.IsNullOrEmpty(this._entity.EntityDescription) ? "" : this._entity.EntityDescription;

                this.doBindFields();
                if (this._entity.Fields.Count > 0 && this._entity.Data == null)
                    this._entity.UpdateInitData();
                this.dataGridView1.DataSource = this._entity.Data;
                //this.updateData();
            }
            finally
            {
                dataBinding = false;
            }
        }

        private void doBindFields()
        {
            this.comEditor.Hide();
            this.txtEditor.Hide();

            this.currentSB = null;
            this.currentitem = null;

            Dictionary<Guid, Guid> checkedItems = new Dictionary<Guid, Guid>();
            foreach (ListViewItem item in this.listView1.CheckedItems)
            {
                EntityField field = item.Tag as EntityField;
                checkedItems.Add(field.ID, field.ID);
            }

            this.listView1.SuspendLayout();
            this.listView1.BeginUpdate();

            this.listView1.Items.Clear();
            this.listView1.Groups.Clear();

            EntityField[] keyFields = this._entity.GetKeyFields();
            if (keyFields.Length > 0)
            {
                ListViewGroup keygroup = new ListViewGroup("主键", HorizontalAlignment.Left);
                this.listView1.Groups.Add(keygroup);

                foreach (EntityField field in keyFields)
                {
                    ListViewItem viewitem = new ListViewItem(new string[] { field.FieldName, field.DataType.ToString(), "是", field.IsDBField ? "是" : "否", field.GridColumnName, field.FieldDescription }, keygroup);
                    viewitem.Tag = field;
                    if (checkedItems.ContainsKey(field.ID))
                        viewitem.Checked = true;
                    this.listView1.Items.Add(viewitem);
                }
            }

            EntityField[] nonKeyFields = this._entity.GetNonKeyFields();
            if (nonKeyFields.Length > 0)
            {
                ListViewGroup nonkeygroup = new ListViewGroup("非主键", HorizontalAlignment.Left);
                this.listView1.Groups.Add(nonkeygroup);

                foreach (EntityField field in nonKeyFields)
                {
                    ListViewItem viewitem = new ListViewItem(new string[] { field.FieldName, field.DataType.ToString(), "否", field.IsDBField ? "是" : "否", field.GridColumnName, field.FieldDescription }, nonkeygroup);
                    viewitem.Tag = field;
                    if (checkedItems.ContainsKey(field.ID))
                        viewitem.Checked = true;
                    this.listView1.Items.Add(viewitem);
                }
            }

            this.listView1.EndUpdate();
            this.listView1.ResumeLayout();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnAddNew_Click(object sender, EventArgs e)
        {
            Debug.Assert(this._entity != null);
            if (this._entity == null)
                return;

            this._entity.Fields.Add(new EntityField());
            this.doBindFields();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (!HasChecked())
                return;

            if (MessageBox.Show("确定要删除选中字段吗？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == System.Windows.Forms.DialogResult.Yes)
                this.DeleteSelectedItems();
        }

        private bool HasChecked()
        {
            foreach (ListViewItem item in this.listView1.Items)
            {
                if (item.Checked)
                    return true;
            }
            return false;
        }

        public void DeleteSelectedItems()
        {
            Debug.Assert(this._entity != null);
            if (this._entity == null)
                return;

            bool deleted = false;
            foreach (ListViewItem item in this.listView1.Items)
            {
                Debug.Assert(item != null);
                if (item.Checked)
                {
                    EntityField field = item.Tag as EntityField;
                    while (this._entity.Fields.Contains(field))
                    {
                        this._entity.Fields.Remove(field);
                        deleted = true;
                    }
                }
            }

            if (deleted)
            {
                this.doBindFields();
                //this.updateData();
            }
        }


        public void UpdateData()
        {
            Debug.Assert(this._entity  != null);
            if (!string.IsNullOrEmpty(this.txtEntityName.Text.Trim()))
                this._entity.EntityName = this.txtEntityName.Text.Trim();
            this._entity.EntityNameCH = this.txtEntityName_CN.Text.Trim();

            this._entity.Namespace = this.txtNamespace.Text;

            this._entity.EntityDescription = this.txtEntityDescription.Text;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.txtEntityName.Text.Trim()))
            {
                MessageAlertHelper.ShowTipMsg("请输入业务对象名称！");
                this.txtEntityName.Focus();
                return;
            }

            if (!this._entity.ValidateInitData())
            {
                if (MessageAlertHelper.ShowConfirmMsg("检测到初始化数据定义与字段定义有差异。您需要修正数据吗？"))
                {
                    this._entity.UpdateInitData();
                    this.dataGridView1.DataSource = this._entity.Data;
                    this.tabControl1.SelectedIndex = 1;
                    return;
                }
            }

            this.UpdateData();
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Close();
        }


        ListViewItem currentitem = null;
        ListViewItem.ListViewSubItem currentSB = null;
        private void listView1_AfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(e.Label) || string.IsNullOrEmpty(e.Label.Trim()))
                {
                    e.CancelEdit = true;
                    return;
                }

                EntityField field = this.listView1.Items[e.Item].Tag
                    as EntityField;

                EntityField existField = this._entity.GetField(e.Label.Trim());
                if (existField != null)
                {
                    e.CancelEdit = true;
                    if (existField != field)
                    {
                        MessageAlertHelper.ShowErrorMsg("重复的数据字段命名！");
                    }
                    return;
                }

                Debug.Assert(field != null);

                field.FieldName = e.Label.Trim();
            }
            finally
            {
                this.currentitem = null;
                this.currentSB = null;
            }
        }
        bool canceled = false;
        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.txtEditor.Hide();
            this.comEditor.Hide();

            canceled = false;
            currentSB = null;
            currentitem = this.listView1.GetItemAt(e.X, e.Y);
            if (currentitem == null)
                return;

            currentSB = currentitem.GetSubItemAt(e.X, e.Y);
            if (currentSB == null)
            {
                currentitem = null;
                return;
            }

            EntityField field = currentitem.Tag as EntityField;
            Debug.Assert(field != null);
            if (field == null)
            {
                currentitem = null;
                return;
            }

            int subItemIndex = currentitem.SubItems.IndexOf(currentSB);
            if (subItemIndex == 0)
            {
                currentitem.BeginEdit();
                currentitem = null;
                currentSB = null;
                return;
            }

            currentitem.Selected = false;
            if (subItemIndex == 4)
            {
                //启动文本编辑
                this.txtEditor.Location = new Point(currentSB.Bounds.Left + 2 + this.listView1.Left, currentSB.Bounds.Top + listView1.Top);
                this.txtEditor.Size = currentSB.Bounds.Size;
                this.txtEditor.Text = currentSB.Text;
                this.txtEditor.Show();
                this.txtEditor.Focus();
            }
            else if (subItemIndex == 1)
            {
                //启动选择编辑功能:数据类型
                this.comEditor.DataSource = EntityField.GetAllFieldDataType();
                this.comEditor.Text = field.DataType.ToString();
                this.comEditor.Location = new Point(currentSB.Bounds.Left + 2 + this.listView1.Left, currentSB.Bounds.Top + listView1.Top);
                this.comEditor.Size = currentSB.Bounds.Size;
                this.comEditor.Show();
                this.comEditor.Focus();
            }
            else if (subItemIndex == 2 )
            {
                this.comEditor.DataSource = new string[] { "是", "否" };
                this.comEditor.Text = field.Key ? "是" : "否";
                this.comEditor.Location = new Point(currentSB.Bounds.Left + 2 + this.listView1.Left, currentSB.Bounds.Top + listView1.Top);
                this.comEditor.Size = currentSB.Bounds.Size;
                this.comEditor.Show();
                this.comEditor.Focus();
            }
            else if ( subItemIndex == 3 )
            {
                this.comEditor.DataSource = new string[] { "是", "否" };
                this.comEditor.Text = field.IsDBField ? "是" : "否";
                this.comEditor.Location = new Point(currentSB.Bounds.Left + 2 + this.listView1.Left, currentSB.Bounds.Top + listView1.Top);
                this.comEditor.Size = currentSB.Bounds.Size;
                this.comEditor.Show();
                this.comEditor.Focus();
            }
        }
        private void txtEditor_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
            {
                canceled = false;
                e.Handled = true;
                txtEditor.Hide();
            }
            else if (e.KeyChar == (char)Keys.Escape)
            {
                canceled = true;
                e.Handled = true;
                txtEditor.Hide();
            }
        }
        void txtEditor_LostFocus(object sender, EventArgs e)
        {
            try
            {
                this.txtEditor.Hide();
                this.comEditor.Hide();

                if (this.canceled)
                    return;

                Debug.Assert(this.currentitem != null);
                Debug.Assert(this.currentSB != null);

                if (this.currentitem == null || this.currentSB == null)
                    return;

                EntityField field = this.currentitem.Tag
                    as EntityField;

                int subItemIndex = currentitem.SubItems.IndexOf(currentSB);
                if (subItemIndex == 4)
                {
                    field.GridColumnName = this.txtEditor.Text.Trim();
                    currentSB.Text = field.GridColumnName;
                }
                else if (subItemIndex == 5)
                {
                    field.FieldDescription = this.txtEditor.Text.Trim();
                    currentSB.Text = field.FieldDescription;
                }

            }
            finally
            {
                this.currentSB = null;
                this.currentitem = null;
            }
        }
        Point pMouseDown = Point.Empty;
        private void listView1_MouseDown(object sender, MouseEventArgs e)
        {
            pMouseDown = new Point(e.X, e.Y);
        }
        private void listView1_BeforeLabelEdit(object sender, LabelEditEventArgs e)
        {
            this.txtEditor.Hide();
            this.comEditor.Hide();

            canceled = false;

            currentSB = null;
            currentitem = this.listView1.GetItemAt(pMouseDown.X, pMouseDown.Y);
            if (currentitem == null)
            {
                e.CancelEdit = true;
                return;
            }

            EntityField field = currentitem.Tag as EntityField;
            Debug.Assert(field != null);
            if (field == null)
            {
                e.CancelEdit = true;
                return;
            }

            currentSB = currentitem.GetSubItemAt(pMouseDown.X, pMouseDown.Y);
            if (currentSB == null)
            {
                e.CancelEdit = true;
                currentitem = null;
                return;
            }

            int subItemIndex = currentitem.SubItems.IndexOf(currentSB);
            if (subItemIndex == 0)
            {
                return;
            }
            else
            {
                e.CancelEdit = true;
                if (subItemIndex == 5)
                {
                    currentitem.Selected = false;
                    //启动文本编辑
                    this.txtEditor.Location = new Point(currentSB.Bounds.Left + 2 + this.listView1.Left, currentSB.Bounds.Top + listView1.Top);
                    this.txtEditor.Size = currentSB.Bounds.Size;
                    this.txtEditor.Text = currentSB.Text;
                    this.txtEditor.Show();
                    this.txtEditor.Focus();
                }
                if (subItemIndex == 4)
                {
                    currentitem.Selected = false;
                    //启动文本编辑
                    this.txtEditor.Location = new Point(currentSB.Bounds.Left + 2 + this.listView1.Left, currentSB.Bounds.Top + listView1.Top);
                    this.txtEditor.Size = currentSB.Bounds.Size;
                    this.txtEditor.Text = currentSB.Text;
                    this.txtEditor.Show();
                    this.txtEditor.Focus();
                }
                else if (subItemIndex == 1)
                {
                    //启动选择编辑功能:数据类型
                    this.comEditor.DataSource = EntityField.GetAllFieldDataType();
                    this.comEditor.Text = field.DataType.ToString();
                    this.comEditor.Location = new Point(currentSB.Bounds.Left + 2 + this.listView1.Left, currentSB.Bounds.Top + listView1.Top);
                    this.comEditor.Size = currentSB.Bounds.Size;
                    this.comEditor.Show();
                    this.comEditor.Focus();
                }
                else if (subItemIndex == 2)
                {
                    this.comEditor.DataSource = new string[] { "是", "否" };
                    this.comEditor.Text = field.Key ? "是" : "否";
                    this.comEditor.Location = new Point(currentSB.Bounds.Left + 2 + this.listView1.Left, currentSB.Bounds.Top + listView1.Top);
                    this.comEditor.Size = currentSB.Bounds.Size;
                    this.comEditor.Show();
                    this.comEditor.Focus();
                }
                else if ( subItemIndex == 3)
                {
                    this.comEditor.DataSource = new string[] { "是", "否" };
                    this.comEditor.Text = field.IsDBField ? "是" : "否";
                    this.comEditor.Location = new Point(currentSB.Bounds.Left + 2 + this.listView1.Left, currentSB.Bounds.Top + listView1.Top);
                    this.comEditor.Size = currentSB.Bounds.Size;
                    this.comEditor.Show();
                    this.comEditor.Focus();
                }
            }

        }

        private void comEditor_SelectionChangeCommitted(object sender, EventArgs e)
        {
            this.listView1.Focus();
        }

        private void comEditor_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!this.comEditor.Visible)
                return;

            commitDropDownEditor();

        }

        private void commitDropDownEditor()
        {
            Debug.Assert(this.currentitem != null);
            Debug.Assert(currentSB != null);

            if (this.currentitem == null || this.currentSB == null)
                return;

            EntityField field = currentitem.Tag as EntityField;
            Debug.Assert(field != null);
            if (field == null)
            {
                return;
            }

            int subItemIndex = currentitem.SubItems.IndexOf(currentSB);
            if (subItemIndex == 1)
            {
                if (currentSB.Text != this.comEditor.SelectedValue as string)
                {
                    field.DataType = (Keystone.AddIn.Entity.EntityField.FieldDataType)Enum.Parse(typeof(Keystone.AddIn.Entity.EntityField.FieldDataType), this.comEditor.SelectedValue as string);
                    currentSB.Text = this.comEditor.SelectedValue as string;
                    //this.updateData();
                }
            }
            else if (subItemIndex == 2)
            {
                if (currentSB.Text != this.comEditor.SelectedValue as string)
                {
                    field.Key = string.Compare(this.comEditor.SelectedValue as string, "是") == 0 ? true : false;
                    currentSB.Text = field.Key ? "是" : "否";

                    this.doBindFields();
                    //this.updateData();
                }
            }
            else if (subItemIndex == 3)
            {
                if (currentSB.Text != this.comEditor.SelectedValue as string)
                {
                    field.IsDBField = string.Compare(this.comEditor.SelectedValue as string, "是") == 0 ? true : false;
                    currentSB.Text = field.IsDBField ? "是" : "否";

                    this.doBindFields();
                    //this.updateData();
                }
            }
        }

        void comEditor_LostFocus(object sender, EventArgs e)
        {
            commitDropDownEditor();

            this.comEditor.Hide();
            this.txtEditor.Hide();
        }



        //private void updateData()
        //{
        //    this._entity.UpdateInitData();
        //    this.dataGridView1.DataSource = this._entity.Data;
        //}

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.btnAddNew.Visible = this.tabControl1.SelectedIndex == 0;
            this.btnDelete.Visible = this.tabControl1.SelectedIndex == 0;

            if (this.tabControl1.SelectedIndex == 1)
            {
                if (!this._entity.ValidateInitData() && MessageAlertHelper.ShowConfirmMsg("检测到初始化数据定义与字段定义有差异。您需要修正数据吗？"))
                {
                    this._entity.UpdateInitData();
                    this.dataGridView1.DataSource = this._entity.Data;
                }
            }

        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            SuperMCMService.PostMessage(new CopyEntityNodeCodeMsg(this._entity));
        }

        private void btnUp_Click(object sender, EventArgs e)
        {
            if (!HasChecked())
                return;

            Debug.Assert(this._entity != null);
            if (this._entity == null)
                return;

            bool action = false;
            foreach (ListViewItem item in this.listView1.Items)
            {
                Debug.Assert(item != null);
                if (item.Checked)
                {
                    EntityField field = item.Tag as EntityField;
                    int index = this._entity.Fields.IndexOf(field);
                    if (index == 0)
                        continue;

                    this._entity.Fields.RemoveAt(index);
                    this._entity.Fields.Insert(index - 1, field);
                    action = true;
                }
            }

            if (action)
            {
                this.doBindFields();
            }
        }

        private void btnDown_Click(object sender, EventArgs e)
        {
            if (!HasChecked())
                return;


            Debug.Assert(this._entity != null);
            if (this._entity == null)
                return;

            bool action = false;
            for (int i = this.listView1.Items.Count - 2; i >= 0; i--)
            {
                ListViewItem item = this.listView1.Items[i];
                if (item.Checked)
                {
                    EntityField field = item.Tag as EntityField;
                    int index = this._entity.Fields.IndexOf(field);
                    if (index == this.listView1.Items.Count-1)
                        continue;

                    this._entity.Fields.RemoveAt(index);
                    this._entity.Fields.Insert(index + 1, field);
                    action = true;
                }
            }

            if (action)
            {
                this.doBindFields();
            }
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            frmImportTalbe frm = new frmImportTalbe();
            if (frm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                EntityNode node = frm.SelectedEntity;
                if (node == null)
                    return;

                this._entity.Fields.AddRange(node.Fields.ToArray());
                this._entity.EntityName = node.EntityName;
                this.doBindData();
            }
        }

    }
}
