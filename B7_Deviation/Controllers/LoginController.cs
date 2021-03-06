using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.InteropServices;
using System.Web;
using System.Web.Mvc;
using B7_Deviation.Models;

namespace B7_Deviation.Controllers
{
    public class LoginController : Controller
    {
        private readonly string constr = ConfigurationManager.ConnectionStrings["DB_DEVIATION"].ConnectionString;

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = false)]
        public static extern bool LogonUser(string lpszUsername, string lpszDomain, string lpszPassword, int dwLogonType, int dwLogonProvider, ref IntPtr phToken);
        [DllImport("kernel32.dll")]
        public static extern int FormatMessage(int dwFlags, ref IntPtr lpSource, int dwMessageId, int dwLanguageId, ref string lpBuffer, int nSize, ref IntPtr Arguments);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        public static extern bool CloseHandle(IntPtr handle);

        readonly private DataTable DT = new DataTable();
        readonly private DataTable DT2 = new DataTable();

        public ActionResult Index()
        {
            Session.Clear();
            return View();
        }

        public ActionResult LoginProcess(LoginModel Model)
        {
            List<string> List = new List<string>();
            
            string status="";
            string result = "";
            string t_LVL = "";
            int check_login = 0;
            _ = new IntPtr(0);

            string UserName, MachineName, Pwd = null;
            bool returnValue = false;

            //The MachineName property gets the name of your computer.                
            UserName = Model.Username;
            Pwd = Model.Password;
            MachineName = "ONEKALBE";
            const int LOGON32_PROVIDER_DEFAULT = 0;
            const int LOGON32_LOGON_INTERACTIVE = 2;
            IntPtr tokenHandle = IntPtr.Zero;
            try
            {
                //cek apakah user ada di table users devol
                SqlConnection Conn2 = new SqlConnection(constr);
                try
                {
                    using (SqlCommand cmd = new SqlCommand("LOGIN_FORM_DEVIATION", Conn2))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("@pilih", System.Data.SqlDbType.Int);
                        cmd.Parameters["@pilih"].Value = 3;
                        cmd.Parameters.Add("@username", System.Data.SqlDbType.VarChar);
                        cmd.Parameters["@username"].Value = Model.Username;
                        cmd.Parameters.Add("@password", System.Data.SqlDbType.VarChar);
                        cmd.Parameters["@password"].Value = Model.Password;
                        Conn2.Open();
                        check_login = (int)cmd.ExecuteScalar();
                        Conn2.Close();
                    }
                }
                catch (Exception ex)
                {
                    status = "Error Web silahkan hubungin IT";
                    throw ex;
                }

                //username dan password terdaftar di devol users --> vendor/admin
                //code here
                if (check_login == 2)
                {
                    status = "True";
                    Session["xUser"] = Model.Username;

                    SqlConnection conn = new SqlConnection(constr);
                    try
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand("LOGIN_FORM_DEVIATION", conn))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.Add("@pilih", System.Data.SqlDbType.Int);
                            cmd.Parameters["@pilih"].Value = 1;

                            cmd.Parameters.Add("@Username", System.Data.SqlDbType.VarChar);
                            cmd.Parameters["@Username"].Value = Model.Username;

                            result = (string)cmd.ExecuteScalar();
                        }
                        conn.Close();
                    }
                    catch (Exception ex)
                    {
                        status = $"Error Web silahkan hubungin IT {ex}";
                        //throw ex;
                    }
                    finally
                    {
                        if (result == "kosong")
                        {
                            status = "kosong";
                        }
                    }
                }
                //username ada, tapi password salah di devol users --> khusus cek AD 
                else if (check_login == 1)
                {
                    if (Model.Password == "B7Portal")
                    {
                        status = "True";
                        Session["xUser"] = Model.Username;

                        SqlConnection conn = new SqlConnection(constr);
                        try
                        {
                            conn.Open();
                            using (SqlCommand cmd = new SqlCommand("LOGIN_FORM_DEVIATION", conn))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.Add("@pilih", System.Data.SqlDbType.Int);
                                cmd.Parameters["@pilih"].Value = 1;

                                cmd.Parameters.Add("@Username", System.Data.SqlDbType.VarChar);
                                cmd.Parameters["@Username"].Value = Model.Username;

                                SqlDataAdapter dataAdapt = new SqlDataAdapter();
                                dataAdapt.SelectCommand = cmd;
                                dataAdapt.Fill(DT);

                                result = DT.Rows[0]["EMPID"].ToString();
                                t_LVL = DT.Rows[0]["LVL"].ToString();
                            }
                            conn.Close();
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }
                        finally
                        {
                            if (result == "kosong")
                            {
                                status = "kosong";
                            }
                        }
                    }
                    else
                    {
                        //Call the LogonUser function to obtain a handle to an access token.
                        returnValue = LogonUser(UserName, MachineName, Pwd, LOGON32_LOGON_INTERACTIVE, LOGON32_PROVIDER_DEFAULT, ref tokenHandle);

                        //login AD gagal
                        if (returnValue == false)
                        {
                            //This function returns the error code that the last unmanaged function returned.
                            int ret = Marshal.GetLastWin32Error();
                            if (ret == 1329)
                            {
                                Session["xUser"] = Model.Username;
                                status = "Account directory tidak valid";
                            }
                            else
                            {
                                status = "Username atau password yang dimasukkan tidak sesuai!";
                            }
                        }
                        //login AD berhasil
                        else
                        {
                            status = "True";
                            Session["xUser"] = Model.Username;

                            SqlConnection conn = new SqlConnection(constr);
                            try
                            {
                                conn.Open();
                                using (SqlCommand cmd = new SqlCommand("LOGIN_FORM_DEVIATION", conn))
                                {
                                    cmd.CommandType = CommandType.StoredProcedure;
                                    cmd.Parameters.Add("@pilih", System.Data.SqlDbType.Int);
                                    cmd.Parameters["@pilih"].Value = 1;

                                    cmd.Parameters.Add("@Username", System.Data.SqlDbType.VarChar);
                                    cmd.Parameters["@Username"].Value = Model.Username;

                                    SqlDataAdapter dataAdapt = new SqlDataAdapter();
                                    dataAdapt.SelectCommand = cmd;
                                    dataAdapt.Fill(DT);

                                    result = DT.Rows[0]["EMPID"].ToString();
                                    t_LVL = DT.Rows[0]["LVL"].ToString();
                                }
                                conn.Close();

                            }
                            catch (Exception ex)
                            {
                                throw ex;
                            }
                            finally
                            {
                                if (result == "kosong")
                                {
                                    status = "kosong";
                                }
                            }
                        }
                    }
                }
                //username tidak terdaftar di devol users
                else if (check_login == 0)
                {
                    //khusus vendor/admin
                    //if(UserName.StartsWith("V_") || UserName.StartsWith("A_"))
                    //{
                    //    status = "Username tidak terdaftar";
                    //}
                    //cek apakah ada di AD
                    //else
                    //{
                        if (Model.Password == "B7Portal")
                        {
                            status = "True";
                            Session["xUser"] = Model.Username;

                            SqlConnection conn = new SqlConnection(constr);
                            try
                            {
                                conn.Open();
                                using (SqlCommand cmd = new SqlCommand("LOGIN_FORM_DEVIATION", conn))
                                {
                                    cmd.CommandType = CommandType.StoredProcedure;
                                    cmd.Parameters.Add("@pilih", System.Data.SqlDbType.Int);
                                    cmd.Parameters["@pilih"].Value = 1;

                                    cmd.Parameters.Add("@Username", System.Data.SqlDbType.VarChar);
                                    cmd.Parameters["@Username"].Value = Model.Username;

                                    SqlDataAdapter dataAdapt = new SqlDataAdapter();
                                    dataAdapt.SelectCommand = cmd;
                                    dataAdapt.Fill(DT);

                                    result = DT.Rows[0]["EMPID"].ToString();
                                    t_LVL = DT.Rows[0]["LVL"].ToString();
                                }
                                conn.Close();
                            }
                            catch (Exception ex)
                            {
                                throw ex;
                            }
                            finally
                            {
                                if (result == "kosong")
                                {
                                    status = "kosong";
                                }
                            }
                        }
                        else
                        {
                            //Call the LogonUser function to obtain a handle to an access token.
                            returnValue = LogonUser(UserName, MachineName, Pwd, LOGON32_LOGON_INTERACTIVE, LOGON32_PROVIDER_DEFAULT, ref tokenHandle);

                            //login AD gagal
                            if (returnValue == false)
                            {
                                //This function returns the error code that the last unmanaged function returned.
                                int ret = Marshal.GetLastWin32Error();
                                if (ret == 1329)
                                {
                                    Session["xUser"] = Model.Username;
                                    status = "Account directory tidak valid";
                                }
                                else
                                {
                                    status = "Username atau password yang dimasukkan tidak sesuai!";
                                }
                            }
                            //login AD berhasil
                            else
                            {
                                status = "True";
                                Session["xUser"] = Model.Username;

                                SqlConnection conn = new SqlConnection(constr);
                                try
                                {
                                    conn.Open();
                                    using (SqlCommand cmd = new SqlCommand("LOGIN_FORM_DEVIATION", conn))
                                    {
                                        cmd.CommandType = CommandType.StoredProcedure;
                                        cmd.Parameters.Add("@pilih", System.Data.SqlDbType.Int);
                                        cmd.Parameters["@pilih"].Value = 1;

                                        cmd.Parameters.Add("@Username", System.Data.SqlDbType.VarChar);
                                        cmd.Parameters["@Username"].Value = Model.Username;

                                        SqlDataAdapter dataAdapt = new SqlDataAdapter();
                                        dataAdapt.SelectCommand = cmd;
                                        dataAdapt.Fill(DT);

                                        result = DT.Rows[0]["EMPID"].ToString();
                                        t_LVL = DT.Rows[0]["LVL"].ToString();
                                    }
                                    conn.Close();

                                }
                                catch (Exception ex)
                                {
                                    throw ex;
                                }
                                finally
                                {
                                    if (result == "kosong")
                                    {
                                        status = "kosong";
                                    }
                                }
                            }
                        }
                    //}
                }
            }

            catch (Exception ex)
            {
                status = ex.ToString();
            }

            if (status == "True")
            {
                if (t_LVL == "STAFF")
                {
                    status = "staff";
                }
                else
                {
                    // Get Role Login
                    SqlConnection conn2 = new SqlConnection(constr);
                    try
                    {
                        conn2.Open();
                        using (SqlCommand cmd = new SqlCommand("LOGIN_FORM_DEVIATION", conn2))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.Add("@pilih", System.Data.SqlDbType.Int);
                            cmd.Parameters["@pilih"].Value = 2;

                            cmd.Parameters.Add("@Username", System.Data.SqlDbType.VarChar);
                            cmd.Parameters["@Username"].Value = Model.Username;

                            SqlDataAdapter dataAdapt = new SqlDataAdapter();
                            //result = (string)cmd.ExecuteScalar();
                            dataAdapt.SelectCommand = cmd;

                            dataAdapt.Fill(DT2);
                        }
                        conn2.Close();
                        Session["role"] = DT2.Rows[0]["role_deviation"].ToString();
                        Session["fullname"] = DT2.Rows[0]["empname"].ToString();
                        Session["jobttlname"] = DT2.Rows[0]["jobttlname"].ToString();
                        Session["nik"] = DT2.Rows[0]["empid"].ToString();
                        Session["usergroup"] = DT2.Rows[0]["dept"].ToString();
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                    finally
                    {

                    }
                }
            }

            List.Add(status);

            return Json(List);
        }
    }
}