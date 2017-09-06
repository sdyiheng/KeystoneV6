using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Drawing;
using Keystone.Common.Utility;
using System.Diagnostics;

namespace Keystone.AddIn.Entity
{
    [Serializable]
    [System.Reflection.ObfuscationAttribute(Feature = "renaming", ApplyToMembers = true)]
    public class EntityNode //: INode
    {
        public EntityNode()
        {
            this.ID = Guid.NewGuid();
            this.Fields = new List<EntityField>();
        }

        public Guid ID { get; set; }

        public override string ToString()
        {
            return this.EntityName;
        }
 
        /// <summary>
        /// 实体名称
        /// </summary>
        public string EntityName { get; set; }
        public string EntityNameCH { get; set; }

        /// <summary>
        /// 实体描述
        /// </summary>
        public string EntityDescription { get; set; }

        public string Namespace { get; set; }

        /// <summary>
        /// 字段列表
        /// </summary>
        public List<EntityField> Fields { get; set; }

        public EntityField[] GetKeyFields()
        {
            List<EntityField> list = new List<EntityField>();
            foreach (EntityField field in this.Fields.ToArray())
            {
                if (field.Key)
                    list.Add(field);
            }
            return list.ToArray();
        }

        public EntityField[] GetNonKeyFields()
        {
            List<EntityField> list = new List<EntityField>();
            foreach (EntityField field in this.Fields.ToArray())
            {
                if (!field.Key)
                    list.Add(field);
            }
            return list.ToArray();
        }

        public EntityField[] GetValidFieds()
        {

            List<EntityField> list = new List<EntityField>();
            if (this.Fields != null)
            {
                foreach (EntityField f in this.Fields)
                {
                    if (f.IsValid)
                        list.Add(f);
                }
            }
            return list.ToArray();
        }

        public string PrimaryKeys
        {
            get
            {
                EntityField[] fs = this.GetKeyFields();
                if (fs == null || fs.Length == 0)
                    return string.Empty;

                if (fs.Length == 1)
                    return fs[0].FieldName;

                return string.Join(",", from f in fs select f.FieldName );
                //string k = "";
                //foreach (EntityField f in fs)
                //{
                //    if (k == null)
                //        k = f.FieldName;
                //    else
                //        k += "," + f.FieldName;
                //}
                //return k;
            }
        }

        /// <summary>
        /// 数据
        /// </summary>
        public DataTable Data { get; set; }

        public EntityField GetField(string fieldName)
        {
            foreach (EntityField f in this.Fields.ToArray())
                if (f.FieldName == fieldName)
                    return f;
            return null;
        }



        public Bitmap BuildEnitytImage()
        {
            return null;
        }

        public string GetNodeViewTextValue()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("").Append(this.EntityName);
            if (!string.IsNullOrEmpty(this.EntityNameCH))
                sb.Append("(").Append(this.EntityNameCH).Append(")");
            sb.AppendLine().AppendLine("----[Fields]----");
            foreach (EntityField field in this.GetKeyFields())
                sb.AppendLine(field.ToString());
            foreach (EntityField field in this.GetNonKeyFields())
                sb.AppendLine(field.ToString());
            return sb.ToString();
        }

        public void UpdateInitData()
        {
            DataTable dt = this.buildDataTableSchema();
            if (dt == null)
                return;

            if (this.Data == null)
            {
                this.Data = dt;
                return;
            }

            this.tryToImprtData(this.Data, dt);

            this.Data = dt;
        }

        private void tryToImprtData(DataTable from, DataTable to)
        {
            foreach (DataRow dr in from.Rows)
            {
                List<object> fieldValues = new List<object>();
                foreach (DataColumn col in to.Columns)
                {
                    if (from.Columns.Contains(col.ColumnName) && from.Columns[col.ColumnName].DataType == col.DataType )
                    {
                        //列名称相同，类型相同
                        fieldValues.Add(dr[col.ColumnName]);
                    }
                    else
                    {
                        DataColumn targetCol = null;
                        foreach (DataColumn fromCol in from.Columns)
                        {
                            if (fromCol.Caption == col.Caption)
                            {
                                targetCol = fromCol;
                                break;
                            }
                        }
                        if (targetCol != null && targetCol.DataType == col.DataType)
                        {
                            fieldValues.Add(dr[targetCol.ColumnName]);
                        }
                        else
                        {
                            EntityField f = this.GetField(col.ColumnName);
                            Debug.Assert(f != null);
                            fieldValues.Add(f == null ? null : f.GetSystemTypeDefaultValue());
                        }
                    }
                }
                DataTableBuilderHelper.drwAddRow(to, fieldValues.ToArray());
            }
        }

        public bool ValidateInitData()
        {
            DataTable dt = this.buildDataTableSchema();
            if (dt == null)
                return true;

            if (this.Data == null)
            {
                this.Data = dt;
                return true;
            }

            if (this.Data.Columns.Count != dt.Columns.Count)
                return false;

            int index = -1;
            foreach (DataColumn colNew in dt.Columns  )
            {
                index++;

                //if (!this.Data.Columns.Contains(colNew.ColumnName))
                //    return false;

                DataColumn col = this.Data.Columns[index];
                if (col.ColumnName != colNew.ColumnName)
                    return false;

                if (col.DataType != colNew.DataType)
                    return false;

                col.Caption = colNew.Caption;
            }
            //EntityField[] keyFields = this.GetKeyFields();


            ////主键判断
            //if (keyFields.Length == 0)
            //{
            //    if (this.Data.PrimaryKey != null && this.Data.PrimaryKey.Length > 0)
            //        return false;
            //}
            //else
            //{
            //    if (this.Data.PrimaryKey == null || this.Data.PrimaryKey.Length != keyFields.Length)
            //        return false;

            //    foreach (EntityField f in keyFields)
            //    {
            //        IEnumerable<DataColumn> cols = from col in this.Data.PrimaryKey where col.ColumnName == f.FieldName select col;
            //        if (cols.Count() != 1)
            //            return false;
            //    }
            //}


            return true;
        }

        private DataTable buildDataTableSchema()
        {
            if (this.Fields.Count == 0)
                return null;

            DataColumn[] cols = this.buildDataColumn();
            if (cols == null || cols.Length == 0)
                return null;

            return DataTableBuilderHelper.tblBuildTable("tblInitData", cols, null);//getIndexColumns()
        }
        private DataColumn[] buildDataColumn()
        {
            Dictionary<string, DataColumn> dic = new Dictionary<string, DataColumn>(StringComparer.OrdinalIgnoreCase );
            List<DataColumn> cols = new List<DataColumn>();
            foreach( EntityField f in this.Fields )
            {
                string colDataType = f.SystemDataType;
                if (string.IsNullOrEmpty(colDataType))
                    continue;

                if (string.IsNullOrEmpty(f.FieldName))
                    continue;

                if (dic.ContainsKey (f.FieldName))
                    continue;

                DataColumn col = DataTableBuilderHelper.dclBuildDataColumn(f.FieldName, f.ID.ToString(), colDataType);
                Debug.Assert(col != null);
                if (col != null)
                {
                    cols.Add(col);
                    dic.Add(f.FieldName, col);
                }
            }
            return cols.ToArray();
        }
        //private string[] getIndexColumns()
        //{
        //    List<string> cols = new List<string>();
        //    foreach (EntityField f in this.Fields)
        //    {
        //        if (f.Key)
        //            cols.Add(f.FieldName);
        //    }
        //    return cols.ToArray();
        //}
    }
}
