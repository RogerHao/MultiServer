using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Net.Sockets;


namespace DelsysPlugin
{
    public class DelsysEmgSolver
    {
        #region Public Properties
        public int ChannelNum { get; private set; }
        public int GestureNum { get; private set; }
        public int GestureCutNum { get; private set; }
        public int GestureSampleNum { get; private set; }
        public int GestureWholeNum { get; private set; }

        public double RestState { get; private set; }
        public bool ModelGenerated { get; private set; }
        public bool IsPredicting { get; private set; }
        public int GestureResult { get; private set; }

        public static List<List<double>> EmgData = new List<List<double>>();
        public int DataNowCount => emgDataList.Count;
        public int DataTotal => EmgData.Count;

        public List<bool> Sensors => Connected ? _sensors : null;
        private List<bool> _sensors = new List<bool>();

        public bool Connected { get; private set; } = false;
        public bool EmgGetting { get; private set; } = false;
        public bool AccGetting { get; private set; } = false;

#endregion

#region Private Properties
        private Classifier _emgClassifier;

        private string _serverUrl = "localhost";

        //Socket for all data comm
        private TcpClient commandSocket;
        private TcpClient emgSocket;
        private TcpClient accSocket;

        //socket port for all client
        private const int commandPort = 50040;
        private const int emgPort = 50041;
        private const int accPort = 50042;

        //streams and readers/writers for communication
        private NetworkStream commandStream;
        private StreamReader commandReader;
        private StreamWriter commandWriter;
        private NetworkStream emgStream;
        private NetworkStream accStream;

        //Thread for acquiring emg and acc data
        private Thread emgThread;
        private Thread accThread;
        private Thread predictThread;

        private CancellationTokenSource emgCancellationTokenSource = new CancellationTokenSource();
        private CancellationTokenSource predictCancellationTokenSource = new CancellationTokenSource();
        private CancellationTokenSource accCancellationTokenSource = new CancellationTokenSource();

        private static object emgLock = new object();
        private static object accLock = new object();

        //Server Command
        private const string COMMAND_QUIT = "QUIT";
        private const string COMMAND_START = "START";
        private const string COMMAND_STOP = "STOP";
        private const string COMMAND_SENSOR_TYPE = "TYPE?";

        private List<List<double>> emgDataList = new List<List<double>>();
        private List<List<double>> accXDataList = new List<List<double>>();
        private List<List<double>> accYDataList = new List<List<double>>();
        private List<List<double>> accZDataList = new List<List<double>>();
#endregion


        public void InitClassifier(int getsture,int channel,int gestureSample,int gestureWhole, int gestureCut)
        {
            ChannelNum = channel;
            GestureNum = getsture;
            GestureSampleNum = gestureSample;
            GestureWholeNum = gestureWhole;
            GestureCutNum = gestureCut;
            _emgClassifier = new Classifier(300,100,ChannelNum, GestureNum);
            EmgData.Clear();
            emgCancellationTokenSource.Cancel();
            predictCancellationTokenSource.Cancel();
        }


        public async void GetRestState()
        {
            if(_emgClassifier==null) return;
            RestState = _emgClassifier.RestState(await Task.Run(() => GetAmouuntEmgData(GestureWholeNum, GestureCutNum, GestureSampleNum)));
        }

        public async void GenerateModel()
        {
            if(EmgData.Count!=GestureNum*GestureSampleNum) return;
            var label = new int[EmgData.Count];
            for (var i = 0; i < GestureNum; i++)
            for (var j = i * GestureSampleNum; j < EmgData.Count; j++)
                label[j] = i;
            _emgClassifier.AddFeatureLabelFromData(EmgData, label);
            ModelGenerated  = await Task.Run(()=>_emgClassifier.GenerateModel());
        }

        public int TestModel(int startIndex)
        {
            if (!ModelGenerated) return -1;
            if (startIndex + 300 > EmgData.Count) return -2;
            return _emgClassifier.Predict(EmgData.GetRange(startIndex, 300));
        }

