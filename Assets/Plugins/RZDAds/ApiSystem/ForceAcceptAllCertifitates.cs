using UnityEngine.Networking;

// Overrides standard certificate verification method. when requesting a certificate, method allways returns certificate validation  => true 
	namespace Plugins.RZDAds.ApiSystem
	{
		public class ForceAcceptAllCertificates : CertificateHandler {
			protected override bool ValidateCertificate(byte[] certificateData) => true;
		}
	}
