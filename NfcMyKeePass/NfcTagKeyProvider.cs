using KeePassLib.Keys;
using System.Windows.Forms;

namespace NfcMyKeePass
{
    public class NfcTagKeyProvider : KeyProvider
    {
        public override string Name => "Unlock via NFC tag";

        public override byte[] GetKey(KeyProviderQueryContext ctx)
        {
            NfcPrompt prompt = new NfcPrompt();
            if(prompt.Result != null)
            {
                return prompt.Result; // there is already an nfc tag.
            }

            prompt.Owner = Form.ActiveForm;
            prompt.Icon = Form.ActiveForm.Icon;

            if (prompt.ShowDialog() == DialogResult.OK)
            {
                return prompt.Result;
            }
         
            return null;
        }
    }
}
