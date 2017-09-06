using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Keystone.AddIn.Entity
{
    [Serializable]
    [System.Reflection.ObfuscationAttribute(Feature = "renaming", ApplyToMembers = true)]
    public class EntityField
    {
        public enum FieldDataType
        {
            None,
            Text,
            TinyText,
            LargeText,
            Int32,
            Int64,
            Float,
            Double,
            Decimal,
            Bytes,
            Byte,
            Guid,
            //DateTime,
        }

        private static string[] allFieldDataType = null;
        public static string[] GetAllFieldDataType()
        {
            if (allFieldDataType == null)
            {
                allFieldDataType = Enum.GetNames(typeof(FieldDataType));
            }

            return allFieldDataType;
        }

        public EntityField()
        {
            this.ID = Guid.NewGuid();
            this.FieldName = "NewField";
            IsDBField = true;
            this.DefaultValue = string.Empty;
        }

        public Guid ID { get; set; }

        public string FieldName { get; set; }
        public FieldDataType DataType { get; set; }

        public string SystemDataType
        {
            get
            {
                switch (this.DataType)
                {
                    //case FieldDataType.DateTime:
                    //    return "System.DateTime";
                    case FieldDataType.Guid:
                        return "System.String";
                    case FieldDataType.Byte:
                        return "System.Byte";
                    case FieldDataType.Bytes:
                        return "System.Byte[]";
                    case FieldDataType.Decimal:
                        return "System.Decimal";
                    case FieldDataType.Double:
                        return "System.Double";
                    case FieldDataType.Float:
                        return "System.Single";
                    case FieldDataType.Int64:
                        return "System.Int64";
                    case FieldDataType.Int32:
                        return "System.Int32";
                    case FieldDataType.Text:
                    case FieldDataType.TinyText:
                    case FieldDataType.LargeText:
                        return "System.String";
                    case FieldDataType.None:
                    default:
                        return string.Empty;
                }
            }
            set
            {
                switch (value)
                {
                    //case "System.DateTime":
                    //    this.DataType = FieldDataType.DateTime;return;
                    case "System.Guid"://:
                        this.DataType = FieldDataType.Guid; return;
                    case "System.Byte"://:
                        this.DataType = FieldDataType.Byte; return;
                    case "System.Byte[]":// :
                        this.DataType = FieldDataType.Bytes; return;
                    case "System.Decimal"://:
                        this.DataType = FieldDataType.Decimal; return;
                    case "System.Double"://:
                        this.DataType = FieldDataType.Double; return;
                    case "System.Single"://:
                        this.DataType = FieldDataType.Float; return;
                    case "System.Int64"://:
                        this.DataType = FieldDataType.Int64; return;
                    case "System.Int32"://:
                        this.DataType = FieldDataType.Int32; return;
                    case "System.String":
                        this.DataType = FieldDataType.Text; return;
                    default:
                        this.DataType = FieldDataType.None;
                        return;
                }
            }
        }
        public string SqlDataType
        {
            get
            {
                switch (this.DataType)
                {
                    //case FieldDataType.DateTime:
                    //    return "[bigint]";
                    case FieldDataType.Guid:
                        return "[char](36)";
                    case FieldDataType.Byte:
                        return "[smallint]";
                    case FieldDataType.Bytes:
                        return "[varbinary](8000)";
                    case FieldDataType.Decimal:
                        return "[decimal](18, 6)";
                    case FieldDataType.Double:
                        return "[real]";
                    case FieldDataType.Float:
                        return "[float]";
                    case FieldDataType.Int64:
                        return "[bigint]";
                    case FieldDataType.Int32:
                        return "[int]";
                    case FieldDataType.Text:
                        return "[nvarchar](1000)";
                    case FieldDataType.TinyText:
                        return "[nvarchar](100)";
                    case FieldDataType.LargeText:
                        return "[nvarchar](4000)";
                    case FieldDataType.None:
                    default:
                        return string.Empty;
                }
            }
        }
        public object GetSystemTypeDefaultValue()
        {
            switch (this.DataType)
            {
                //case FieldDataType.DateTime:
                //    {
                //        if (string.IsNullOrEmpty(this.DefaultValue))
                //            return DateTime.MinValue;
                //        else
                //        {
                //            DateTime v = DateTime.MinValue;
                //            if (DateTime.TryParse(this.DefaultValue, out v))
                //                return v;
                //            else
                //                return DateTime.MinValue;
                //        }
                //    }
                case FieldDataType.Guid:
                    {
                        if (string.IsNullOrEmpty(this.DefaultValue))
                            return Guid.Empty;
                        else
                        {
                            Guid v = Guid.Empty;
                            if (Guid.TryParse(this.DefaultValue, out v))
                                return v;
                            else
                                return Guid.Empty;
                        }
                    }
                case FieldDataType.Byte:
                    {
                        if (string.IsNullOrEmpty(this.DefaultValue))
                            return 0;
                        else
                        {
                            byte v = 0;
                            if (byte.TryParse(this.DefaultValue, out v))
                                return v;
                            else
                                return 0;
                        }
                    }
                case FieldDataType.Bytes:
                    {
                        return null;
                    }
                case FieldDataType.Decimal:
                    {
                        if (string.IsNullOrEmpty(this.DefaultValue))
                            return 0;
                        else
                        {
                            decimal  v = 0;
                            if (decimal.TryParse(this.DefaultValue, out v))
                                return v;
                            else
                                return 0;
                        }
                    }
                case FieldDataType.Double:
                    {
                        if (string.IsNullOrEmpty(this.DefaultValue))
                            return 0;
                        else
                        {
                            double v = 0;
                            if (double.TryParse(this.DefaultValue, out v))
                                return v;
                            else
                                return 0;
                        }
                    }
                case FieldDataType.Float:
                    {
                        if (string.IsNullOrEmpty(this.DefaultValue))
                            return 0;
                        else
                        {
                            float v = 0;
                            if (float.TryParse(this.DefaultValue, out v))
                                return v;
                            else
                                return 0;
                        }
                    }
                case FieldDataType.Int64:
                    {
                        if (string.IsNullOrEmpty(this.DefaultValue))
                            return 0;
                        else
                        {
                            long v = 0;
                            if (long.TryParse(this.DefaultValue, out v))
                                return v;
                            else
                                return 0;
                        }
                    }
                case FieldDataType.Int32:
                    {
                        if (string.IsNullOrEmpty(this.DefaultValue))
                            return 0;
                        else
                        {
                            int v = 0;
                            if (int.TryParse(this.DefaultValue, out v))
                                return v;
                            else
                                return 0; 
                        }
                    }
                case FieldDataType.Text:
                case FieldDataType.TinyText:
                case FieldDataType.LargeText:
                    return DefaultValue == null ? "" : DefaultValue;
                case FieldDataType.None:
                default:
                    return null;
            }
        }
        public string FieldDescription { get; set; }
        public bool Key { get; set; }
        public string DefaultValue { get; set; }
        public bool IsDBField { get; set; }
        public string GridColumnName { get; set; }

        public bool IsValid
        {
            get
            {
                return !string.IsNullOrEmpty(this.FieldName) && this.DataType != FieldDataType.None;
            }
        }

        public override string ToString()
        {
            string desc = this.FieldDescription;
            if (string.IsNullOrEmpty(desc))
                desc = this.GridColumnName;
            return string.Format("{0}{1}：{2}{3}{4}{5}"
                , this.Key ? "[K]" : ""
                , (string.IsNullOrEmpty(this.FieldName) ? "[NULL]" : this.FieldName)
                , this.DataType
                , string.IsNullOrEmpty(desc) ? "" : " ("
                , desc
                , string.IsNullOrEmpty(desc) ? "" : ")"
                );
        }
    }
}
