using NetPing.Models;
using System;
using System.Collections.Generic;
using NetPing_modern.DAL.Model;

namespace NetPing.DAL
{
    public interface IRepository
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

        TreeNode<Device> DevicesTree(Device root, IEnumerable<Device> devices);

        IEnumerable<Device> GetDevices(String id, String groupId);

        IEnumerable<HTMLInjection> HtmlInjections { get; }

        UserManualModel GetUserManual(String id);

        String UpdateAll();

        String UpdateAllAsync(String name);
    }
}