        public string Connect(string serverUrl = "localhost")
        {
            if (Connected) return "Connected";
            try
            {
                _serverUrl = serverUrl;
                commandSocket = new TcpClient(serverUrl, commandPort);
                commandStream = commandSocket.GetStream();
                commandReader = new StreamReader(commandStream, Encoding.ASCII);
                commandWriter = new StreamWriter(commandStream, Encoding.ASCII);

                var response = commandReader.ReadLine();
                commandReader.ReadLine();

                Connected = true;
                _sensors = new List<bool>();
                for (int i = 1; i <= 16; i++)
                {
                    string query = "SENSOR " + i + " " + COMMAND_SENSOR_TYPE;
                    string queryResponse = SendCommand(query);
                    _sensors.Add(!queryResponse.Contains("INVALID"));
                }
                SendCommand("UPSAMPLE OFF");

                return _sensors.Count == ChannelNum ? "Success" : "NotEnoughSensor";
            }
            catch (Exception e)
            {
                Connected = false;
                return "ConnectFail: " + e.Message;
            }

        }
        public string Disconnect()
        {
            if (EmgGetting) return "isRunning";
            SendCommand(COMMAND_QUIT);
            Connected = false;
            commandReader.Close();
            commandWriter.Close();
            commandStream.Close();
            emgStream?.Close();
            emgSocket?.Close();
            accStream?.Close();
            accSocket?.Close();
            return "OK";
        }

        public string SendCommand(string command)
        {
            string response = "";
            if (Connected)
            {
                commandWriter.WriteLine(command);
                commandWriter.WriteLine();
                commandWriter.Flush();

                response = commandReader.ReadLine();
                commandReader.ReadLine();
            }
            else response = "NotConnected";
            return response;
        }

        public async void GetOneGesture()
        {
            EmgData.AddRange(await Task.Run(()=>GetAmouuntEmgData(GestureWholeNum,GestureCutNum,GestureSampleNum)));
        }
        public List<List<double>> GetAmouuntEmgData(int amount,int startCut,int sample)
        {
            if (!EmgGetting) StartGetEmgData();
            else emgDataList.Clear();
            while (emgDataList.Count < amount)
            {
            }
            return emgDataList.GetRange(startCut,sample);
        }
        public int StartPredict()
        {
            if (!ModelGenerated) return -1;
            if (IsPredicting) return -2;
            StartGetEmgData();
            predictThread = new Thread(PredictWorker) { IsBackground = true };
            IsPredicting = true;
            predictThread.Start();
            return 0;
        }
        public int StopPredict()
        {
            if (!IsPredicting) return -1;

            IsPredicting = false;
            predictThread.Join();

            StopGetEmgData();
            return 0;
        }
        private string StartGetEmgData()
        {
            if (!Connected) return "NotConnected";
            if (EmgGetting) return "isRunning";
            
            emgDataList.Clear();
            
            emgSocket = new TcpClient(_serverUrl, emgPort);
            emgStream = emgSocket.GetStream();
            emgThread = new Thread(EmgWorker) { IsBackground = true };
            EmgGetting = true;
            emgThread.Start();

            if (AccGetting) return "Started";
            string response = SendCommand(COMMAND_START);
            if (response.StartsWith("OK")) return "Started";
            EmgGetting = false;
            return "StartFail";
        }
        private void StopGetEmgData()
        {
            EmgGetting = false;
            string response = SendCommand(COMMAND_STOP);
            emgDataList.Clear();
            emgThread.Join();
            return;
        }

