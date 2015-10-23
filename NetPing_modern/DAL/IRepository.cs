using NetPing.Models;
using System;
using System.Collections.Generic;

namespace NetPing.DAL
{
    public interface IRepository : IDisposable
    {
        IEnumerable<SPTerm> Terms { get; }
        IEnumerable<SPTerm> TermsDestinations { get; }
        IEnumerable<SPTerm> TermsDeviceParameters { get; }
        IEnumerable<SPTerm> TermsLabels { get; }
        IEnumerable<SPTerm> TermsCategories { get; }
        IEnumerable<Device> Devices { get; }
        IEnumerable<Post> Posts { get; }
        IEnumerable<SFile> SFiles { get; }
        IEnumerable<DeviceParameter> DevicesParameters { get; }
        IEnumerable<SPTerm> TermsFileTypes { get; }
        IEnumerable<PubFiles> PubFiles { get; }
        IEnumerable<SiteText> SiteTexts { get; }
        //        IEnumerable<SPTerm> TermsSiteTexts { get; }

        TreeNode<Device> DevicesTree(Device root, IEnumerable<Device> devices);
        IEnumerable<Device> GetDevices(string id, string groupId);
        IEnumerable<HTMLInjection> HtmlInjections { get; }
        string UpdateAll();

        string UpdateAllAsync(string name);
    }
}
