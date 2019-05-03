
using Affirma.ThreeSharp.Model;
using Affirma.ThreeSharp.Wrapper;
using Amazon.CloudFront;
using Amazon.CloudFront.Util;
using Amazon.Util;
using System;
using System.Collections.Generic;
using System.Net;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace roxority_S3Sync {

	public static class Program {

		public static void Main (string [] args) {
			bool done = false;
			string result, cfDate, cfVersion = "2010-08-01", cfContent, cfKey1 = "AKIAJARKKNFC2QKIOQFA", cfKey2 = "qMlOAQioqwN0ho0XyDOYfl/V1GXEivZHYPyo//Qu", cfSign;
			ThreeSharpWrapper s3 = new ThreeSharpWrapper (cfKey1, cfKey2);
			SecureString cfSecret = new SecureString ();
			s3.config.IsSecure = false;
			Console.Write ("Upload " + args [0] + "? Y/N");
			if ("y".Equals (Console.ReadLine (), StringComparison.InvariantCultureIgnoreCase))
				while (!done)
					try {
						Console.Write ("Uploading...");
						s3.AddFileObject ("roxority", args [0] + ".zip", @"c:\s3sync\" + args [0] + ".zip");
						Console.WriteLine ("Done.");
						done = true;
					} catch (Exception ex) {
						Console.WriteLine ("\r\n" + ex.Message + " ---retrying.");
						done = false;
					}
			done = false;
			while (!done)
				try {
					Console.Write ("Setting permissions...");
					using (ACLChangeRequest request = new ACLChangeRequest ("roxority", args [0] + ".zip?acl", "public-read"))
						s3.service.ACLChange (request).Dispose ();
					Console.WriteLine ("Done.");
					done = true;
				} catch (Exception ex) {
					Console.WriteLine ("\r\n" + ex.Message + " ---retrying.");
					done = false;
				}
			done = false;
			cfDate = AmazonCloudFrontUtil.FormattedCurrentTimestamp;
			cfContent = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><InvalidationBatch xmlns=\"http://cloudfront.amazonaws.com/doc/" + cfVersion + "/\"><Path>/" + args [0] + ".zip</Path><CallerReference>" + DateTime.Now.Ticks + "</CallerReference></InvalidationBatch>";
			foreach (char c in cfKey2)
				cfSecret.AppendChar (c);
			using (AmazonCloudFrontClient cfc = new Amazon.CloudFront.AmazonCloudFrontClient (cfKey1, cfSecret, new AmazonCloudFrontConfig ()))
			using (WebClient wc = new WebClient ())
				while (!done)
					try {
						Console.Write ("CloudFront invalidation...");
						wc.Headers ["x-amz-date"] = cfDate;
						wc.Headers [HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded; charset=utf-8";
						using (HMACSHA1 sha1 = new HMACSHA1 ())
							cfSign = AWSSDKUtils.HMACSign (cfDate, cfSecret, sha1);
						wc.Headers [HttpRequestHeader.Authorization] = "AWS " + cfKey1 + ":" + cfSign;
						if (string.IsNullOrEmpty (result = wc.UploadString ("https://cloudfront.amazonaws.com/" + cfVersion + "/distribution/E14JHINLXE5EL1/invalidation", cfContent)) || !result.Contains ("<Status>InProgress</Status>"))
							throw new Exception ("\r\n" + result + "\r\n");
						Console.WriteLine ("Done.");
						done = true;
					} catch (Exception ex) {
						Console.WriteLine ("\r\n" + ex.Message + " ---retrying.");
						done = false;
					}
			Console.Beep ();
			Console.WriteLine ("DONE");
			Console.ReadLine ();
		}

	}

}
