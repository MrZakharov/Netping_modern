using System.Collections.Generic;
using System.Linq;
using NetPing.Models;

namespace NetPing.DAL
{
    internal class DevicesTreeHelper
    {
        public static void BuildTree(TreeNode<Device> dev, IEnumerable<Device> list)
        {
            var devices = list as IList<Device> ?? list.ToList();

            var childrens =
                devices.Where(
                    d => d.Name.Level == dev.Value.Name.Level + 1
                         && d.Name.Path.Contains(dev.Value.Name.Path));

            foreach (var child in childrens)
            {
                BuildTree(dev.AddChild(child), devices);
            }
        }
    }
}