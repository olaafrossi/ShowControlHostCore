using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Sockets;
using System.Threading;

namespace ShowControlHostCoreConsoleUI
{
    public class AsyncNetworkLink : IDisposable, INotifyPropertyChanged
    {
        //private readonly ILog log = LogManager.GetLogger(typeof(AsyncNetworkLink));

        //Observable Interface
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info) 
        {
            if(PropertyChanged != null) 
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        private const int BUF_SIZE = 8092;
        private const int MAX_DATA_SIZE = 100;


        #region Public Properties
        public string Address { get; private set; }
        public int Port { get; private set; }

        public bool HasData 
        {
            get 
            {
                return (_incomingData.Count > 0);
            }
        }


        private bool _enabled;
        /// <summary>
        /// Gets or sets a value indicating whether messages should be propogated to the network or not
        /// </summary>
        public bool Enabled 
        {
            get 
            {
                return _enabled;
            }
            set 
            {
                _enabled = value;
                if(!_enabled) 
                {
                    SafeClose();
                } else if(!IsConnected) 
                {
                    SafeConnect();
                }
                NotifyPropertyChanged("Enabled");
            }
        }

        private bool _isConnected;
        /// <summary>
        /// Gets a value indicating whether or not there is current network activity with this node
        /// </summary>
        public bool IsConnected 
        {
            get
            {
             return _isConnected;   
            }
            private set 
            {
                if(value != _isConnected) 
                {
                    _isConnected = value;
                    NotifyPropertyChanged("IsConnected");
                }
            }
        }

        public event EventHandler DataReceived;

        private Exception _error;
        public Exception Error 
        {
            get 
            {
                return _error;
            }
            set {
                Exception oldError = _error;
                _error = value;
                if(oldError != _error) 
                {
                    NotifyPropertyChanged("Error");
                }
            }
        }
        #endregion //Public Properties

        private List<byte[]> _incomingData;

        private TcpClient _tcpClient;
        private object _clientLock = new object();

        private NetworkStream _networkStream;

        private IAsyncResult _connectResult = null;
        private IAsyncResult _readResult = null;
        private IAsyncResult _writeResult = null;


        public AsyncNetworkLink(string address, int port, bool enabled = true) 
        {
            Address = address;
            Port = port;

            _incomingData = new List<byte[]>();

            Enabled = enabled;  //Default is true
        }

        private bool _disposed = false;
        /// <summary>
        /// Implementation of IDisposable interface.  Cancels the thread and releases resources.
        /// Clients of this class are responsible for calling it.
        /// </summary>
        public void Dispose() 
        {
            if(_disposed) 
            {
                return;  //Dispose has already been called
            }
            _disposed = true;
            //log.Info("Cleaning up network resources");

            SafeClose();
        }

        /// <summary>
        /// Very carefully checks and shuts down the tcpClient and sets it to null
        /// </summary>
        private void SafeClose() 
        {
            //log.Debug("Safe Close");
            lock(_clientLock) 
            {
                //Resolve outstanding connections
                if(_connectResult != null) 
                {
                    //End the connection process
                    _connectResult = null;
                }
                if(_readResult != null) 
                {
                    //End the read process
                    _readResult = null;
                }
                if(_writeResult != null) 
                {
                    //End the write process
                    _writeResult = null;
                }

                _networkStream = null;
                if(_tcpClient != null) 
                {
                    if(_tcpClient.Client != null) 
                    {
                        _tcpClient.Client.Close();
                    }
                    _tcpClient.Close();
                }
                _tcpClient = null;

                lock(_incomingData) 
                {
                    _incomingData.Clear();
                }
            }
            IsConnected = false;
        }

        /// <summary>
        /// Carefully check to see if the link is connected or can be reestablished
        /// </summary>
        private void SafeConnect() 
        {
            if(_disposed) 
            {
                return;
            }

            lock(_clientLock) 
            {
                if(_connectResult != null) 
                {
                    //There is already a connection attempt in progress
                    return;
                }
               
                if(_tcpClient == null || !_tcpClient.Connected) 
                {
                    SafeClose();
                    _tcpClient = new TcpClient();
                }
               
                //See if the TCP connection is open
                if(!_tcpClient.Connected) 
                {
                    //log.Info("Connecting: " + Address + " / " + Port);
                    try 
                    {
                        _connectResult = _tcpClient.BeginConnect(Address, Port, ConnectCallback, null);
                    } 
                    catch(Exception ex) 
                    {
                        //log.Error("Connection Error", ex);
                        Error = ex;
                        IsConnected = false;
                    }
                }
            }
        }

        private void SafeConnect(object state) 
        {
            //DateTime started = (DateTime)state;
            //log.Debug("Waited " + (DateTime.Now - started));
            SafeConnect();
        }

        private void ConnectCallback(IAsyncResult asyncResult) 
        {
            //log.Info("Connect Callback: " + Address + " / " + Port);
            lock(_clientLock) 
            {
                try 
                {                 
                    _networkStream = null;
                    if(_tcpClient != null) 
                    {
                        _tcpClient.EndConnect(asyncResult);
                        IsConnected = _tcpClient.Connected;
                        _networkStream = _tcpClient.GetStream();

                        Error = null;
                        IsConnected = true;
                    } else 
                    {
                        IsConnected = false;
                    }
                    if(!Enabled) 
                    {
                        SafeClose();
                    }
                }
                catch(Exception ex)
                {
                    //log.Error("Connection Error", ex);
                    Error = ex;
                    IsConnected = false;
                }
                if(_connectResult == asyncResult) 
                {
                    //log.Debug("Clearing Connect Result");
                    _connectResult = null;
                    if(Enabled) 
                    {
                        if(!IsConnected) 
                        {
                            Timer timer = new Timer(SafeConnect, DateTime.Now, TimeSpan.FromSeconds(3), TimeSpan.FromMilliseconds(-1));
                        } else 
                        {
                            ReceiveData();//Begin listening for data
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Asynchronously sends the tcp message, waiting until the connection is reestablihsed if necessary
        /// </summary>
        /// <param name="message">binary message to be sent</param>
        public void SendMessage(byte[] message)
        {
            if(Enabled) 
            {
                lock(_clientLock) 
                {
                    if(_networkStream != null) 
                    {
                        try 
                        {
                            _writeResult = _networkStream.BeginWrite(message, 0, message.Length, WriteCallback, null);  //May throw SocketException
                        } 
                        catch(Exception ex) 
                        {
                            //log.Error("Cannot Write", ex);
                            Error = ex;
                            IsConnected = false;
                            SafeClose();
                            SafeConnect();
                        }
                    } else 
                    {
                        IsConnected = false;
                        SafeConnect();
                    }
                }
            }
        }

        private void WriteCallback(IAsyncResult asyncResult) 
        {

            lock(_clientLock) 
            {
                try 
                {
                    _networkStream.EndWrite(asyncResult);
                    Error = null;
                    IsConnected = true;
                } 
                catch(Exception ex) 
                {
                    //log.Error("Error writing to stream", ex);
                    Error = ex;
                    SafeConnect();
                }
                if(_writeResult == asyncResult) 
                {
                    //log.Debug("Clearing Write Result");
                    _writeResult = null;
                }
            }
        }

        private void ReceiveData() 
        {
            if(Enabled) 
            {
                byte[] buf = new byte[BUF_SIZE];
                lock(_clientLock) 
                {
                    if(_networkStream != null) 
                    {
                        try 
                        {
                            _readResult = _networkStream.BeginRead(buf, 0, buf.Length, ReadCallback, buf);
                            Error = null;
                            IsConnected = true;
                        } 
                        catch(Exception ex) 
                        {
                            //log.Error("Error reading from stream", ex);
                            Error = ex;
                            IsConnected = false;
                            SafeConnect();
                        }  
                    }
                }
            }
        }

        private void ReadCallback(IAsyncResult asyncResult) 
        {

            byte[] buffer = (byte[])(asyncResult.AsyncState);
            bool hasNewData = false;
            int bytesRead = 0;

            lock(_clientLock) 
            {
                try 
                {
                    if(_networkStream != null) 
                    {
                        bytesRead = _networkStream.EndRead(asyncResult);
                    }
                    //If the remote host shuts down the Socket connection and all available data has been received, the EndRead method completes immediately and returns zero bytes.
                    if(bytesRead == 0) 
                    {
                        SafeClose();
                        SafeConnect();
                    }
                    if(bytesRead > 0) 
                    {
                        lock(_incomingData) 
                        {
                            byte[] truncatedBuffer = new byte[bytesRead];
                            Array.Copy(buffer, truncatedBuffer, bytesRead);
                            _incomingData.Add(truncatedBuffer);
                            hasNewData = true;
                            if(_incomingData.Count > MAX_DATA_SIZE) 
                            {
                                //Purge messages from the end of the list to prevent overflow
                                //log.Error("Too many incoming messages to handle: " + _incomingData.Count);
                                _incomingData.RemoveAt(_incomingData.Count - 1);
                            }
                        }
                        Error = null;
                        IsConnected = true;
                    }

                } 
                catch(Exception ex) 
                {
                    //log.Error("Error Reading from stream", ex);
                    Error = ex;
                    //Try to reopen the connection
                    SafeConnect();
                }
                if(_readResult == asyncResult) 
                {
                    //log.Debug("Clearing Read Result");
                    _readResult = null;
                }
            }
            
            if(hasNewData && DataReceived != null && !_disposed) 
            {
                DataReceived(this, new EventArgs());
            }

            ReceiveData();
        }

        /// <summary>
        /// Fetches and removes (pops) the next available group of bytes as received on this link in order (FIFO)
        /// </summary>
        /// <returns>null if the link is not Enabled or there is no data currently queued to return, an array of bytes otherwise.</returns>
        public byte[] GetMessage() 
        {
            if(_disposed) 
            {
                throw new ObjectDisposedException("Cannot get message from disposed NetworkLink");
            }

            //Return null if the link is not enabled
            if(!Enabled) return null;

            byte[] newMessage = null;
            lock(_incomingData) 
            {
                if(HasData) 
                {
                    newMessage = _incomingData[0];
                    _incomingData.RemoveAt(0);
                }
            }
            return newMessage;
        }
    }
}
