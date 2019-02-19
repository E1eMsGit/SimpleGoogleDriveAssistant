using SimpleGoogleDriveAssistant.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SimpleGoogleDriveAssistant.Models
{  
    class UploadFileAccess 
    {
        private static string md = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private static string appCatalog = md + "\\" + Resources.ApplicationName;

        private string _uploadCatalogsFileName = "\\upload_catalogs_info.xml";

        private static XDocument _xDoc;
        private static XElement _root;

        public static int Id { get; set; }

        public void Init()
        {
            if (Directory.Exists(appCatalog) == false)
            {
                Directory.CreateDirectory(appCatalog);
            }

            try
            {
                _xDoc = XDocument.Load(appCatalog + _uploadCatalogsFileName);
                _root = _xDoc.Element("catalogs");
            }
            catch (Exception)
            {
                _xDoc = new XDocument();
                _root = new XElement("catalogs");
                _xDoc.Add(_root);
                _xDoc.Save(appCatalog + _uploadCatalogsFileName);
            }
        }

        public IEnumerable<CatalogModel> GetData()
        {
            Id = 0;

            var items = from xe in _root.Elements("catalog")
                        select new CatalogModel
                        {
                            Id = Id++,
                            Name = xe.Element("name").Value,
                            UploadTime = xe.Element("upload_time").Value,
                            CatalogPath = xe.Element("catalog_path").Value
                        };

            return items.OrderBy(item => item.Name);
        }

        public List<string> GetDataNames()
        {
            List<string> names = new List<string>();
            var items = GetData();

            foreach (var item in items)
            {
                names.Add(item.Name);
            }

            return names;
        }

        public void AddCatalog(CatalogModel newCatalog)
        {
            XElement catalog = new XElement("catalog");
            XAttribute catalogIdAttr = new XAttribute("Id", Id);
            XElement catalogTitleElem = new XElement("name", newCatalog.Name);
            XElement catalogLoginElem = new XElement("upload_time", newCatalog.UploadTime);
            XElement catalogPasswordElem = new XElement("catalog_path", newCatalog.CatalogPath);

            catalog.Add(catalogIdAttr);
            catalog.Add(catalogTitleElem);
            catalog.Add(catalogLoginElem);
            catalog.Add(catalogPasswordElem);

            _root.Add(catalog);
            _xDoc.Save(appCatalog + _uploadCatalogsFileName);

            Id++;
        }

        public void DeleteCatalogs(List<CatalogModel> deleteCatalogs)
        {
            foreach (var note in deleteCatalogs)
            {
                foreach (XElement element in GetElements(note))
                    element.Remove();
            }

            UpdateId();

            _xDoc.Save(appCatalog + _uploadCatalogsFileName);
        }

        public void ChangeUploadTime(CatalogModel catalog)
        {
            foreach (XElement element in GetElements(catalog))
                element.Element("upload_time").Value = DateTime.Now.ToString();

            _xDoc.Save(appCatalog + _uploadCatalogsFileName);
        }

        private void UpdateId()
        {
            int id = 0;

            foreach (XElement xe in _root.Elements("catalog").ToList())
            {
                xe.Attribute("Id").Value = id++.ToString();
            }
        }

        private IEnumerable<XElement> GetElements(CatalogModel catalog) => from element in _root.Elements("catalog")
                                                                           where element.Attribute("Id").Value == catalog.Id.ToString()
                                                                           select element;
    }
}
