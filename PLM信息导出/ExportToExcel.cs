using System;
using System.Collections;
using System.Text;
using System.Data;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;
//using Microsoft.Office.Core;
//using Excel = Microsoft.Office.Interop.Excel;
using Spire.Xls;
using System.Drawing;
namespace plm_tools
{
    public class ExportToExcel
    {
        // public Excel.Application m_xlApp = null;

        public void OutputAsExcelFile(DataGridView dataGridView)
        {


            if (dataGridView.Rows.Count <= 0)
            {
                MessageBox.Show("无数据！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning); return;
            }
            string filePath = "";
            SaveFileDialog s = new SaveFileDialog();
            s.Title = "保存Excel文件";
            s.Filter = "Excel文件(*.xls)|*.xls";
            s.FilterIndex = 1;
            if (s.ShowDialog() == DialogResult.OK)
                filePath = s.FileName;
            else
                return;



            Workbook WB = new Workbook();
            Worksheet table = WB.Worksheets[0];

            table.Name = "导出->" + DateTime.Now.ToShortDateString();

            DataTable dt = GetDgvToTable(dataGridView);

            table.InsertDataTable(dt, true, 1, 1, -1, -1);
            table.Range[1, 1, table.LastRow, table.LastColumn].AutoFitColumns();
            table.AllocatedRange.BorderAround(LineStyleType.Thin, borderColor: ExcelColors.Black);
            table.AllocatedRange.BorderInside(LineStyleType.Thin, borderColor: ExcelColors.Black);


            //文字水平垂直对齐中心
            table.Range[table.FirstRow, table.FirstColumn, table.LastRow, table.LastColumn].Style.HorizontalAlignment = HorizontalAlignType.Center;  //水平对齐
            table.Range[table.FirstRow, table.FirstColumn, table.LastRow, table.LastColumn].Style.VerticalAlignment = VerticalAlignType.Center;  //垂直对齐


            try
            {
                WB.SaveToFile(filePath, ExcelVersion.Version97to2003);
                MessageBox.Show("导出成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("导出失败，原因：！" + ex.Message.ToString(), "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning); return;
            }

        }



        /// <summary>
        /// DataGridView 转DataTable
        /// </summary>
        /// <param name="dgv"></param>
        /// <returns></returns>
        public DataTable GetDgvToTable(DataGridView dgv)
        {
            DataTable dt = new DataTable();

            //Column
            for (int count = 0; count < dgv.Columns.Count; count++)
            {
                DataColumn dc = new DataColumn(dgv.Columns[count].HeaderText);
                dt.Columns.Add(dc);
            }

            //Row
            for (int count = 0; count < dgv.Rows.Count; count++)
            {
                DataRow dr = dt.NewRow();
                for (int countsub = 0; countsub < dgv.Columns.Count; countsub++)
                //for (int countsub = 0; countsub < dgv.SelectedRows.Count; countsub++)
                {
                    dr[countsub] = Convert.ToString(dgv.Rows[count].Cells[countsub].Value);
                }
                dt.Rows.Add(dr);
            }

            return dt;
        }
    }
}