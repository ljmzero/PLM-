using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using plm_tools;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
namespace PLM信息导出
{
    public partial class Form1 : Form
    {
        private DataTable _bomTable; // 用于累计多个JSON的行数据

        public Form1()
        {
            InitializeComponent();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (comboBox1.Text == "")
            {
                MessageBox.Show("请选择导出类型！");
                return;
            }
            if (comboBox2.Text == "")
            {
                MessageBox.Show("请选择导出时间种类！");
                return;
            }



            string str = "";
            string where = "";
            if (comboBox1.Text == "导出料号")
            {
                 str = @"select 
					(select partdefine0256 from part_extend where part_id=(select top 1 part_id from part_base where part_code= A.part_code order by create_time DESC)) as 旧物料

                    ,A.part_code as 物料编码,A.part_name as 物料名称, REPLACE(REPLACE(REPLACE(A.spec, '@', ''), '_', ''), '无', '') as 规格,A.originalfig_id as 图号,
 
                     (case when A.kind='1' then '零件' 
 	                       when A.kind='2' then '部件' 
	                       when A.kind='3' then '总成' 
                           when A.kind='4' then '产品' 
 	                       when A.kind='5' then '原材料' end)
                      as 种类,B.partdefine0274 AS
                    中类代号,c.unit_name  AS 计量单位, B.partdefine0292 as 停用代号,  b.partdefine0273 as 仓位,
                    (case when a.source='01' then '自制件' 
                          when a.source='02' then '外购件' 
	                      when a.source='03' then '内购' 
	                      when a.source='04' then '待自制' 
	                      when a.source='05' then '虚拟件' 
	                      when a.source='06' then 'SMT' 
	                      when a.source='07' then '加工' 
	                      when a.source='08' then '托工' 
	                      when a.source='09' then '制板' 
	                      when a.source='10' then '组装' end) 
                     as 属性,
                    A.part_ver as 物料版本,B.partdefine0253 as 环保类别 ,B.partdefine0248 AS 物料状态,B.partdefine0263 as 重量
                    ,a.create_time as 创建时间,A.update_time AS  修改时间 ,
                     (case when A.part_state ='1' then '归档状态' 
 	                       when A.part_state ='0' then '设计状态' 
	                       when A.part_state ='3' then '申请状态' 
 	                       when A.part_state ='2' then '变更状态' end)
                    as PLM状态,
					  US.user_name AS '创建人'
                     from  part_base A join  sys_user US  on A.create_person = US.user_id,part_extend B,unit C where A.part_id=B.part_id  AND A.unit_id=C.unit_id 
                        ";

                if (comboBox2.Text == "修改时间")
                {
                    where += " and a.update_time>='" + dateTimePicker3.Value.ToString("yyyy-MM-dd HH:mm:ss") + "' and a.update_time<='" + dateTimePicker4.Value.ToString("yyyy-MM-dd HH:mm:ss") + "' and isnull(a.update_time,'')!=''";

                }
                else if (comboBox2.Text == "创建时间")
                {
                    where += " and a.create_time>='" + dateTimePicker1.Value.ToString("yyyy-MM-dd HH:mm:ss") + "' and a.create_time<='" + dateTimePicker2.Value.ToString("yyyy-MM-dd HH:mm:ss") + "' and isnull(a.create_time,'')!=''";
                }

            }
            else if (comboBox1.Text == "导出新增BOM")
            {
                str = @" WITH BOMList AS (
                                    SELECT 
                                        A.part_code,
                                        B.bom_id AS bom_id,
                                        A.part_ver,
                                        B.create_time,
                                        B.state,
                                        B.create_person,
		                                B.part_id
                                    FROM 
                                        part_base A
                                    JOIN 
                                        Dbom B ON A.part_id = B.part_id
                                )
                                SELECT 
                                    E.partdefine0256 AS 母件旧料号,
                                    BL.part_ver AS 母件版本,
                                    BL.part_code AS 母件代号,
                                    A.part_code AS 子件代号,
                                    A.part_ver AS 子件版本,
                                    C.amount AS 用量,
                                    C.dbomnodedefine0003 AS 基数,
                                    C.dbomnodedefine0005 AS 损耗率,
                                    ROW_NUMBER() OVER (PARTITION BY BL.part_code ORDER BY C.display_no) AS 序号,  -- 重新编号
                                    '' AS 制造部门,
                                    '' AS 帐套,
                                    '' AS 审核人名称,
                                    '' AS 审核人代号,
                                    C.location_number AS 组装位置,
                                    ex.partdefine0256 AS '子件旧物料',
                                    CASE 
                                        WHEN BL.state = '0' THEN '设计中' 
                                        WHEN BL.state = '2' THEN '归档'
                                        WHEN BL.state = '1' THEN '审批中'
                                    END AS 状态, 
	                                US.user_name AS '创建人',
                                    BL.create_time
                                FROM 
                                    BOMList BL
                                JOIN 
                                    dbom_node C ON BL.bom_id = C.bom_id
                                JOIN 
                                    part_base A ON C.part_id = A.part_id
                                JOIN 
                                    sys_user US ON BL.create_person = US.user_id
                                JOIN 
                                    part_extend ex ON A.part_id = ex.part_id
                                JOIN 
	                                part_extend E on BL.part_id=E.part_id
                    ";
                if (comboBox2.Text == "修改时间")
                {
                    where += @" and BL.update_time>='" + dateTimePicker3.Value.ToString("yyyy-MM-dd HH:mm:ss") + "' and BL.update_time<='" + dateTimePicker4.Value.ToString("yyyy-MM-dd HH:mm:ss") + "'" +
                           " and isnull(BL.create_time,'')!=''ORDER BY  BL.part_code,  -- 先按母件代号排序\r\n    C.display_no        -- 再按重新生成的序号排序 ";
                }
                else if (comboBox2.Text == "创建时间")
                {
                    where += @" and BL.create_time>='" + dateTimePicker1.Value.ToString("yyyy-MM-dd HH:mm:ss") + "' and BL.create_time<='" + dateTimePicker2.Value.ToString("yyyy-MM-dd HH:mm:ss") + "'" +
                        " and isnull(BL.create_time,'')!='' ORDER BY     BL.part_code,  -- 先按母件代号排序\r\n    C.display_no        -- 再按重新生成的序号排序";
                }
            }
            else if (comboBox1.Text == "导出物料变更")
            {
                str = @" SELECT
                     bg_type AS  '变更类型',
                     PRD_NO AS '旧料号',
                     NEW_PRD_NO AS '新老料号',
                     new_value AS '新值',
                     OLD_VALUE AS '旧值',
                     sys_date AS '日期'


                    FROM change_log where 1=1 ";
                if (comboBox2.Text == "修改时间")
                {
                    where += " and sys_date>='" + dateTimePicker3.Value.ToString("yyyy-MM-dd HH:mm:ss") + "' and sys_date<='" + dateTimePicker4.Value.ToString("yyyy-MM-dd HH:mm:ss") + "' and isnull(sys_date,'')!=''";
                }


            }
            else if (comboBox1.Text == "导出BOM变更")
            {
                str = @"SELECT 
                     BOM_ACTION AS  '变更类型',
                     BOM_NO AS '母件',
                     FieldName AS '变动字段名',
                     FieldValue AS '新值',
                  ITM AS '行号',
                     create_time AS '日期'


                    FROM plm_chg_log_tyt where 1=1 ";

                where = " and create_time>='" + dateTimePicker1.Value.ToString("yyyy-MM-dd HH:mm") + "' and create_time<='" + dateTimePicker2.Value.ToString("yyyy-MM-dd HH:mm") + "' ";


            }

