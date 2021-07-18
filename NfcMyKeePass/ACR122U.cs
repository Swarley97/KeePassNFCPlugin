using PCSC;
using PCSC.Monitoring;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace NfcMyKeePass
{
    internal class ACR122U : IDisposable
    {
        private ISCardMonitor _cardMonitor;

        private int _maxReadWriteLength = 50;
        private int _blockSize = 4;
        private int _startBlock = 4;
        private int _readbackDelayMilliseconds = 100;
        private string[] _cardReaderNames;
        private ISCardContext _cardContext;
        private bool _buzzerSet;
        private bool _buzzerOnOff;

        public event EventHandler<ICardReader> CardInserted;
        public event EventHandler CardRemoved;

        public void Init(bool buzzerOnOff, int maxReadWriteLength, int blockSize, int startBlock, int readbackDelayMilliseconds)
        {
            _buzzerOnOff = buzzerOnOff;
            _maxReadWriteLength = maxReadWriteLength;
            _blockSize = blockSize;
            _startBlock = startBlock;
            _readbackDelayMilliseconds = readbackDelayMilliseconds;
            _cardContext = ContextFactory.Instance.Establish(SCardScope.System);
            _cardReaderNames = _cardContext.GetReaders();

            _cardMonitor = MonitorFactory.Instance.Create(SCardScope.System);
            _cardMonitor.CardInserted += Monitor_CardInserted;
            _cardMonitor.CardRemoved += Monitor_CardRemoved;
            _cardMonitor.Start(_cardReaderNames);
        }

        public CardReaderSession GetCurrentCardReader()
        {
            try
            {
                ICardReader cardReader = _cardContext.ConnectReader(_cardReaderNames[0], SCardShareMode.Shared, SCardProtocol.Any);
                return new CardReaderSession(cardReader);
            }
            catch
            {
                return null;
            }
        }

        private void Monitor_CardInserted(object sender, CardStatusEventArgs e)
        {
            ICardReader cardReader = null;
            try
            {
                cardReader = _cardContext.ConnectReader(_cardReaderNames[0], SCardShareMode.Shared, SCardProtocol.Any);
            }
            catch(Exception ex)
            {
                Debug.Fail(ex.ToString());
            }
            if (cardReader != null)
            {
                if (!_buzzerSet)
                {
                    _buzzerSet = true;
                    SetBuzzer(cardReader, _buzzerOnOff);
                }
                CardInserted?.Invoke(this, cardReader);
                try
                {
                    cardReader.Disconnect(SCardReaderDisposition.Leave);
                }
                catch (Exception ex)
                {
                    Debug.Fail(ex.ToString());
                }
            }
        }

        private void Monitor_CardRemoved(object sender, CardStatusEventArgs e)
        {
            try
            {
                CardRemoved?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Debug.Fail(ex.ToString());
            }
        }

        public byte[] GetUID(ICardReader reader)
        {
            byte[] array = new byte[10];
            reader.Transmit(new byte[5] { 255, 202, 0, 0, 0 }, array);
            Array.Resize(ref array, 7);
            return array;
        }

        public byte[] Read(ICardReader reader, int block, int len)
        {
            byte[] array = new byte[len + 2];
            reader.Transmit(new byte[5]
            {
                255,
                176,
                0,
                (byte)block,
                (byte)len
            }, array);
            Array.Resize(ref array, len);
            return array;
        }

        public void Write(ICardReader reader, int block, int len, byte[] data)
        {
            byte[] receiveBuffer = new byte[2];
            List<byte> list = new byte[5]
            {
                255,
                214,
                0,
                (byte)block,
                (byte)len
            }.ToList();
            list.AddRange(data);
            reader.Transmit(list.ToArray(), receiveBuffer);
        }

        public bool WriteData(ICardReader reader, byte[] data)
        {
            Array.Resize(ref data, _maxReadWriteLength);
            for (int i = 0; i < data.Length; i += _blockSize)
            {
                byte[] array = new byte[_blockSize];
                int length = ((data.Length - i > _blockSize) ? _blockSize : (data.Length - i));
                Array.Copy(data, i, array, 0, length);
                Write(reader, i / _blockSize + _startBlock, _blockSize, array);
            }
            Thread.Sleep(_readbackDelayMilliseconds);
            byte[] second = ReadData(reader);
            return data.SequenceEqual(second);
        }

        public byte[] ReadData(ICardReader reader)
        {
            List<byte> list = new List<byte>();
            for (int i = 0; i < _maxReadWriteLength; i += _blockSize)
            {
                int len = ((_maxReadWriteLength - i > _blockSize) ? _blockSize : (_maxReadWriteLength - i));
                byte[] collection = Read(reader, i / _blockSize + _startBlock, len);
                list.AddRange(collection);
            }
            return list.ToArray();
        }

        public void SetBuzzer(ICardReader reader, bool on)
        {
            byte[] receiveBuffer = new byte[2];
            reader.Transmit(new byte[5]
            {
                255,
                0,
                82,
                (byte)(on ? 255u : 0u),
                0
            }, receiveBuffer);
        }

        public void Dispose()
        {
            if (_cardMonitor != null)
            {
                _cardMonitor.CardInserted += Monitor_CardInserted;
                _cardMonitor.CardRemoved += Monitor_CardRemoved;

                _cardMonitor.Dispose();
            }
        }
    }
}
