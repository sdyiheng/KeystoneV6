using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PetaPoco;
using System.Dynamic;
using System.Collections;
using MessageEngine.Messages;
using MessageEngine;
using System.Reflection;
using System.IO;


namespace Keystone.AddIn.Entity
{
    internal partial class frmImportTalbe : Form
    {
        public frmImportTalbe()
        {
            InitializeComponent();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (this.tableList.SelectedIndex == -1)
                return;

            this.doQueryEntityFields(this.SelectedEntity);

            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Close();
        }

        string connectionString = string.Empty;
        private void btnQuery_Click(object sender, EventArgs e)
        {
            try
            {
                connectionString =
                    "Data Source=" + this.txtServer.Text +
                    ";Initial Catalog=" + this.txtDBName.Text +
                    ";User ID=" + this.txtUserName.Text +
                    ";Password=" + this.txtPwd.Text;

                List<EntityNode> entityList = new List<EntityNode>();
                using (var db = new PetaPoco.Database(connectionString, ""))
                {
                    EntityNode entity = null;
                    foreach (DynObj obj in db.Query("SELECT Name FROM  SysObjects Where XType='U' ORDER BY Name"))
                    {
                        string tableName = obj.GetPropertyValue("Name") as string;
                        entity = new EntityNode() {
                            EntityName = tableName, 
                            ID = Guid.NewGuid()
                        };

                        entityList.Add(entity);
                    }

                    this.tableList.DataSource = entityList.ToArray();

                }

                MyPrivateSetting.MySetting.SqlServer = this.txtServer.Text;
                MyPrivateSetting.MySetting.DBName = this.txtDBName.Text;
                MyPrivateSetting.MySetting.UserName = this.txtUserName.Text;
                MyPrivateSetting.MySetting.Pwd = this.txtPwd.Text;
                MyPrivateSetting.MySetting.SaveToDefaultConfigFile();
            }
            catch(Exception ex)
            {
                new ExceptionMessage(ex).PostMessage();
                return;
            }
        }

        private void doQueryEntityFields(EntityNode entity)
        {
            if (string.IsNullOrEmpty(connectionString))
                return;
            if (entity == null)
                return;

            string sqlModel = string.Empty;
            using (Stream stream = Assembly.GetExecutingAssembly().
                GetManifestResourceStream("AddIn.REAF.Entity.col.txt"))
            {
                using (StreamReader sr = new StreamReader(stream))
                    sqlModel = sr.ReadToEnd();
            }

            using (var db = new PetaPoco.Database(connectionString, ""))
            {
                foreach (DynObj col in db.Query("select * from syscolumns where id = object_id('" + entity.EntityName + "') "))
                {
                    EntityField f = new EntityField()
                    {
                        FieldName = col.GetPropertyValue("name") as string
                        ,
                        ID = Guid.NewGuid()
                        ,
                        IsDBField = true
                        ,
                        SystemDataType = DbTypeToTypeString.GetTypeString(Convert.ToInt32(col.GetPropertyValue("xtype")))
                    };
                    entity.Fields.Add(f);
                }


                string q = sqlModel;
                q = q.Replace("%TABLENAME%", entity.EntityName);
                foreach (DynObj col in db.Query(q))
                {
                    string colName = col.GetPropertyValue("col_name") as string;
                    int iskey = (int)col.GetPropertyValue("is_key");
                    if (iskey == 1)
                    {
                        entity.GetField(colName).Key = true;
                    }
                }
            }
        }

        //private void doQueryFields(object[] paras)
        //{
        //    string connectionString = paras[0] as string;
        //    EntityNode[] entityList = paras[1] as EntityNode[];

        //    using (var db = new PetaPoco.Database(connectionString, ""))
        //    {
        //        foreach (EntityNode entity in entityList)
        //        {
        //            foreach (DynObj col in db.Query("select * from syscolumns where id = object_id('" + entity.EntityName + "') "))
        //            {
        //                EntityField f = new EntityField()
        //                {
        //                    FieldName = col.GetPropertyValue("name") as string
        //                    ,
        //                    ID = Guid.NewGuid()
        //                    ,
        //                    IsDBField = true
        //                    ,
        //                    SystemDataType = DbTypeToTypeString.GetTypeString(Convert.ToInt32(col.GetPropertyValue("xtype")))
        //                };
        //                entity.Fields.Add(f);
        //            }
        //        }

