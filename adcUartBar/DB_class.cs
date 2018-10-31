using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data.SqlClient;
using System.Data;
using System.IO;

namespace adcUartBar
{
    class DB_class
    {
        const int MAX_CAPTURE = 200; // this will hold the maximaum adc values we store for a movement
        public int captureCount = 0;
        public SqlConnection dbConnection;
        public int maxColumns = 0;
        public int currentColumn = 0;
        DataTable tableListStruct = null;
        DataTable columnListStruct = null;
        public List<string> tableList = new List<string>();
        public List<string> columnList = new List<string>();
        public bool isCapturing = false;
        public int DB_DEFAULT_TABLE_COUNT = 7;  // by property
        
        
        // Initialization
        public DB_class(string dbName)
        {
            string initialConnectionString = "Data Source=(localdb)\\MSSQLLocalDB;Trusted_Connection=true;Integrated Security=SSPI;";
            dbConnection = new SqlConnection(initialConnectionString); // +
                //";Initial Catalog=" + dbName + ".mdf");// +
                //";User=LAPTOP-3G2MUB7S\\chitresh gupta");
                //"Data Source=local;" +
                //                             "Initial Catalog=" +
                //                             dbName + ";" +
                //                             "User ID=chit;" +
                //                             "Password=chit");
                // Create DB
            if (File.Exists(Directory.GetCurrentDirectory() + "\\" + dbName + ".mdf") == false)
            {
                string createCmd = "CREATE DATABASE " + dbName + " ON PRIMARY " +
                       "(NAME = " + dbName + ", " +
                       "FILENAME = '" + Directory.GetCurrentDirectory() + "\\" + dbName + ".mdf', " +
                       //"FILENAME = 'F:\\Work\\avr\\adc\\winApp\\adcUartBar\\adcUartBar\\bin\\Debug\\" + dbName + ".mdf', " +
                       "SIZE = 2MB, MAXSIZE = 10MB, FILEGROWTH = 10%) " +
                       "LOG ON (NAME = MyDatabase_Log, " +
                       "FILENAME = '" + Directory.GetCurrentDirectory() + "\\" + dbName + ".ldf', " +
                       //"FILENAME = 'F:\\Work\\avr\\adc\\winApp\\adcUartBar\\adcUartBar\\bin\\Debug\\" + dbName + ".ldf', " +
                       "SIZE = 1MB, " +
                       "MAXSIZE = 5MB, " +
                       "FILEGROWTH = 10%)";
                if (ExecuteCmd(createCmd) == false)
                {
                    maxColumns = 0;
                    currentColumn = 0;
                    dbConnection = null;
                    isCapturing = false;
                    return;
                }

            }
            // Add the database in connection string
            // Else it will go to master DB
            dbConnection = new SqlConnection(initialConnectionString + "Initial Catalog=TestDB2;");
            // Test Open DB
            if (DB_open() == false)
            {
                maxColumns = 0;
                currentColumn = 0;
                dbConnection = null;
                isCapturing = false;
                return;
            }
            DB_close();

            // Get already existing tables in
            UpdateTableList();
            //UpdateColumnList();

            // Set values
            // maxColumns = tableListStruct.Rows.Count - DB_DEFAULT_TABLE_COUNT;
            currentColumn = 0;
            isCapturing = false;
        }

