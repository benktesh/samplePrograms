using System;
using System.Data.OleDb;
using System.Windows.Forms;//Remove/disable this once allmessage boxes are removed.
using System.Data.SqlClient;
using System.Diagnostics;
using System.Data;

namespace SummarizeBiosum
{
    class Database
    {
        //This method initiates the processing of the selected accessdbs and tables in those dbs
        public void processDB(String folderPath, String[] fileNames, String[] tableNames)
        {
            String myFolder = "";
            String myFile = "";
            String DS = "";
            String pkgid = "";
            foreach (String fname in fileNames)
            {
                Debug.WriteLine("Working on file " + fname);
                foreach (String tableName in tableNames)
                {
                    Debug.WriteLine("Working on table " + tableName);
                    myFolder = folderPath;
                    myFile = fname;

                    DS = myFolder + @"\" + myFile;
                    pkgid = myFile.Substring(11, 3);
                    #region tableselector
                    String SQLString = "";
                    if (tableName.Equals("FVS_POTFIRE"))
                    {
                        SQLString = "Select ID, '" + pkgid + "' as PkgID, CaseID,	StandID,	Year,	Surf_Flame_Sev,	Surf_Flame_Mod,	Tot_Flame_Sev,	Tot_Flame_Mod,	Fire_Type_Sev,	Fire_Type_Mod,	PTorch_Sev,	PTorch_Mod,	Torch_Index,	Crown_Index,	Canopy_Ht,	Canopy_Density,	Mortality_BA_Sev,	Mortality_BA_Mod,	Mortality_VOL_Sev,	Mortality_VOL_Mod,	Pot_Smoke_Sev,	Pot_Smoke_Mod,	Fuel_Mod1,	Fuel_Mod2,	Fuel_Mod3,	Fuel_Mod4,	Fuel_Wt1,	Fuel_Wt2,	Fuel_Wt3,	Fuel_Wt4 FROM " + tableName;
                    }
                    else if (tableName.Equals("FVS_Summary"))
                    {
                        SQLString = "Select Id, '" + pkgid + "' as PkgID,	CaseID, StandID,	Year,	Age,	Tpa,	BA,	SDI,	CCF,	TopHt,	QMD,	TCuFt,	MCuFt,	BdFt,	RTpa,	RTCuFt,	RMCuFt,	RBdFt,	ATBA,	ATSDI,	ATCCF,	ATTopHt,	ATQMD,	PrdLen,	Acc,	Mort,	MAI,	ForTyp,	SizeCls,	StkCls FROM " + tableName;
                    }
                   else
                        MessageBox.Show("Table not found");
                    #endregion

                    transferData(DS, pkgid, tableName, SQLString);
                 }
            }

            Debug.WriteLine("Completed processing file from pkgid " + pkgid);
        }

        private static void transferData(String DS, String pkgid, String tableName, String SQLString)
        {
            using (OleDbConnection con = new OleDbConnection(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + DS + ";Persist Security Info=False;"))
            {
                con.Open();
                OleDbCommand command = new OleDbCommand(SQLString, con);
                SQLString = "";
                try
                {
                    OleDbDataReader rdr = command.ExecuteReader();
                    toServer(pkgid, tableName, rdr);
                }
                catch (Exception e) { Console.WriteLine(e.ToString()); }
            }
        }

        private static void toServer(String pkgid, String tableName, OleDbDataReader rdr)
        {
            using (SqlConnection conn = new SqlConnection("Data Source=BENKTESH-UC;Initial Catalog=FVSOUT;Integrated Security=True;Connect Timeout=30000;Encrypt=False;TrustServerCertificate=False;"))
            {
                conn.Open();
                String SQLString = "Delete FROM " + tableName + "  WHERE PkgID = '" + pkgid + "';";
                SqlCommand cmd = new SqlCommand(SQLString, conn);
                //Debug.WriteLine(SQLString);
                cmd.ExecuteNonQuery();
                Debug.WriteLine("Deleted " + tableName + " from pkgid " + pkgid + "with SQL " + SQLString);
                SQLString = "";
                Debug.WriteLine("Going to Bulk Copy " + tableName + " " + DateTime.Now.ToString());
                using (SqlBulkCopy s = new SqlBulkCopy(conn))
                {
                    s.BulkCopyTimeout = 300000;
                    s.DestinationTableName = tableName;
                    s.WriteToServer(rdr);
                    Debug.WriteLine("Completed writing data to " + tableName);
                }
            }
        }

        //This method process the batch sql over mulitple access database files.
        public void processBatchSQL(String folderPath, String[] fileNames, string[] batchSQL)
        {
            Debug.WriteLine("*************Processing the Batch SQL***************");
            String myFolder = "";
            String myFile = "";
            String DS = "";
            foreach (String fname in fileNames)
            {
                myFolder = folderPath; myFile = fname; DS = myFolder + @"\" + myFile;
                using (OleDbConnection con = new OleDbConnection(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + DS + ";Persist Security Info=False;"))
                {
                    con.Open(); Debug.WriteLine("Made connection with db: " + fname);
                    OleDbCommand cmd = con.CreateCommand();
                    foreach (String q in batchSQL)
                    {
                        cmd.CommandText = q; //Debug.WriteLine("   Command prepared for " + q);
                        try
                        {
                            cmd.ExecuteNonQuery();
                            Debug.WriteLine("   Executed: " + q);
                        }
                        catch (Exception e) { throw new Exception(e.ToString()); }
                    }
                    if (con.State == ConnectionState.Open) { con.Close(); }
                    Debug.WriteLine("Completed processing db: " + fname);
                }
            }  
            Debug.Write("*********** Done Processing Batch SQL*******************");
        }

     
        //This private method programmetrically compact the accessdb.
        private static void CompactAccessDB(string connectionString, string mdwfilename)
        {
            Debug.WriteLine("Connection String is :" + connectionString + " \n fname is: " + mdwfilename);
            object[] oParams;
            object objJRO = Activator.CreateInstance(Type.GetTypeFromProgID("JRO.JetEngine"));
            Debug.Write("JRO engine activaited");
            oParams = new object[] { connectionString, "Provider=Microsoft.Jet.OLEDB.12.0;Data Source=C:\temp\tempdb.mdb;Jet OLEDB:Engine Type=5" };
            objJRO.GetType().InvokeMember("CompactDatabase",
                System.Reflection.BindingFlags.InvokeMethod,
                null,
                objJRO,
                oParams);
            System.IO.File.Delete(mdwfilename);
            Debug.WriteLine("Delete Old Fie");
            System.IO.File.Move("C:\temp\tempdb.mdb", mdwfilename);
            Debug.WriteLine("Replacing with new file");

            //clean up (just in case)
            System.Runtime.InteropServices.Marshal.ReleaseComObject(objJRO);
            objJRO = null;
        }

        //This method invokes access db compaction
        public void processCompactDB(string folderPath, string[] fileNames)
        {
            Debug.WriteLine("*************Processing the Batch Compact Request***************");
            String myFolder = ""; String myFile = ""; String DS = "";
            foreach (String fname in fileNames)
            {
                myFolder = folderPath; myFile = fname; DS = myFolder + @"\" + myFile;
                String con = (@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + DS + ";Persist Security Info=False;");
                CompactAccessDB(con, DS);
            }
            Debug.Write("*********** Done Processing Batch Batch Compact Request*******************");
        }
    }
}