            string sql = str + where;
            DataTable dt = plm_tools.ErpSqlServer.Query(sql).Tables[0];
            dataGridView1.DataSource = dt;

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count > 0)
            {
                plm_tools.ExportToExcel xls = new plm_tools.ExportToExcel();
                xls.OutputAsExcelFile(dataGridView1);

            }
            else
            {
                MessageBox.Show("没有数据，无法导出！");
            }

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            dataGridView1.DataSource = null;
            if (comboBox1.Text.ToString().Trim() == "导出BOM变更")
            {
                button2.Enabled = false;
                button3.Enabled = true;
                dateTimePicker3.Enabled = false;
                dateTimePicker4.Enabled = false;

            }
            else
            {
                button2.Enabled = true;
                button3.Enabled = false;
                dateTimePicker3.Enabled = true;
                dateTimePicker4.Enabled = true;
            }

        }

        private void button3_Click(object sender, EventArgs e)
        {
            button3.Enabled = false;

            if (comboBox1.Text != "导出BOM变更")
            {
                MessageBox.Show("请选择导出类型为BOM变更,然后类型以[创建时间]作为查询条件！");
                return;
            }
            else
            {
                dataGridView1.DataSource = null;//先清空
                _bomTable = null;
                var ak = "aa427c55de884b1abd7a22e817ca5597";
                var sk = "f4e8f019fab5ba858d7b77fea634abe0f819b18c96e12ac7a8ce053adbd29beb";

                //string ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                //string sign = SM4Helper.Sign(sk, ak, ts);

                StringBuilder sql = new StringBuilder();
                sql.Append("SELECT id,obj_id,new_obj_id,create_time FROM chg_obj A  WHERE A.obj_type='part'");
                sql.Append("  AND A.operate_type='1'  and a.create_time>='" + dateTimePicker1.Value.ToString("yyyy-MM-dd HH:mm") + "' and a.create_time<='" + dateTimePicker2.Value.ToString("yyyy-MM-dd HH:mm") + "' ");
                // sql.Append(" and a.id='1947956982308667392'");
                sql.Append(" order by a.create_time asc");
                DataTable dt = plm_tools.ErpSqlServer.Query(sql.ToString()).Tables[0];
                var plm_chg_url_server = string.Empty;
                foreach (DataRow dr in dt.Rows)
                {
                    var bom_id = dr["id"].ToString().Trim();
                    var obj_id = dr["obj_id"].ToString();
                    var before_ver = string.Empty;
                    var after_ver = string.Empty;
                    string createtime = dr["create_time"].ToString();
                    var new_obj_id = dr["new_obj_id"].ToString();
                    string ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                    string sign = SM4Helper.Sign(sk, ak, ts);

                    var plm_arr_comp_url = string.Format("http://192.168.0.21:7999/biz/chg/task/attrCompare?ak={0}&ts={1}&sign={2}&objId={3}&objType=part&newObjId={4}", ak, ts, sign, obj_id, new_obj_id);

                    var arrComp = HttpHelper.HttpGet(plm_arr_comp_url);
                    if (!string.IsNullOrEmpty(arrComp))
                    {
                        JObject root = JObject.Parse(arrComp);
                        if (root.ContainsKey("code") && root["code"].ToString() == "200")
                        {
                            var versionChange = ParseVersionChange(arrComp);
                            before_ver = versionChange.BeforeVersion; //变更前版本
                            after_ver = versionChange.AfterVersion;//变更后版本

                        }
                    }

                    //获取BOM新版本
                    var getPartCodeVersql = string.Format("SELECT part_code FROM part_base  WHERE part_id='{0}' and part_ver='{1}'", new_obj_id, after_ver);
                    var part_code = plm_tools.ErpSqlServer.SelectValue(getPartCodeVersql);//获取新版本物料号

                    //  #region BOM对比分析

                    ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                    sign = SM4Helper.Sign(sk, ak, ts);
                    plm_chg_url_server = string.Format("http://192.168.0.21:7999/biz/chg/task/bomCompare?ak={0}&ts={1}&sign={2}&id={3}", ak, ts, sign, bom_id);


                    string request = string.Empty;
                    using (var client = new System.Net.Http.HttpClient())
                    {

                        var response = client.GetAsync(plm_chg_url_server).Result;
                        request = response.Content.ReadAsStringAsync().Result;

                    }

                    if (!string.IsNullOrEmpty(request))
                    {

                        JObject root = JObject.Parse(request);
                        if (root.ContainsKey("code") && root["code"].ToString() == "200") //找到变更记录
                        {

                            json_format(request, part_code, after_ver, createtime);
                        }
                    }
                }

                MessageBox.Show("请求成功");
                button3.Enabled = true;
            }
        }


        /// <summary>
        /// PLM第三方系统获取token
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async Task<string> GetThirdPartyToken()
        {



            var ak = "aa427c55de884b1abd7a22e817ca5597";
            var sk = "f4e8f019fab5ba858d7b77fea634abe0f819b18c96e12ac7a8ce053adbd29beb";

            string ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            string reqfd = Guid.NewGuid().ToString("N"); // 生成UUID

            string userKey = "T00597"; // 替换为实际用户编码

            string API_URL = "http://192.168.0.84:7999/biz/third/app/genToken";



            string sign = SM4Helper.Sign(sk, ak, ts);
            // 2. 构建请求体
            var requestBody = new
            {
                ak = ak,
                ts = ts,
                reqfd = reqfd,
                sign = sign,
                userKey = userKey
            };

            string jsonBody = JsonConvert.SerializeObject(requestBody);



            // 3. 创建HTTP客户端
            using (var client = new HttpClient())
            {
                // 4. 设置请求头
                client.DefaultRequestHeaders.Add("version", "xzh");
                //client.DefaultRequestHeaders.Add("token", ""); // 根据实际情况填写
                //client.DefaultRequestHeaders.Add("error_stack", "true");
                //client.DefaultRequestHeaders.Add("dev_debug", "true");

                // 5. 发送POST请求
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(API_URL, content);

                // 6. 处理响应
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"HTTP错误: {response.StatusCode}");
                }

                string responseBody = await response.Content.ReadAsStringAsync();

                // 7. 解析响应数据（根据实际响应结构调整）
                var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseBody);
                return result?["token"] ?? throw new Exception("未找到token字段");
            }


        }




        public void json_format(string json, string ppart_code, string ppart_ver, string createtime)
        {


            // 解析并获取行数据
            var newRows = ParseJsonToRows(json, ppart_code, ppart_ver, ref _bomTable, createtime);
            foreach (var row in newRows)
            {
                _bomTable.Rows.Add(row);
            }

            // 首次绑定 DataGridView
            if (dataGridView1.DataSource == null)
            {
                dataGridView1.DataSource = _bomTable;

                // 设置中文列头
                if (_bomTable.ExtendedProperties["HeaderMapping"] is Dictionary<string, string> headerMap)
                {
                    dataGridView1.Columns["ppart_code"].HeaderText = "母件代号";
                    dataGridView1.Columns["ppart_ver"].HeaderText = "母件料号版本";
                    dataGridView1.Columns["createtime"].HeaderText = "BOM变更日期";

                    foreach (DataGridViewColumn col in dataGridView1.Columns)
                    {
                        if (headerMap.TryGetValue(col.Name, out var cn))
                            col.HeaderText = cn;
                    }
                }
            }

            dataGridView1.Refresh();
        }

        public List<DataRow> ParseJsonToRows(string json, string ppart_code, string ppart_ver, ref DataTable templateTable, string createtime)
        {
            var rowList = new List<DataRow>();

            var root = JsonConvert.DeserializeObject<RootObject>(json);
            if (root?.data == null)
                return rowList;

            var fieldDict = root.data.filedList.ToDictionary(f => f.attributeName, f => f.chineseName);

            // 如果第一次构建模板表
            if (templateTable == null)
            {
                templateTable = new DataTable();
                templateTable.Columns.Add("ppart_code", typeof(string));
                templateTable.Columns.Add("ppart_ver", typeof(string));
                templateTable.Columns.Add("createtime", typeof(string));

                foreach (var field in root.data.filedList)
                {
                    templateTable.Columns.Add(field.attributeName, typeof(string));
                }

                // 可选：将中文列名保存到 Tag 属性中，后续用来设置 DataGridView 表头
                templateTable.ExtendedProperties["HeaderMapping"] = fieldDict;
            }

            foreach (var row in root.data.dataList)
            {
                bool hasColor = row.Any(c => c.color != "0");
                if (!hasColor) continue;

                var dr = templateTable.NewRow();
                dr["ppart_code"] = ppart_code;
                dr["ppart_ver"] = ppart_ver;
                dr["createtime"] = createtime;
                foreach (var cell in row)
                {
                    if (templateTable.Columns.Contains(cell.field))
                        dr[cell.field] = cell.value;
                }

                // 检查是否是有效行
                bool allEmpty = true;
                for (int i = 2; i < templateTable.Columns.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(dr[i]?.ToString()))
                    {
                        allEmpty = false;
                        break;
                    }
                }
                if (!allEmpty &&!string.IsNullOrWhiteSpace(dr[5].ToString()))
                {
                        rowList.Add(dr);
                }
                    
            }

            return rowList;
        }


        public VersionChange ParseVersionChange(string jsonString)
        {
            JObject root = JObject.Parse(jsonString);
            JArray data = (JArray)root["data"];

            VersionChange result = new VersionChange();

            foreach (JObject item in data)
            {
                string attrName = item["attrName"]?.ToString();

                if (attrName == "版本")
                {
                    result.BeforeVersion = item["beforeValue"]?.ToString();
                    result.AfterVersion = item["afterValue"]?.ToString();
                    break;
                }
            }

            return result;
        }

    }


    public class VersionChange
    {
        public string BeforeVersion { get; set; }
        public string AfterVersion { get; set; }
    }
}