        public async Task<string> GetOneGestureAsync(int amount, int startCut, int sample)
        {
            if (!Connected) return "NotConnected";
            if (!emgCancellationTokenSource.IsCancellationRequested) return "isRunning";
            emgDataList.Clear();
            emgSocket = new TcpClient(_serverUrl, emgPort);
            emgStream = emgSocket.GetStream();
            SendCommand(COMMAND_START);
            emgCancellationTokenSource = new CancellationTokenSource();
            await Task.Run(() => EmgCetter(amount), emgCancellationTokenSource.Token);
            EmgData.AddRange(emgDataList.GetRange(startCut, sample));
            SendCommand(COMMAND_STOP);
            emgStream.Close();
            emgSocket.Close();
            return "OK";
        }
        public int StartPredictAsync()
        {
            if (!ModelGenerated) return -1;
            if (!predictCancellationTokenSource.IsCancellationRequested) return -2;
            predictCancellationTokenSource = new CancellationTokenSource();
            StartGetEmgDateAsync();
            Task.Run(() => Predictter(), predictCancellationTokenSource.Token);
            return 0;
        }
        public int StopPredictAsync()
        {
            if (predictCancellationTokenSource.IsCancellationRequested) return -1;
            predictCancellationTokenSource.Cancel();
            StopGetEmgDataAsync(true);
            return 0;
        }
        private void StartGetEmgDateAsync()
        {
            if (!Connected) return;
            if (!emgCancellationTokenSource.IsCancellationRequested) return;

            emgDataList.Clear();
            emgSocket = new TcpClient(_serverUrl, emgPort);
            emgStream = emgSocket.GetStream();

            emgCancellationTokenSource = new CancellationTokenSource();
            Task.Run(()=>EmgCetter(0),emgCancellationTokenSource.Token);
            string response = SendCommand(COMMAND_START);
            return;
        }
        private void StopGetEmgDataAsync(bool clear=false)
        {
            emgCancellationTokenSource.Cancel();
            string response = SendCommand(COMMAND_STOP);
            if(clear) emgDataList.Clear();
            emgStream.Close();
            emgSocket.Close();
            return;
        }

        public string StartGetAccData()
        {
            if (!Connected) return "NotConnected";
            if (AccGetting) return "isRunning";
            
            accXDataList.Clear();
            accYDataList.Clear();
            accZDataList.Clear();

            accSocket = new TcpClient(_serverUrl, accPort);
            accStream = accSocket.GetStream();
            accThread = new Thread(AccWorker) { IsBackground = true };
            AccGetting = true;
            accThread.Start();

            if (EmgGetting) return "Started";
            string response = SendCommand(COMMAND_START);
            if (response.StartsWith("OK")) return "Started";
            AccGetting = false;
            return "StartFail";
        }
        public string StopGetAccData()
        {
            AccGetting = false;
            string response = SendCommand(COMMAND_STOP);
            accThread.Join();
            return "OK";
        }

        public void SaveDataToCsv(string filePath, string commment,List<List<double>> _trainData)
        {
            if (_trainData == null) _trainData = EmgData;
            if (string.IsNullOrEmpty(filePath)) filePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (!File.Exists($"{filePath}//emgdata"+commment+".csv"))
            {
                FileStream fs = new FileStream($"{filePath}//emgdata" + commment + ".csv", FileMode.Create, FileAccess.Write);
                fs.Close();
                fs.Dispose();
            }
            var sw = new StreamWriter($"{filePath}//emgdata" + commment + ".csv", false);
            var iColCount = _trainData.Count;
            foreach (var row in _trainData)
            {
                for (var i = 0; i < row.Count; i++)
                {
                    if (!Convert.IsDBNull(row[i]))
                    {
                        sw.Write(",");
                        sw.Write(row[i].ToString());
                    }
                }
                sw.Write(sw.NewLine);
            }
            sw.Close();
            sw.Dispose();
        }

