using KeePass.Plugins;

namespace NfcMyKeePass
{
    public class NfcMyKeePassExt : Plugin
    {
		private IPluginHost _pluginHost;
        private NfcTagKeyProvider _keyProvider = new();

		public override bool Initialize(IPluginHost host)
		{
			if (host == null) 
				return false;

			_pluginHost = host;
            _pluginHost.KeyProviderPool.Add(_keyProvider);

			return true;
		}

        public override string UpdateUrl => "https://github.com/brendes333/KeePassNFCPlugin/blob/master/Version.txt";

        public override void Terminate()
        {
            _pluginHost.KeyProviderPool.Remove(_keyProvider);
            base.Terminate();
        }
    }
}
