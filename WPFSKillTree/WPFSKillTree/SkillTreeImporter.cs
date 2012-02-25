using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Diagnostics;

namespace POESKillTree
{
    public static class SkillTreeImporter
    {
        /// <summary>
        /// Loads from the unofficial online tool
        /// </summary>
        public static void LoadBuildFromPoezone( SkillTree tree, string buildUrl )
        {
            if ( !buildUrl.Contains( '#' ) ) throw new FormatException( );

            const string dataUrl = "http://poezone.ru/skilltree/data.js";
            const string buildPostUrl = "http://poezone.ru/skilltree/";
            string build = buildUrl.Substring( buildUrl.LastIndexOf( '#' ) + 1 );

            string dataFile, buildFile;
            {
                HttpWebRequest req = ( HttpWebRequest )WebRequest.Create( dataUrl );
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/534.30 (KHTML, like Gecko) Iron/12.0.750.0 Chrome/12.0.750.0 Safari/534.30";
                WebResponse resp = req.GetResponse( );
                dataFile = new StreamReader( resp.GetResponseStream( ) ).ReadToEnd( );
            }

            {
                string postData = "build:" + build;
                byte[] postBytes = Encoding.Default.GetBytes( postData );
                HttpWebRequest req = ( HttpWebRequest )WebRequest.Create( buildPostUrl );
                req.Method = "POST";
                req.ContentLength = postBytes.Length;
                req.ContentType = "application/x-www-form-urlencoded";
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/534.30 (KHTML, like Gecko) Iron/12.0.750.0 Chrome/12.0.750.0 Safari/534.30";
                req.Accept = "application/json, text/javascript, */*; q=0.01";
                req.Host = "poezone.ru";
                req.Referer = "http://poezone.ru/skilltree/";
                req.AutomaticDecompression = DecompressionMethods.GZip;
                //req.Headers.Add( "Accept", "application/json, text/javascript" );
                req.Headers.Add( "Accept-Charset", "ISO-8859-1,utf-8;q=0.7,*;q=0.3" );
                req.Headers.Add( "Accept-Encoding", "gzip,deflate,sdch" );
                req.Headers.Add( "Accept-Language", "en-US,en;q=0.8" );

                //req.Headers.Add( "Connection", "keep-alive" );
                //req.Headers.Add( "Host", "poezone.ru" );
                //req.Headers.Add( "Origin", "http://poezone.ru" );
                //req.Headers.Add( "Referer", "http://poezone.ru/skilltree/" );
                //req.Headers.Add( "User-Agent", );
                req.Headers.Add( "X-Requested-With", "XMLHttpRequest" );
                Stream dataStream = req.GetRequestStream( );
                dataStream.Write( postBytes, 0, postBytes.Length );
                dataStream.Close( );
                WebResponse resp = req.GetResponse( );
                buildFile = new StreamReader( resp.GetResponseStream( ) ).ReadToEnd( );
            }

            Debugger.Break( );

            //string dataFile = 
        }
    }
}
