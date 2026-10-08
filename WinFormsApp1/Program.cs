namespace WinFormsApp1
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) => ShowFatalError(e.Exception, "UI thread");
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                var exception = e.ExceptionObject as Exception ?? new Exception(e.ExceptionObject?.ToString());
                ShowFatalError(exception, "AppDomain");
            };
            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                StartupDiagnostics.Write(e.Exception, "Unobserved task");
                e.SetObserved();
            };
            try { Application.Run(new Form1()); }
            catch (Exception ex) { ShowFatalError(ex, "Application startup"); }
        }

        private static void ShowFatalError(Exception exception, string source)
        {
            StartupDiagnostics.Write(exception, source);
            try
            {
                MessageBox.Show($"RSS Reader Liteでエラーが発生しました。\r\n\r\n{exception.Message}\r\n\r\n診断ログ: {StartupDiagnostics.LogPath}", "RSS Reader Lite エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch { }
        }
    }
}
