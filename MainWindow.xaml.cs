using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FPGA_UART_Dashboard.Modules; // Import Module đã viết

namespace FPGA_UART_Dashboard
{
    public partial class MainWindow : Window
    {
        private UartManager _uart;

        public MainWindow()
        {
            InitializeComponent();

            _uart = new UartManager();
            _uart.OnLogMessage += Uart_OnLogMessage; // Đăng ký nhận Log từ Module UART

            LoadAvailablePorts();
        }

        private void LoadAvailablePorts()
        {
            string[] ports = _uart.GetAvailablePorts();
            cmbPorts.ItemsSource = ports;

            if (ports.Length > 0) cmbPorts.SelectedIndex = 0;
            else MessageBox.Show("Không tìm thấy cổng COM nào!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        // Đẩy thông báo log từ Module UART lên TextBox UI một cách an toàn (Dispatcher)
        private void Uart_OnLogMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                txtConsole.AppendText($"{message}\n");
                txtConsole.ScrollToEnd();
            });
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (btnConnect.Content.ToString() == "Connect")
            {
                if (cmbPorts.SelectedItem == null || cmbBaudrate.SelectedItem == null)
                {
                    MessageBox.Show("Vui lòng chọn cổng COM và Baudrate!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    string selectedPort = cmbPorts.SelectedItem.ToString();
                    int baudrate = int.Parse(((ComboBoxItem)cmbBaudrate.SelectedItem).Content.ToString());

                    _uart.Connect(selectedPort, baudrate);

                    btnConnect.Content = "Disconnect";
                    cmbPorts.IsEnabled = false;
                    cmbBaudrate.IsEnabled = false;
                    txtStatus.Text = $"Connected {selectedPort} @ {baudrate} bps";
                    txtStatus.Foreground = Brushes.Green;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Không thể mở cổng COM.\nChi tiết: {ex.Message}", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                _uart.Disconnect();
                btnConnect.Content = "Connect";
                cmbPorts.IsEnabled = true;
                cmbBaudrate.IsEnabled = true;
                txtStatus.Text = "Disconnected";
                txtStatus.Foreground = Brushes.Red;
            }
        }

        private async void btnTestConnect_Click(object sender, RoutedEventArgs e)
        {
            if (!_uart.IsConnected)
            {
                MessageBox.Show("Vui lòng kết nối cổng COM trước!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            btnTestConnect.IsEnabled = false;

            // Gọi hàm xử lý vạn năng từ UartManager
            bool success = await _uart.SendCommandAsync("ping", "pong", 1000);

            if (success) MessageBox.Show("Kết nối thành công!\nPhần cứng phản hồi: pong", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            else MessageBox.Show("Time-out 1s!\nKhông nhận được phản hồi 'pong'.", "Lỗi giao tiếp", MessageBoxButton.OK, MessageBoxImage.Error);

            btnTestConnect.IsEnabled = true;
        }

        private async void btnLedOn_Click(object sender, RoutedEventArgs e)
        {
            if (!_uart.IsConnected) return;

            btnLedOn.IsEnabled = false;
            bool success = await _uart.SendCommandAsync("#SON*\n", "#LON*\n", 1000);

            if (!success) MessageBox.Show("Time-out!\nKhông nhận được phản hồi '#LON*'.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            btnLedOn.IsEnabled = true;
        }

        private async void btnLedOff_Click(object sender, RoutedEventArgs e)
        {
            if (!_uart.IsConnected) return;

            btnLedOff.IsEnabled = false;
            bool success = await _uart.SendCommandAsync("#SOF*\n", "#LOF*\n", 1000);

            if (!success) MessageBox.Show("Time-out!\nKhông nhận được phản hồi '#LOF*'.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            btnLedOff.IsEnabled = true;
        }

        protected override void OnClosed(EventArgs e)
        {
            _uart?.Dispose();
            base.OnClosed(e);
        }
    }
}