        public bool DB_open()
        {
            try
            {   // Test Coonection
                dbConnection.Open();
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }

        public bool DB_close()
        {
            try
            {   // Test Coonection
                dbConnection.Close();
            }
            catch
            {
                return false;
            }
            return true;
        }

        public bool UpdateTableList()  
        {
            try
            {
                //int loopTableCount = 0;
                DB_open();
                // Get already existing tables in
                tableListStruct = dbConnection.GetSchema("Tables");
                // Update lsit
                foreach (DataRow row in tableListStruct.Rows)
                {
                    if (row[2].ToString().Contains("Table") == true) // (loopTableCount >= DB_DEFAULT_TABLE_COUNT)
                    {
                        string tablename = (string)row[2];
                        tableList.Add(tablename);
                        //maxColumns++;
                    }
                    //loopTableCount++;
                }
                DB_close();

            }
            catch
            {
                DB_close();
                return false;
            }
            return true;
        }
        public bool UpdateColumnList(string tableName)
        {
            try
            {
                DB_open();
                int tempColumnCount = 0;

                string[] restrictionsColumns = new string[4];
                restrictionsColumns[2] = tableName;

                // Get already existing tables in
                columnListStruct = dbConnection.GetSchema("Columns", restrictionsColumns);
                // Update lsit
                foreach (DataRow row in columnListStruct.Rows)
                {
                    string columnName = (string)row[3].ToString();
                    columnList.Add(columnName);
                    tempColumnCount++;
                }
                DB_close();
                maxColumns = tempColumnCount - 1;  // we are reducing one as the first column is for ID

            }
            catch
            {
                DB_close();
                return false;
            }
            return true;
        }
        // Creation
        public bool CreateTable(string tableName)
        {
            string cmdStr = "CREATE TABLE " + tableName + " (Num int, run_0 int);"; 

            if (ExecuteCmd(cmdStr) == false)
                return false;

            // Add uniquie ID in "Num"
            for (int i = 0; i < MAX_CAPTURE; i++)
                AddColumnValue(tableName, "Num", i);

            // Update informations
            UpdateTableList();
            UpdateColumnList(tableName);

            return true;  
        }

        public bool CreateColumnGeneral(string tableName, string columnName)
        {
            //alter table [Product] add [ProductId] int default 0 NOT NULL
            string cmdStr = "ALTER TABLE " + tableName + " ADD " + columnName + " int default 0;";
            if (ExecuteCmd(cmdStr) == true)
            {
                UpdateColumnList(tableName);
                // this is updated in Update Column list maxColumns++;
                return true;
            }

            return false;
        }

        public bool CreateADCColumn(string tableName, string columnName)
        {
            //alter table [Product] add [ProductId] int default 0 NOT NULL
            string cmdStr = "ALTER TABLE " + tableName + " ADD run_" + columnName + " int default 0;";
            if (ExecuteCmd(cmdStr) == true)
            {
                UpdateColumnList(tableName);
                // this is updated in Update Column list maxColumns++;
                return true;
            }

            return false;
        }
        // Search a column
        public bool SearchColumn(string tableName, string columnName)
        {
            try
            {
                string[] restrictionsColumns = new string[4];
                restrictionsColumns[2] = tableName;

                DB_open();
                // Get already existing tables in
                DataTable TempColumnListStruct = dbConnection.GetSchema("Columns", restrictionsColumns);
                DB_close();
                // search the column
                foreach (DataRow row in TempColumnListStruct.Rows)
                {
                    if (columnName == (string)row[3].ToString())
                        return true;
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        // Usage
        public bool UpdateADCValue(string tableName, string columnName, int value, int rowID)
        {
            string cmdStr = "UPDATE " + tableName + " SET run_" + columnName +
                            " = " + Convert.ToString(value) + " WHERE Num = " + rowID.ToString();

            return ExecuteCmd(cmdStr);
        }

        public bool AddColumnValue(string tableName, string columnName, int value)
        {
            string cmdStr = "INSERT INTO " + tableName + " (" + columnName +
                            ") VALUES" + " (" + Convert.ToString(value) + ");";

            return ExecuteCmd(cmdStr);
        }

        public bool AddADCValue(string tableName, string columnName, int value)
        {
            string cmdStr = "INSERT INTO " + tableName + " (run_" + columnName +
                            ") VALUES" + " (" + Convert.ToString(value) + ");";

            return ExecuteCmd(cmdStr);
        }

        public bool ADCCapture(string tableName, int value)
        {
            if (isCapturing == false)
                return false;

            // Only capture to a limit
            if (captureCount < MAX_CAPTURE)
            {
                if (UpdateADCValue(tableName, Convert.ToString(currentColumn), value, captureCount) == false)
                    return false;

                captureCount++;
            }

            // Stop capturing if maximum limit reached
            if (captureCount >= MAX_CAPTURE)
            {
                captureCount = 0;
                isCapturing = false;
            }

            return true;
        }

        public bool SearchTable(string tableName)
        {
            int loopTableCount = 0;
            foreach (DataRow row in tableListStruct.Rows)
            {
                string tablename = (string)row[2];
                if (tablename == tableName)
                    return true;

                loopTableCount++;
            }
            return false;
        }

        // General command exeqution
        bool ExecuteCmd(string cmdStr)
        {
            try
            {
                using (SqlCommand command = new SqlCommand(cmdStr, dbConnection))
                {
                    //dbConnection.Open();
                    command.Connection.Open();
                    command.ExecuteNonQuery();
                    command.Connection.Close();
                }
            }
            catch (Exception ex)
            {
                DB_close();
                return false;
            }
            return true;
        }


    }
}
