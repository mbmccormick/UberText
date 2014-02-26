using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp;

namespace UberText.Geocoding
{
    public class GeocodeClient
    {
        public void GeocodeAddress(string address, bool sensor, Action<GeocodeResponse> callback)
        {
            if (address == null) throw new ArgumentNullException("address");

            var url = string.Format(
                "http://maps.googleapis.com/maps/api/geocode/json?address={0}&sensor={1}",
                Uri.EscapeDataString(address),
                sensor.ToString().ToLower());

            RestClient client = new RestClient();
            RestRequest request = new RestRequest(url, Method.GET);

            client.ExecuteAsync(request, (response) =>
            {
                MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(response.Content));
                StreamReader sr = new StreamReader(stream);

                JsonTextReader tr = new JsonTextReader(sr);
                GeocodeResponse data = new JsonSerializer().Deserialize<GeocodeResponse>(tr);

                callback(data);
            });
        }
    }
}
