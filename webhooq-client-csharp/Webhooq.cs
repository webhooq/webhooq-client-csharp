using System;
using System.Net;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Web;
using System.Collections.Specialized;


namespace webhooq
{
	public enum ExchangeType {Direct, Fanout, Topic};

	public class WebhooqClient
	{
		private Uri BaseUri;

		public WebhooqClient (Uri BaseUri)
		{
			this.BaseUri = BaseUri;
		}

		public void declareExchange(ExchangeType Type, String Name) {
			Uri u = new AccumulatingUri (this.BaseUri).appendPaths("exchange",Name).appendQueryParam("type",Type.ToString().ToLower()).toUri();
			HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create (u);
			req.Method = "POST";
			HttpWebResponse resp = (HttpWebResponse)req.GetResponse ();
			if (resp.StatusCode != HttpStatusCode.Created) {
				throw new Exception(String.Format("Expected Declare Exchange to return a 201 Created status code, got %s instead", resp.StatusCode.ToString()));
			}
		}

		public void deleteExchange(String Name) {
			Uri u = new AccumulatingUri (this.BaseUri).appendPaths("exchange",Name).toUri();
			HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create (u);
			req.Method = "DELETE";
			HttpWebResponse resp = (HttpWebResponse)req.GetResponse ();
			if (resp.StatusCode != HttpStatusCode.Created) {
				throw new Exception(String.Format("Expected Delete Exchange to return a 204 No Content status code, got %s instead", resp.StatusCode.ToString()));
			}
		}

		public void declareQueue(String Name) {
			Uri u = new AccumulatingUri (this.BaseUri).appendPaths("queue",Name).toUri();
			HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create (u);
			req.Method = "POST";
			HttpWebResponse resp = (HttpWebResponse)req.GetResponse ();
			if (resp.StatusCode != HttpStatusCode.Created) {
				throw new Exception(String.Format("Expected Declare Queue to return a 201 Created status code, got %s instead", resp.StatusCode.ToString()));
			}
		}
		
		public void deleteQueue(String Name) {
			Uri u = new AccumulatingUri (this.BaseUri).appendPaths("queue",Name).toUri();
			HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create (u);
			req.Method = "DELETE";
			HttpWebResponse resp = (HttpWebResponse)req.GetResponse ();
			if (resp.StatusCode != HttpStatusCode.Created) {
				throw new Exception(String.Format("Expected Delete Queue to return a 204 No Content status code, got %s instead", resp.StatusCode.ToString()));
			}
		}

		public void bindExchange(String SourceName, String RoutingKey, String DestinationName) {
			Uri u = new AccumulatingUri (this.BaseUri).appendPaths("exchange",SourceName,"bind").toUri();
			HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create (u);
			req.Headers ["x-wq-exchange"] = DestinationName;
			req.Method = "POST";
			HttpWebResponse resp = (HttpWebResponse)req.GetResponse ();
			if (resp.StatusCode != HttpStatusCode.Created) {
				throw new Exception(String.Format("Expected Bind Exchange to return a 201 Created status code, got %s instead", resp.StatusCode.ToString()));
			}
		}
		
		public void bindQueue(String SourceName, String RoutingKey, String DestinationName, Link webhook) {
			Uri u = new AccumulatingUri (this.BaseUri).appendPaths("exchange",SourceName,"bind").toUri();
			HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create (u);
			req.Headers ["x-wq-queue"] = DestinationName;
			req.Headers ["x-wq-link"] = webhook.ToString ();
			req.Method = "POST";
			HttpWebResponse resp = (HttpWebResponse)req.GetResponse ();
			if (resp.StatusCode != HttpStatusCode.Created) {
				throw new Exception(String.Format("Expected Bind Queue to return a 201 Created status code, got %s instead", resp.StatusCode.ToString()));
			}
		}

		public void unbindExchange(String SourceName, String RoutingKey, String DestinationName) {

		}
		
		public void unbindQueue() {

		}

		public void publish (String Name, String RoutingKey, String MimeType) {

		}

	}

	public class Link : List<LinkValue> {
		public override string ToString ()
		{
			StringBuilder s = new StringBuilder ();
			bool firstValue = true;
			foreach (LinkValue v in this) {
				if (firstValue)
					firstValue = false;
				else 
					s.Append (",");

				s.Append ("<");
				s.Append (v.uri.ToString ());
				s.Append (">");
				foreach (KeyValuePair<String,String> kv in v.LinkParams) {
					s.Append (";");
					s.Append (kv.Key);
					s.Append ("=\"");
					s.Append (kv.Value);
					s.Append ("\"");
				}
			}
			return s.ToString ();
		}
	}

	public class LinkValue {
		public Uri uri;
		public Dictionary<String,String> LinkParams;

		//convenience constructor
		public LinkValue (Uri uri, params String[] lps) {
			this.uri = uri;
			if (lps.Length % 2 == 0) {
				for (int i=0; i<lps.Length; i++) {
					LinkParams.Add (lps[i], lps [++i]);
				}
			}
		}
	}

	public class AccumulatingUri {
		public String Scheme;
		public String Host;
	
		public List<String> PathSegments = new List<String>();
		public Dictionary<String, List<String>> QueryParams = new Dictionary<String, List<String>>();

		public AccumulatingUri (Uri source) {
			this.Scheme = source.Scheme;
			this.Host = source.Host;
		}

		public AccumulatingUri appendPaths (params String[] Paths) {
			foreach (String PathSegment in Paths) {
				this.PathSegments.Add (PathSegment);
			}
			return this;
		}

		public AccumulatingUri appendQueryParam (String Key, String Value) {
			if (!this.QueryParams.ContainsKey (Key))
				this.QueryParams.Add (Key, new List<String> ());
			this.QueryParams [Key].Add (Value);
			return this;
		}

		public Uri toUri () {
			UriBuilder builder = new UriBuilder ();
			builder.Scheme = this.Scheme;
			builder.Host = this.Host;

			StringBuilder pathAccumulator = new StringBuilder ();
			foreach (String PathSegment in PathSegments) {
				pathAccumulator.Append ("/").Append (PathSegment);
			}
			builder.Path = pathAccumulator.ToString();

			
			StringBuilder queryAccumulator = new StringBuilder ();
			bool first = true;
			foreach (KeyValuePair<String, List<String>> kv in QueryParams) {
				if (first) {
					queryAccumulator.Append ("?");
					first = false;
				} else {
					queryAccumulator.Append ("&");
				}
				foreach (String value in kv.Value) {
					queryAccumulator.Append (kv.Key);
					queryAccumulator.Append ("=");
					queryAccumulator.Append (HttpUtility.UrlEncode(value));
				}
			}
			builder.Query = queryAccumulator.ToString();
			return builder.Uri;
		}
	}
}

