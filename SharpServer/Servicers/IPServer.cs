using System.Net;
using System.Text.RegularExpressions;
using Uri = System.Uri;

namespace SharpServer.Services {
    /// <summary>
    /// Enumerator that represents the different support web IP APIs.
    /// </summary>
    public enum IPAPI {
        IPIfy, IPInfo, IPAPI, CheckIPDyDNS
    }

    /// <summary>
    /// Provides an interface for prompting web IP APIs for IP address info.
    /// </summary>
    public class IPServicer {
        private const string webIPify = "https://api.ipify.org";
        private const string webIPInfo = "http://ipinfo.io/ip";
        private const string webIPAPI = "http://ip-api.com/json";
        private const string webCheckIPDynDNS = "http://checkip.dyndns.org/";

        /// <summary>
        /// Gets your external router IP address from a public web IP API.
        /// <para>See System.Net.WebClient.DownloadString(string) on MSDN for exception info.</para>
        /// </summary>
        /// <param name="api">The API to get your IP Address from.</param>
        /// <param name="timeOut">The timeout in milliseconds before the function fails.</param>
        /// <returns>Returns a new IPAddress with the resuling IP address from the IP API, else null on fail/timeout.</returns>
        /// <exception cref="System.ArgumentNullException"/>
        /// <exception cref="System.Net.WebException"/>
        /// <exception cref="System.NotSupportedException"/>
        public virtual IPAddress IPFromAPI( IPAPI api, int timeOut ) {
            try {
                using( WebTimeoutClient client = new WebTimeoutClient( timeOut ) ) {
                    string webAddress = null, response = string.Empty;
                    IPAddress address = null;
                    
                    switch( api ) {
                        case IPAPI.IPIfy:
                            webAddress = webIPify;
                        break;
                        case IPAPI.IPInfo:
                            webAddress = webIPInfo;
                        break;
                        case IPAPI.IPAPI:
                            response = client.DownloadString( webIPAPI );
                            int index = response.IndexOf( "query" ) + 8;
                            int length = response.IndexOf( "region" ) - 3 - index;
                            if ( IPAddress.TryParse( response, out address ) ) return address;
                        return null;
                        case IPAPI.CheckIPDyDNS:
                            response = client.DownloadString( webCheckIPDynDNS );
                            string IP = new Regex( @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}" ).Match( response ).Value;
                            if ( IPAddress.TryParse( response, out address ) ) return address;
                        return null;
                    }

                    response = client.DownloadString( webAddress );
                    if ( IPAddress.TryParse( response, out address ) ) return address;
                    return null;
                }
            } catch( WebException ex ) {
                if ( ex.Status == WebExceptionStatus.Timeout ) return null;
                throw ex;
            }
        }

        /// <summary>
        /// Attempts to get your IP from one of several API until one returns an IP address.
        /// <para>See System.Net.WebClient.DownloadString(string) on MSDN for exception info.</para>
        /// </summary>
        /// <param name="timeOut"></param>
        /// <param name="apiOrder"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"/>
        /// <exception cref="System.Net.WebException"/>
        /// <exception cref="System.NotSupportedException"/>
        public IPAddress AutoIPFromAPI( int timeOut, IPAPI[] apiOrder = null ) {
            if ( apiOrder == null ) return null;

            foreach( IPAPI type in apiOrder ) {
                IPAddress address = IPFromAPI( type, timeOut );
                if ( address != null ) return address;
            }

            return null;
        }

        /// <summary>
        /// Gets the web address for the particular IP API.
        /// </summary>
        /// <param name="api">The API to get the web address(URL) of.</param>
        /// <returns>Returns the web address(URL) of the specified IP API.</returns>
        public virtual string APIWebAddress( IPAPI api ) {
            switch( api ) {
                case IPAPI.IPIfy:
                    return webIPify;
                case IPAPI.IPInfo:
                    return webIPInfo;
                case IPAPI.IPAPI:
                    return webIPAPI;
            }

            return null;
        }
    }
}