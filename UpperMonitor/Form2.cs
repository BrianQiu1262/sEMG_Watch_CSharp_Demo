using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace UpperMonitor
{
    
    public partial class Form2 : Form
    {
        string path = Application.StartupPath;
        public Form2()
        {
            InitializeComponent();

            //数据保存地址
            Directory.SetCurrentDirectory(Directory.GetParent(path).FullName);
            path = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(Directory.GetParent(path).FullName);
            path = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(Directory.GetParent(path).FullName);
            path = Directory.GetCurrentDirectory() + @"\DATA\";
        }

        public delegate void TextEventHandler(string strText);

        public TextEventHandler TextHandler;

        //保存数据
        private void button1_Click(object sender, EventArgs e)
        {
            int file_name_bug_flag = -1;
            string input = textBox1.Text;

            DirectoryInfo folder = new DirectoryInfo(path);

            if (input == string.Empty)
            {
                file_name_bug_flag = 1;
                MessageBox.Show("This file name can not be empty!", "Warning");
            }
            else
            {
                file_name_bug_flag = 0;
            }
            
            foreach (FileInfo file in folder.GetFiles("*.csv"))
            {
                if (file.Name == (input+".csv"))
                {
                    MessageBox.Show("This file name has been used!", "Warning");
                    file_name_bug_flag = 1;
                    break;
                }
            }

            if (null != TextHandler && file_name_bug_flag == 0)
            {
                TextHandler.Invoke(input);
                DialogResult = DialogResult.OK;
            }
        }

        //取消保存
        private void button2_Click(object sender, EventArgs e)
        {
            string input = string.Empty;
            TextHandler.Invoke(input);
            DialogResult = DialogResult.OK;
        }
    }
}