        private void AccWorker()
        {
            accStream.ReadTimeout = 1000;    //set timeout
            BinaryReader reader = new BinaryReader(accStream);
            while (EmgGetting)
            {
                try
                {
                    List<double> XrowData = new List<double>();
                    List<double> YrowData = new List<double>();
                    List<double> ZrowData = new List<double>();
                    for (int sn = 0; sn < 16; ++sn)
                    {
                        float XoneData = reader.ReadSingle();
                        float YoneData = reader.ReadSingle();
                        float ZoneData = reader.ReadSingle();
                        if (sn < ChannelNum)
                        {
                            XrowData.Add(XoneData);
                            YrowData.Add(YoneData);
                            ZrowData.Add(ZoneData);
                        }
                        lock (accLock)
                        {
                            accXDataList.Add(XrowData);
                            accYDataList.Add(YrowData);
                            accZDataList.Add(ZrowData);
                        }

                    }
                }
                catch (IOException)
                {
                    //catch errors
                }
            }
            reader.Close(); //close the reader. This also disconnects
        }

        private void EmgWorker()
        {
            emgStream.ReadTimeout = 1000;
            BinaryReader reader = new BinaryReader(emgStream);
            while (EmgGetting)
            {
                try
                {
                    List<double> rowData = new List<double>();
                    for (int sn = 0; sn < 16; sn++)
                    {
                        float oneData = reader.ReadSingle();
                        if (sn<ChannelNum) rowData.Add(oneData);
                    }
                    lock (emgLock) emgDataList.Add(rowData);
                }
                catch (Exception)
                {
                    //Console.WriteLine(e);
                }
            }
            reader.Close();
        }

        private void PredictWorker()
        {
            while (IsPredicting)
            {
                try
                {
                    if (emgDataList.Count < 300) continue;
                    List<List<double>> dataWin = new List<List<double>>();
                    dataWin = emgDataList.GetRange(emgDataList.Count - 300, 300);
                    lock (emgLock) emgDataList.RemoveRange(emgDataList.Count - 300, 300);
                    GestureResult = _emgClassifier.Predict(dataWin);
                }
                catch (Exception)
                {
                    //                    Console.WriteLine(e);
                }
            }
        }

        private Task AccGetter()
        {
            accStream.ReadTimeout = 1000;    //set timeout
            using (var reader = new BinaryReader(accStream))
            {
                while (true)
                {
                    try
                    {
                        List<double> XrowData = new List<double>();
                        List<double> YrowData = new List<double>();
                        List<double> ZrowData = new List<double>();
                        for (int sn = 0; sn < 16; ++sn)
                        {
                            float XoneData = reader.ReadSingle();
                            float YoneData = reader.ReadSingle();
                            float ZoneData = reader.ReadSingle();
                            if (sn < ChannelNum)
                            {
                                XrowData.Add(XoneData);
                                YrowData.Add(YoneData);
                                ZrowData.Add(ZoneData);
                            }
                            lock (accLock)
                            {
                                accXDataList.Add(XrowData);
                                accYDataList.Add(YrowData);
                                accZDataList.Add(ZrowData);
                            }

                        }
                    }
                    catch (IOException)
                    {
                        //catch errors
                    }
                }
                
            }
        }

        private void EmgCetter(int number)
        {
            emgStream.ReadTimeout = 1000;
            using (var reader = new BinaryReader(emgStream))
            {
                while (!emgCancellationTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        var rowData = new List<double>();
                        for (int sn = 0; sn < 16; sn++)
                        {
                            var oneData = reader.ReadSingle();
                            if (sn < ChannelNum) rowData.Add(oneData);
                        }
                        lock (emgLock) emgDataList.Add(rowData);
                        if(emgDataList.Count>=number && number!=0) StopGetEmgDataAsync();
                    }
                    catch (Exception)
                    {
                        //Console.WriteLine(e);
                    }
                }
            }
        }

        private void Predictter()
        {
            while (!predictCancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    if (emgDataList.Count < 300) continue;
                    List<List<double>> dataWin = new List<List<double>>();
                    dataWin = emgDataList.GetRange(emgDataList.Count - 300, 300);
                    lock (emgLock) emgDataList.RemoveRange(emgDataList.Count - 300, 300);
                    GestureResult = _emgClassifier.Predict(dataWin);
                }
                catch (Exception)
                {
                    //                    Console.WriteLine(e);
                }
            }
        }
    }
}
