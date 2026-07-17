using Npgsql;
using System.Windows;

namespace HardwarePaintShop.Desktop.Views.Shell;

public partial class DatabaseSetupWindow : Window
{
    public DatabaseSetupWindow(string error)
    {
        InitializeComponent(); ErrorDetails.Text = FriendlyError(error);
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(PortBox.Text, out var port) || port is < 1 or > 65535)
        {
            StatusText.Text = "رقم المنفذ غير صحيح."; return;
        }
        SaveButton.IsEnabled = false; StatusText.Text = "جارٍ اختبار الاتصال...";
        try
        {
            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = HostBox.Text.Trim(), Port = port, Database = DatabaseBox.Text.Trim(),
                Username = UsernameBox.Text.Trim(), Password = PasswordBox.Password,
                Timeout = 8, CommandTimeout = 30
            };
            try
            {
                await using var connection = new NpgsqlConnection(builder.ConnectionString);
                await connection.OpenAsync();
            }
            catch (PostgresException ex) when (ex.SqlState == "3D000" && CreateDatabaseBox.IsChecked == true)
            {
                var databaseName = builder.Database;
                if (string.IsNullOrWhiteSpace(databaseName)) throw new InvalidOperationException("اسم قاعدة البيانات مطلوب.");
                var adminBuilder = new NpgsqlConnectionStringBuilder(builder.ConnectionString) { Database = "postgres" };
                await using var admin = new NpgsqlConnection(adminBuilder.ConnectionString);
                await admin.OpenAsync();
                await using var create = admin.CreateCommand();
                create.CommandText = $"CREATE DATABASE \"{databaseName.Replace("\"", "\"\"")}\"";
                await create.ExecuteNonQueryAsync();
                await using var verify = new NpgsqlConnection(builder.ConnectionString);
                await verify.OpenAsync();
            }
            Environment.SetEnvironmentVariable("HARDWARE_PAINT_SHOP_CONNECTION_STRING", builder.ConnectionString, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("HARDWARE_PAINT_SHOP_CONNECTION_STRING", builder.ConnectionString, EnvironmentVariableTarget.User);
            StatusText.Text = "تم الاتصال والحفظ بنجاح."; DialogResult = true; Close();
        }
        catch (Exception ex) { StatusText.Text = FriendlyError(ex.Message); }
        finally { SaveButton.IsEnabled = true; }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
    private static string FriendlyError(string error)
    {
        if (error.Contains("password authentication failed", StringComparison.OrdinalIgnoreCase))
            return "رفض PostgreSQL كلمة المرور. راجع اسم المستخدم وكلمة المرور.";
        if (error.Contains("refused", StringComparison.OrdinalIgnoreCase) || error.Contains("connect", StringComparison.OrdinalIgnoreCase))
            return "تعذر الاتصال بـ PostgreSQL. تأكد أن الخدمة تعمل وأن السيرفر والمنفذ صحيحان.";
        return error.Length > 350 ? error[..350] : error;
    }
}
