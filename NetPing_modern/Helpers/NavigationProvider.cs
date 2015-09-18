using System.Collections.Generic;
using Microsoft.SharePoint.Client;
using NetPing_modern.Models;
using NetPing_modern.Resources.Views.Catalog;

namespace NetPing_modern.Helpers
{
    public class NavigationProvider
    {
        public static class Groups
        {
            public static readonly string Monitoring = "monitoring-servernoj";
            public static readonly string Power = "upravljaemye-rozetki-ip-pdu";
            public static readonly string Switch = "kommutatory-ethernet";
        }

        public static List<SectionModel> GetAllSections()
        {
            var sections = new List<SectionModel>();

            sections.Add(new SectionModel
            {
                Description = Index.Sec_monitoring_descr,
                ImageUrl = "../../Content/Images/present_left_img1.png",
                Title = Index.Sec_monitoring,
                Url = Groups.Monitoring,
                Sections = new List<SectionModel>
                                                {
                                                    new SectionModel
                                                    {
                                                        Title = Index.Sec_sub_devices,
                                                        Url = "devices",
                                                        FullUrl="products/monitoring-servernoj"
                                                    },
                                                    new SectionModel
                                                    {
                                                        Title = Index.Sec_sub_sensors,
                                                        Url = "sensors",
                                                        FullUrl="products/monitoring-servernoj/sensors"
                                                    },
                                                    new SectionModel
                                                    {
                                                        Title = Index.Sec_sub_access,
                                                        Url = "accessories",
                                                        FullUrl="products/monitoring-servernoj/accessories"
                                                    },
                                                    new SectionModel
                                                    {
                                                        Title = Index.Sec_sub_solutions,
                                                        Url = "dlja-servernyh-komnat-i-6kafov",
                                                        FullUrl="solutions/monitoring-servernoj/dlja-servernyh-komnat-i-6kafov"
                                                    }
                                                }
            });

            sections.Add(new SectionModel
            {
                Description = Index.Sec_power_descr,
                ImageUrl = "../../Content/Images/present_left_img2.png",
                Title = Index.Sec_power,
                Url = Groups.Power,
                Sections = new List<SectionModel>
                                                {
                                                    new SectionModel
                                                    {
                                                        Title = Index.Sec_sub_devices,
                                                        Url = "devices",
                                                        FullUrl="products/upravljaemye-rozetki-ip-pdu"
                                                    },
                                                    new SectionModel
                                                    {
                                                        Title = Index.Sec_sub_sensors,
                                                        Url = "sensors",
                                                        FullUrl="products/upravljaemye-rozetki-ip-pdu/sensors"
                                                    },
                                                    new SectionModel
                                                    {
                                                        Title = Index.Sec_sub_access,
                                                        Url = "accessories",
                                                        FullUrl="products/upravljaemye-rozetki-ip-pdu/accessories"
                                                    },
                                                    new SectionModel
                                                    {
                                                        Title = Index.Sec_sub_solutions,
                                                        Url = "udaljonnoe-upravlenie-jelektropitaniem",
                                                        FullUrl="solutions/upravljaemye-rozetki-ip-pdu/udaljonnoe-upravlenie-jelektropitaniem"
                                                    }
                                                }
            });

            sections.Add(new SectionModel
            {
                Description = Index.Sec_switch_descr,
                ImageUrl = "../../Content/Images/present_left_img3.png",
                Title = Index.Sec_switch,
                Url = Groups.Switch,
                Sections = new List<SectionModel>
                                                {
                                                    new SectionModel
                                                    {
                                                        Title = Index.Sec_sub_devices,
                                                        Url = "devices",
                                                        FullUrl="products/kommutatory-ethernet"
                                                    },
                                                    new SectionModel
                                                    {
                                                        Title = Index.Sec_sub_access,
                                                        Url = "accessories",
                                                        FullUrl= "products/kommutatory-ethernet/accessories"
                                                    },
                                                    new SectionModel
                                                    {
                                                        Title = Index.Sec_sub_solutions,
                                                        Url = "re6enija-na-osnove-POE",
                                                        FullUrl="solutions/kommutatory-ethernet/re6enija-na-osnove-POE"
                                                    }
                                                }
            });
            sections.Add(new SectionModel
            {
                Description = Index.Sec_dev_descr,
                Title = Index.Sec_dev,
                Url = "development"
            });
            return sections;
        }
    }
}