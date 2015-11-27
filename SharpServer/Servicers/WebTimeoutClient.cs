using System.Net;

namespace SharpServer.Services {
    /// <summary>
    /// Provides common methods for sending data to and receiving data from a resource identified by a URI with an explicit timeout in milliseconds.
    /// </summary>
    public class WebTimeoutClient : WebClient {
       /// <summary>
        /// Timeout in milliseconds for web requests.
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// Instantiates an instance of the Sharp_Server.Services.WebTimeoutClient class with the specified timeout in milliseconds.
        /// </summary>
        /// <param name="timeOut">The timeout in milliseconds.</param>
        public WebTimeoutClient( int timeOut ) {
            Timeout = timeOut;
        }

        /// <summary>
        /// Returns a System.Net.WebRequest object for the specified resource.
        /// </summary>
        /// <param name="address">A System.Uri that identifies the resource to request.</param>
        /// <returns>A new System.Net.WebRequest object for the specified resource.</returns>
        protected override WebRequest GetWebRequest(System.Uri address) {
            WebRequest request = base.GetWebRequest(address);
            request.Timeout = Timeout;
            return request;
        }
    }
}