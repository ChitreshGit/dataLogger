using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.IO.Ports;


namespace adcUartBar
{
    public partial class Form1 : Form
    {
        ADC_class adcObj = new ADC_class();  // One ADC channel
        DB_class myDB;
        string DB_NAME = "TestDB2";
        string db_currentTable = "TestTable";
        int oldAdcValue = 0;


        static int count = 0;
        // UART initialize
        SerialPort ComPort = new SerialPort();


        /* MAIN code */
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Initialize the timer for automaticaly getting
            // the latest ADC value from UART
            timer1.Enabled = true;
            chart1.Series["Series1"].Points.AddXY("ADC1", "1024");
            timer1.Stop();
            
            // UART List box update
            {
                string[] ArrayComPortsNames = new string[] { "NONE", null, null, null };

                ArrayComPortsNames = SerialPort.GetPortNames();

                listBox1.Size = new System.Drawing.Size(140, 35);

                int numComPorts = ArrayComPortsNames.GetUpperBound(0)+1;
                if (numComPorts > 0) // check for no COM ports
                {
                    listBox1.BeginUpdate();
                    for (int i = 0; i < numComPorts; i++)
                    {
                        listBox1.Items.Add(ArrayComPortsNames[i]);
                    }
                    listBox1.EndUpdate();
                }
                else
                {
                    listBox1.BeginUpdate();
                    listBox1.Items.Add("NONE");
                    listBox1.EndUpdate();
                }
            
                listBox1.Visible = true;
            }
            // Connect DB
            myDB = new DB_class(DB_NAME);

            if (myDB != null)
            {
                // Set properties on FORM
                this.textBox2.Text = Convert.ToString(myDB.maxColumns);
                this.textBox1.Text = Convert.ToString(myDB.currentColumn);
                if (myDB.SearchTable(db_currentTable) == true)  // Table not present?
                {
                    // It can be looped for more than 1 table
                    //this.domainUpDown1.Items.Add(myDB.tableList.Rows[myDB.DB_DEFAULT_TABLE_COUNT].ItemArray[2]);
                    this.domainUpDown1.Items.Add(myDB.tableList[0]);
                    this.domainUpDown1.SelectedIndex = 1;
                    db_currentTable = domainUpDown1.SelectedItem.ToString();
                }
                else
                {   // Create table
                    myDB.CreateTable(db_currentTable);
                    this.domainUpDown1.Items.Add(db_currentTable);
                    //myDB.UpdateTableList();
                    myDB.CreateADCColumn(db_currentTable, myDB.currentColumn.ToString());
                }
                // Now the table is create or exist
                // We need to update the table list here
                myDB.UpdateColumnList(db_currentTable);

                // Search if unique rowID is present or not

                if (myDB.SearchColumn(db_currentTable, "Num") == false)
                    myDB.CreateColumnGeneral(db_currentTable, "Num");
            }
            else
            {
                // issue to aonnect to DB
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            // Read ADC from COM port
            if (adcObj.Get_ADC_Values(ComPort, ref adcObj.adc) == false)
                adcObj.adc[0] = oldAdcValue;

            if (myDB.isCapturing == true)
            {
                myDB.ADCCapture(db_currentTable, adcObj.adc[0]); 
            }
            else
            {
                // Release form lock when capture is complete
                domainUpDown1.Enabled = true;
                textBox1.Enabled = true;
            }

             oldAdcValue = adcObj.adc[0];

            // Update graph
            if (count > 100)
                count = 0;

            chart1.Series["Series1"].Points.RemoveAt(chart1.Series["Series1"].Points.Count-1);
            // here the chart will be updated
            // simulate chart1.Series["Series1"].Points.AddXY("ADC1", Convert.ToString(count));

            chart1.Series["Series1"].Points.AddXY("ADC1", Convert.ToString(adcObj.adc[0]));

            count++;
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {
           
        }

        private void button1_MouseClick(object sender, MouseEventArgs e)
        {
           
            if ((listBox1.Items.Count > 0) && (listBox1.SelectedIndex >= 0))
            {
                ComPort.PortName = listBox1.SelectedItem.ToString();
                if (ComPort.PortName != "NONE")
                {
                    ComPort.BaudRate = 9600;
                    ComPort.DataBits = 8;
                    ComPort.Parity = 0;
                    ComPort.StopBits = (StopBits)1;
                    ComPort.ReadTimeout = 10;
                    // open comp port
                    ComPort.Open();
                    // Set DB parameters
                    // Set properties on FORM
                    this.textBox2.Text = Convert.ToString(myDB.maxColumns);
                    this.textBox1.Text = Convert.ToString(myDB.currentColumn);
                    // Hide menu view
                    //groupBox1.Visible = false;
                    groupBox1.SendToBack();
                    groupBox1.Hide(); 
                    // Start timer
                    timer1.Start();
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Set DB capture flag
            myDB.isCapturing = true;
            // update the current capture data
            textBox1.Text = myDB.currentColumn.ToString();
            textBox2.Text = myDB.maxColumns.ToString();
            // Lock the from controls and release when capture is done
            domainUpDown1.Enabled = false;
            textBox1.Enabled = false;

        }

        private void domainUpDown1_SelectedItemChanged(object sender, EventArgs e)
        {
            // later to select among tables
            // Update column list
        }

        private void btnConfig_Click(object sender, EventArgs e)
        {
            // Set the column number. 
            // Create new column if required.
            if (textBox1.Text != "")
            {
                int columnNum = Convert.ToInt32(textBox1.Text);
                if (columnNum < myDB.maxColumns)
                {
                    myDB.currentColumn = columnNum;
                }
                else if (columnNum > myDB.maxColumns)
                {
                    //textBox1.Text = myDB.currentColumn.ToString();
                }
                else
                {  // Create new column in table
                    if (myDB.CreateADCColumn(db_currentTable, Convert.ToString(columnNum)) == true)
                    {
                        myDB.currentColumn = columnNum;
                    }
                }
                textBox1.Text = myDB.currentColumn.ToString();
                textBox2.Text = myDB.maxColumns.ToString();
            }
        }
    }
}
