using System;
using System.Threading.Tasks;
using RJCP.IO.Ports;

namespace FPGA_UART_Dashboard.Modules
{
    public class UartManager : IDisposable
    {
        private SerialPortStream _serialPort;
        private string _dataBuffer = "";

        // Sự kiện (Event) để gửi dữ liệu log ngược lên giao diện (UI)
        public event Action<string> OnLogMessage;

        // Các biến phục vụ cơ chế chờ phản hồi (ACK)
        private bool _isWaitingForAck = false;
        private string _expectedAck = "";
        private bool _ackReceived = false;

        public UartManager()
        {
            _serialPort = new SerialPortStream();
        }

        public string[] GetAvailablePorts()
        {
            return System.IO.Ports.SerialPort.GetPortNames();
        }

        public bool IsConnected => _serialPort != null && _serialPort.IsOpen;

        public void Connect(string portName, int baudRate)
        {
            if (_serialPort != null)
            {
                _serialPort.DataReceived -= SerialPort_DataReceived;
                if (_serialPort.IsOpen) _serialPort.Close();
                _serialPort.Dispose();
            }

            _serialPort = new SerialPortStream
            {
                PortName = portName,
                BaudRate = baudRate,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                DtrEnable = false,
                RtsEnable = false
            };

            _serialPort.DataReceived += SerialPort_DataReceived;
            _serialPort.Open();
        }

        public void Disconnect()
        {
            if (IsConnected)
            {
                _serialPort.Close();
            }
        }

        // Hàm vạn năng: Gửi lệnh và chờ chuỗi phản hồi cụ thể
        public async Task<bool> SendCommandAsync(string txCommand, string expectedRx, int timeoutMs = 1000)
        {
            if (!IsConnected) return false;

            _expectedAck = expectedRx;
            _ackReceived = false;
            _isWaitingForAck = true;

            // Ghi log lên giao diện trước khi gửi
            string logTx = txCommand.Replace("\n", "\\n"); // Hiển thị rõ ký tự ngắt dòng
            OnLogMessage?.Invoke($"[Tx] {logTx}");

            _serialPort.Write(txCommand);

            // Vòng lặp chờ phản hồi
            int timeoutCounter = 0;
            int maxLoops = timeoutMs / 100; // Mỗi vòng 100ms
            while (!_ackReceived && timeoutCounter < maxLoops)
            {
                await Task.Delay(100);
                timeoutCounter++;
            }

            _isWaitingForAck = false;
            return _ackReceived;
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (!IsConnected) return;

                string incomingData = _serialPort.ReadExisting();
                _dataBuffer += incomingData;

                // 1. Quét chuỗi phản hồi lệnh (Ping-Pong, LED ON/OFF)
                if (_isWaitingForAck && !string.IsNullOrEmpty(_expectedAck))
                {
                    if (_dataBuffer.Contains(_expectedAck))
                    {
                        _ackReceived = true;

                        // Ghi log lên giao diện
                        string logRx = _expectedAck.Replace("\n", "\\n");
                        OnLogMessage?.Invoke($"[Rx] {logRx}");

                        // Cắt bỏ chuỗi đã xử lý khỏi bộ đệm
                        _dataBuffer = _dataBuffer.Replace(_expectedAck, "");
                    }
                }

                // 2. Tương lai: Logic bóc tách dữ liệu #S,30,60,150*A5 sẽ được thêm vào đây
            }
            catch { }
        }

        public void Dispose()
        {
            Disconnect();
            if (_serialPort != null)
            {
                _serialPort.Dispose();
            }
        }
    }
}