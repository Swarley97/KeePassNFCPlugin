using PCSC;
using System;

namespace NfcMyKeePass
{
    public class CardReaderSession : IDisposable
    {
		public ICardReader CardReader { get; }

        public CardReaderSession(ICardReader cardReader)
        {
            CardReader = cardReader;
        }

        public void Dispose()
        {
			CardReader.Disconnect(SCardReaderDisposition.Leave);
        }
    }
}
