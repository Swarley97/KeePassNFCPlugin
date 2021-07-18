using NdefLibrary.Ndef;
using PCSC;
using System;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace NfcMyKeePass
{
    public partial class NfcPrompt : Form
    {
        private ACR122U _acr122U;
        private SynchronizationContext _syncContext;

        public NfcPrompt()
        {
            InitializeComponent();

            _acr122U = new ACR122U();
            _acr122U.Init(false, 50, 4, 4, 200);

            using CardReaderSession session = _acr122U.GetCurrentCardReader();
            if(session != null && TryGetMessageFromReader(session.CardReader, out byte[] message))
            {
                Result = message;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        protected override void OnShown(EventArgs e)
        {
            _syncContext = SynchronizationContext.Current;

            base.OnShown(e);

            _acr122U.CardInserted += Acr122u_CardInserted;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _acr122U.CardInserted -= Acr122u_CardInserted;
            _acr122U.Dispose();
        }

        public byte[] Result { get; private set; }

        private void Acr122u_CardInserted(ICardReader reader)
        {
            if (TryGetMessageFromReader(reader, out byte[] message))
            {
                _syncContext.Post(x =>
                {
                    Result = message;
                    DialogResult = DialogResult.OK;
                    Close();

                }, null);
            }
        }

        private bool TryGetMessageFromReader(ICardReader reader, out byte[] message)
        {
            byte[] tagData = _acr122U.ReadData(reader); // byte[]
            NdefMessage msg = new();
            int offset = 0;
            while (offset < tagData.Length)
            {
                byte tag = tagData[offset++];
                int len = (tagData[offset++] & 0x0FF);
                if (len == 255)
                {
                    len = ((tagData[offset++] & 0x0FF) << 8);
                    len |= (tagData[offset++] & 0x0FF);
                }
                if (tag == (byte)0x03)
                {
                    byte[] msgBytes = new byte[len];
                    Array.Copy(tagData, offset, msgBytes, 0, len);
                    msg = NdefMessage.FromByteArray(msgBytes);
                }
                else if (tag == (byte)0xFE)
                {
                    break;
                }
                offset += len;
            }

            if (msg.Count > 0)
            {
                NdefRecord firstRecord = msg[0];
                NdefTextRecord textRecord = new(firstRecord);
                message = Encoding.UTF8.GetBytes(textRecord.Text);
                return true;
            }

            message = null;
            return false;
        }
    }
}
