using System.Data.SqlClient;

namespace wsn_keboo.Data;

public abstract class DbConnection
{
    private readonly static string stringConnection = @"Data Source=test10102023.database.windows.net;
                                                        Initial Catalog=zyp8x;
                                                        Persist Security Info=True;
                                                        User ID=CloudSA9d561a4b;
                                                        Password=Lienhoax2";
    public static SqlConnection GetConnection()
    {
        return new SqlConnection(stringConnection);
    }

}
