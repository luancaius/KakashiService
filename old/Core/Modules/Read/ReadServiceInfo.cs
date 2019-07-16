﻿using KakashiService.Core.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Xml;

namespace KakashiService.Core.Modules.Read
{
    public class ReadServiceInfo
    {
        private List<XmlDocument> _xmlDocs;

        public ReadServiceInfo()
        {
            _xmlDocs = new List<XmlDocument>();
        }

        private XmlDocument GetXmlFromStream(Stream stream)
        {
            XmlDocument xmlDoc = new XmlDocument();
            try
            {
                if (stream != null)
                {
                    xmlDoc.Load(stream);
                }
                return xmlDoc;
            }
            catch (WebException e)
            {
                throw new WebException("Error while reading service. Check if it is online.", e);
            }
            catch (Exception e)
            {
                throw new WebException("Error while reading service. General error", e);
            }
        }

        private XmlDocument GetXmlFromUrl(String url)
        {
            var xml = new XmlDocument();
            try
            {
                UriBuilder uriBuilder = new UriBuilder(url);

                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uriBuilder.Uri);
                webRequest.ContentType = "text/xml;charset=\"utf-8\"";
                webRequest.Method = "GET";
                webRequest.Accept = "text/xml";

                using (WebResponse response = webRequest.GetResponse())
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        xml.Load(stream);
                    }
                }
                return xml;
            }
            catch (WebException e)
            {
                throw new WebException("Error while reading service. Check if it is online.", e);
            }
            catch (Exception e)
            {
                throw new WebException("Error while reading service. General error", e);
            }
        }

        private XmlDocument GetXml(ServiceObject serviceObject)
        {
            if (serviceObject.FileStream == null)
            {
                return GetXmlFromUrl(serviceObject.Url);
            }
            return GetXmlFromStream(serviceObject.FileStream);
        }

        private void AddImportedFiles()
        {
            // check for import tags

            // add new xml document to the list
        }

        public void GetInfoFromService(ServiceObject serviceObject)
        {
            var xml = GetXml(serviceObject);

            _xmlDocs.Add(xml);

            AddImportedFiles();

            var parseWsdl = new ParseWsdl(_xmlDocs);
            serviceObject.Functions = parseWsdl.Functions;
            serviceObject.OriginServiceName = parseWsdl.ServiceAddress;
            serviceObject.ServiceClientName = parseWsdl.ServiceClientName;
            adjustArrayOf(serviceObject);
        }

        public void adjustArrayOf(ServiceObject serviceObject)
        {
            for (int i = serviceObject.Functions.Count - 1; i >= 0; i--)
            {
                if (serviceObject.Functions[i].ReturnType.StartsWith("ArrayOf"))
                {
                    var temp = serviceObject.Functions[i].ReturnType;
                    temp = temp.Substring(7);
                    if (temp.Equals("guid", StringComparison.CurrentCultureIgnoreCase))
                        temp = "string";
                    serviceObject.Functions[i].ReturnType = temp + "[]";
                }
                foreach (var parameters in serviceObject.Functions[i].Parameters)
                {
                    if (parameters.Type.StartsWith("ArrayOf"))
                    {
                        var temp = parameters.Type;
                        temp = temp.Substring(7);
                        if (temp.Equals("guid", StringComparison.CurrentCultureIgnoreCase))
                            temp = "string";
                        parameters.Type = temp + "[]";
                    }
                }             
            }
        }
    }
}