        //        Dictionary<string, EntityField> dic = new Dictionary<string, EntityField>();
        //        foreach (EntityNode n in entityList)
        //        {
        //            foreach (EntityField f in n.Fields)
        //            {
        //                dic.Add(n.EntityName + "," + f.FieldName, f);
        //            }
        //        }


        //        string q = string.Empty;
        //        using (Stream stream = Assembly.GetExecutingAssembly().
        //            GetManifestResourceStream("EntityBuilder.col.txt"))
        //        {
        //            using (StreamReader sr = new StreamReader(stream))
        //                q = sr.ReadToEnd();
        //        }

        //        foreach (DynObj col in db.Query(q))
        //        {
        //            int iskey = (int)col.GetPropertyValue("is_key");
        //            if (iskey == 1)
        //            {
        //                string tablename = col.GetPropertyValue("tb_name") as string;
        //                string colName = col.GetPropertyValue("col_name") as string;
        //                dic[tablename + "," + colName].Key = true;
        //            }
        //        }

        //    }
        //}

        public class DbTypeToTypeString
        {
            static private Hashtable _mapping = new Hashtable();

            static DbTypeToTypeString()
            {
                Build();

            }
            static void Build()
            {


                Hashtable mapping = new Hashtable();
                Hashtable mappingNull = new Hashtable();


                mapping.Add(127, "System.Int64");        //BigInt;

                mapping.Add(241, "System.String");   //xml

                mapping.Add(56, "System.Int32");        //int;
                mapping.Add(52, "System.Int32");      //shortint
                mapping.Add(48, "System.Byte");      //tinyint

                mapping.Add(60, "System.Decimal");    //money
                mapping.Add(122, "System.Decimal");    //smallmoney
                mapping.Add(106, "System.Decimal");    //decimal

                mapping.Add(104, "System.Byte");      //bit

                mapping.Add(40, "System.DateTime");    //Date SQlServer 2008 Higher
                mapping.Add(58, "System.DateTime");    //SmallDateTime
                mapping.Add(61, "System.DateTime");    //DateTime

                mapping.Add(59, "System.Single");    //real

                mapping.Add(36, "System.Guid");    //uniqueidentifier

                mapping.Add(62, "System.Single");    //float

                //----
                mapping.Add(35, "System.String");    //text

                mapping.Add(189, "System.Byte[]");    //timestamp

                mapping.Add(99, "System.String");    //ntext

                mapping.Add(239, "System.String");    //nchar

                mapping.Add(175, "System.String");    //char

                mapping.Add(34, "System.Byte[]");    //image

                mapping.Add(165, "System.Byte[]");    //varbinary

                mapping.Add(231, "System.String");    //nvarchar

                mapping.Add(167, "System.String");    //varchar 'ddd' as name

                _mapping = mapping;
            }

            public static string GetTypeString(int dbtypeid)
            {
                return (string)_mapping[dbtypeid];
            }
        }

        private void frmImportTalbe_Load(object sender, EventArgs e)
        {
            this.txtServer.Text = MyPrivateSetting.MySetting.SqlServer;
            this.txtDBName.Text = MyPrivateSetting.MySetting.DBName;
            this.txtUserName.Text = MyPrivateSetting.MySetting.UserName;
            this.txtPwd.Text = MyPrivateSetting.MySetting.Pwd;

            if (!string.IsNullOrEmpty(this.txtServer.Text)
                && !string.IsNullOrEmpty(this.txtDBName.Text)
                && !string.IsNullOrEmpty(this.txtUserName.Text)
            )
            {
                this.btnQuery.PerformClick();
            }
        }

        internal EntityNode SelectedEntity
        {
            get
            {
                if (this.tableList.SelectedValue == null)
                    return null;

                return this.tableList.SelectedValue as EntityNode;
            }
        }
    }
}
