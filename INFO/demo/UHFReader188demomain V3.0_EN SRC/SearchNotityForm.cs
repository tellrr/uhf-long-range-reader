using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace UHFReader188demomain
{
    public partial class SearchNotityForm : Form
    {
        public SearchNotityForm()
        {
            InitializeComponent();
        }

        int Count = 0;
        private void SearchNotityForm_Load(object sender, EventArgs e)
        {
            Count = 0;
            progressBar1.Value = 0;
            timer1.Enabled = true;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            progressBar1.Value += 10;
            if (progressBar1.Value>=100 || Form1.mthread==null)
            {
                timer1.Enabled = false;
                this.Close();
            }
        }
    }
}
