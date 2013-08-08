﻿// 
// Comprobante.Custom.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
// 
// Copyright (C) 2012-2013 Eddy Zavaleta, Mictlanix, and contributors.
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Xsl;
using Mictlanix.CFDv32.Resources;

namespace Mictlanix.CFDv32
{
    public partial class Comprobante
    {
		string schema_location;
		XmlSerializerNamespaces xmlns;

		[XmlAttributeAttribute("schemaLocation", Namespace = "http://www.w3.org/2001/XMLSchema-instance")]
		public string SchemaLocation {
			get {
				if (schema_location == null) {
					schema_location = "http://www.sat.gob.mx/cfd/3 http://www.sat.gob.mx/sitio_internet/cfd/3/cfdv32.xsd";

					if (Complemento != null) {
						foreach (var item in Complemento) {
							if(item is TimbreFiscalDigital) {
								var obj = (TimbreFiscalDigital)item;
								if (!schema_location.Contains (obj.SchemaLocation)) {
									schema_location += " " + obj.SchemaLocation;
								}
								obj.SchemaLocation = null;
							} else if(item is LeyendasFiscales) {
								var obj = (LeyendasFiscales)item;
								if (!schema_location.Contains (obj.SchemaLocation)) {
									schema_location += " " + obj.SchemaLocation;
								}
								obj.SchemaLocation = null;
							}
						}
					}
				}

				return schema_location;
			}
			set { schema_location = value; }
		}

        [XmlNamespaceDeclarations]
        public XmlSerializerNamespaces Xmlns {
            get {
                if (xmlns == null) {
                    xmlns = new XmlSerializerNamespaces(new XmlQualifiedName[] {
                        new XmlQualifiedName("cfdi", "http://www.sat.gob.mx/cfd/3"),
                        new XmlQualifiedName("xsi", "http://www.w3.org/2001/XMLSchema-instance")
                    });

					if (Complemento != null) {
						foreach (var item in Complemento) {
							if (item is TimbreFiscalDigital) {
								xmlns.Add ("tfd", "http://www.sat.gob.mx/TimbreFiscalDigital");
							} else if (item is LeyendasFiscales) {
								xmlns.Add ("leyendasFisc", "http://www.sat.gob.mx/leyendasFiscales");
							}
						}
					}
                }

                return xmlns;
            }
            set { xmlns = value; }
        }
        
        public override string ToString()
        {
			var resolver = new EmbeddedResourceResolver ();

			using (var xml = ToXmlStream()) {
				using (var output = new StringWriter()) {
                    using (var xsl_stream = resolver.GetResource ("cadenaoriginal_3_2.xslt")) {
						XslCompiledTransform xslt = new XslCompiledTransform ();
						xslt.Load (XmlReader.Create (xsl_stream), XsltSettings.TrustedXslt, resolver);
						xslt.Transform (XmlReader.Create (xml), null, output);
						return output.ToString ();
					}
				}
			}
		}
		
		public MemoryStream ToXmlStream()
		{
			return CFDLib.Utils.SerializeToXmlStream (this, Xmlns);
		}
		
		public byte[] ToXmlBytes()
		{
			using (var ms = ToXmlStream()) {
				return ms.ToArray ();
			}
		}
		
		public string ToXmlString()
        {
			using (var ms = ToXmlStream()) {
				return Encoding.UTF8.GetString (ms.ToArray ());
			}
		}

		public void Sign (byte[] privateKey, byte[] password)
		{
			sello = CFDLib.Utils.SHA1WithRSA (ToString (), privateKey, password);
		}
		
		public static Comprobante FromXml (string xml)
		{
			using(var ms = new MemoryStream (Encoding.UTF8.GetBytes(xml))) {
				return FromXml (ms);
			}
		}

		public static Comprobante FromXml (Stream xml)
		{
			var xs = new XmlSerializer (typeof(Comprobante));
			object obj = xs.Deserialize(xml);
			return obj as Comprobante;
		}
    }
}
