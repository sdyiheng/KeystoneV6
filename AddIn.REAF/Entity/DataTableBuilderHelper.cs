using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Diagnostics;

namespace Keystone.AddIn.Entity
{
    class DataTableBuilderHelper
    {
        public static DataColumn dclBuildDataColumn(string strName, string strCaption, string strType)
        {
            #region build a datacolumn object
            System.Type aType = System.Type.GetType(strType);
            Debug.Assert(aType != null);
            if (aType == null) return null;

            DataColumn aColumn = new DataColumn(strName, aType);
            aColumn.ReadOnly = false;
            aColumn.Caption = strCaption;
            #endregion
            return aColumn;
        }
        public static DataTable tblBuildTable(string strTableName, DataColumn[] dclA, string[] strIndexs)
        {
            #region build table with an auto-incrementing index if strIndex defined
            DataTable tblNew = new DataTable(strTableName);
            tblNew.Columns.AddRange(dclA);
            if (strIndexs != null && strIndexs.Length > 0)
            {
                List<DataColumn> indexCols = new List<DataColumn>();
                foreach (string strIndex in strIndexs)
                {
                    // set the index field as unique, no null allowed in prep for autocount field
                    try
                    {
                        DataColumn dclIndex = tblNew.Columns[strIndex];
                        dclIndex.AllowDBNull = false;
                        dclIndex.Unique = true;
                        dclIndex.AutoIncrement = true;
                        dclIndex.AutoIncrementSeed = 1;
                        dclIndex.AutoIncrementStep = 1;

                        indexCols.Add(dclIndex);
                    }
                    catch
                    {
                    }
                }

                if (indexCols.Count > 0)
                    tblNew.PrimaryKey = indexCols.ToArray();
            }
            #endregion
            return tblNew;
        }
        public static DataRow drwAddRow(DataTable tblTable, object[] objA)
        {
            Debug.Assert(objA.Length == tblTable.Columns.Count);

            #region build a row from the provided object array with auto-alignment of primary keys
            DataRow drwRow = tblTable.NewRow();
            // check for primary key position in table
            if (tblTable.PrimaryKey != null && tblTable.PrimaryKey.Length > 0)
            {
                DataColumn dcKey = tblTable.PrimaryKey[0];
                if (dcKey.AutoIncrement == true)
                {
                    int idx = tblTable.Columns.IndexOf(dcKey.ColumnName);
                    objA[idx] = drwRow.ItemArray[idx];
                }
            }
            drwRow.ItemArray = objA;
            tblTable.Rows.Add(drwRow);
            #endregion
            return drwRow;
        }

    }
}
