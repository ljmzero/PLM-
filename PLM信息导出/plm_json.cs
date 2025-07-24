using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLM信息导出
{
    public class FieldItem
    {
        public string attributeSource { get; set; }
        public string attributeName { get; set; }
        public string chineseName { get; set; }
    }

    public class CellItem
    {
        public string value { get; set; }
        public string field { get; set; }
        public string color { get; set; }
        public string line { get; set; }
    }

    public class ResponseData
    {
        public List<List<CellItem>> dataList { get; set; }
        public List<FieldItem> filedList { get; set; }
    }

    public class RootObject
    {
        public int code { get; set; }
        public ResponseData data { get; set; }
        public string message { get; set; }
    }

}
